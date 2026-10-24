using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Web;
using System.Web.SessionState;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.Pruebas;
using VitaPet.SECURITY;

static class EsquemaActualTests
{
    static int checks;
    static readonly SecurityManager seguridad = new SecurityManager();
    static void Check(bool ok,string texto) { if (!ok) throw new Exception(texto); checks++; Console.WriteLine("PASS " + texto); }
    static void Sql(string sql)
    { using (var cn = new ConexionDAL().CrearConexion()) using (var cmd = new SqlCommand(sql,cn)) { cn.Open(); cmd.ExecuteNonQuery(); } }
    static int Count(string sql)
    { using (var cn = new ConexionDAL().CrearConexion()) using (var cmd = new SqlCommand(sql,cn)) { cn.Open(); return Convert.ToInt32(cmd.ExecuteScalar()); } }
    static void Sesion(int id)
    {
        var ctx = new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current = ctx;
        if (id > 0) SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }
    static string SinUltimaVersion(string esquema) => string.Concat(esquema.Split(';').Where(s => s.Length > 0 && !s.StartsWith("035_",StringComparison.Ordinal)).Select(s => s + ";"));
    static void Rechaza(Action accion,string texto)
    { bool fallo=false;try { accion(); } catch (InvalidDataException) { fallo=true; } Check(fallo,texto); }

