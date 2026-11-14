using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using VitaPet.BE;
using VitaPet.DAL;
using VitaPet.SECURITY;
using VitaPet.BLL.Seguridad;

namespace VitaPet.BLL
{
    public class BitacoraBLL
    {
        private const int DiasActivos = 30;
        private const string TablaBitacora = "Bitacora";
        private readonly BitacoraDAL _bitacoraDAL;
        private readonly IntegridadBLL _integridadBLL;

        public BitacoraBLL()
        {
            _bitacoraDAL = new BitacoraDAL();
            _integridadBLL = new IntegridadBLL();
        }

        public ResultadoIntegridadBE RegistrarErroresIntegridad()
        {
            using (EstadoAplicacion.BloquearOperacion())
            using (var operacion = new OperacionIntegridadDAL())
            {
                var seguridad = new SecurityManager();
                var usuario = seguridad.ExigirWebmaster();
                var resultado = seguridad.DiagnosticarIntegridad();
                if (resultado.EsValida) { operacion.Complete(); return resultado; }

                // La evidencia se agrega al histórico XML sin escribir ni reparar la BD corrupta.
                var historico = new ConsultaBitacoraBLL().LeerHistorico();
                int id = historico.Select(r => r.IdBitacora).DefaultIfEmpty(0).Max();
                string incidente = Guid.NewGuid().ToString("N").Substring(0, 8);
                var documento = new XmlDocument { XmlResolver = null };
                documento.AppendChild(documento.CreateElement("Bitacora"));
                foreach (var registro in historico)
                    documento.DocumentElement.AppendChild(CrearRegistroXml(documento, registro));
                var nuevos = new List<BitacoraBE>();
                foreach (var detalle in resultado.Detalles)
                {
                    var registro = new BitacoraBE {
                        IdBitacora = checked(++id), IdUsuario = usuario.IdUsuario, Fecha = DateTime.Now,
                        Modulo = "Integridad", Criticidad = "Grave",
                        Accion = "ER023 [" + incidente + "] Tabla=" + detalle.Tabla + "; Fila=" + detalle.Fila +
                            "; Campo=" + detalle.Columna + "; " + detalle.Motivo
                    };
                    registro.DVH = _integridadBLL.CalcularDVH(registro);
                    nuevos.Add(registro);
                    documento.DocumentElement.AppendChild(CrearRegistroXml(documento, registro));
                }
                string archivo = ObtenerRutaArchivo();
                Directory.CreateDirectory(Path.GetDirectoryName(archivo));
                // salida xml Bitacora.xml
                EstadoAplicacion.EscribirAtomico(archivo, documento.OuterXml);
                var verificados = new ConsultaBitacoraBLL().LeerHistorico();
                if (nuevos.Any(n => !verificados.Any(v => v.IdBitacora == n.IdBitacora && v.DVH == n.DVH)))
                    throw new InvalidDataException("No se pudo verificar el diagnóstico guardado en bitácora.");
                operacion.Complete();
                return resultado;
            }
        }

        public int Registrar(
            BitacoraBE registro,
            DVVBE dvv)
        {
            int idBitacora = _bitacoraDAL.Registrar(
                registro,
                dvv);

            ArchivarExcedentes();

            return idBitacora;
        }

        public int Mantener()
        {
            using (var operacion = new OperacionIntegridadDAL())
            {
                new SecurityManager().ExigirIntegridad();
                int cantidad = ArchivarExcedentes();
                if (cantidad > 0) new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                operacion.Complete();
                return cantidad;
            }
        }

        private int ArchivarExcedentes()
        {
            var registros = _bitacoraDAL.ObtenerParaIntegridad();
            DateTime limite = DateTime.Now.AddDays(-DiasActivos);
            var excedentes = registros.Where(r => !r.Archivado && r.Fecha < limite).ToList();
            if (excedentes.Count == 0)
            {
                return 0;
            }

            string rutaArchivo = ObtenerRutaArchivo();
            // Validar todo el histórico antes de reemplazarlo; no tapar evidencia alterada.
            var historico = new ConsultaBitacoraBLL().LeerHistorico();
            var documento = new XmlDocument { XmlResolver = null };
            documento.AppendChild(documento.CreateElement("Bitacora"));
            XmlElement raiz = documento.DocumentElement;
            foreach (var registro in historico) raiz.AppendChild(CrearRegistroXml(documento, registro));

            foreach (BitacoraBE registro in excedentes)
            {
                if (!EstaArchivado(raiz, registro.IdBitacora, registro.DVH))
                {
                    raiz.AppendChild(CrearRegistroXml(documento, registro));
                }
            }

            GuardarYVerificar(documento, rutaArchivo, excedentes);

            foreach (var registro in excedentes)
            {
                registro.Archivado = true;
                registro.DVH = _integridadBLL.CalcularDVH(registro);
            }

            DVVBE dvv = new DVVBE
            {
                NombreTabla = TablaBitacora,
                ValorDVV = _integridadBLL.CalcularDVV(registros.Select(r => r.DVH))
            };

            _bitacoraDAL.ArchivarYActualizarDVV(
                excedentes,
                dvv);
            return excedentes.Count;
        }

