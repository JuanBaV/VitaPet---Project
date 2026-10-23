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
    static DatosPagoBE Datos(){return new DatosPagoBE {MedioPago=GestionPagoBLL.Credito,Titular="Prueba Prueba",NumeroTarjeta="1111111111111111",Vencimiento="12/2030",CodigoSeguridad="111",CorreoFactura="factura@example.test"};}
    static CotizacionPlanBE Preparar(){var b=new ContratacionPlanBLL();var c=b.ConsultarDetalle(b.ConsultarPlanes()[0].IdPlan);return b.PrepararContratacion(c.Plan.IdPlan,c.Plan.Version,c.CantidadMascotas);}
    static void Limpiar(){Sql("DELETE dbo.Factura;DELETE dbo.DetallePago;DELETE dbo.Pago;DELETE dbo.Suscripcion;");foreach(string t in new[]{"Factura","DetallePago","Pago","Suscripcion"})Refrescar(t);}
    static void Main(string[] args)
    {
        if(args.Contains("--certificados")){CertificadosPagoTests.Run();return;}
        if(args.Contains("--correo")){CorreoFacturaTests.Run();return;}
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);var cs=new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if(!cs.InitialCatalog.StartsWith("VitaPet_Verificacion_Rework",StringComparison.Ordinal))throw new Exception("Only isolated databases allowed");
        cs.InitialCatalog="VitaPet_Verificacion_Rework_"+Guid.NewGuid().ToString("N");connection=cs.ConnectionString;
        var runtime=Path.Combine(Path.GetTempPath(),cs.InitialCatalog);Directory.CreateDirectory(runtime);
        cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=connection;cfg.AppSettings.Settings["RutaRecuperacion"].Value=Path.Combine(runtime,"recuperacion");cfg.AppSettings.Settings["RutaBitacoraXml"].Value=Path.Combine(runtime,"Bitacora.xml");
        cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");ConfigurationManager.RefreshSection("appSettings");bool keep=false;
        try
        {
            DatabaseMigrator.Migrate("031_AutorizacionPorPantallas.sql");DatabaseMigrator.Migrate("031_AutorizacionPorPantallas.sql");
            Sql("INSERT dbo.Usuario(correoElectronico,contrasena,nombre,rol,estado,dvh) VALUES('otro@test.local','hash','Otro','Cliente',1,'');INSERT dbo.Cliente(idUsuario,nombre,apellido,telefono,tipoCliente,estado,dvh) VALUES(1,'Juan Bautista','Villaverde','55555555','Individual',1,''),(4,'Otro','Cliente','55555555','Individual',1,'');INSERT dbo.Mascota(idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh) VALUES(1,'Luna','Perro','Mestizo','20200101',1,''),(1,'Sol','Gato','Mestizo','20210101',1,''),(1,'Inactiva','Gato','Mestizo','20210101',0,''),(2,'Ajena','Perro','Mestizo','20210101',1,'');");
            var users=new UsuarioDAL();var h=new IntegridadBLL();foreach(var u in users.ObtenerParaIntegridad())
            {
                if(u.IdUsuario==1||u.IdUsuario==4||args.Contains("--web")){u.Contrasena=h.CalcularHash("Prueba2026!");using(var cn=new SqlConnection(connection))using(var cmd=new SqlCommand("UPDATE dbo.Usuario SET contrasena=@p WHERE idUsuario=@id",cn)){cmd.Parameters.AddWithValue("@p",u.Contrasena);cmd.Parameters.AddWithValue("@id",u.IdUsuario);cn.Open();cmd.ExecuteNonQuery();}}
                users.ActualizarDVH(u.IdUsuario,h.CalcularDVH(u));
            }
            new DVVDAL().Guardar(new DVVBE {NombreTabla="Usuario",ValorDVV=h.CalcularDVV(users.ObtenerDVHs())});new MigracionRolesBLL().Ejecutar();new PerfilBLL().AsegurarIniciales();
            var err=new ErrorSistemaBLL();err.AsegurarErroresPlanes();err.AsegurarErroresPago();err.AsegurarErroresFacturacion();
            new ContratacionPlanBLL().AsegurarCatalogo();SessionAs(1);var b=new GestionPagoBLL();var f=new FacturaBLL();var sec=new SecurityManager();
            Check(sec.VerificarIntegridad(),"Esquema Rework 029 y controles íntegros");
            string checksumCorreo;
            using(var cn=new SqlConnection(connection))using(var cmd=new SqlCommand("SELECT checksum FROM dbo.__SchemaMigrations WHERE migrationId='029_CorreoFactura.sql'",cn))
            {cn.Open();checksumCorreo=(string)cmd.ExecuteScalar();}
            Sql("DELETE dbo.__SchemaMigrations WHERE migrationId='029_CorreoFactura.sql'");
            Check(!sec.VerificarIntegridad()&&sec.DiagnosticarIntegridad(true).EsValida,"Solo la validación previa a migrar admite scripts pendientes, conservando la validación de datos");
            Sql("INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES('029_CorreoFactura.sql',REPLICATE('0',64))");
            Check(!sec.DiagnosticarIntegridad(true).EsValida,"La validación previa a migrar sigue rechazando un checksum aplicado alterado");
            Sql("UPDATE dbo.__SchemaMigrations SET checksum='"+checksumCorreo+"' WHERE migrationId='029_CorreoFactura.sql'");
            Check(sec.VerificarIntegridad(),"Historial de migraciones restituido sin modificar datos ni verificadores");
            if(args.Contains("--cifrado")){CifradoTests.Run(runtime);return;}
            if(args.Contains("--xml"))
            {
                Sql("INSERT dbo.Reintegro(idCliente,idMascota,fechaSolicitud,importe,estado,detalle,dvh) VALUES(1,1,GETDATE(),1234.56,'Pendiente','Revisión & análisis <Luna>',''),(1,2,GETDATE(),22.50,'Aprobado',NULL,''),(2,4,GETDATE(),999,'Pendiente','Ajeno reservado','');INSERT dbo.Turno(idMascota,fecha,horario,prestacion,zona,estado,dvh) VALUES(1,DATEADD(day,2,CONVERT(date,GETDATE())),'10:00','Control & revisión <Luna>','CABA','Confirmado',''),(1,DATEADD(day,3,CONVERT(date,GETDATE())),'11:30','Vacunación','Norte','Confirmado',''),(4,DATEADD(day,2,CONVERT(date,GETDATE())),'12:00','Ajeno reservado','Sur','Confirmado','');");
                Refrescar("Reintegro");Refrescar("Turno");
                Sql("UPDATE dbo.Cobertura SET detalle='Cobertura & atención <preventiva>' WHERE idCobertura=(SELECT MIN(idCobertura) FROM dbo.Cobertura)");Refrescar("Cobertura");
                Check(sec.VerificarIntegridad(),"Datos sintéticos de reintegros y turnos protegidos para las salidas XML");
                if(!args.Contains("--web"))
                {
                    var cotizacion=Preparar();b.Pagar(cotizacion.IdSuscripcion,cotizacion.ImporteUSD,Datos());
                    new ContratacionPlanBLL().ConfirmarPago(cotizacion.IdSuscripcion);
                    XmlTests.Run();return;
                }
            }
            if(args.Contains("--web"))
            {
                if(args.Contains("--clinica"))
                {
                    Sql("INSERT dbo.HistorialClinico(idMascota,fechaRegistro,diagnostico,tratamiento,observaciones,dvh) VALUES(1,GETDATE(),'Control SOAP Luna','Seguimiento de prueba','Historia propia',''),(4,GETDATE(),'Historia reservada Ajena','Otro tratamiento','No divulgar','');INSERT dbo.DatosClinicos(idMascota,peso,temperatura,frecuenciaCardiaca,observaciones,dvh) VALUES(1,12.50,38.20,80,'Dato SOAP Luna',''),(4,7,38,90,'Dato reservado Ajena','');");
                    Refrescar("HistorialClinico");Refrescar("DatosClinicos");
                    Check(sec.VerificarIntegridad(),"Datos clínicos sintéticos protegidos para probar los webservices");
                }
                File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"web-environment.txt"),new[]{cs.InitialCatalog,runtime});keep=true;return;
            }
            if(args.Contains("--perfiles")){PerfilesTests.Run();return;}
            if(args.Contains("--contrasenas")){ContrasenasTests.Run();return;}
            if(args.Contains("--accesos")){AccesosTests.Run();return;}
            if(args.Contains("--idioma")){IdiomaTests.Run();return;}
            if(args.Contains("--patrones")){PatronesTests.Run();return;}
            var c=Preparar();Check(c.ImporteUSD==34.2m&&b.Consultar(c.IdSuscripcion).Importe==34.2m,"Descuento 20% coincide en contratación y pago Rework");
            Check(f.ListarPropias().Count==0,"Perfil vacío no fabrica facturas");
            foreach(int id in new[]{0,2,3}){SessionAs(id);bool denied=false;try{f.ListarPropias();}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Lista restringida a Cliente: "+id);}SessionAs(1);
            var sinCorreo=Datos();sinCorreo.CorreoFactura="";Error(()=>b.Pagar(c.IdSuscripcion,c.ImporteUSD,sinCorreo),"ER002");
            foreach(string correo in new[]{"sin-arroba","a@@ejemplo.com","a@ejemplo","a@ejemplo.com\r\nBcc: otro@ejemplo.com",new string('a',95)+"@ejemplo.com"})
            {var invalido=Datos();invalido.CorreoFactura=correo;Error(()=>b.Pagar(c.IdSuscripcion,c.ImporteUSD,invalido),"ER025");}
            Check(Count("Pago")==0&&Count("Factura")==0,"Correo vacío o inválido no registra cobros ni facturas");
            var datos=Datos();var p=b.Pagar(c.IdSuscripcion,c.ImporteUSD,datos);
            Check(p.Factura!=null&&p.Factura.Importe==34.2m&&p.Factura.Titular=="Prueba Prueba","Reutiliza simulador existente y emite factura del cobro");
            Check(p.Factura.CorreoFactura=="factura@example.test"&&p.Factura.Correo!=p.Factura.CorreoFactura,"Conserva el correo elegido en el pago, separado del correo del cliente");
            var reconsulta=b.ObtenerComprobante(c.IdSuscripcion);
            Check(reconsulta.Factura.CorreoFactura=="factura@example.test","Reconsulta conserva el destinatario elegido para los reintentos");
            Sql("UPDATE dbo.DetallePago SET correoFactura='alterado@example.test'");
            Check(!sec.VerificarIntegridad(),"El correo elegido está protegido por los verificadores de integridad");
            Sql("UPDATE dbo.DetallePago SET correoFactura='factura@example.test'");
            Check(sec.VerificarIntegridad(),"Recupera integridad al restituir el correo original sin recalcular dígitos");
            Check(datos.NumeroTarjeta==null&&datos.CodigoSeguridad==null,"Descarta datos sensibles del cobro");
            Check(Count("Pago")==1&&Count("Factura")==1&&Count("DetallePago")==1&&sec.VerificarIntegridad(),"Pago, detalle y factura protegidos y auditados");
            Check(b.ObtenerCertificado(c.IdSuscripcion).Certificado.Length>0,"Cliente obtiene certificado del pago aprobado");
            Check(f.ListarPropias().Single().NumeroFactura==p.Factura.Numero,"Perfil lista la factura propia");
            SessionAs(4);Check(f.ListarPropias().Count==0,"Cliente ajeno no ve la factura");Error(()=>f.Emitir(c.IdSuscripcion),"ER054");Error(()=>f.Descargar(c.IdSuscripcion),"ER081");Error(()=>b.ObtenerCertificado(c.IdSuscripcion),"ER054");SessionAs(1);
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
            Limpiar();c=Preparar();var debito=Datos();debito.MedioPago=GestionPagoBLL.Debito;
            p=b.Pagar(c.IdSuscripcion,c.ImporteUSD,debito);
            Check(p.MedioPago==GestionPagoBLL.Debito&&p.Importe==34.2m&&p.Factura.MedioPago==GestionPagoBLL.Debito,"Débito conserva el medio y los cálculos en pago y factura");
            Check(p.Factura.CorreoFactura=="factura@example.test"&&sec.VerificarIntegridad(),"Débito conserva destinatario, verificadores y referencias");
            b.Pagar(c.IdSuscripcion,c.ImporteUSD,null);
            Check(Count("Pago")==1&&Count("Factura")==1,"Repetir un pago de débito no duplica cobros ni facturas");
            File.WriteAllBytes(Path.Combine(runtime,"factura-debito.pdf"),f.Descargar(c.IdSuscripcion));
            new ContratacionPlanBLL().ConfirmarPago(c.IdSuscripcion);
            Check(sec.VerificarIntegridad(),"Débito permite completar la contratación sin perder integridad");
            Console.WriteLine("TOTAL: "+checks+" verificaciones Rework; PDF "+Path.Combine(runtime,"factura-rework.pdf"));
        }
        finally
        {
            if(!keep){HttpContext.Current=null;SqlConnection.ClearAllPools();var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};using(var cn=new SqlConnection(master.ConnectionString))using(var cmd=cn.CreateCommand()){cn.Open();cmd.CommandText="ALTER DATABASE ["+cs.InitialCatalog+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE ["+cs.InitialCatalog+"];";cmd.ExecuteNonQuery();}}
        }
    }
}
