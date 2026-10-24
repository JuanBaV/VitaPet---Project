using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;
namespace VitaPet.BLL
{
    public sealed class ContingenciaWebMasterBLL
    {
        private readonly SecurityManager seguridad = new SecurityManager();
        private readonly ContingenciaArchivoDAL archivos = new ContingenciaArchivoDAL(Path.Combine(EstadoAplicacion.Directorio, "webmaster"));
        private readonly ContingenciaDatosDAL datos = new ContingenciaDatosDAL();
        private readonly BackupDAL backups = new BackupDAL();
        public bool Configurada => archivos.ExisteCuenta;
        private CuentaContingenciaBE LeerCuenta() => archivos.LeerCuenta();
        private void GuardarCuenta(CuentaContingenciaBE cuenta) => archivos.GuardarCuenta(cuenta);
        private bool HayWebmasterConfiable() => new UsuarioDAL().ObtenerParaIntegridad().Any(u => seguridad.EsWebmasterConfiable(u));
        public bool RequiereAccesoExterno()
        {
            if (!Configurada) return false;
            if (EstadoAplicacion.Mantenimiento) return true;
            try { return !HayWebmasterConfiable(); }
            catch (SqlException) { return true; }
            catch (InvalidOperationException) { return true; }
        }
        public bool Necesaria()
        {
            if (EstadoAplicacion.Mantenimiento) return true;
            try { return !seguridad.DiagnosticarIntegridad().EsValida || !HayWebmasterConfiable(); }
            catch (SqlException) { return true; } // La autenticación de emergencia no consulta SQL.
            catch (InvalidOperationException) { return true; }
        }
        private static bool CorreoValido(string correo)
        {
            try { return new MailAddress(correo).Address == correo; }
            catch (FormatException) { return false; }
        }
        public void Configurar(string correo, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(correo) || correo.Length > 100 || !CorreoValido(correo) ||
                string.IsNullOrEmpty(contrasena) || contrasena.Length < 12 || contrasena.Length > 100 || !seguridad.CumpleRequisitosContrasena(contrasena))
                throw new ArgumentException("Ingrese un correo válido y una contraseña de 12 a 100 caracteres, con mayúscula, número y símbolo.");
            using (EstadoAplicacion.BloquearOperacion())
            using (var op = new OperacionIntegridadDAL())
            {
                seguridad.ExigirWebmaster(); seguridad.ExigirIntegridad();
                if (EstadoAplicacion.Mantenimiento) throw new InvalidOperationException("No se puede configurar durante una restauración.");
                archivos.Preparar();
                var cuenta = Configurada ? LeerCuenta() : new CuentaContingenciaBE { VersionFormato = 1, Id = Guid.NewGuid().ToString("N"), ClaveFirma = ProteccionContingencia.Aleatorio(32) };
                cuenta.Correo = correo; cuenta.Sal = ProteccionContingencia.Aleatorio(32);
                cuenta.Hash = ProteccionContingencia.HashContrasena(contrasena, cuenta.Sal);
                cuenta.Intentos = 0; cuenta.BloqueadaHastaUtc = DateTime.MinValue;
                // El punto sano queda listo antes de publicar las credenciales.
                GuardarRespaldo(cuenta);
                GuardarCuenta(cuenta);
                archivos.Auditar("Cuenta de emergencia configurada", cuenta.Id, null);
                EstadoAplicacion.InvalidarSesiones();
                op.Complete();
            }
        }
        public string CrearRespaldo()
        {
            using (EstadoAplicacion.BloquearOperacion())
            using (var op = new OperacionIntegridadDAL())
            {
                seguridad.ExigirWebmaster(); seguridad.ExigirIntegridad();
                if (EstadoAplicacion.Mantenimiento) throw new InvalidOperationException("Restauración en curso.");
                var id = GuardarRespaldo(LeerCuenta()); op.Complete(); return id;
            }
        }
        // El llamador ya posee el bloqueo de la operación de backup.
        internal void CapturarDesdeBackup()
        {
            if (!Configurada) return;
            using (var op = new OperacionIntegridadDAL())
            { seguridad.ExigirWebmaster(); seguridad.ExigirIntegridad(); GuardarRespaldo(LeerCuenta()); op.Complete(); }
        }
        private string GuardarRespaldo(CuentaContingenciaBE cuenta)
        {
            if (!HayWebmasterConfiable()) throw new InvalidOperationException("Se requiere un WebMaster activo y confiable en el respaldo.");
            var builder = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString);
            string historico = new BitacoraBLL().ObtenerRutaArchivo();
            var b = new RespaldoContingenciaBE { Id = Guid.NewGuid().ToString("N"), CuentaId = cuenta.Id,
                BaseDatos = builder.InitialCatalog, Servidor = builder.DataSource, FechaUtc = DateTime.UtcNow,
                Esquema = backups.Esquema(), DatosXml = datos.ExportarXml(),
                HistoricoXml = File.Exists(historico) ? File.ReadAllText(historico) : "<Bitacora />" };
            ValidarHistorico(b.HistoricoXml);
            b.Firma = ProteccionContingencia.Firmar(ContingenciaArchivoDAL.Serializar(b), cuenta.ClaveFirma);
            archivos.GuardarRespaldo(b.Id, ContingenciaArchivoDAL.Serializar(b));
            archivos.Auditar("Respaldo XML creado", cuenta.Id, b.Id); return b.Id;
        }
        private static void ValidarHistorico(string xml)
        {
            using (var s = new StringReader(xml))
            using (var r = System.Xml.XmlReader.Create(s, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null }))
            {
                var d = new System.Xml.XmlDocument { XmlResolver = null }; d.Load(r);
                if (d.DocumentElement == null || d.DocumentElement.Name != "Bitacora") throw new InvalidDataException("Histórico XML inválido.");
            }
        }
        public bool Autenticar(string correo, string contrasena)
        {
            using (EstadoAplicacion.BloquearOperacion())
            {
                if (!Configurada || !Necesaria()) return false;
                var cuenta = LeerCuenta();
                if (cuenta.BloqueadaHastaUtc > DateTime.UtcNow) return false;
                bool valido = contrasena != null && contrasena.Length <= 100 &&
                    ProteccionContingencia.Iguales(cuenta.Hash, ProteccionContingencia.HashContrasena(contrasena, cuenta.Sal)) &&
                    string.Equals(cuenta.Correo, correo, StringComparison.OrdinalIgnoreCase);
                if (!valido)
                {
                    cuenta.Intentos++;
                    if (cuenta.Intentos >= 3) { cuenta.Intentos = 0; cuenta.BloqueadaHastaUtc = DateTime.UtcNow.AddMinutes(15); }
                    GuardarCuenta(cuenta); archivos.Auditar("Autenticación de emergencia rechazada", cuenta.Id, null); return false;
                }
                cuenta.Intentos = 0; cuenta.BloqueadaHastaUtc = DateTime.MinValue; GuardarCuenta(cuenta);
                archivos.Auditar("Autenticación de emergencia aceptada", cuenta.Id, null);
                SessionManager.Instancia.IniciarContingencia(cuenta.Id); return true;
            }
        }
        private CuentaContingenciaBE ExigirAcceso()
        {
            var sesion = SessionManager.Instancia;
            if (!sesion.HaySesionContingencia || !Configurada) throw new UnauthorizedAccessException("Acceso de emergencia requerido.");
            var cuenta = LeerCuenta();
            if (sesion.CuentaContingencia != cuenta.Id || !Necesaria())
            { sesion.CerrarSesion(); throw new UnauthorizedAccessException("La contingencia ha finalizado."); }
            return cuenta;
        }
        private RespaldoContingenciaBE LeerRespaldo(string id, CuentaContingenciaBE cuenta)
        {
            var b = ContingenciaArchivoDAL.Deserializar<RespaldoContingenciaBE>(archivos.LeerRespaldo(id));
            var firma = b.Firma; b.Firma = null;
            var builder = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString);
            if (b.Id != id || b.CuentaId != cuenta.Id || b.BaseDatos != builder.InitialCatalog ||
                !ProteccionContingencia.Iguales(firma, ProteccionContingencia.Firmar(ContingenciaArchivoDAL.Serializar(b), cuenta.ClaveFirma)))
                throw new InvalidDataException("El respaldo XML fue alterado o no corresponde a la cuenta o base de datos configuradas.");
            b.Firma = firma; ValidarHistorico(b.HistoricoXml); return b;
        }
        public List<RespaldoContingenciaBE> Listar()
        {
            var cuenta = ExigirAcceso();
            var lista = new List<RespaldoContingenciaBE>();
            foreach (string id in archivos.Respaldos())
            {
                try { lista.Add(LeerRespaldo(id, cuenta)); }
                catch (InvalidDataException) { } // No ofrecer archivos alterados; otros puntos sanos siguen disponibles.
                catch (InvalidOperationException) { }
            }
            return lista.OrderByDescending(b => b.FechaUtc).ToList();
        }
        public void Restaurar(string id)
        {
            using (EstadoAplicacion.BloquearOperacion())
            {
                var cuenta = ExigirAcceso(); var b = LeerRespaldo(id, cuenta);
                string temporal = "VitaPet_Verificacion_" + Guid.NewGuid().ToString("N");
                string carpeta = EstadoAplicacion.ResolverRuta("RutaBackups", "~/App_Data/Backups");
                Directory.CreateDirectory(carpeta);
                string reconstruido = Path.Combine(carpeta, "xml_" + Guid.NewGuid().ToString("N") + ".bak");
                bool iniciado = false;
                try
                {
                    if (!BackupBLL.EsquemaCompatible(b.Esquema,EsquemaDAL.VersionActual))
                        throw new InvalidDataException("El respaldo XML no corresponde al esquema actual.");
                    EsquemaDAL.CrearTemporal(temporal);
                    datos.ImportarTemporal(temporal, b.DatosXml);
                    using (ConexionDAL.UsarBaseTemporal(temporal))
                    {
                        seguridad.ExigirIntegridad();
                        if (!HayWebmasterConfiable()) throw new InvalidDataException("El XML no recupera un WebMaster válido.");
                    }
                    backups.CrearDesdeTemporal(temporal, reconstruido); backups.VerificarArchivo(reconstruido);
                    archivos.Auditar("Restauración XML iniciada", cuenta.Id, id);
                    EstadoAplicacion.IniciarMantenimiento("xml:" + id); iniciado = true;
                    EstadoAplicacion.InvalidarSesiones();
                    backups.RestaurarReconstruccionXml(reconstruido);
                    seguridad.ExigirIntegridad();
                    string historico = new BitacoraBLL().ObtenerRutaArchivo();
                    Directory.CreateDirectory(Path.GetDirectoryName(historico));
                    EstadoAplicacion.EscribirAtomico(historico, b.HistoricoXml);
                    seguridad.RegistrarBitacora(new BitacoraBE { IdUsuario = null, Fecha = DateTime.Now,
                        Accion = "Restauración XML de contingencia", Modulo = "Base de datos", Criticidad = "Informativa" });
                    seguridad.ExigirIntegridad();
                    archivos.Auditar("Restauración XML completada", cuenta.Id, id);
                    EstadoAplicacion.FinalizarMantenimiento();
                    SessionManager.Instancia.CerrarSesion();
                }
                catch
                {
                    archivos.Auditar(iniciado ? "Restauración fallida; mantenimiento activo" : "Respaldo XML rechazado", cuenta.Id, id);
                    throw;
                }
                finally
                {
                    backups.EliminarTemporal(temporal);
                    if (File.Exists(reconstruido)) File.Delete(reconstruido);
                }
            }
        }
    }
}