        internal string ObtenerRutaArchivo()
        {
            string rutaConfigurada =
                ConfigurationManager.AppSettings["RutaBitacoraXml"];

            if (string.IsNullOrWhiteSpace(rutaConfigurada))
            {
                throw new ConfigurationErrorsException(
                    "No se encontró la configuración RutaBitacoraXml.");
            }

            if (Path.IsPathRooted(rutaConfigurada))
            {
                return rutaConfigurada;
            }

            string rutaRelativa = rutaConfigurada.StartsWith("~/")
                ? rutaConfigurada.Substring(2)
                : rutaConfigurada.TrimStart('/', '\\');

            rutaRelativa = rutaRelativa.Replace(
                '/',
                Path.DirectorySeparatorChar);

            return Path.GetFullPath(
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    rutaRelativa));
        }

        internal XmlElement CrearRegistroXml(
            XmlDocument documento,
            BitacoraBE registro)
        {
            XmlElement elementoRegistro =
                documento.CreateElement("Registro");

            AgregarElemento(
                documento,
                elementoRegistro,
                "IdBitacora",
                registro.IdBitacora.ToString(
                    CultureInfo.InvariantCulture));

            AgregarElemento(
                documento,
                elementoRegistro,
                "IdUsuario",
                registro.IdUsuario.HasValue
                    ? registro.IdUsuario.Value.ToString(
                        CultureInfo.InvariantCulture)
                    : string.Empty);

            AgregarElemento(
                documento,
                elementoRegistro,
                "Fecha",
                registro.Fecha.ToString(
                    "o",
                    CultureInfo.InvariantCulture));

            AgregarElemento(
                documento,
                elementoRegistro,
                "Accion",
                registro.Accion);

            AgregarElemento(
                documento,
                elementoRegistro,
                "Modulo",
                registro.Modulo);

            AgregarElemento(
                documento,
                elementoRegistro,
                "Criticidad",
                registro.Criticidad);

            AgregarElemento(
                documento,
                elementoRegistro,
                "DVH",
                registro.DVH);

            if (registro.Archivado) AgregarElemento(documento, elementoRegistro, "Archivado", "true");

            return elementoRegistro;
        }

        private void AgregarElemento(
            XmlDocument documento,
            XmlElement padre,
            string nombre,
            string valor)
        {
            XmlElement elemento = documento.CreateElement(nombre);
            elemento.InnerText = valor ?? string.Empty;
            padre.AppendChild(elemento);
        }

        private bool EstaArchivado(
            XmlElement raiz,
            int idBitacora, string dvh)
        {
            foreach (XmlNode nodo in raiz.SelectNodes("Registro"))
            {
                XmlElement idArchivado =
                    nodo["IdBitacora"];

                int valorArchivado;

                if (idArchivado != null &&
                    int.TryParse(
                        idArchivado.InnerText,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out valorArchivado) &&
                    valorArchivado == idBitacora && nodo["DVH"]?.InnerText == dvh)
                {
                    return true;
                }
            }

            return false;
        }

        private void GuardarYVerificar(
            XmlDocument documento,
            string rutaArchivo,
            List<BitacoraBE> registros)
        {
            string directorio =
                Path.GetDirectoryName(rutaArchivo);

            try
            {
                Directory.CreateDirectory(directorio);
                // salida xml Bitacora.xml
                EstadoAplicacion.EscribirAtomico(rutaArchivo, documento.OuterXml);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new BitacoraException("ER033", "No se pudo archivar la bitácora; se conservaron los registros activos.", ex);
            }
            var verificacion = new ConsultaBitacoraBLL().LeerHistorico();

            foreach (BitacoraBE registro in registros)
            {
                if (!verificacion.Any(r => r.IdBitacora == registro.IdBitacora && r.DVH == registro.DVH))
                {
                    throw new InvalidDataException(
                        "No se pudo verificar un registro histórico.");
                }
            }
        }
    }
}
