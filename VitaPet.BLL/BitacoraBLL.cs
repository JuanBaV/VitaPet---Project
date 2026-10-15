using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using VitaPet.BE;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public class BitacoraBLL
    {
        private const int CantidadRegistrosActivos = 50;
        private const string TablaBitacora = "Bitacora";
        private readonly BitacoraDAL _bitacoraDAL;
        private readonly IntegridadBLL _integridadBLL;

        public BitacoraBLL()
        {
            _bitacoraDAL = new BitacoraDAL();
            _integridadBLL = new IntegridadBLL();
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

        private void ArchivarExcedentes()
        {
            List<BitacoraBE> excedentes =
                _bitacoraDAL.ObtenerExcedentes(CantidadRegistrosActivos);

            if (excedentes.Count == 0)
            {
                return;
            }

            string rutaArchivo = ObtenerRutaArchivo();
            XmlDocument documento = CargarDocumento(rutaArchivo);
            XmlElement raiz = documento.DocumentElement;

            foreach (BitacoraBE registro in excedentes)
            {
                if (!EstaArchivado(raiz, registro.IdBitacora, registro.DVH))
                {
                    raiz.AppendChild(CrearRegistroXml(documento, registro));
                }
            }

            GuardarYVerificar(documento, rutaArchivo, excedentes);

            HashSet<int> idsExcedentes = new HashSet<int>(
                excedentes.Select(registro => registro.IdBitacora));

            IEnumerable<string> dvhsRestantes =
                _bitacoraDAL.ObtenerParaIntegridad()
                    .Where(registro =>
                        !idsExcedentes.Contains(registro.IdBitacora))
                    .Select(registro => registro.DVH);

            DVVBE dvv = new DVVBE
            {
                NombreTabla = TablaBitacora,
                ValorDVV = _integridadBLL.CalcularDVV(dvhsRestantes)
            };

            _bitacoraDAL.EliminarYActualizarDVV(
                idsExcedentes,
                dvv);
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

        private XmlDocument CargarDocumento(string rutaArchivo)
        {
            XmlDocument documento = new XmlDocument();

            if (File.Exists(rutaArchivo))
            {
                documento.Load(rutaArchivo);
            }
            else
            {
                XmlDeclaration declaracion =
                    documento.CreateXmlDeclaration("1.0", "utf-8", null);

                documento.AppendChild(declaracion);
                documento.AppendChild(
                    documento.CreateElement("Bitacora"));
            }

            if (documento.DocumentElement == null ||
                documento.DocumentElement.Name != "Bitacora")
            {
                throw new InvalidDataException(
                    "El archivo de bitácora no posee una raíz válida.");
            }

            return documento;
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

            if (!Directory.Exists(directorio))
            {
                Directory.CreateDirectory(directorio);
            }

            using (XmlTextWriter escritor =
                new XmlTextWriter(
                    rutaArchivo,
                    new UTF8Encoding(false)))
            {
                escritor.Formatting = Formatting.Indented;
                documento.Save(escritor);
            }

            XmlDocument verificacion = new XmlDocument();
            verificacion.Load(rutaArchivo);

            if (verificacion.DocumentElement == null ||
                verificacion.DocumentElement.Name != "Bitacora")
            {
                throw new InvalidDataException(
                    "No se pudo verificar el archivo histórico.");
            }

            foreach (BitacoraBE registro in registros)
            {
                if (!EstaArchivado(
                    verificacion.DocumentElement,
                    registro.IdBitacora, registro.DVH))
                {
                    throw new InvalidDataException(
                        "No se pudo verificar un registro histórico.");
                }
            }
        }
    }
}
