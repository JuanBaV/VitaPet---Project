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

static class AccesosTests
{
    static int checks;
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static void Rechaza(Action accion,string text)
    {bool denied=false;try{accion();}catch(UnauthorizedAccessException){denied=true;}Check(denied,text);}
    static void Sesion(int id)
    {
        var ctx=new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current=ctx;if(id>0)SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }
    static string Conteos()
    {
        using(var cn=new SqlConnection(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString))
        {
            cn.Open();return string.Join(",",new[]{"Usuario","Cliente","Mascota","Empresa","Plan","Pago","Factura","Rol","RolPermiso","PermisoComponente","Bitacora"}.Select(t=>{
                using(var cmd=new SqlCommand("SELECT COUNT(*) FROM dbo.["+t+"]",cn))return Convert.ToString(cmd.ExecuteScalar());}));
        }
    }
    public static void Run()
    {
        var sec=new SecurityManager();string antes=Conteos();
        var datos=new RegistroClienteBE {Nombre="Intento",Apellido="No autorizado",CorreoElectronico="no.autorizado@test.local",Contrasena="Aa1!aaaa",Telefono="55555555"};
        foreach(int id in new[]{0,1,2,3})
        {
            Sesion(id);string rol=id==0?"Sin sesión":new UsuarioDAL().ObtenerPorId(id).Rol;
            foreach(var pantalla in PerfilBLL.Catalogo)
            {
                bool permitido=id>0&&(rol==pantalla.Value||(rol=="Webmaster"&&pantalla.Key=="ConsultarPerfiles"));
                Check(new PerfilBLL().PuedeAcceder(pantalla.Key)==permitido,rol+": permiso de pantalla "+pantalla.Key);
                if(permitido)Check(sec.ExigirPermiso(pantalla.Key).IdUsuario==id,rol+": BLL autoriza "+pantalla.Key);
                else Rechaza(()=>sec.ExigirPermiso(pantalla.Key),rol+": BLL rechaza "+pantalla.Key);
            }
            if(rol!="Webmaster")
            {
                Rechaza(()=>new AltaUsuarioBLL().Registrar(datos,1),rol+": no crea usuarios por BLL");
                Rechaza(()=>new PerfilBLL().Guardar(0,null,"No autorizado",new[]{1}),rol+": no crea perfiles por BLL");
                Rechaza(()=>new BackupBLL().Crear(),rol+": no inicia backups por BLL");
                Rechaza(()=>new BackupBLL().Restaurar(Guid.NewGuid().ToString("N")),rol+": no inicia restauraciones por BLL");
            }
            if(rol!="Administrador")Rechaza(()=>new PlanBLL().PrepararAlta(),rol+": no prepara el alta de planes por BLL");
        }
        Check(Conteos()==antes,"Los intentos rechazados no agregan registros de negocio, permisos ni bitácora");
        Check(sec.VerificarIntegridad(),"Los rechazos conservan DVH, DVV y referencias");
        Console.WriteLine("TOTAL ACCESOS BLL: "+checks+" comprobaciones");
    }
}
