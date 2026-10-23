using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;

static class PerfilesTests
{
    static int checks;
    static void Check(bool ok,string texto) {if(!ok)throw new Exception(texto);checks++;Console.WriteLine("PASS "+texto);}
    static void Sesion(int id)
    {
        var ctx=new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current=ctx;if(id>0)SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }
    static void Rechaza<T>(Action action,string texto) where T:Exception {bool falla=false;try{action();}catch(T){falla=true;}Check(falla,texto);}
    public static void Run()
    {
        Sesion(3);var b=new PerfilBLL();var lista=b.Consultar();
        var cliente=lista.OfType<PerfilBE>().Single(p => p.Nombre=="Cliente");
        var admin=lista.OfType<PerfilBE>().Single(p => p.Nombre=="Administrador");
        var wm=lista.OfType<PerfilBE>().Single(p => p.Nombre=="Webmaster");
        Check(lista.OfType<PerfilBE>().Count()==3,"Conserva los tres perfiles iniciales");
        Check(cliente.Pantallas().Contains("GestionPago") && !cliente.Pantallas().Contains("AltaUsuario"),"Cliente conserva sus pantallas sin gestión de usuarios");
        Check(admin.Pantallas().Contains("ConsultarPerfiles") && !admin.Pantallas().Contains("GestionPerfiles"),"Administrador consulta perfiles sin modificarlos");
        int consulta=lista.OfType<PermisoPantallaBE>().Single(p => p.Pantalla=="ConsultaPlan").Id;
        b.Guardar(0,null,"Consulta comercial",new[]{consulta});
        var padre=b.Consultar().OfType<PerfilBE>().Single(p => p.Nombre=="Consulta comercial");
        b.Guardar(0,null,"Supervisor comercial",new[]{padre.Id});
        var hijo=b.Consultar().OfType<PerfilBE>().Single(p => p.Nombre=="Supervisor comercial");
        Check(hijo.Hijos.Single() is PerfilBE && hijo.Pantallas().SequenceEqual(new[]{"ConsultaPlan"}),"Composite hereda permisos de un perfil incluido");
        Rechaza<ArgumentException>(() => b.Guardar(padre.Id,padre.Version,padre.Nombre,new[]{hijo.Id}),"Rechaza ciclos indirectos entre perfiles");
        Rechaza<ArgumentException>(() => b.Eliminar(cliente.Id,cliente.Version),"Conserva los perfiles iniciales al intentar eliminarlos");
        Rechaza<ArgumentException>(() => b.Eliminar(padre.Id,padre.Version),"No elimina un perfil incluido en otro");
        Rechaza<ArgumentException>(() => b.Guardar(0,null,"Consulta comercial",new[]{consulta}),"Rechaza nombres duplicados");
        Rechaza<ArgumentException>(() => b.Guardar(0,null,"Inválido",new[]{wm.Id}),"No hereda funciones exclusivas del Webmaster");
        Rechaza<ArgumentException>(() => b.Guardar(wm.Id,wm.Version,wm.Nombre,new[]{consulta}),"No quita accesos esenciales al Webmaster");
        var datos=new RegistroClienteBE {Nombre="Usuario",Apellido="Perfiles",CorreoElectronico="perfiles@test.local",Contrasena="Prueba2026!",Telefono="55555555"};
        int id=new AltaUsuarioBLL().Registrar(datos,hijo.Id);
        Check(new UsuarioDAL().ObtenerPorId(id).Rol==hijo.Nombre,"Alta del Webmaster asigna el perfil adicional");
        Check(new UsuarioDAL().ObtenerPorId(id).Contrasena!=datos.Contrasena,"El alta almacena hash y no la contraseña original");
        Rechaza<RegistroClienteException>(() => new AltaUsuarioBLL().Registrar(datos,hijo.Id),"Rechaza correo de usuario duplicado");
        Rechaza<ArgumentException>(() => b.Eliminar(hijo.Id,hijo.Version),"No elimina perfiles asignados a usuarios");
        Sesion(id);Check(b.PuedeAcceder("ConsultaPlan") && !b.PuedeAcceder("ModificarPlan"),"Perfil adicional permite solo sus pantallas");
        Sesion(2);var plan=new PlanBLL().Listar().First();Sesion(id);
        Check(new PlanBLL().Consultar(plan.IdPlan)!=null,"SQL permite consultar planes desde un perfil adicional autorizado");
        Rechaza<UnauthorizedAccessException>(() => new PlanBLL().PrepararAlta(),"Permiso de consulta no habilita alta de planes");
        Check(b.PuedeAcceder("Home") && b.PuedeAcceder("Logout"),"Perfil adicional puede entrar al inicio y cerrar sesión");
        Sesion(3);padre=b.Consultar().OfType<PerfilBE>().Single(p => p.Id==padre.Id);
        int empresas=b.Consultar().OfType<PermisoPantallaBE>().Single(p => p.Pantalla=="Empresas").Id;
        b.Guardar(padre.Id,padre.Version,padre.Nombre,new[]{empresas});
        Rechaza<ArgumentException>(() => b.Guardar(padre.Id,padre.Version,padre.Nombre,new[]{consulta}),"Rechaza una edición con versión anterior al cambio de permisos");
        Sesion(id);Check(!b.PuedeAcceder("ConsultaPlan") && b.PuedeAcceder("Empresas"),"Cambiar el perfil padre actualiza permisos heredados sin reloguearse");
        Sesion(2);Check(b.Consultar().OfType<PerfilBE>().Any(),"Administrador puede visualizar el Composite");
        Rechaza<UnauthorizedAccessException>(() => b.Guardar(0,null,"No permitido",new[]{consulta}),"Administrador no crea perfiles por BLL");
        Rechaza<UnauthorizedAccessException>(() => new AltaUsuarioBLL().Registrar(datos,cliente.Id),"Administrador no da de alta usuarios por BLL");
        var u=new UsuarioDAL().ObtenerPorId(id);
        Rechaza<UnauthorizedAccessException>(() => new UsuarioBLL().ModificarUsuario(id,u.DVH,new CambiosUsuarioBE {Nombre=u.Nombre,CorreoElectronico=u.CorreoElectronico,IdRol=wm.Id,Estado=true}),"Administrador no cambia el perfil de un usuario por BLL");
        Sesion(3);b.Guardar(0,null,"Temporal",new[]{consulta});var temporal=b.Consultar().OfType<PerfilBE>().Single(p => p.Nombre=="Temporal");
        b.Eliminar(temporal.Id,temporal.Version);Check(!b.Consultar().OfType<PerfilBE>().Any(p => p.Nombre=="Temporal"),"Elimina un perfil adicional sin asignaciones");
        Check(new SecurityManager().VerificarIntegridad(),"Operaciones de perfiles y alta conservan DVH, DVV y referencias");
        Sesion(0);
        int registroPublico=new ClienteBLL().RegistrarCliente(new RegistroClienteBE {Nombre="Registro",Apellido="Publico",CorreoElectronico="publico.perfiles@test.local",Contrasena="Prueba2026!",Telefono="55555555"});
        Check(new UsuarioDAL().ObtenerPorId(registroPublico).Rol=="Cliente","Registro público sin sesión asigna Cliente automáticamente");
        Check(typeof(RegistroClienteBE).GetProperty("Rol")==null,"El contrato del registro público no acepta un rol enviado por el navegador");
        var compatible=typeof(BackupBLL).GetMethod("EsquemaCompatible",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Check((bool)compatible.Invoke(null,new object[]{"029_CorreoFactura.sql:hash;","029_CorreoFactura.sql:hash;030_PerfilesComposite.sql:hash;031_AutorizacionPorPantallas.sql:hash;"}),"Compatibilidad de respaldos admite las migraciones nuevas de perfiles");
        Console.WriteLine("TOTAL PERFILES: "+checks+" comprobaciones");
    }
}
