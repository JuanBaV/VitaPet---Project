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

static class ContrasenasTests
{
    static int checks;
    static void Check(bool ok,string texto) {if(!ok)throw new Exception(texto);checks++;Console.WriteLine("PASS "+texto);}
    static void Rechaza<T>(Action action,string texto) where T:Exception
    {bool falla=false;try{action();}catch(T){falla=true;}Check(falla,texto);}
    static void Sesion(int id)
    {
        var ctx=new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current=ctx;if(id>0)SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }
    static RegistroClienteBE Datos(string clave,string correo)
    {return new RegistroClienteBE {Nombre="Prueba",Apellido="Contrasena",CorreoElectronico=correo,Telefono="55555555",Contrasena=clave};}

    public static void Run()
    {
        var sec=new SecurityManager();var usuarios=new UsuarioDAL();var alta=new AltaUsuarioBLL();
        Sesion(3);
        int rol=new PerfilBLL().Consultar().OfType<RolBE>().Single(p=>p.Nombre=="Cliente"&&p.Sistema).Id;
        int cantidad=usuarios.ObtenerParaIntegridad().Count;
        string hashOriginal=usuarios.ObtenerPorId(1).Contrasena;
        foreach(string clave in new[]{null,"","A","A1","A1!","Aa1!","Aa1!a","Aa1!aa","Aa1!aaa"})
        {
            string largo=clave==null?"null":clave.Length.ToString();
            Check(!sec.CumpleRequisitosContrasena(clave),"Política rechaza longitud "+largo);
            Sesion(0);
            Rechaza<RegistroClienteException>(()=>new ClienteBLL().RegistrarCliente(Datos(clave,"publico.clave@test.local")),"Registro público rechaza longitud "+largo);
            Sesion(3);
            Rechaza<RegistroClienteException>(()=>alta.Registrar(Datos(clave,"alta.clave@test.local"),rol),"Alta del Webmaster rechaza longitud "+largo);
            Rechaza<ArgumentException>(()=>new RecuperacionContrasenaBLL().ActualizarContrasena(1,clave),"Restablecimiento rechaza longitud "+largo);
            Rechaza<ArgumentException>(()=>new ContingenciaWebMasterBLL().Configurar("emergencia@test.local",clave),"Emergencia rechaza longitud "+largo);
        }
        Check(usuarios.ObtenerParaIntegridad().Count==cantidad&&usuarios.ObtenerPorId(1).Contrasena==hashOriginal,
            "Intentos inválidos no crean usuarios ni cambian la contraseña existente");
        Check(sec.VerificarIntegridad(),"Rechazos conservan integridad");
        foreach(string clave in new[]{"abcdefgh","Abcdefg!","Abcdefg1","abcdef1!"})
            Check(!sec.CumpleRequisitosContrasena(clave),"Ocho caracteres sin complejidad suficiente se rechazan");
        foreach(string clave in new[]{"Aa1!aaaa","Aa1!"+new string('a',96)})
        {
            Sesion(0);
            string correo="publico"+clave.Length+"@test.local";
            int id=new ClienteBLL().RegistrarCliente(Datos(clave,correo));
            Check(usuarios.ObtenerPorId(id).Contrasena==sec.CalcularHash(clave),"Registro público acepta "+clave.Length+" caracteres y guarda su hash");
            Sesion(3);
            id=alta.Registrar(Datos(clave,"alta"+clave.Length+"@test.local"),rol);
            Check(usuarios.ObtenerPorId(id).Contrasena==sec.CalcularHash(clave),"Alta del Webmaster acepta "+clave.Length+" caracteres y guarda su hash");
            new RecuperacionContrasenaBLL().ActualizarContrasena(id,"Bb2!"+new string('b',clave.Length-4));
            Check(usuarios.ObtenerPorId(id).Contrasena==sec.CalcularHash("Bb2!"+new string('b',clave.Length-4)),"Restablecimiento acepta "+clave.Length+" caracteres y actualiza su hash");
        }
        foreach(string clave in new[]{"Aa1!aaaa","Aa1!aaaaaaa"})
            Rechaza<ArgumentException>(()=>new ContingenciaWebMasterBLL().Configurar("emergencia@test.local",clave),"Emergencia conserva mínimo de 12: rechaza "+clave.Length);
        Check(sec.VerificarIntegridad(),"Altas y restablecimientos conservan DVH, DVV y referencias");
        Console.WriteLine("TOTAL CONTRASEÑAS: "+checks+" comprobaciones");
    }
}
