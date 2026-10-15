using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;

class Program
{
    static string connection;static int checks;
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static void Sql(string text){using(var c=new SqlConnection(connection))using(var cmd=new SqlCommand(text,c)){c.Open();cmd.ExecuteNonQuery();}}
    static int Count(string table){using(var c=new SqlConnection(connection))using(var cmd=new SqlCommand("SELECT COUNT(*) FROM dbo.["+table+"]",c)){c.Open();return(int)cmd.ExecuteScalar();}}
    static void SessionAs(int id)
    {
        var ctx=new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current=ctx;if(id>0)SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }
    static void Error(Action action,string code){try{action();throw new Exception("Expected "+code);}catch(RegistroMascotaException e){Check(e.Codigo==code,"Error "+code);}}
    static void Refrescar(string table)
    {
        var d=new IntegridadTablasDAL();var t=d.Leer(table);var h=new IntegridadTablasBLL();
        foreach(DataRow r in t.Rows){string v=h.CalcularDVH(r);d.GuardarDvh(table,r[IntegridadTablasDAL.Claves[table]],v);r["dvh"]=v;}
        new DVVDAL().Guardar(new DVVBE{NombreTabla=table,ValorDVV=new IntegridadBLL().CalcularDVV(t.Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))});
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(new ReferenciaIntegridadBLL(),null);
    }
    static RegistroMascotaBE Datos(){return new RegistroMascotaBE{Nombre="  Nube  ",Especie=" Perro ",Raza=" Mestizo ",FechaNacimiento=new DateTime(2020,1,2)};}
    static void Main(string[] args)
    {
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);var cs=new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if(!cs.InitialCatalog.StartsWith("VitaPet_Verificacion_Mascota",StringComparison.Ordinal))throw new Exception("Only isolated databases allowed");
        cs.InitialCatalog="VitaPet_Verificacion_Mascota_"+Guid.NewGuid().ToString("N");connection=cs.ConnectionString;
        var runtime=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"runtime",cs.InitialCatalog);Directory.CreateDirectory(runtime);
        cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=connection;cfg.AppSettings.Settings["RutaRecuperacion"].Value=Path.Combine(runtime,"recuperacion");cfg.AppSettings.Settings["RutaBitacoraXml"].Value=Path.Combine(runtime,"Bitacora.xml");cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");ConfigurationManager.RefreshSection("appSettings");
        bool keep=false;
        try
        {
            DatabaseMigrator.Migrate();DatabaseMigrator.Migrate();
            Sql("INSERT dbo.Usuario(correoElectronico,contrasena,nombre,rol,estado,dvh) VALUES('otro@test.local','hash','Otro','Cliente',1,''),('sinperfil@test.local','hash','Sin Perfil','Cliente',1,''); INSERT dbo.Cliente(idUsuario,nombre,apellido,telefono,tipoCliente,estado,dvh) VALUES(1,'Cliente','Prueba','55555555','Individual',1,''),(4,'Otro','Cliente','55555555','Individual',1,''); INSERT dbo.Mascota(idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh) VALUES(1,'Luna','Perro','Mestizo','20200101',1,''),(1,'Sol','Gato','Mestizo','20210101',1,''),(1,'Inactiva','Gato','Mestizo','20210101',0,''),(2,'Ajena','Perro','Mestizo','20210101',1,'');");
            var users=new UsuarioDAL();var h=new IntegridadBLL();
            foreach(var u in users.ObtenerParaIntegridad())
            {
                if(u.IdUsuario==1||u.IdUsuario==4||u.IdUsuario==5)
                {
                    u.Contrasena=h.CalcularHash("Prueba2026!");
                    using(var c=new SqlConnection(connection))using(var cmd=new SqlCommand("UPDATE dbo.Usuario SET contrasena=@p WHERE idUsuario=@id",c)){cmd.Parameters.AddWithValue("@p",u.Contrasena);cmd.Parameters.AddWithValue("@id",u.IdUsuario);c.Open();cmd.ExecuteNonQuery();}
                }
                users.ActualizarDVH(u.IdUsuario,h.CalcularDVH(u));
            }
            new DVVDAL().Guardar(new DVVBE{NombreTabla="Usuario",ValorDVV=h.CalcularDVV(users.ObtenerDVHs())});
            new ReferenciaIntegridadBLL().Inicializar();var errors=new ErrorSistemaBLL();errors.AsegurarErroresPlanes();errors.AsegurarErroresPlanes();
            var b=new ContratacionPlanBLL();b.AsegurarCatalogo();b.AsegurarCatalogo();var sec=new SecurityManager();
            Check(sec.VerificarIntegridad(),"Migración, catálogo y errores conservan integridad");
            errors.AsegurarErroresRegistroMascota();errors.AsegurarErroresRegistroMascota();
            Check(new ErrorSistemaDAL().ObtenerPorCodigo("ER060")!=null&&sec.VerificarIntegridad(),"ER060 registrado con DVH, DVV y referencias");
            if(args.Contains("--web")){File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"web-environment.txt"),new[]{cs.InitialCatalog,runtime});keep=true;return;}
            var bll=new RegistroMascotaBLL();
            foreach(int actor in new[]{0,2,3}){SessionAs(actor);bool denied=false;try{bll.Registrar(Datos());}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Alta exclusiva Cliente: "+actor);}
            SessionAs(5);Error(()=>bll.Registrar(Datos()),"ER011");SessionAs(1);
            Error(()=>bll.Registrar(null),"ER002");
            foreach(string campo in new[]{"Nombre","Especie","Raza"}){var v=Datos();typeof(RegistroMascotaBE).GetProperty(campo).SetValue(v,"   ");Error(()=>bll.Registrar(v),"ER002");}
            var datos=Datos();datos.FechaNacimiento=null;Error(()=>bll.Registrar(datos),"ER002");
            foreach(string campo in new[]{"Nombre","Especie","Raza"}){var v=Datos();typeof(RegistroMascotaBE).GetProperty(campo).SetValue(v,new string('x',campo=="Especie"?41:61));Error(()=>bll.Registrar(v),"ER025");}
            datos=Datos();datos.FechaNacimiento=DateTime.Today.AddDays(1);Error(()=>bll.Registrar(datos),"ER025");
            datos=Datos();datos.Nombre="Mascota\u0001";Error(()=>bll.Registrar(datos),"ER025");
            datos=Datos();datos.Nombre="Mascota🐕";Error(()=>bll.Registrar(datos),"ER025");
            int antes=Count("Mascota"),eventos=Count("Bitacora");int id=bll.Registrar(Datos());var filas=new IntegridadTablasDAL().Leer("Mascota");var mascota=filas.Rows.Cast<DataRow>().Single(f=>(int)f["idMascota"]==id);
            Check(Count("Mascota")==antes+1&&(int)mascota["idCliente"]==1&&(bool)mascota["estado"],"Alta activa vinculada al cliente de la sesion");
            Check((string)mascota["nombre"]=="Nube"&&(string)mascota["especie"]=="Perro"&&(string)mascota["raza"]=="Mestizo"&&(DateTime)mascota["fechaNacimiento"]==new DateTime(2020,1,2),"Campos normalizados sin espacios laterales y fecha correcta");
            var evento=new BitacoraDAL().ObtenerParaIntegridad().Last();Check(Count("Bitacora")==eventos+1&&evento.IdUsuario==1&&evento.Accion=="Registro de mascota"&&evento.Modulo=="Mascotas"&&evento.Criticidad=="Informativa","Bitacora de alta con usuario, modulo y criticidad");
            Check(sec.VerificarIntegridad(),"DVH, DVV y referencias validos despues del alta");
            datos=Datos();datos.Nombre=new string('n',60);datos.Especie=new string('e',40);datos.Raza=new string('r',60);datos.FechaNacimiento=DateTime.Today;Check(bll.Registrar(datos)>id,"Admite limites de longitud y fecha de nacimiento actual");
            Check(bll.Registrar(Datos())>id,"Permite mascotas diferentes con el mismo nombre");
            SessionAs(4);id=bll.Registrar(Datos());Check((int)new IntegridadTablasDAL().Leer("Mascota").Rows.Cast<DataRow>().Single(f=>(int)f["idMascota"]==id)["idCliente"]==2,"Segundo cliente registra en su propia cuenta");SessionAs(1);
            antes=Count("Mascota");eventos=Count("Bitacora");Sql("CREATE TRIGGER dbo.TestMascotaInsertFail ON dbo.Mascota AFTER INSERT AS THROW 50060,'Test insert failure',1;");Error(()=>bll.Registrar(Datos()),"ER060");Sql("DROP TRIGGER dbo.TestMascotaInsertFail");Check(Count("Mascota")==antes&&Count("Bitacora")==eventos&&sec.VerificarIntegridad(),"Falla de guardado revierte el alta completa");
            Sql("CREATE TRIGGER dbo.TestMascotaAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");Error(()=>bll.Registrar(Datos()),"ER040");Sql("DROP TRIGGER dbo.TestMascotaAuditFail");Check(Count("Mascota")==antes&&Count("Bitacora")==eventos&&sec.VerificarIntegridad(),"Falla de bitacora revierte mascota, DVH, DVV y referencias");
            antes=Count("Mascota");Parallel.For(0,2,i=>{SessionAs(1);new RegistroMascotaBLL().Registrar(Datos());});SessionAs(1);Check(Count("Mascota")==antes+2&&sec.VerificarIntegridad(),"Altas concurrentes mantienen los controles de integridad");
            var originalConnection=cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString;
            try{var missing=new SqlConnectionStringBuilder(originalConnection){InitialCatalog=cs.InitialCatalog+"_Ausente"};cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=missing.ConnectionString;cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");Error(()=>bll.Registrar(Datos()),"ER003");}finally{cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=originalConnection;cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");}
            Sql("UPDATE dbo.Cliente SET estado=0 WHERE idCliente=1");Refrescar("Cliente");Error(()=>bll.Registrar(Datos()),"ER011");Sql("UPDATE dbo.Cliente SET estado=1 WHERE idCliente=1");Refrescar("Cliente");
            Sql("UPDATE dbo.Mascota SET nombre='Alterado' WHERE idMascota=1");Error(()=>bll.Registrar(Datos()),"ER023");
            Console.WriteLine("TOTAL: "+checks+" checks passed");
        }
        finally
        {
            if(!keep)
            {HttpContext.Current=null;SqlConnection.ClearAllPools();var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};using(var cn=new SqlConnection(master.ConnectionString))using(var cmd=cn.CreateCommand()){cn.Open();cmd.CommandText="ALTER DATABASE ["+cs.InitialCatalog+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+cs.InitialCatalog+"];";cmd.ExecuteNonQuery();}}
        }
    }
}
