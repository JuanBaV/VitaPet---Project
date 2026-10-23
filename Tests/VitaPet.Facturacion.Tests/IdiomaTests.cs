using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;

static class IdiomaTests
{
    static int checks;
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static void Sesion(int id)
    {
        var context=new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(context,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current=context;if(id>0)SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }
    static void Rechaza<T>(Action accion,string texto) where T:Exception
    {bool rechazado=false;try{accion();}catch(T){rechazado=true;}Check(rechazado,texto);}
    public static void Run()
    {
        string database=new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString).InitialCatalog;
        if(!Regex.IsMatch(database,@"\AVitaPet_Verificacion_Rework_[a-f0-9]{32}\z"))throw new InvalidOperationException("La prueba de idioma requiere una base temporal aislada.");
        var usuarios=new UsuarioDAL();var negocio=new UsuarioBLL();var sesion=SessionManager.Instancia;var seguridad=new SecurityManager();
        Sesion(0);Check(sesion.ObtenerIdiomaUsuario()=="es","La sesión pública comienza en español");
        sesion.ActualizarIdiomaUsuario("en");Check(sesion.ObtenerIdiomaUsuario()=="en","El visitante cambia a inglés sin cuenta");
        Rechaza<UnauthorizedAccessException>(()=>negocio.ActualizarIdioma(1,"en"),"El visitante no modifica el idioma de una cuenta");
        foreach(int id in new[]{1,2,3})
        {
            Sesion(id);var original=usuarios.ObtenerPorId(id);
            foreach(string idioma in new[]{"en","es","en"})
            {
                negocio.ActualizarIdioma(id,idioma);var actual=usuarios.ObtenerPorId(id);
                Check(actual.Idioma==idioma&&sesion.ObtenerIdiomaUsuario()==idioma,original.Rol+": idioma persistido y sesión sincronizada "+idioma);
                Check(actual.DVH==seguridad.CalcularDVH(actual)&&seguridad.VerificarIntegridad(),original.Rol+": DVH, DVV y referencias íntegros "+idioma);
                Check(actual.Contrasena==original.Contrasena&&actual.Nombre==original.Nombre&&actual.CorreoElectronico==original.CorreoElectronico,original.Rol+": cambiar idioma conserva los datos de la cuenta");
            }
            Sesion(id);Check(sesion.ObtenerIdiomaUsuario()=="en",original.Rol+": una nueva sesión recupera el idioma guardado");
            foreach(string invalido in new[]{null,"","fr","EN","es-AR"})
                Rechaza<ArgumentException>(()=>negocio.ActualizarIdioma(id,invalido),original.Rol+": rechaza idioma inválido "+(invalido??"NULL"));
            Rechaza<UnauthorizedAccessException>(()=>negocio.ActualizarIdioma(id==1?2:1,"es"),original.Rol+": no modifica preferencias ajenas");
            Check(usuarios.ObtenerPorId(id).Idioma=="en"&&seguridad.VerificarIntegridad(),original.Rol+": los rechazos no alteran la preferencia ni la integridad");
        }
        Console.WriteLine("TOTAL IDIOMA BLL: "+checks+" comprobaciones");
    }
}
