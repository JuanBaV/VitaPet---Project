using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;
partial class Program
{
    static int checks;
    static string connection, name, runtime;
    const string Correo = "emergencia@test.local", Clave = "Contingencia2026!Prueba";
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; Console.WriteLine("PASS " + label); }
    static void Denied(Action action, string label)
    { bool denied = false; try { action(); } catch (UnauthorizedAccessException) { denied = true; } Check(denied, label); }
    static void SessionAs(UsuarioBE user = null)
    {
        var c = new HttpContext(new HttpRequest("", "http://localhost/", ""), new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(c, new HttpSessionStateContainer(Guid.NewGuid().ToString(), new SessionStateItemCollection(), new HttpStaticObjectsCollection(), 20, true, HttpCookieMode.UseCookies, SessionStateMode.InProc, false));
        HttpContext.Current = c; if (user != null) SessionManager.Instancia.IniciarSesion(user);
    }
    static void Sql(string query, bool master = false)
    {
        var b = new SqlConnectionStringBuilder(connection); if (master) b.InitialCatalog = "master";
        using (var c = new SqlConnection(b.ConnectionString)) using (var cmd = new SqlCommand(query, c)) { c.Open(); cmd.ExecuteNonQuery(); }
    }
    static void Drop()
    {
        if (!name.StartsWith("VitaPet_Verificacion_Contingencia_", StringComparison.Ordinal)) throw new Exception("Unsafe test database");
        SqlConnection.ClearAllPools();
        Sql("IF DB_ID(N'" + name + "') IS NOT NULL BEGIN ALTER DATABASE [" + name + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [" + name + "]; END", true);
    }
    static void Main(string[] args)
    {
        var cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var b = new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if (!b.InitialCatalog.StartsWith("VitaPet_Verificacion_Contingencia", StringComparison.Ordinal)) throw new Exception("Isolated database required");
        name = "VitaPet_Verificacion_Contingencia_" + Guid.NewGuid().ToString("N"); b.InitialCatalog = name; connection = b.ConnectionString;
        runtime = Path.Combine(Path.GetTempPath(), "vp-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(runtime);
        cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString = connection;
        cfg.AppSettings.Settings["RutaRecuperacion"].Value = Path.Combine(runtime, "recuperacion");
        cfg.AppSettings.Settings["RutaBitacoraXml"].Value = Path.Combine(runtime, "Bitacora.xml");
        cfg.AppSettings.Settings["RutaBackups"].Value = Path.Combine(runtime, "backups");
        var backupDir = Directory.CreateDirectory(Path.Combine(runtime, "backups"));
        var acl = backupDir.GetAccessControl();
        acl.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(@"NT SERVICE\MSSQLSERVER",
            System.Security.AccessControl.FileSystemRights.Modify,
            System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
            System.Security.AccessControl.PropagationFlags.None, System.Security.AccessControl.AccessControlType.Allow));
        backupDir.SetAccessControl(acl);
        cfg.Save(); ConfigurationManager.RefreshSection("connectionStrings"); ConfigurationManager.RefreshSection("appSettings");
        bool conservar = false;
        try
        {
            DatabaseMigrator.Migrate(); SembrarTablas(); new ReferenciaIntegridadBLL().Inicializar();
            var sec = new SecurityManager(); var dal = new UsuarioDAL(); var app = new ContingenciaWebMasterBLL();
            var webmaster = dal.ObtenerPorCorreo("webmaster@vitapet.local");
            if (args.Length > 0 && args[0] == "--web")
            {
                new RecuperacionContrasenaBLL().ActualizarContrasena(webmaster.IdUsuario, "WebMaster2026!Prueba");
                File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web-environment.txt"), new[] { name, runtime });
                conservar = true; Console.WriteLine("Web fixture: " + name); return;
            }
            ProbarIntegridadTablas();
            SessionAs(); Denied(() => app.Configurar(Correo, Clave), "Anonymous cannot provision account");
            SessionAs(dal.ObtenerPorCorreo("cliente@vitapet.local")); Denied(() => app.Configurar(Correo, Clave), "Client cannot provision account");
            SessionAs(webmaster); app.Configurar(Correo, Clave);
            Check(app.Configurada && !app.Necesaria(), "Emergency account provisioned only on healthy database");
            Check(!SessionManager.Instancia.HaySesionActiva, "Provision invalidates prior sessions");
            Check(!app.Autenticar(Correo, Clave), "Emergency credentials disabled while database is healthy");
            string folder = Path.Combine(EstadoAplicacion.Directorio, "webmaster");
            string accountXml = File.ReadAllText(Path.Combine(folder, "cuenta.xml"));
            Check(!accountXml.Contains(Clave) && accountXml.Contains(Correo), "Portable account contains no plaintext password");
            var portableStore = new ContingenciaArchivoDAL(folder);
            var portableAccount = portableStore.LeerCuenta();
            Check(portableAccount.VersionFormato == 1 && Convert.FromBase64String(portableAccount.Sal).Length == 32,
                "Portable format has a 32-byte random salt");
            Check(ProteccionContingencia.Iguales(portableAccount.Hash, ProteccionContingencia.HashContrasena(Clave, portableAccount.Sal)),
                "Persisted verifier matches PBKDF2 password derivation");
            Check(!ProteccionContingencia.Iguales(portableAccount.Hash, ProteccionContingencia.HashContrasena("wrong", portableAccount.Sal)),
                "Incorrect password does not match portable verifier");
            var copiedFolder = Path.Combine(runtime, "copied-account"); Directory.CreateDirectory(copiedFolder);
            File.Copy(Path.Combine(folder, "cuenta.xml"), Path.Combine(copiedFolder, "cuenta.xml"));
            var copiedStore = new ContingenciaArchivoDAL(copiedFolder);
            var copiedAccount = copiedStore.LeerCuenta();
            Check(copiedAccount.Hash == portableAccount.Hash && copiedAccount.ClaveFirma == portableAccount.ClaveFirma,
                "Copied account is readable without machine-specific decryption");
            copiedAccount.VersionFormato = 99;
            bool badFormat = false; try { copiedStore.GuardarCuenta(copiedAccount); } catch (InvalidDataException) { badFormat = true; }
            Check(badFormat, "Unknown account format is rejected");
            File.WriteAllText(Path.Combine(copiedFolder, "cuenta.xml"), "<string>previous-format</string>");
            badFormat = false; try { copiedStore.LeerCuenta(); } catch (InvalidDataException) { badFormat = true; }
            Check(badFormat, "Previous account format is rejected without resetting credentials");
            SessionAs(webmaster);
            int anteriores = Directory.GetFiles(folder, "respaldo-*.xml").Length;
            new BackupBLL().Crear();
            Check(Directory.GetFiles(folder, "respaldo-*.xml").Length == anteriores + 1, "Normal backup also captures a signed XML point");
            ProbarBackupAntesDeSimplificar();
            ProbarMigracionAnterior();
            string snapshot = app.CrearRespaldo();
            Sql("DELETE dbo.Bitacora WHERE idUsuario IN (SELECT idUsuario FROM dbo.Usuario WHERE rol='Webmaster'); DELETE dbo.Usuario WHERE rol='Webmaster'");
            Check(app.Necesaria() && sec.AutenticarRecuperacion("webmaster@vitapet.local", "invalid") == null, "Deleted webmaster requires external contingency");
            SessionAs(); Check(!app.Autenticar(Correo, "bad"), "Bad credentials rejected");
            Check(app.Autenticar(Correo, Clave), "External credentials work with deleted webmaster");
            Check(SessionManager.Instancia.HaySesionContingencia && !SessionManager.Instancia.HaySesionActiva && SessionManager.Instancia.UsuarioActual == null, "Emergency session has no normal user or role");
            Denied(() => sec.ExigirWebmaster(), "Emergency session cannot become regular webmaster");
            Denied(() => new UsuarioBLL().ConsultarUsuarios(new FiltroUsuariosBE()), "Emergency cannot consult users");
            Denied(() => new BackupBLL().Crear(), "Emergency cannot create normal backups");
            Check(app.Listar().Any(x => x.Id == snapshot), "Only authenticated contingency lists recovery points");
            string snapshotFile = Path.Combine(folder, "respaldo-" + snapshot + ".xml"), original = File.ReadAllText(snapshotFile);
            File.WriteAllText(snapshotFile, original.Replace("webmaster@vitapet.local", "intruso@vitapet.local"));
            bool rejected = false; try { app.Restaurar(snapshot); } catch (InvalidDataException) { rejected = true; }
            Check(rejected && dal.ObtenerPorCorreo("webmaster@vitapet.local") == null, "Altered XML cannot modify database");
            File.WriteAllText(snapshotFile, original);
            var store = new ContingenciaArchivoDAL(folder);
            var account = store.LeerCuenta();
            var moved = ContingenciaArchivoDAL.Deserializar<RespaldoContingenciaBE>(original);
            moved.Servidor = "previous-host\\SQLINSTANCE"; moved.Firma = null;
            moved.Firma = ProteccionContingencia.Firmar(ContingenciaArchivoDAL.Serializar(moved), account.ClaveFirma);
            File.WriteAllText(snapshotFile, ContingenciaArchivoDAL.Serializar(moved));
            Check(app.Listar().Any(x => x.Id == snapshot), "Signed backup accepts a different origin server");
            moved.CuentaId = Guid.NewGuid().ToString("N"); moved.Firma = null;
            moved.Firma = ProteccionContingencia.Firmar(ContingenciaArchivoDAL.Serializar(moved), account.ClaveFirma);
            File.WriteAllText(snapshotFile, ContingenciaArchivoDAL.Serializar(moved));
            Check(!app.Listar().Any(x => x.Id == snapshot), "Backup from a different account remains rejected");
            moved.CuentaId = account.Id; moved.BaseDatos = "OtraBase"; moved.Firma = null;
            moved.Firma = ProteccionContingencia.Firmar(ContingenciaArchivoDAL.Serializar(moved), account.ClaveFirma);
            File.WriteAllText(snapshotFile, ContingenciaArchivoDAL.Serializar(moved));
            Check(!app.Listar().Any(x => x.Id == snapshot), "Backup from a different database remains rejected");
            File.WriteAllText(snapshotFile, original);
            var invalid = ContingenciaArchivoDAL.Deserializar<RespaldoContingenciaBE>(original);
            invalid.DatosXml = invalid.DatosXml.Replace("Webmaster de Prueba", "Webmaster alterado");
            invalid.Firma = null;
            invalid.Firma = ProteccionContingencia.Firmar(ContingenciaArchivoDAL.Serializar(invalid), account.ClaveFirma);
            File.WriteAllText(snapshotFile, ContingenciaArchivoDAL.Serializar(invalid));
            rejected = false; try { app.Restaurar(snapshot); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected && !EstadoAplicacion.Mantenimiento && dal.ObtenerPorCorreo("webmaster@vitapet.local") == null,
                "Even signed XML with invalid integrity is rejected in staging before live mutation");
            File.WriteAllText(snapshotFile, original);
            HttpContext.Current.Session["VenceContingencia"] = DateTime.UtcNow.AddSeconds(-1);
            Denied(() => app.Listar(), "Expired session denied");
            SessionAs(); Check(app.Autenticar(Correo, Clave), "Can authenticate again after expiry");
            app.Restaurar(snapshot);
            Check(sec.VerificarIntegridad() && dal.ObtenerPorCorreo("webmaster@vitapet.local") != null, "XML restores original webmaster and DVH DVV references");
            Check(!EstadoAplicacion.Mantenimiento && !SessionManager.Instancia.HaySesionContingencia, "Recovery closes contingency session and maintenance");
            Check(!app.Autenticar(Correo, Clave), "Emergency entry closes when normal access is restored");
            Check(File.Exists(Path.Combine(runtime, "Bitacora.xml")), "Historical XML restored");
            // Simulate a file failure after replacing SQL: maintenance and retry must remain available.
            SessionAs(dal.ObtenerPorCorreo("webmaster@vitapet.local")); snapshot = app.CrearRespaldo();
            Sql("UPDATE dbo.Usuario SET nombre='Alterado' WHERE rol='Webmaster'"); SessionAs();
            Check(app.Autenticar(Correo, Clave), "Recovery available for untrusted webmaster record");
            bool failed = false;
            using (var locked = new FileStream(Path.Combine(runtime, "Bitacora.xml"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            { try { app.Restaurar(snapshot); } catch (IOException) { failed = true; } }
            Check(failed && EstadoAplicacion.Mantenimiento && !SessionManager.Instancia.HaySesionContingencia,
                "Mid-restore failure preserves maintenance and invalidates sessions");
            SessionAs(); Check(app.Autenticar(Correo, Clave), "Recovery login works while maintenance remains active");
            app.Restaurar(snapshot); Check(!EstadoAplicacion.Mantenimiento && sec.VerificarIntegridad(), "Retry completes recovery and releases maintenance");
            // A database failure cannot lock out the recovery page or external identity.
            SessionAs(dal.ObtenerPorCorreo("webmaster@vitapet.local")); snapshot = app.CrearRespaldo();
            Sql("ALTER TABLE dbo.Cliente DROP CONSTRAINT FK_Cliente_Usuario; ALTER TABLE dbo.Bitacora DROP CONSTRAINT FK_Bitacora_Usuario; DROP TABLE dbo.Usuario;");
            SessionAs(); Check(app.Autenticar(Correo, Clave), "Emergency login survives missing Usuario table");
            app.Restaurar(snapshot); Check(sec.VerificarIntegridad(), "XML restores deleted table and foreign keys");
            SessionAs(dal.ObtenerPorCorreo("webmaster@vitapet.local")); snapshot = app.CrearRespaldo();
            Drop(); SessionAs();
            Check(app.Necesaria() && app.Autenticar(Correo, Clave), "Emergency authentication survives missing database");
            app.Restaurar(snapshot);
            Check(sec.VerificarIntegridad() && dal.ObtenerPorCorreo("webmaster@vitapet.local") != null, "XML reconstructs missing database and procedures");
            Sql("DELETE dbo.Bitacora WHERE idUsuario IN (SELECT idUsuario FROM dbo.Usuario WHERE rol='Webmaster'); DELETE dbo.Usuario WHERE rol='Webmaster'"); SessionAs();
            Check(!app.Autenticar(Correo, "bad1") && !app.Autenticar(Correo, "bad2") && !app.Autenticar(Correo, "bad3"), "Three failures rejected");
            SessionAs(); Check(!app.Autenticar(Correo, Clave), "Lockout survives a new HTTP session");
            Console.WriteLine("TOTAL: " + checks + " checks passed");
        }
        finally { HttpContext.Current = null; if (!conservar) Drop(); }
    }
}