    public static void Run(string runtime)
    {
        string claveAnterior=Environment.GetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO");
        string archivo=Path.Combine(runtime,"cifrado.key");
        byte[] secreto=new byte[32];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(secreto);
        File.WriteAllText(archivo,Convert.ToBase64String(secreto));
        Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO",archivo);
        var config=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        string carpeta=Path.Combine(Directory.GetCurrentDirectory(),"VitaPet Rework","App_Data","Backups",Path.GetFileName(runtime));
        Directory.CreateDirectory(carpeta);
        config.AppSettings.Settings.Remove("RutaBackups");config.AppSettings.Settings.Add("RutaBackups",carpeta);config.Save();ConfigurationManager.RefreshSection("appSettings");
        try
        {
            BasePruebas.Cifrar();Sesion(3);
            foreach (var assembly in new[] {typeof(SecurityManager).Assembly,typeof(ConexionDAL).Assembly,typeof(UsuarioBE).Assembly,typeof(SessionManager).Assembly})
            {
                Check(!assembly.GetTypes().Any(t => t.Name.IndexOf("Migracion",StringComparison.OrdinalIgnoreCase)>=0 || t.Name=="DatabaseMigrator"),assembly.GetName().Name+": sin clases de migración");
                Check(!assembly.GetManifestResourceNames().Any(n => n.Contains(".Migrations.") || n.Contains(".Cifrado.034_")),assembly.GetName().Name+": sin scripts históricos ejecutables");
            }
            string datos=new ContingenciaDatosDAL().ExportarXml();
            for(int i=0;i<3;i++)Check(EsquemaDAL.Inconsistencias().Count==0&&seguridad.VerificarIntegridad(),"Validación de esquema actual de sólo lectura: "+i);
            Check(datos==new ContingenciaDatosDAL().ExportarXml(),"Validar el esquema no modifica filas, verificadores ni historial");
            using(var tx=new OperacionIntegridadDAL())
            {
                Sql("DELETE dbo.__SchemaMigrations WHERE migrationId='035_BitacoraBajaLogica.sql'");
                string incompleto=new ContingenciaDatosDAL().ExportarXml();
                Check(!seguridad.VerificarIntegridad(),"Esquema incompleto se rechaza sin aplicar migraciones");
                Check(incompleto==new ContingenciaDatosDAL().ExportarXml(),"El rechazo no inicializa ni repara los controles");
            }
            bool nombreRechazado=false;try{EsquemaDAL.CrearTemporal("VitaPet");}catch(ArgumentException){nombreRechazado=true;}
            Check(nombreRechazado,"Creación de esquema rechaza el nombre de la base de uso");
            string temporal="VitaPet_Verificacion_"+Guid.NewGuid().ToString("N");
            try
            {
                EsquemaDAL.CrearTemporal(temporal);
                bool existente=false;try{EsquemaDAL.CrearTemporal(temporal);}catch(SqlException){existente=true;}
                Check(existente,"Reconstrucción no reemplaza una base que ya existe");
                new ContingenciaDatosDAL().ImportarTemporal(temporal,datos);
                using(ConexionDAL.UsarBaseTemporal(temporal))Check(seguridad.VerificarIntegridad(),"Esquema fijo reconstruye XML cifrado con los verificadores originales");
            }
            finally {new BackupDAL().EliminarTemporal(temporal);}

            var contingencia=new ContingenciaWebMasterBLL();
            const string correo="recuperacion@example.test",password="Recuperacion2026!";
            contingencia.Configurar(correo,password);Sesion(3);
            var respaldos=new BackupBLL();var backup=respaldos.Crear();
            string catalogo=Path.Combine(EstadoAplicacion.Directorio,"catalogo",backup.Id+".xml");
            string catalogoOriginal=File.ReadAllText(catalogo),firmaOriginal=File.ReadAllText(catalogo+".firma");
            try
            {
                var antiguo=ContingenciaArchivoDAL.Deserializar<BackupBE>(catalogoOriginal);antiguo.Esquema=SinUltimaVersion(antiguo.Esquema);
                string xml=ContingenciaArchivoDAL.Serializar(antiguo);File.WriteAllText(catalogo,xml);File.WriteAllText(catalogo+".firma","sha256:"+EstadoAplicacion.CalcularHuella(xml));
                string antes=new ContingenciaDatosDAL().ExportarXml();
                Check(!respaldos.Listar().Any(b=>b.Id==backup.Id),"Selector SQL no ofrece respaldos de versiones anteriores");
                Rechaza(()=>respaldos.Restaurar(backup.Id),"Restauración SQL rechaza versiones anteriores");
                Check(antes==new ContingenciaDatosDAL().ExportarXml()&&!EstadoAplicacion.Mantenimiento,"Backup incompatible no altera la base ni inicia mantenimiento");
            }
            finally {File.WriteAllText(catalogo,catalogoOriginal);File.WriteAllText(catalogo+".firma",firmaOriginal);}
            seguridad.RegistrarBitacora(new BitacoraBE {IdUsuario=3,Fecha=DateTime.Now,Accion="Posterior al respaldo de prueba",Modulo="Prueba",Criticidad="Informativa"});
            Sql("DELETE dbo.__SchemaMigrations WHERE migrationId='035_BitacoraBajaLogica.sql'");
            Check(!seguridad.VerificarIntegridad()&&respaldos.Listar().Any(b=>b.Id==backup.Id),"Backup actual sigue disponible aunque el historial del destino esté dañado");
            respaldos.Restaurar(backup.Id);Sesion(3);
            Check(seguridad.VerificarIntegridad()&&Count("SELECT COUNT(*) FROM dbo.Bitacora WHERE accion='Posterior al respaldo de prueba'")==0,"Restauración SQL actual conserva cifrado e integridad sin conversiones");

            string id=contingencia.CrearRespaldo();
            var archivos=new ContingenciaArchivoDAL(Path.Combine(EstadoAplicacion.Directorio,"webmaster"));
            var cuenta=archivos.LeerCuenta();string original=archivos.LeerRespaldo(id);
            Sql("UPDATE dbo.Usuario SET nombre='VP1:00' WHERE idUsuario=1");
            Check(!seguridad.VerificarIntegridad()&&contingencia.Necesaria(),"Alteración detectada habilita únicamente la recuperación");
            Check(contingencia.Autenticar(correo,password),"Cuenta externa permite recuperar la base sin migraciones");
            try
            {
                var antiguo=ContingenciaArchivoDAL.Deserializar<RespaldoContingenciaBE>(original);
                antiguo.Esquema=SinUltimaVersion(antiguo.Esquema);antiguo.Firma=null;
                antiguo.Firma=ProteccionContingencia.Firmar(ContingenciaArchivoDAL.Serializar(antiguo),cuenta.ClaveFirma);
                archivos.GuardarRespaldo(id,ContingenciaArchivoDAL.Serializar(antiguo));
                string antes=new ContingenciaDatosDAL().ExportarXml();
                Check(!contingencia.Listar().Any(b=>b.Id==id),"Selector XML no ofrece respaldos de versiones anteriores");
                Rechaza(()=>contingencia.Restaurar(id),"Recuperación XML rechaza un esquema anterior aunque su firma sea válida");
                Check(antes==new ContingenciaDatosDAL().ExportarXml()&&!EstadoAplicacion.Mantenimiento,"XML incompatible conserva la evidencia y el estado original");
            }
            finally {archivos.GuardarRespaldo(id,original);}
            contingencia.Restaurar(id);Sesion(3);
            Check(seguridad.VerificarIntegridad()&&!EstadoAplicacion.Mantenimiento,"Recuperación XML completa restaura el esquema cifrado actual sin migraciones");
            Check(new UsuarioDAL().ObtenerPorId(1).Nombre!="VP1:00","Recuperación XML devuelve el dato original descifrado");
            var builder=new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString);
            string nombre=builder.InitialCatalog;
            if(!nombre.StartsWith("VitaPet_Verificacion_Rework_",StringComparison.Ordinal))throw new Exception("Sólo base aislada.");
            builder.InitialCatalog="master";SqlConnection.ClearAllPools();
            using(var cn=new SqlConnection(builder.ConnectionString))
            using(var cmd=new SqlCommand("ALTER DATABASE ["+nombre+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+nombre+"];",cn))
            {cn.Open();cmd.ExecuteNonQuery();}
            Sesion(0);Check(contingencia.Autenticar(correo,password),"Cuenta externa funciona aunque falte la base principal");
            contingencia.Restaurar(id);Sesion(3);
            Check(seguridad.VerificarIntegridad()&&!EstadoAplicacion.Mantenimiento,"XML reconstruye una base principal inexistente sin ejecutar migraciones");
            Console.WriteLine("TOTAL: "+checks+" verificaciones de aplicación sin migraciones");
        }
        finally
        {
            Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO",claveAnterior);
            foreach(string file in Directory.GetFiles(carpeta,"*.bak"))File.Delete(file);
        }
    }
}
