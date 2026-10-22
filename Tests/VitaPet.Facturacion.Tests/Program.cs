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
        new DVVDAL().Guardar(new DVVBE {NombreTabla=table,ValorDVV=new IntegridadBLL().CalcularDVV(t.Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))});
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(new ReferenciaIntegridadBLL(),null);
    }
    static DatosPagoBE Datos(){return new DatosPagoBE {MedioPago=GestionPagoBLL.Credito,Titular="Prueba Prueba",NumeroTarjeta="1111111111111111",Vencimiento="12/2030",CodigoSeguridad="111"};}
    static CotizacionPlanBE Preparar(){var b=new ContratacionPlanBLL();var c=b.ConsultarDetalle(b.ConsultarPlanes()[0].IdPlan);return b.PrepararContratacion(c.Plan.IdPlan,c.Plan.Version,c.CantidadMascotas);}
    static void Limpiar(){Sql("DELETE dbo.Factura;DELETE dbo.DetallePago;DELETE dbo.Pago;DELETE dbo.Suscripcion;");foreach(string t in new[]{"Factura","DetallePago","Pago","Suscripcion"})Refrescar(t);}
    static void Main(string[] args)
    {
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);var cs=new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if(!cs.InitialCatalog.StartsWith("VitaPet_Verificacion_Rework",StringComparison.Ordinal))throw new Exception("Only isolated databases allowed");
        cs.InitialCatalog="VitaPet_Verificacion_Rework_"+Guid.NewGuid().ToString("N");connection=cs.ConnectionString;
        var runtime=Path.Combine(Path.GetTempPath(),cs.InitialCatalog);Directory.CreateDirectory(runtime);
        cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=connection;cfg.AppSettings.Settings["RutaRecuperacion"].Value=Path.Combine(runtime,"recuperacion");cfg.AppSettings.Settings["RutaBitacoraXml"].Value=Path.Combine(runtime,"Bitacora.xml");
        cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");ConfigurationManager.RefreshSection("appSettings");bool keep=false;
        try
        {
            DatabaseMigrator.Migrate();DatabaseMigrator.Migrate();
            Sql("INSERT dbo.Usuario(correoElectronico,contrasena,nombre,rol,estado,dvh) VALUES('otro@test.local','hash','Otro','Cliente',1,'');INSERT dbo.Cliente(idUsuario,nombre,apellido,telefono,tipoCliente,estado,dvh) VALUES(1,'Juan Bautista','Villaverde','55555555','Individual',1,''),(4,'Otro','Cliente','55555555','Individual',1,'');INSERT dbo.Mascota(idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh) VALUES(1,'Luna','Perro','Mestizo','20200101',1,''),(1,'Sol','Gato','Mestizo','20210101',1,''),(1,'Inactiva','Gato','Mestizo','20210101',0,''),(2,'Ajena','Perro','Mestizo','20210101',1,'');");
            var users=new UsuarioDAL();var h=new IntegridadBLL();foreach(var u in users.ObtenerParaIntegridad())
            {
                if(u.IdUsuario==1||u.IdUsuario==4){u.Contrasena=h.CalcularHash("Prueba2026!");using(var cn=new SqlConnection(connection))using(var cmd=new SqlCommand("UPDATE dbo.Usuario SET contrasena=@p WHERE idUsuario=@id",cn)){cmd.Parameters.AddWithValue("@p",u.Contrasena);cmd.Parameters.AddWithValue("@id",u.IdUsuario);cn.Open();cmd.ExecuteNonQuery();}}
                users.ActualizarDVH(u.IdUsuario,h.CalcularDVH(u));
            }
            new DVVDAL().Guardar(new DVVBE {NombreTabla="Usuario",ValorDVV=h.CalcularDVV(users.ObtenerDVHs())});new ReferenciaIntegridadBLL().Inicializar();
            var err=new ErrorSistemaBLL();err.AsegurarErroresPlanes();err.AsegurarErroresPago();err.AsegurarErroresFacturacion();
            new ContratacionPlanBLL().AsegurarCatalogo();SessionAs(1);var b=new GestionPagoBLL();var f=new FacturaBLL();var sec=new SecurityManager();
            Check(sec.VerificarIntegridad(),"Esquema Rework 027 y controles íntegros");
            if(args.Contains("--web")){File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"web-environment.txt"),new[]{cs.InitialCatalog,runtime});keep=true;return;}
            var c=Preparar();Check(c.ImporteUSD==34.2m&&b.Consultar(c.IdSuscripcion).Importe==34.2m,"Descuento 20% coincide en contratación y pago Rework");
            Check(f.ListarPropias().Count==0,"Perfil vacío no fabrica facturas");
            foreach(int id in new[]{0,2,3}){SessionAs(id);bool denied=false;try{f.ListarPropias();}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Lista restringida a Cliente: "+id);}SessionAs(1);
            var datos=Datos();var p=b.Pagar(c.IdSuscripcion,c.ImporteUSD,datos);
            Check(p.Factura!=null&&p.Factura.Importe==34.2m&&p.Factura.Titular=="Prueba Prueba","Reutiliza simulador existente y emite factura del cobro");
            Check(datos.NumeroTarjeta==null&&datos.CodigoSeguridad==null,"Descarta datos sensibles del cobro");
            Check(Count("Pago")==1&&Count("Factura")==1&&Count("DetallePago")==1&&sec.VerificarIntegridad(),"Pago, detalle y factura protegidos y auditados");
            Check(f.ListarPropias().Single().NumeroFactura==p.Factura.Numero,"Perfil lista la factura propia");
            SessionAs(4);Check(f.ListarPropias().Count==0,"Cliente ajeno no ve la factura");Error(()=>f.Emitir(c.IdSuscripcion),"ER054");Error(()=>f.Descargar(c.IdSuscripcion),"ER081");SessionAs(1);
            b.Pagar(c.IdSuscripcion,0,null);f.Emitir(c.IdSuscripcion);Check(Count("Pago")==1&&Count("Factura")==1,"Reenvíos y reemisión no duplican cobro ni factura");
            new ContratacionPlanBLL().ConfirmarPago(c.IdSuscripcion);Check(f.ListarPropias().Single().NumeroFactura==p.Factura.Numero,"Consulta sigue disponible tras activar la suscripción");
            File.WriteAllBytes(Path.Combine(runtime,"factura-rework.pdf"),f.Descargar(c.IdSuscripcion));
            Limpiar();c=Preparar();Sql("CREATE TRIGGER dbo.TestFacturaAudit ON dbo.Bitacora AFTER INSERT AS IF EXISTS(SELECT 1 FROM inserted WHERE accion='Emisión de factura') THROW 50040,'Audit invoice failure',1;");
            p=b.Pagar(c.IdSuscripcion,c.ImporteUSD,Datos());Check(p.Factura==null&&p.CodigoErrorFactura=="ER040"&&Count("Pago")==1&&sec.VerificarIntegridad(),"Falla de factura conserva pago y verificadores");
            Check(f.ListarPropias().Single().NumeroFactura==null,"Mi perfil identifica factura pendiente");Sql("DROP TRIGGER dbo.TestFacturaAudit");f.Emitir(c.IdSuscripcion);Check(Count("Pago")==1&&Count("Factura")==1,"Reintento de facturación no vuelve a cobrar");
            Limpiar();c=Preparar();var cc=c;Parallel.For(0,2,i=>{SessionAs(1);new GestionPagoBLL().Pagar(cc.IdSuscripcion,cc.ImporteUSD,Datos());});SessionAs(1);Check(Count("Pago")==1&&Count("Factura")==1,"Concurrencia conserva un solo cobro y factura");
            Limpiar();c=Preparar();Sql("INSERT dbo.Pago(idSuscripcion,importe,medioPago,fechaPago,estado,dvh) VALUES("+c.IdSuscripcion+",38,'Tarjeta de credito','20261001','Aprobado','')");Refrescar("Pago");
            p=b.ObtenerComprobante(c.IdSuscripcion);Check(p.Factura.Importe==38m&&p.Factura.CantidadMascotas==0&&p.Factura.Titular==null,"Pago previo conserva importe histórico sin inventar titular ni descuento");
            Check(sec.VerificarIntegridad(),"Factura de pago previo mantiene integridad");
            Console.WriteLine("TOTAL: "+checks+" verificaciones Rework; PDF "+Path.Combine(runtime,"factura-rework.pdf"));
        }
        finally
        {
            if(!keep){HttpContext.Current=null;SqlConnection.ClearAllPools();var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};using(var cn=new SqlConnection(master.ConnectionString))using(var cmd=cn.CreateCommand()){cn.Open();cmd.CommandText="ALTER DATABASE ["+cs.InitialCatalog+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE ["+cs.InitialCatalog+"];";cmd.ExecuteNonQuery();}}
        }
    }
}
