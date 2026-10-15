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
    static void Error(Action action,string code){try{action();throw new Exception("Expected "+code);}catch(ContratacionPlanException e){Check(e.Codigo==code,"Error "+code);}}
    static void Refrescar(string table)
    {
        var d=new IntegridadTablasDAL();var t=d.Leer(table);var h=new IntegridadTablasBLL();
        foreach(DataRow r in t.Rows){string v=h.CalcularDVH(r);d.GuardarDvh(table,r[IntegridadTablasDAL.Claves[table]],v);r["dvh"]=v;}
        new DVVDAL().Guardar(new DVVBE{NombreTabla=table,ValorDVV=new IntegridadBLL().CalcularDVV(t.Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))});
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(new ReferenciaIntegridadBLL(),null);
    }
    static void Main(string[] args)
    {
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);var cs=new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if(!cs.InitialCatalog.StartsWith("VitaPet_Verificacion_Planes",StringComparison.Ordinal))throw new Exception("Only isolated databases allowed");
        cs.InitialCatalog="VitaPet_Verificacion_Planes_"+Guid.NewGuid().ToString("N");connection=cs.ConnectionString;
        var runtime=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"runtime",cs.InitialCatalog);Directory.CreateDirectory(runtime);
        cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=connection;cfg.AppSettings.Settings["RutaRecuperacion"].Value=Path.Combine(runtime,"recuperacion");cfg.AppSettings.Settings["RutaBitacoraXml"].Value=Path.Combine(runtime,"Bitacora.xml");cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");ConfigurationManager.RefreshSection("appSettings");
        bool keep=false;
        try
        {
            DatabaseMigrator.Migrate();DatabaseMigrator.Migrate();
            Sql("INSERT dbo.Usuario(correoElectronico,contrasena,nombre,rol,estado,dvh) VALUES('otro@test.local','hash','Otro','Cliente',1,''); INSERT dbo.Cliente(idUsuario,nombre,apellido,telefono,tipoCliente,estado,dvh) VALUES(1,'Cliente','Prueba','55555555','Individual',1,''),(4,'Otro','Cliente','55555555','Individual',1,''); INSERT dbo.Mascota(idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh) VALUES(1,'Luna','Perro','Mestizo','20200101',1,''),(1,'Sol','Gato','Mestizo','20210101',1,''),(1,'Inactiva','Gato','Mestizo','20210101',0,''),(2,'Ajena','Perro','Mestizo','20210101',1,'');");
            var users=new UsuarioDAL();var h=new IntegridadBLL();
            foreach(var u in users.ObtenerParaIntegridad())
            {
                if(u.IdUsuario==1)
                {
                    u.Contrasena=h.CalcularHash("Prueba2026!");
                    using(var c=new SqlConnection(connection))using(var cmd=new SqlCommand("UPDATE dbo.Usuario SET contrasena=@p WHERE idUsuario=1",c)){cmd.Parameters.AddWithValue("@p",u.Contrasena);c.Open();cmd.ExecuteNonQuery();}
                }
                users.ActualizarDVH(u.IdUsuario,h.CalcularDVH(u));
            }
            new DVVDAL().Guardar(new DVVBE{NombreTabla="Usuario",ValorDVV=h.CalcularDVV(users.ObtenerDVHs())});
            new ReferenciaIntegridadBLL().Inicializar();var errors=new ErrorSistemaBLL();errors.AsegurarErroresPlanes();errors.AsegurarErroresPlanes();
            var b=new ContratacionPlanBLL();b.AsegurarCatalogo();b.AsegurarCatalogo();var sec=new SecurityManager();
            Check(sec.VerificarIntegridad(),"Migración, catálogo y errores conservan integridad");
            if(args.Contains("--web")){File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"web-environment.txt"),new[]{cs.InitialCatalog,runtime});keep=true;return;}
            foreach(int id in new[]{0,2,3})
            {SessionAs(id);bool denied=false;try{b.ConsultarPlanes();}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Consulta reservada a Cliente: "+id);}
            SessionAs(1);var planes=b.ConsultarPlanes();
            Check(planes.Count==3&&planes.Select(p=>p.Precio).SequenceEqual(new[]{19m,28m,39m}),"Planes contratables y precios exactos");
            int corp=(int)new IntegridadTablasDAL().Leer("Plan").Rows.Cast<DataRow>().Single(r=>(string)r["nombre"]=="Plan Corporativo")["idPlan"];
            Error(()=>b.ConsultarDetalle(corp),"ER050");Error(()=>b.PrepararContratacion(corp,"",2),"ER050");
            var coti=b.ConsultarDetalle(planes[0].IdPlan);
            Check(coti.CantidadMascotas==2&&coti.ImporteUSD==38m,"Total por mascotas propias activas; omite ajenas e inactivas");
            Check(b.ConsultarCobertura(planes[0].IdPlan).Count==1,"Cobertura disponible para el servicio");
            var other=b.ConsultarDetalle(planes[1].IdPlan);Check(other.ImporteUSD==56m,"Preventivo: 28 por dos mascotas");
            Check(b.ConsultarDetalle(planes[2].IdPlan).ImporteUSD==78m,"Integral: 39 por dos mascotas");
            int audit=Count("Bitacora");var pending=b.PrepararContratacion(coti.Plan.IdPlan,coti.Plan.Version,coti.CantidadMascotas);
            Check(pending.IdSuscripcion>0&&Count("Suscripcion")==1&&Count("Pago")==0,"Prepara pendiente sin cobrar ni crear Pago");
            var row=new IntegridadTablasDAL().Leer("Suscripcion").Rows[0];Check((string)row["estado"]=="Pendiente","No activa la suscripción");
            var eventLog=new BitacoraDAL().ObtenerParaIntegridad().Last();Check(eventLog.Accion=="Contratación de plan pendiente"&&eventLog.IdUsuario==1,"Preparación auditada");
            Check(sec.VerificarIntegridad(),"DVH, DVV y referencias de la preparación");
            Check(b.PrepararContratacion(coti.Plan.IdPlan,coti.Plan.Version,2).IdSuscripcion==pending.IdSuscripcion&&Count("Bitacora")==audit+1,"Reintento reutiliza pendiente y no duplica bitácora");
            Error(()=>b.PrepararContratacion(other.Plan.IdPlan,other.Plan.Version,2),"ER005");
            SessionAs(4);Error(()=>b.ObtenerPendiente(pending.IdSuscripcion),"ER011");SessionAs(1);
            Sql("UPDATE dbo.Mascota SET estado=0 WHERE nombre='Sol'");Refrescar("Mascota");Error(()=>b.PrepararContratacion(coti.Plan.IdPlan,coti.Plan.Version,2),"ER053");
            Sql("UPDATE dbo.[Plan] SET precio=20 WHERE idPlan="+coti.Plan.IdPlan);Refrescar("Plan");Error(()=>b.PrepararContratacion(coti.Plan.IdPlan,coti.Plan.Version,1),"ER053");
            Sql("UPDATE dbo.Mascota SET estado=0 WHERE idCliente=1");Refrescar("Mascota");var q=b.ConsultarDetalle(coti.Plan.IdPlan);Error(()=>b.PrepararContratacion(q.Plan.IdPlan,q.Plan.Version,0),"ER052");
            Sql("UPDATE dbo.Mascota SET estado=1 WHERE nombre='Luna'; DELETE dbo.Suscripcion;");Refrescar("Mascota");Refrescar("Suscripcion");
            q=b.ConsultarDetalle(coti.Plan.IdPlan);Sql("CREATE TRIGGER dbo.TestPlanAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");Error(()=>b.PrepararContratacion(q.Plan.IdPlan,q.Plan.Version,1),"ER040");Sql("DROP TRIGGER dbo.TestPlanAuditFail");Check(Count("Suscripcion")==0&&sec.VerificarIntegridad(),"Falla de bitácora revierte la suscripción y controles");
            var ids=new int[2];Parallel.For(0,2,i=>{SessionAs(1);ids[i]=new ContratacionPlanBLL().PrepararContratacion(q.Plan.IdPlan,q.Plan.Version,1).IdSuscripcion;});
            Check(ids[0]==ids[1]&&Count("Suscripcion")==1,"Solicitudes concurrentes generan un solo pendiente");SessionAs(1);
            Sql("UPDATE dbo.Suscripcion SET estado='Activa'");Refrescar("Suscripcion");Error(()=>b.PrepararContratacion(q.Plan.IdPlan,q.Plan.Version,1),"ER005");
            Sql("UPDATE dbo.[Plan] SET estado=0 WHERE idPlan="+q.Plan.IdPlan);Refrescar("Plan");Error(()=>b.ConsultarDetalle(q.Plan.IdPlan),"ER050");
            Sql("UPDATE dbo.[Plan] SET estado=0");Refrescar("Plan");Error(()=>b.ConsultarPlanes(),"ER051");
            Sql("UPDATE dbo.Mascota SET nombre='Alterada' WHERE nombre='Luna'");Error(()=>b.ConsultarPlanes(),"ER023");
            Console.WriteLine("TOTAL: "+checks+" checks passed");
        }
        finally
        {
            if(!keep)
            {HttpContext.Current=null;SqlConnection.ClearAllPools();var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};using(var cn=new SqlConnection(master.ConnectionString))using(var cmd=cn.CreateCommand()){cn.Open();cmd.CommandText="ALTER DATABASE ["+cs.InitialCatalog+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+cs.InitialCatalog+"];";cmd.ExecuteNonQuery();}}
        }
    }
}
