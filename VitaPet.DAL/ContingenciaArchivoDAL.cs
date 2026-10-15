using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using VitaPet.BE;
namespace VitaPet.DAL
{
    // Persistencia externa a SQL: sólo la identidad del proceso, SYSTEM y administradores.
    public sealed class ContingenciaArchivoDAL
    {
        private readonly string directorio;
        public ContingenciaArchivoDAL(string directorio) { this.directorio = Path.GetFullPath(directorio); }
        public bool ExisteCuenta => File.Exists(Path.Combine(directorio, "cuenta.xml"));
        public void Preparar()
        {
            Directory.CreateDirectory(directorio);
            var acl = new DirectorySecurity();
            acl.SetAccessRuleProtection(true, false);
            foreach (var sid in new[] { WindowsIdentity.GetCurrent().User,
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null) })
                acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(directorio).SetAccessControl(acl);
        }
        public static string Serializar<T>(T valor)
        {
            using (var s = new StringWriter())
            {
                // Preservar CR/LF dentro del XML anidado para verificar exactamente los bytes firmados.
                using (var w = XmlWriter.Create(s, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true, NewLineHandling = NewLineHandling.Entitize }))
                    new XmlSerializer(typeof(T)).Serialize(w, valor);
                return s.ToString();
            }
        }
        public static T Deserializar<T>(string xml)
        {
            using (var s = new StringReader(xml))
            using (var r = XmlReader.Create(s, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null, MaxCharactersInDocument = 128L * 1024 * 1024 }))
                return (T)new XmlSerializer(typeof(T)).Deserialize(r);
        }
        public CuentaContingenciaBE LeerCuenta()
        {
            CuentaContingenciaBE cuenta;
            try { cuenta = Deserializar<CuentaContingenciaBE>(File.ReadAllText(Path.Combine(directorio, "cuenta.xml"))); }
            catch (InvalidOperationException ex)
            { throw new InvalidDataException("La cuenta de emergencia no tiene un formato portable válido. Convierta el archivo anterior antes de utilizarlo.", ex); }
            ValidarCuenta(cuenta);
            return cuenta;
        }
        public void GuardarCuenta(CuentaContingenciaBE cuenta)
        {
            ValidarCuenta(cuenta);
            Guardar("cuenta.xml", Serializar(cuenta));
        }
        private static void ValidarCuenta(CuentaContingenciaBE cuenta)
        {
            Guid id;
            if (cuenta == null || cuenta.VersionFormato != 1 ||
                !Guid.TryParseExact(cuenta.Id, "N", out id) || string.IsNullOrWhiteSpace(cuenta.Correo) ||
                cuenta.Correo.Length > 100 || cuenta.Intentos < 0 || cuenta.Intentos > 2)
                throw new InvalidDataException("Datos de cuenta de emergencia inválidos.");
            try
            {
                if (Convert.FromBase64String(cuenta.Sal ?? "").Length != 32 ||
                    Convert.FromBase64String(cuenta.Hash ?? "").Length != 32 ||
                    Convert.FromBase64String(cuenta.ClaveFirma ?? "").Length != 32)
                    throw new InvalidDataException("Material de verificación de la cuenta inválido.");
            }
            catch (FormatException ex) { throw new InvalidDataException("Material de verificación de la cuenta inválido.", ex); }
        }
        public void GuardarRespaldo(string id, string xml) => Guardar(Nombre(id), xml);
        public string LeerRespaldo(string id) => File.ReadAllText(Path.Combine(directorio, Nombre(id)));
        public IEnumerable<string> Respaldos()
        {
            if (!Directory.Exists(directorio)) yield break;
            foreach (var file in Directory.GetFiles(directorio, "respaldo-*.xml"))
                yield return Path.GetFileNameWithoutExtension(file).Substring("respaldo-".Length);
        }
        private static string Nombre(string id)
        {
            Guid guid;
            if (!Guid.TryParseExact(id, "N", out guid)) throw new ArgumentException("Respaldo inválido.");
            return "respaldo-" + id + ".xml";
        }
        private void Guardar(string nombre, string contenido)
        {
            string file = Path.Combine(directorio, nombre), temp = Path.Combine(directorio, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temp, contenido, Encoding.UTF8);
                if (File.Exists(file)) File.Replace(temp, file, null); else File.Move(temp, file);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        public void Auditar(string accion, string cuenta, string respaldo)
        {
            Preparar();
            using (var x = XmlWriter.Create(Path.Combine(directorio, "evento-" + Guid.NewGuid().ToString("N") + ".xml")))
            {
                x.WriteStartElement("EventoContingencia");
                x.WriteElementString("FechaUtc", DateTime.UtcNow.ToString("O"));
                x.WriteElementString("Accion", accion); x.WriteElementString("Cuenta", cuenta);
                x.WriteElementString("Respaldo", respaldo); x.WriteEndElement();
            }
        }
    }
}
