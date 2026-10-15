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
    static void Error(Action action,string code){try{action();throw new Exception("Expected "+code);}catch(GestionPagoException e){Check(e.Codigo==code,"Error "+code);}}
    static void Refrescar(string table)
    {
        var d=new IntegridadTablasDAL();var t=d.Leer(table);var h=new IntegridadTablasBLL();
        foreach(DataRow r in t.Rows){string v=h.CalcularDVH(r);d.GuardarDvh(table,r[IntegridadTablasDAL.Claves[table]],v);r["dvh"]=v;}
        new DVVDAL().Guardar(new DVVBE{NombreTabla=table,ValorDVV=new IntegridadBLL().CalcularDVV(t.Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))});
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(new ReferenciaIntegridadBLL(),null);
    }
    static DatosPagoBE Datos(string numero="1111111111111111") {return new DatosPagoBE{MedioPago=GestionPagoBLL.Credito,Titular="Prueba Prueba",NumeroTarjeta=numero,Vencimiento="12/"+DateTime.Today.AddYears(2).Year,CodigoSeguridad="111"};}
    static int Pendiente(){var c=new ContratacionPlanBLL().ConsultarDetalle(1);return new ContratacionPlanBLL().PrepararContratacion(1,c.Plan.Version,c.CantidadMascotas).IdSuscripcion;}
    static void Reset(){Sql("DELETE dbo.Pago; DELETE dbo.Suscripcion;");Refrescar("Pago");Refrescar("Suscripcion");}
    static object Scalar(string query){using(var c=new SqlConnection(connection))using(var cmd=new SqlCommand(query,c)){c.Open();return cmd.ExecuteScalar();}}
    static void Main(string[] args)
    {
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);var cs=new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if(!cs.InitialCatalog.StartsWith("VitaPet_Verificacion_Pago",StringComparison.Ordinal))throw new Exception("Only isolated databases allowed");
        cs.InitialCatalog="VitaPet_Verificacion_Pago_"+Guid.NewGuid().ToString("N");connection=cs.ConnectionString;
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
            errors.AsegurarErroresPago();errors.AsegurarErroresPago();
            Check(new ErrorSistemaDAL().ObtenerPorCodigo("ER054")!=null&&new ErrorSistemaDAL().ObtenerPorCodigo("ER057")!=null,"Errores nuevos registrados con integridad");
            if(args.Contains("--web")){File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"web-environment.txt"),new[]{cs.InitialCatalog,runtime});keep=true;return;}
            var pago=new GestionPagoBLL();SessionAs(1);int id=Pendiente();
            foreach(int actor in new[]{0,2,3}){SessionAs(actor);bool denied=false;try{pago.Consultar(id);}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Pago exclusivo Cliente: "+actor);}
            SessionAs(4);Error(()=>pago.Consultar(id),"ER054");Error(()=>pago.Pagar(id,38m,Datos()),"ER054");Error(()=>pago.ObtenerComprobante(id),"ER054");SessionAs(1);
            Check(pago.Consultar(id).Importe==38m,"Importe por dos mascotas propias activas");
            Error(()=>pago.Consultar(999999),"ER054");
            Error(()=>pago.Pagar(id,38m,new DatosPagoBE()),"ER002");
            var bad=Datos();bad.Vencimiento="01/2020";Error(()=>pago.Pagar(id,38m,bad),"ER025");Check(bad.NumeroTarjeta==null&&bad.CodigoSeguridad==null,"Datos transitorios descartados aun al fallar");
            bad=Datos();bad.CodigoSeguridad="1x1";Error(()=>pago.Pagar(id,38m,bad),"ER025");
            bad=Datos();bad.MedioPago="Transferencia";Error(()=>pago.Pagar(id,38m,bad),"ER025");
            bad=Datos();bad.NumeroTarjeta="1234567890123456";Error(()=>pago.Pagar(id,38m,bad),"ER025");
            Error(()=>pago.Pagar(id,1m,Datos()),"ER025");
            Error(()=>pago.Pagar(id,38m,Datos("4000000000000002")),"ER006");
            Error(()=>pago.Pagar(id,38m,Datos("5555555555554444")),"ER055");
            Check(Count("Pago")==0&&(string)Scalar("SELECT estado FROM dbo.Suscripcion WHERE idSuscripcion="+id)=="Pendiente","Rechazos y fallas no crean pagos ni activan la cuenta");
            Sql("CREATE TRIGGER dbo.TestPagoFail ON dbo.Pago AFTER INSERT AS THROW 50057,'Test payment failure',1;");
            Error(()=>pago.Pagar(id,38m,Datos()),"ER057");Sql("DROP TRIGGER dbo.TestPagoFail");
            Check(Count("Pago")==0&&sec.VerificarIntegridad(),"Falla al guardar revierte Pago y controles");
            Sql("CREATE TRIGGER dbo.TestAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");
            Error(()=>pago.Pagar(id,38m,Datos()),"ER040");Sql("DROP TRIGGER dbo.TestAuditFail");
            Check(Count("Pago")==0&&sec.VerificarIntegridad(),"Falla de auditoria revierte pago, DVH, DVV y referencias");
            var data=Datos();var result=pago.Pagar(id,38m,data);
            Check(result.IdPago>0&&result.Importe==38m&&result.Estado=="Aprobado"&&result.FechaPago==DateTime.Today,"Pago aprobado con importe, estado y fecha");
            Check(data.NumeroTarjeta==null&&data.Titular==null&&data.CodigoSeguridad==null,"No conserva tarjeta ni titular luego de procesar");
            Check((string)Scalar("SELECT estado FROM dbo.Suscripcion WHERE idSuscripcion="+id)=="Pendiente","El pago deja la activacion a CU-001-002");
            Check(sec.VerificarIntegridad(),"Pago, bitacora y verificadores integros");
            Check(new BitacoraDAL().ObtenerParaIntegridad().Last().Accion=="Gestión de pago","Auditoria del pago sin datos de tarjeta");
            Check(pago.ObtenerComprobante(id).IdPago==result.IdPago,"Comprobante propio disponible para recargar");
            Error(()=>pago.Pagar(id,38m,Datos()),"ER054");Check(Count("Pago")==1,"Reenvio no duplica el pago");
            SessionAs(4);Error(()=>pago.ObtenerComprobante(id),"ER054");SessionAs(1);
            Sql("CREATE TRIGGER dbo.TestActivationAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test activation audit failure',1;");
            bool activationFailed=false;try{b.ConfirmarPago(id);}catch(ContratacionPlanException ex){activationFailed=ex.Codigo=="ER040";}
            Sql("DROP TRIGGER dbo.TestActivationAuditFail");Check(activationFailed&&(string)Scalar("SELECT estado FROM dbo.Suscripcion WHERE idSuscripcion="+id)=="Pendiente"&&Count("Pago")==1&&sec.VerificarIntegridad(),"Falla de activacion conserva pago aprobado y permite continuar sin cobrar otra vez");
            b.ConfirmarPago(id);int events=Count("Bitacora");b.ConfirmarPago(id);
            Check((string)Scalar("SELECT estado FROM dbo.Suscripcion WHERE idSuscripcion="+id)=="Activa"&&Count("Bitacora")==events,"Continuar activa una sola vez con auditoria idempotente");
            Check(sec.VerificarIntegridad(),"Activacion conserva DVH/DVV y referencias");
            SessionAs(4);bool foreign=false;try{b.ConfirmarPago(id);}catch(ContratacionPlanException){foreign=true;}Check(foreign,"Otro cliente no puede activar la suscripcion");SessionAs(1);
            Reset();id=Pendiente();int approved=0,rejected=0;
            Parallel.For(0,2,i=>{SessionAs(1);try{new GestionPagoBLL().Pagar(id,38m,Datos());System.Threading.Interlocked.Increment(ref approved);}catch(GestionPagoException ex){if(ex.Codigo!="ER054")throw;System.Threading.Interlocked.Increment(ref rejected);}});SessionAs(1);
            Check(approved==1&&rejected==1&&Count("Pago")==1,"Dos solicitudes concurrentes generan un solo pago aprobado");
            Reset();id=Pendiente();Sql("UPDATE dbo.Mascota SET estado=0 WHERE nombre='Sol'");Refrescar("Mascota");
            Error(()=>pago.Pagar(id,38m,Datos()),"ER025");Check(Count("Pago")==0,"No cobra un importe distinto del confirmado");
            Sql("UPDATE dbo.Mascota SET estado=0 WHERE idCliente=1");Refrescar("Mascota");Error(()=>pago.Consultar(id),"ER052");
            Sql("UPDATE dbo.Mascota SET estado=1 WHERE nombre='Luna'; UPDATE dbo.[Plan] SET estado=0 WHERE idPlan=1");Refrescar("Mascota");Refrescar("Plan");Error(()=>pago.Consultar(id),"ER050");
            Sql("UPDATE dbo.[Plan] SET estado=1,precio=99999999.99 WHERE idPlan=1; UPDATE dbo.Mascota SET estado=1 WHERE nombre='Sol'");Refrescar("Plan");Refrescar("Mascota");Error(()=>pago.Consultar(id),"ER025");
            Sql("UPDATE dbo.Mascota SET nombre='Alterada' WHERE nombre='Luna'");Error(()=>pago.Consultar(id),"ER023");
            Console.WriteLine("TOTAL: "+checks+" checks passed");
        }
        finally
        {
            if(!keep)
            {HttpContext.Current=null;SqlConnection.ClearAllPools();var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};using(var cn=new SqlConnection(master.ConnectionString))using(var cmd=cn.CreateCommand()){cn.Open();cmd.CommandText="ALTER DATABASE ["+cs.InitialCatalog+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+cs.InitialCatalog+"];";cmd.ExecuteNonQuery();}}
        }
    }
}
