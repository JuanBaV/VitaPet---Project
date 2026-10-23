using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Serialization;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;
namespace VitaPet.BLL
{
    public sealed class BackupBLL
    {
        private readonly BackupDAL dal=new BackupDAL();
        private readonly SecurityManager seguridad=new SecurityManager();
        private string Catalogo { get { string p=Path.Combine(EstadoAplicacion.Directorio,"catalogo"); Directory.CreateDirectory(p); return p; } }
        private string Carpeta { get { string p=EstadoAplicacion.ResolverRuta("RutaBackups","~/App_Data/Backups"); Directory.CreateDirectory(p); return p; } }
        private string Archivo(string id)
        {
            Guid parsed; if(!Guid.TryParseExact(id,"N",out parsed)) throw new ArgumentException("Identificador de backup inválido.");
            return Path.Combine(Carpeta,id+".bak");
        }
        private static string Hash(string file)
        { using(var s=File.OpenRead(file)) using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(s)).Replace("-","").ToLowerInvariant(); }
        internal static bool EsquemaCompatible(string respaldo,string actual)
        {
            if (string.IsNullOrEmpty(respaldo) || string.IsNullOrEmpty(actual) ||
                !respaldo.EndsWith(";", StringComparison.Ordinal) ||
                !actual.EndsWith(";", StringComparison.Ordinal)) return false;
            if(respaldo==actual) return true;
            if (!actual.StartsWith(respaldo, StringComparison.Ordinal)) return false;
            var pendientes = actual.Substring(respaldo.Length)
                .Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            if (pendientes.Length == 1 &&
                pendientes[0].StartsWith("006_HuellasSinClave.sql:", StringComparison.Ordinal)) return true;
            // Adaptaciones conocidas, verificadas en la base temporal antes de restaurar.
            // 013-015 cambian SP; 016 admite precio NULL sin alterar filas existentes.
            var admitidas = new[] { "012_IntegridadTodasLasTablas.sql", "013_ProcedimientosSimples.sql",
                "014_IdiomaUsuario.sql", "015_RegistroCliente.sql", "016_ContratacionPlanes.sql", "017_GestionPago.sql", "018_ConsultaCobertura.sql", "019_ConsultaReintegros.sql", "020_RegistroMascota.sql", "021_GestionMascotas.sql", "022_Turnos.sql",
                "026_SimplificacionProcedimientos.sql", "027_Facturacion.sql", "028_OptimizarLecturas.sql",
                "029_CorreoFactura.sql", "030_PerfilesComposite.sql", "031_AutorizacionPorPantallas.sql", "032_RolesRelacionales.sql", "033_EliminarPermisoRelacion.sql", CifradoDatosDAL.Migracion, "035_BitacoraBajaLogica.sql" };
            return pendientes.Length > 0 && pendientes.All(p =>
                admitidas.Any(id => p.StartsWith(id + ":", StringComparison.Ordinal)));
        }
        private void Guardar(BackupBE backup)
        {
            string xml; using(var sw=new StringWriter()) { new XmlSerializer(typeof(BackupBE)).Serialize(sw,backup); xml=sw.ToString(); }
            string file=Path.Combine(Catalogo,backup.Id+".xml");
            EstadoAplicacion.EscribirAtomico(file,xml);
            EstadoAplicacion.EscribirAtomico(file+".firma","sha256:"+EstadoAplicacion.CalcularHuella(xml));
        }
        private BackupBE Leer(string id)
        {
            Archivo(id);
            string file=Path.Combine(Catalogo,id+".xml"); string xml=File.ReadAllText(file);
            string firma=File.ReadAllText(file+".firma");
            string actual="sha256:"+EstadoAplicacion.CalcularHuella(xml);
            bool anterior=firma.Length==64 && firma.All(Uri.IsHexDigit);
            if(!anterior && firma!=actual) throw new InvalidDataException("El catálogo fue alterado.");
            using(var sr=new StringReader(xml)) using(var reader=XmlReader.Create(sr,new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null }))
            {
                var b=(BackupBE)new XmlSerializer(typeof(BackupBE)).Deserialize(reader);
                if(b.Id!=id || b.BaseDatos!=dal.BaseDatos) throw new InvalidDataException("El backup pertenece a otra base.");
                if(anterior)
                {
                    // La firma HMAC anterior no puede verificarse sin la clave. El hash
                    // del archivo y la integridad de la BD se validan al restaurar.
                    if(!b.Verificado || Hash(Archivo(id))!=b.HashArchivo)
                        throw new InvalidDataException("El backup anterior no coincide con su catálogo.");
                    EstadoAplicacion.EscribirAtomico(file+".firma",actual);
                }
                return b;
            }
        }
        public List<BackupBE> Listar()
        {
            seguridad.ExigirWebmaster("BaseDatos");
            return Directory.GetFiles(Catalogo,"*.xml").Select(f=>Leer(Path.GetFileNameWithoutExtension(f))).OrderByDescending(b=>b.FechaUtc).ToList();
        }
        private void ValidarContenido(string file,string esquema)
        {
            dal.VerificarArchivo(file);
            string temporal=dal.CrearTemporal(file);
            try
            {
                using(ConexionDAL.UsarBaseTemporal(temporal))
                {
                    if(dal.Esquema()!=esquema) throw new InvalidDataException("Esquema de backup incompatible.");
                    if(esquema.IndexOf("006_HuellasSinClave.sql:",StringComparison.Ordinal)>=0)
                        seguridad.ExigirIntegridad();
                    else if(!seguridad.DiagnosticarDigitos().EsValida)
                        throw new InvalidDataException("El backup tiene DVH o DVV inconsistentes.");
                }
                // Validar también las actualizaciones de procedimientos antes de restaurar.
                {
                    using (ConexionDAL.UsarBaseTemporal(temporal))
                    { new MigracionRolesBLL().Ejecutar(); new ReferenciaIntegridadBLL().Inicializar(); new PerfilBLL().AsegurarIniciales(); seguridad.ExigirIntegridad(); }
                }
            }
            finally { dal.EliminarTemporal(temporal); }
        }
        public BackupBE Crear()
        {
            using(EstadoAplicacion.BloquearOperacion())
            {
                var actor=seguridad.ExigirWebmaster("BaseDatos");
                if(EstadoAplicacion.Mantenimiento) throw new InvalidOperationException("Restauración pendiente: no se puede crear un backup sano.");
                seguridad.ExigirIntegridad();
                var b=new BackupBE { Id=Guid.NewGuid().ToString("N"),BaseDatos=dal.BaseDatos,FechaUtc=DateTime.UtcNow,IdOperador=actor.IdUsuario,Esquema=dal.Esquema() };
                AuditarExterno("Backup iniciado",actor.IdUsuario,b.Id);
                try
                {
                    dal.Crear(Archivo(b.Id));
                    ValidarContenido(Archivo(b.Id),b.Esquema);
                    b.HashArchivo=Hash(Archivo(b.Id)); b.Verificado=true;
                    Guardar(b);
                    seguridad.RegistrarBitacora(Evento(actor.IdUsuario,"Backup realizado "+b.Id));
                    AuditarExterno("Backup realizado",actor.IdUsuario,b.Id);
                    new ContingenciaWebMasterBLL().CapturarDesdeBackup();
                    return b;
                }
                catch { AuditarExterno("Backup fallido",actor.IdUsuario,b.Id); throw; }
            }
        }
        public void Restaurar(string id)
        {
            using(EstadoAplicacion.BloquearOperacion())
            {
                var actor=seguridad.ExigirWebmaster("BaseDatos"); var b=Leer(id); string file=Archivo(id);
                if(!b.Verificado || Hash(file)!=b.HashArchivo || !EsquemaCompatible(b.Esquema,dal.Esquema())) throw new InvalidDataException("El respaldo no es válido o compatible.");
                ValidarContenido(file,b.Esquema);
                AuditarExterno("Restore iniciado",actor.IdUsuario,id);
                EstadoAplicacion.IniciarMantenimiento(id);
                try
                {
                    // Copia de evidencia: jamás se incorpora al catálogo de puntos sanos.
                    dal.Crear(Path.Combine(Carpeta,"contingencia_"+Guid.NewGuid().ToString("N")+".bak"));
                    EstadoAplicacion.InvalidarSesiones();
                    dal.Restaurar(file);
                    new MigracionRolesBLL().Ejecutar();
                    new ReferenciaIntegridadBLL().Inicializar();
                    new PerfilBLL().AsegurarIniciales();
                    seguridad.ExigirIntegridad();
                    // El operador puede no existir en un backup antiguo: conservar identidad en el evento externo.
                    var usuario=new UsuarioDAL().ObtenerPorCorreo(actor.CorreoElectronico);
                    int? actorRestaurado=usuario!=null && usuario.IdUsuario==actor.IdUsuario ? (int?)actor.IdUsuario : null;
                    seguridad.RegistrarBitacora(Evento(actorRestaurado,"Restore realizado "+id));
                    seguridad.ExigirIntegridad();
                    AuditarExterno("Restore realizado",actor.IdUsuario,id);
                    EstadoAplicacion.FinalizarMantenimiento();
                }
                catch { AuditarExterno("Restore fallido; mantenimiento activo",actor.IdUsuario,id); throw; }
            }
        }
        private static BitacoraBE Evento(int? usuario,string accion) => new BitacoraBE { IdUsuario=usuario,Fecha=DateTime.Now,Accion=accion,Modulo="Base de datos",Criticidad="Informativa" };
        private static void AuditarExterno(string accion,int actor,string backup)
        {
            string folder=Path.Combine(EstadoAplicacion.Directorio,"auditoria"); Directory.CreateDirectory(folder);
            string file=Path.Combine(folder,DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+"_"+Guid.NewGuid().ToString("N")+".xml");
            using(var x=new XmlTextWriter(file,System.Text.Encoding.UTF8))
            {
                x.WriteStartDocument(); x.WriteStartElement("EventoRecuperacion");
                x.WriteElementString("FechaUtc",DateTime.UtcNow.ToString("O")); x.WriteElementString("IdUsuario",actor.ToString());
                x.WriteElementString("Accion",accion); x.WriteElementString("Backup",backup); x.WriteEndElement(); x.WriteEndDocument();
            }
        }
    }
}
