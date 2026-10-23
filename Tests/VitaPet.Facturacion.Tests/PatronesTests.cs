using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Estados;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;

static class PatronesTests
{
    static int checks;
    static void Check(bool ok,string texto){if(!ok)throw new Exception(texto);checks++;Console.WriteLine("PASS "+texto);}
    static void Rechaza<T>(Action accion,string texto) where T:Exception
    {bool rechaza=false;try{accion();}catch(T){rechaza=true;}Check(rechaza,texto);}
    static HttpContext Sesion(int id)
    {
        var ctx=new HttpContext(new HttpRequest("","http://localhost/Home",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current=ctx;SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));return ctx;
    }
    public static void Run()
    {
        var session=SessionManager.Instancia;
        var instancias=new SessionManager[64];Parallel.For(0,instancias.Length,i=>instancias[i]=SessionManager.Instancia);
        Check(instancias.All(i=>ReferenceEquals(i,session)),"Singleton: los accesos concurrentes reciben la misma instancia");
        var a=Sesion(1);session.ActualizarIdiomaUsuario("es");
        var b=Sesion(4);session.ActualizarIdiomaUsuario("en");session.AutorizarRecuperacionContrasena(4);
        Check(session.UsuarioActual.IdUsuario==4&&session.ObtenerIdiomaUsuario()=="en"&&session.IdUsuarioRecuperacion==4,"Singleton: la segunda sesión conserva su usuario, idioma y recuperación");
        HttpContext.Current=a;
        Check(session.UsuarioActual.IdUsuario==1&&session.ObtenerIdiomaUsuario()=="es"&&!session.RecuperacionContrasenaAutorizada,"Singleton: compartir el gestor no comparte datos entre sesiones");
        HttpContext.Current=b;session.CerrarSesion();
        Check(!session.HaySesionActiva&&!session.RecuperacionContrasenaAutorizada,"Singleton: logout limpia la sesión actual y su recuperación");
        HttpContext.Current=a;Check(session.HaySesionActiva&&session.UsuarioActual.IdUsuario==1,"Singleton: cerrar otra sesión no desconecta a este cliente");
        HttpContext.Current=null;Rechaza<InvalidOperationException>(()=>{var sinContexto=session.UsuarioActual;},"Singleton: no permite leer un usuario sin contexto HTTP");

        var usuarioEstado=new UsuarioBE {Estado=true};var estado=new UsuarioStateBLL(usuarioEstado);
        Check(estado.PuedeIniciarSesion(),"State: el usuario activo puede iniciar sesión");
        usuarioEstado.Estado=false;Check(!estado.PuedeIniciarSesion(),"State: una cuenta inactiva no puede iniciar sesión");usuarioEstado.Estado=true;
        usuarioEstado.RequiereRestablecerContrasena=true;Check(!estado.PuedeIniciarSesion(),"State: el restablecimiento obligatorio impide iniciar sesión");usuarioEstado.RequiereRestablecerContrasena=false;
        for(int i=1;i<=3;i++)
        {estado.RegistrarIntentoFallido();Check(usuarioEstado.IntentosFallidos==i&&estado.PuedeIniciarSesion()==(i<3),"State: transición del intento fallido "+i);}
        estado.RegistrarIntentoFallido();estado.RegistrarIngresoExitoso();
        Check(usuarioEstado.Bloqueado&&usuarioEstado.IntentosFallidos==3&&!estado.PuedeIniciarSesion(),"State: intentos adicionales o un ingreso exitoso no eluden el bloqueo");
        estado.Desbloquear();Check(!usuarioEstado.Bloqueado&&usuarioEstado.IntentosFallidos==0&&estado.PuedeIniciarSesion(),"State: desbloquear vuelve al estado activo y reinicia intentos");
        estado.RegistrarIntentoFallido();estado.RegistrarIngresoExitoso();Check(usuarioEstado.IntentosFallidos==0,"State: el ingreso válido de una cuenta activa reinicia intentos");
        Rechaza<ArgumentNullException>(()=>new UsuarioStateBLL(null),"State: rechaza un contexto sin usuario");

        Sesion(1);var sec=new SecurityManager();var usuarios=new UsuarioDAL();var negocio=new UsuarioBLL();
        var usuario=usuarios.ObtenerPorId(1);
        for(int i=1;i<=3;i++)
        {
            negocio.RegistrarIntentoFallido(usuario);var actual=usuarios.ObtenerPorId(1);
            Check(actual.IntentosFallidos==i&&actual.Bloqueado==(i==3)&&sec.VerificarIntegridad(),"State persistido: intento "+i+" conserva DVH, DVV y referencias");
        }
        Check(!new PerfilBLL().PantallasActuales().Any(),"State persistido: el bloqueo revoca los permisos de una sesión ya abierta");
        Sesion(3);negocio.DesbloquearUsuario(1);var desbloqueado=usuarios.ObtenerPorId(1);
        Check(!desbloqueado.Bloqueado&&desbloqueado.IntentosFallidos==0&&sec.VerificarIntegridad(),"State persistido: desbloquear conserva datos e integridad");

        Check(sec.CalcularHash("abc")=="ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad","Facade: SHA-256 coincide con un vector conocido");
        Check(sec.ValidarDatosObligatorios("nombre","correo")&&!sec.ValidarDatosObligatorios("nombre"," "),"Facade: unifica la validación de campos obligatorios");
        Check(sec.CumpleRequisitosContrasena("Prueba2026!")&&!sec.CumpleRequisitosContrasena("abc"),"Facade: aplica la política de contraseña");
        Check(sec.VerificarDVH(desbloqueado),"Facade: verifica el DVH de un usuario recuperado de la base");
        desbloqueado.Nombre+=" alterado";Check(!sec.VerificarDVH(desbloqueado),"Facade: detecta la alteración del contenido de un usuario");
        string[] dvhs={sec.CalcularHash("uno"),sec.CalcularHash("dos")};var dvv=sec.CalcularDVV(dvhs);
        Check(sec.VerificarDVV(dvhs,dvv),"Facade: verifica el resumen vertical");dvhs[0]=sec.CalcularHash("otro");
        Check(!sec.VerificarDVV(dvhs,dvv),"Facade: detecta un registro cambiado en el resumen vertical");
        int registro=sec.RegistrarBitacora(new BitacoraBE {IdUsuario=3,Fecha=DateTime.Now,Accion="Prueba del patron Facade",Modulo="Patrones",Criticidad="Informativa"});
        Check(new BitacoraDAL().ObtenerParaIntegridad().Any(r=>r.IdBitacora==registro&&r.Accion=="Prueba del patron Facade"),"Facade: registra la auditoría mediante la interfaz de seguridad");
        Check(sec.VerificarIntegridad(),"Facade: la auditoría mantiene la integridad global");
        Sesion(1);Check(sec.ExigirCliente().IdUsuario==1,"Facade: autoriza al cliente vigente");
        Rechaza<UnauthorizedAccessException>(()=>sec.ExigirWebmaster(),"Facade: deniega operaciones de Webmaster al cliente");

        // Reutilizar las pruebas de persistencia, herencia, ciclos y permisos del Composite.
        PerfilesTests.Run();
        Check(sec.VerificarIntegridad(),"Patrones: sesión, estados, fachada y perfiles terminan con integridad válida");
        Console.WriteLine("TOTAL PATRONES BLL: "+checks+" comprobaciones, además de la suite de perfiles");
    }
}
