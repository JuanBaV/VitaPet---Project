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
    static void Error(Action action,string code){try{action();throw new Exception("Expected "+code);}catch(ConsultaCoberturaException e){Check(e.Codigo==code,"Error "+code);}}
    static void Refrescar(string table)
    {
        var d=new IntegridadTablasDAL();var t=d.Leer(table);var h=new IntegridadTablasBLL();
        foreach(DataRow r in t.Rows){string v=h.CalcularDVH(r);d.GuardarDvh(table,r[IntegridadTablasDAL.Claves[table]],v);r["dvh"]=v;}
        new DVVDAL().Guardar(new DVVBE{NombreTabla=table,ValorDVV=new IntegridadBLL().CalcularDVV(t.Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))});
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(new ReferenciaIntegridadBLL(),null);
    }
    static string Dominio(){return string.Join("|",new[]{"Cliente","Suscripcion","Plan","Cobertura"}.Select(t=>string.Join(",",new IntegridadTablasDAL().Leer(t).Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))));}
    static void SembrarCoberturas()
    {
        Sql("INSERT dbo.Suscripcion(idCliente,idPlan,fechaAlta,fechaVencimiento,estado,dvh) VALUES(1,1,CONVERT(DATE,GETDATE()),DATEADD(MONTH,1,CONVERT(DATE,GETDATE())),'Activa',''),(2,2,CONVERT(DATE,GETDATE()),DATEADD(MONTH,1,CONVERT(DATE,GETDATE())),'Activa',''); INSERT dbo.Cobertura(idPlan,prestacion,detalle,estado,dvh) VALUES(1,'Consulta veterinaria','Consulta clínica general.',1,''),(1,'Prestación inactiva','No debe mostrarse.',0,''),(4,'Convenio de empresa','Prestación corporativa.',1,'');");
        Refrescar("Suscripcion");Refrescar("Cobertura");
    }
    static void Main(string[] args)
    {
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);var cs=new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if(!cs.InitialCatalog.StartsWith("VitaPet_Verificacion_Cobertura",StringComparison.Ordinal))throw new Exception("Only isolated databases allowed");
        cs.InitialCatalog="VitaPet_Verificacion_Cobertura_"+Guid.NewGuid().ToString("N");connection=cs.ConnectionString;
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
            errors.AsegurarErroresCobertura();errors.AsegurarErroresCobertura();
            Check(new ErrorSistemaDAL().ObtenerPorCodigo("ER058")!=null&&sec.VerificarIntegridad(),"ER058 registrado con DVH, DVV y referencias");
            if(args.Contains("--web")){SembrarCoberturas();File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"web-environment.txt"),new[]{cs.InitialCatalog,runtime});keep=true;return;}
            var consulta=new ConsultaCoberturaBLL();
            foreach(int actor in new[]{0,2,3}){SessionAs(actor);bool denied=false;try{consulta.Consultar();}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Consulta exclusiva Cliente: "+actor);}
            SessionAs(5);Error(()=>consulta.Consultar(),"ER011");SessionAs(1);int events=Count("Bitacora");
            Error(()=>consulta.Consultar(),"ER008");Check(Count("Bitacora")==events,"Sin suscripcion no registra una consulta exitosa");
            SembrarCoberturas();string dominio=Dominio();var detalle=consulta.Consultar();
            Check(detalle.IdPlan==1&&detalle.NombrePlan=="Plan Esencial"&&detalle.EstadoSuscripcion=="Activa","Resuelve el plan vigente de la cuenta autenticada");
            Check(detalle.FechaAlta.Date==DateTime.Today&&detalle.FechaVencimiento.Date==DateTime.Today.AddMonths(1),"Fechas de vigencia y limites inclusivos");
            Check(detalle.Coberturas.Count==2&&detalle.Coberturas.Any(x=>x.Prestacion=="Consulta veterinaria")&&!detalle.Coberturas.Any(x=>x.Prestacion=="Prestación inactiva"),"Solo prestaciones activas con sus detalles");
            Check(Dominio()==dominio&&Count("Bitacora")==events+1,"Consulta de solo lectura modifica unicamente auditoria y controles");
            var audit=new BitacoraDAL().ObtenerParaIntegridad().Last();Check(audit.Accion=="Consulta de cobertura"&&audit.IdUsuario==1&&audit.Modulo=="Planes","Evento de consulta asociado al usuario");
            Check(sec.VerificarIntegridad(),"DVH, DVV y referencias de auditoria validos");
            SessionAs(4);var otra=consulta.Consultar();Check(otra.IdPlan==2&&otra.Coberturas.Count==1,"Otro cliente obtiene exclusivamente su propio plan");SessionAs(1);
            using(var tx=new OperacionIntegridadDAL()){
                bool denied=false;try{new ConsultaCoberturaDAL().ObtenerPrestaciones(1,otra.IdSuscripcion);}catch(SqlException ex){denied=ex.Number==50008;}
                Check(denied,"SP impide consultar una suscripcion ajena aunque se manipule su id");
            }
            Sql("UPDATE dbo.Suscripcion SET estado='Pendiente' WHERE idCliente=1");Refrescar("Suscripcion");Error(()=>consulta.Consultar(),"ER008");
            Sql("UPDATE dbo.Suscripcion SET estado='Activa',fechaAlta=DATEADD(MONTH,-2,GETDATE()),fechaVencimiento=DATEADD(DAY,-1,GETDATE()) WHERE idCliente=1");Refrescar("Suscripcion");Error(()=>consulta.Consultar(),"ER008");
            Sql("UPDATE dbo.Suscripcion SET fechaAlta=DATEADD(DAY,1,GETDATE()),fechaVencimiento=DATEADD(MONTH,1,GETDATE()) WHERE idCliente=1");Refrescar("Suscripcion");Error(()=>consulta.Consultar(),"ER008");
            Sql("UPDATE dbo.Suscripcion SET fechaAlta=CONVERT(DATE,GETDATE()),fechaVencimiento=CONVERT(DATE,GETDATE()) WHERE idCliente=1");Refrescar("Suscripcion");Check(consulta.Consultar().IdPlan==1,"La cobertura sigue vigente durante el dia de vencimiento");
            Sql("UPDATE dbo.Cobertura SET estado=0 WHERE idPlan=1");Refrescar("Cobertura");Error(()=>consulta.Consultar(),"ER008");
            Sql("UPDATE dbo.Cobertura SET estado=1 WHERE idPlan=1 AND prestacion<>'Prestación inactiva'; UPDATE dbo.[Plan] SET estado=0 WHERE idPlan=1");Refrescar("Cobertura");Refrescar("Plan");
            Check(consulta.Consultar().IdPlan==1,"Inhabilitar nuevas contrataciones no elimina cobertura de un contrato vigente");
            Sql("UPDATE dbo.Suscripcion SET idPlan=4 WHERE idCliente=1");Refrescar("Suscripcion");
            Check(consulta.Consultar().NombrePlan=="Plan Corporativo","Puede consultar su plan corporativo vigente sin contratarlo de forma particular");
            Sql("UPDATE dbo.Suscripcion SET idPlan=1 WHERE idCliente=1");Refrescar("Suscripcion");
            events=Count("Bitacora");Sql("CREATE TRIGGER dbo.TestCoberturaAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50033,'Test audit failure',1;");
            Error(()=>consulta.Consultar(),"ER033");Sql("DROP TRIGGER dbo.TestCoberturaAuditFail");
            Check(Count("Bitacora")==events&&sec.VerificarIntegridad(),"Falla de bitacora revierte la transaccion y preserva integridad");
            Sql("CREATE OR ALTER PROCEDURE dbo.sp_Cobertura_ConsultarCuenta @idUsuario INT,@idSuscripcion INT AS THROW 50058,'Test service failure',1;");
            Error(()=>consulta.Consultar(),"ER058");
            string migration;using(var reader=new StreamReader(typeof(DatabaseMigrator).Assembly.GetManifestResourceStream("VitaPet.DAL.Migrations.018_ConsultaCobertura.sql"))){migration=reader.ReadToEnd();}
            Sql(migration.Substring(migration.IndexOf("CREATE OR ALTER PROCEDURE dbo.sp_Cobertura_ConsultarCuenta",StringComparison.Ordinal)));
            Check(sec.VerificarIntegridad(),"Falla de servicio no modifica las tablas funcionales");
            events=Count("Bitacora");Parallel.For(0,2,i=>{SessionAs(1);new ConsultaCoberturaBLL().Consultar();});SessionAs(1);
            Check(Count("Bitacora")==events+2&&sec.VerificarIntegridad(),"Consultas concurrentes mantienen auditoria e integridad");
            Sql("DELETE dbo.Mascota WHERE idCliente=1");Refrescar("Mascota");Check(consulta.Consultar().IdPlan==1,"Consulta de cobertura por cuenta sin seleccionar ni contar mascotas");
            Sql("UPDATE dbo.[Plan] SET descripcion='Alterada' WHERE idPlan=1");Error(()=>consulta.Consultar(),"ER023");
            Console.WriteLine("TOTAL: "+checks+" checks passed");
        }
        finally
        {
            if(!keep)
            {HttpContext.Current=null;SqlConnection.ClearAllPools();var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};using(var cn=new SqlConnection(master.ConnectionString))using(var cmd=cn.CreateCommand()){cn.Open();cmd.CommandText="ALTER DATABASE ["+cs.InitialCatalog+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+cs.InitialCatalog+"];";cmd.ExecuteNonQuery();}}
        }
    }
}
