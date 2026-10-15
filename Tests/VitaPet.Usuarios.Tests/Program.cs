using System;
using System.IO;
using System.Linq;
using System.Configuration;
using System.Data.SqlClient;
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
    static string connection;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; Console.WriteLine("PASS " + message); }
    static void SessionAs(UsuarioBE user)
    {
        var ctx = new HttpContext(new HttpRequest("", "http://localhost/", ""), new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx, new HttpSessionStateContainer(Guid.NewGuid().ToString(), new SessionStateItemCollection(), new HttpStaticObjectsCollection(), 20, true, HttpCookieMode.UseCookies, SessionStateMode.InProc, false));
        HttpContext.Current = ctx;
        if (user != null) SessionManager.Instancia.IniciarSesion(user);
    }
    static void Sql(string sql)
    {
        using (var cn = new SqlConnection(connection)) using (var cmd = new SqlCommand(sql, cn)) { cn.Open(); cmd.ExecuteNonQuery(); }
    }
    static void Error(Action action, string code)
    {
        try { action(); throw new Exception("Expected " + code); }
        catch (ConsultaUsuariosException ex) { Check(ex.Codigo == code, code); }
    }
    static void Main(string[] args)
    {
        var cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var cs = new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if (!cs.InitialCatalog.StartsWith("VitaPet_Verificacion_Usuarios")) throw new Exception("Only isolated databases are allowed");
        cs.InitialCatalog = "VitaPet_Verificacion_Usuarios_" + Guid.NewGuid().ToString("N");
        connection = cs.ConnectionString;
        string runtime = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtime", cs.InitialCatalog);
        Directory.CreateDirectory(runtime);
        cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString = connection;
        cfg.AppSettings.Settings["RutaRecuperacion"].Value = Path.Combine(runtime, "recuperacion");
        cfg.AppSettings.Settings["RutaBitacoraXml"].Value = Path.Combine(runtime, "Bitacora.xml");
        cfg.Save(); ConfigurationManager.RefreshSection("connectionStrings"); ConfigurationManager.RefreshSection("appSettings");
        bool keepForBrowser = false;
        try
        {
            DatabaseMigrator.Migrate(); DatabaseMigrator.Migrate();
            var integrity = new IntegridadBLL(); var dal = new UsuarioDAL();
            // All four state/block combinations, duplicate names, accents and literal wildcards.
            for (int i=0;i<28;i++)
            {
                var u = new UsuarioBE { Nombre = i == 27 ? "Literal %_[ prueba" : "José Prueba " + (i/2), CorreoElectronico = "fixture"+i+"@test.local", Rol="Cliente", Estado=i%2==0, Bloqueado=i%4>=2, Idioma="es", Contrasena=integrity.CalcularHash("Prueba2026!") };
                using(var cn=new SqlConnection(connection)) using(var cmd=cn.CreateCommand())
                {
                    cn.Open();cmd.CommandText="INSERT dbo.Usuario(nombre,correoElectronico,rol,estado,bloqueado,idioma,contrasena,dvh) OUTPUT INSERTED.idUsuario VALUES(@nombre,@correo,'Cliente',@estado,@bloqueado,'es',@hash,'');";
                    cmd.Parameters.AddWithValue("@nombre",u.Nombre);cmd.Parameters.AddWithValue("@correo",u.CorreoElectronico);cmd.Parameters.AddWithValue("@estado",u.Estado);cmd.Parameters.AddWithValue("@bloqueado",u.Bloqueado);cmd.Parameters.AddWithValue("@hash",u.Contrasena);
                    u.IdUsuario=Convert.ToInt32(cmd.ExecuteScalar());
                    cmd.Parameters.Clear();cmd.CommandText="UPDATE dbo.Usuario SET dvh=@dvh WHERE idUsuario=@id";cmd.Parameters.AddWithValue("@dvh",integrity.CalcularDVH(u));cmd.Parameters.AddWithValue("@id",u.IdUsuario);cmd.ExecuteNonQuery();
                }
            }
            using(var cn=new SqlConnection(connection)) using(var cmd=cn.CreateCommand())
            {cn.Open();cmd.CommandText="UPDATE dbo.DVV SET valorDVV=@v WHERE nombreTabla='Usuario'";cmd.Parameters.AddWithValue("@v",integrity.CalcularDVV(dal.ObtenerParaIntegridad().Select(u=>u.DVH)));cmd.ExecuteNonQuery();}
            new ReferenciaIntegridadBLL().Inicializar();
            var sec=new SecurityManager();var bll=new UsuarioBLL();
            var webmaster=dal.ObtenerPorCorreo("webmaster@vitapet.local");
            if (args.Length > 0 && args[0] == "--web")
            {
                new RecuperacionContrasenaBLL().ActualizarContrasena(webmaster.IdUsuario, "PruebaVitaPet2026!");
                File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web-environment.txt"), new[] { cs.InitialCatalog, runtime });
                keepForBrowser = true;
                Console.WriteLine("Browser fixture: " + cs.InitialCatalog);
                return;
            }
            ProbarProcedimientosSimplificados();
            ProbarIdioma(bll, dal, sec, webmaster);
            Check(sec.ObtenerError("ER028").Descripcion.Contains("parámetros"),"ER028 belongs to user filters");
            Check(sec.ObtenerError("ER035").Descripcion.Contains("respuesta"),"Recovery message preserved as ER035");
            foreach(var role in new[]{"cliente","administrador",null})
            {
                SessionAs(role==null ? null : dal.ObtenerPorCorreo(role+"@vitapet.local"));
                bool denied=false;try{bll.ConsultarUsuarios(new FiltroUsuariosBE());}catch(UnauthorizedAccessException){denied=true;}
                Check(denied,"Denied " +(role??"anonymous"));
            }
            SessionAs(webmaster);
            var baseline=dal.ObtenerParaIntegridad().Select(u=>u.IdUsuario+":"+u.DVH).ToArray();
            var all=bll.ConsultarUsuarios(new FiltroUsuariosBE());
            Check(all.Usuarios.Count==31 && all.HayUsuariosRegistrados,"Initial listing includes every user");
            foreach(EstadoFiltroUsuario state in Enum.GetValues(typeof(EstadoFiltroUsuario)))
            {
                var result=bll.ConsultarUsuarios(new FiltroUsuariosBE{Estado=state});
                var expected=all.Usuarios.Where(u=> state==EstadoFiltroUsuario.Todos || state==EstadoFiltroUsuario.Activo&&u.Estado || state==EstadoFiltroUsuario.Inactivo&&!u.Estado || state==EstadoFiltroUsuario.Bloqueado&&u.Bloqueado || state==EstadoFiltroUsuario.Desbloqueado&&!u.Bloqueado).Select(u=>u.IdUsuario).ToArray();
                Check(result.Usuarios.Select(u=>u.IdUsuario).SequenceEqual(expected),"Exact filter "+state);
            }
            var combined=bll.ConsultarUsuarios(new FiltroUsuariosBE{Nombre="  JOSE PRUEBA  ",Estado=EstadoFiltroUsuario.Bloqueado});
            Check(combined.Usuarios.Count==13 && combined.Usuarios.All(u=>u.Bloqueado),"Partial name + state ignores case/accent and trims");
            Check(bll.ConsultarUsuarios(new FiltroUsuariosBE{Nombre="%_["}).Usuarios.Count==1,"Wildcard characters are literal");
            var empty=bll.ConsultarUsuarios(new FiltroUsuariosBE{Nombre="' OR 1=1 --"});
            Check(empty.Usuarios.Count==0 && empty.HayUsuariosRegistrados,"No matches distinguished from empty database; parameterized SQL");
            var audit=new BitacoraDAL().ObtenerParaIntegridad().Last();
            Check(audit.Accion=="Consulta de usuarios"&&audit.Modulo=="Gestión de usuarios"&&audit.IdUsuario==webmaster.IdUsuario&&audit.Criticidad=="Informativa","Empty query is audited");
            Check(typeof(UsuarioListadoBE).GetProperty("Contrasena")==null && typeof(UsuarioListadoBE).GetProperty("DVH")==null,"Listing contract contains no hashes");
            foreach(var u in all.Usuarios)
                Check(u.PuedeBloquear==(u.Estado&&!u.Bloqueado)&&u.PuedeDesbloquear==u.Bloqueado,"Actions "+u.IdUsuario);
            int count=new BitacoraDAL().ObtenerParaIntegridad().Count;
            var selected=bll.ObtenerUsuarioSeleccionado(combined.Usuarios[0].IdUsuario,new FiltroUsuariosBE());
            Check(selected.Bloqueado && new BitacoraDAL().ObtenerParaIntegridad().Count==count,"Selection validates state without auditing another query");
            Check(bll.ObtenerUsuarioSeleccionado(-1,new FiltroUsuariosBE())==null,"Unknown selection rejected");
            Error(()=>bll.ConsultarUsuarios(new FiltroUsuariosBE{Estado=(EstadoFiltroUsuario)99}),"ER025");
            Error(()=>bll.ConsultarUsuarios(new FiltroUsuariosBE{Nombre=new string('a',101)}),"ER025");
            for(int i=0;i<52;i++) bll.ConsultarUsuarios(new FiltroUsuariosBE());
            Check(sec.VerificarIntegridad(),"DVH/DVV and field references remain valid after archive rollover");
            Check(baseline.SequenceEqual(dal.ObtenerParaIntegridad().Select(u=>u.IdUsuario+":"+u.DVH)),"Queries and selection never mutate Usuario");
            ProbarExtensiones(bll, dal, sec, webmaster);
            Sql("UPDATE dbo.Usuario SET nombre='Tampered' WHERE correoElectronico='fixture1@test.local'");
            Error(()=>bll.ConsultarUsuarios(new FiltroUsuariosBE()),"ER023");
            Console.WriteLine("TOTAL: "+checks+" checks passed");
        }
        finally
        {
            if (!keepForBrowser)
            {
            HttpContext.Current=null;SqlConnection.ClearAllPools();
            var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};
            using(var cn=new SqlConnection(master.ConnectionString)) using(var cmd=cn.CreateCommand())
            {cn.Open();cmd.CommandText="ALTER DATABASE ["+cs.InitialCatalog+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+cs.InitialCatalog+"];";cmd.ExecuteNonQuery();}
            }
        }
    }
}
