using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public sealed class ConsultaBitacoraBLL
    {
        private static readonly string[] Criticidades = { "Informativa", "Advertencia", "Grave", "Crítica" };
        private readonly SecurityManager seguridad = new SecurityManager();
        public bool SoloHistoricoIntegridad { get; private set; }

        public List<BitacoraBE> Consultar(FiltroBitacoraBE filtro)
        {
            // Lock shared with writers: a row cannot move to XML between the two reads.
            using (var operacion = new OperacionIntegridadDAL())
            {
                var usuario = seguridad.ExigirWebmaster("ConsultaBitacora");
                ValidarFiltro(filtro);
                SoloHistoricoIntegridad = !seguridad.VerificarIntegridad();
                var activos = SoloHistoricoIntegridad ? new List<BitacoraBE>() : new BitacoraDAL().ObtenerParaIntegridad();
                var historicos = LeerHistorico();
                if (SoloHistoricoIntegridad)
                    historicos = historicos.Where(r => r.Modulo == "Integridad").ToList();
                var registros = activos.Concat(historicos)
                    // Restore can reuse IDs: only an identical ID + DVH is the same event.
                    .GroupBy(r => r.IdBitacora.ToString(CultureInfo.InvariantCulture) + ":" + r.DVH,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First());
                if (filtro.FechaDesde.HasValue)
                    registros = registros.Where(r => r.Fecha.Date >= filtro.FechaDesde.Value.Date);
                if (filtro.FechaHasta.HasValue)
                    registros = registros.Where(r => r.Fecha.Date <= filtro.FechaHasta.Value.Date);
                if (!string.IsNullOrEmpty(filtro.Criticidad))
                    registros = registros.Where(r => string.Equals(r.Criticidad, filtro.Criticidad, StringComparison.Ordinal));
                var resultado = registros.OrderByDescending(r => r.Fecha)
                    .ThenByDescending(r => r.IdBitacora).ThenBy(r => r.DVH, StringComparer.Ordinal).ToList();
                // Materialize before auditing: pagination/export use this exact result set.
                try
                {
                    if (!SoloHistoricoIntegridad) seguridad.RegistrarBitacora(new BitacoraBE {
                        IdUsuario = usuario.IdUsuario, Fecha = DateTime.Now,
                        Accion = "Consulta de bitácora", Modulo = "Bitácora", Criticidad = "Informativa"
                    });
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is XmlException)
                {
                    throw new BitacoraException("ER033", "No se pudo registrar la consulta en bitácora.", ex);
                }
                operacion.Complete();
                return resultado;
            }
        }

        public byte[] ExportarXml(IEnumerable<BitacoraBE> registros)
        {
            using (var operacion = new OperacionIntegridadDAL())
            {
                seguridad.ExigirWebmaster("ConsultaBitacora");
                ExigirIntegridad();
                if (registros == null) throw new ArgumentNullException(nameof(registros));
                var lista = registros.ToList();
                if (lista.Any(r => r == null || !seguridad.VerificarDVH(r)))
                    throw new BitacoraException("ER023", "Inconsistencia en la verificación de integridad.");
                try
                {
                    var documento = new XmlDocument { XmlResolver = null };
                    documento.AppendChild(documento.CreateXmlDeclaration("1.0", "utf-8", null));
                    var raiz = documento.CreateElement("Bitacora"); documento.AppendChild(raiz);
                    var escritor = new BitacoraBLL();
                    foreach (var registro in lista)
                        raiz.AppendChild(escritor.CrearRegistroXml(documento, registro));
                    byte[] contenido;
                    using (var memoria = new MemoryStream())
                    {
                        using (var xml = XmlWriter.Create(memoria, new XmlWriterSettings {
                            Encoding = new UTF8Encoding(false), Indent = true, CloseOutput = false }))
                            documento.Save(xml);
                        contenido = memoria.ToArray();
                    }
                    operacion.Complete();
                    return contenido;
                }
                catch (Exception ex) when (ex is XmlException || ex is ArgumentException || ex is IOException)
                {
                    throw new BitacoraException("ER015", "No se pudo generar la salida XML.", ex);
                }
            }
        }

        public static void ValidarFiltro(FiltroBitacoraBE filtro)
        {
            if (filtro == null) throw new ArgumentNullException(nameof(filtro));
            if (filtro.FechaDesde.HasValue && filtro.FechaHasta.HasValue &&
                filtro.FechaDesde.Value.Date > filtro.FechaHasta.Value.Date)
                throw new BitacoraException("ER027", "La fecha desde es posterior a la fecha hasta.");
            if (!string.IsNullOrEmpty(filtro.Criticidad) && !Criticidades.Contains(filtro.Criticidad))
                throw new BitacoraException("ER025", "Datos inválidos.");
        }

        private void ExigirIntegridad()
        {
            if (!seguridad.VerificarIntegridad())
                throw new BitacoraException("ER023", "Inconsistencia en la verificación de integridad.");
        }

        internal List<BitacoraBE> LeerHistorico()
        {
            try
            {
                var archivo = new BitacoraBLL().ObtenerRutaArchivo();
                var documento = new XmlDocument { XmlResolver = null };
                // No File.Exists: permission failures must not look like an empty history.
                try
                {
                    using (var lector = XmlReader.Create(archivo, new XmlReaderSettings {
                        DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                        documento.Load(lector);
                }
                catch (FileNotFoundException) { return new List<BitacoraBE>(); }
                catch (DirectoryNotFoundException) { return new List<BitacoraBE>(); }
                if (documento.DocumentElement == null || documento.DocumentElement.Name != "Bitacora")
                    throw new InvalidDataException("Raíz de bitácora inválida.");
                var resultado = new List<BitacoraBE>();
                foreach (XmlNode nodo in documento.DocumentElement.ChildNodes)
                {
                    if (nodo.NodeType != XmlNodeType.Element) continue;
                    if (nodo.Name != "Registro") throw new InvalidDataException("Registro desconocido.");
                    string idUsuario = Campo(nodo, "IdUsuario");
                    var registro = new BitacoraBE {
                        IdBitacora = int.Parse(Campo(nodo, "IdBitacora"), CultureInfo.InvariantCulture),
                        IdUsuario = string.IsNullOrEmpty(idUsuario) ? (int?)null : int.Parse(idUsuario, CultureInfo.InvariantCulture),
                        Fecha = DateTime.ParseExact(Campo(nodo, "Fecha"), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        Accion = Campo(nodo, "Accion"), Modulo = Campo(nodo, "Modulo"),
                        Criticidad = Campo(nodo, "Criticidad"), DVH = Campo(nodo, "DVH")
                    };
                    if (registro.IdBitacora <= 0 || (registro.IdUsuario.HasValue && registro.IdUsuario.Value <= 0) ||
                        !Criticidades.Contains(registro.Criticidad))
                        throw new InvalidDataException("Datos de bitácora inválidos.");
                    if (!seguridad.VerificarDVH(registro))
                        throw new BitacoraException("ER023", "Inconsistencia en la verificación de integridad del histórico.");
                    resultado.Add(registro);
                }
                return resultado;
            }
            catch (Exception ex) when (ex is XmlException || ex is IOException || ex is UnauthorizedAccessException ||
                ex is FormatException || ex is OverflowException)
            {
                throw new BitacoraException("ER031", "No se pudo leer el histórico XML de bitácora.", ex);
            }
        }

        private static string Campo(XmlNode registro, string nombre)
        {
            var campos = registro.SelectNodes(nombre);
            if (campos == null || campos.Count != 1)
                throw new InvalidDataException("Campo de bitácora ausente o duplicado: " + nombre);
            return campos[0].InnerText;
        }
    }
}
