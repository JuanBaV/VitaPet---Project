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
    static void Error(Action action,string code){try{action();throw new Exception("Expected "+code);}catch(ConsultaReintegroException e){Check(e.Codigo==code,"Error "+code);}}
    static void Refrescar(string table)
    {
        var d=new IntegridadTablasDAL();var t=d.Leer(table);var h=new IntegridadTablasBLL();
        foreach(DataRow r in t.Rows){string v=h.CalcularDVH(r);d.GuardarDvh(table,r[IntegridadTablasDAL.Claves[table]],v);r["dvh"]=v;}
        new DVVDAL().Guardar(new DVVBE{NombreTabla=table,ValorDVV=new IntegridadBLL().CalcularDVV(t.Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))});
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(new ReferenciaIntegridadBLL(),null);
    }
    static string Dominio(){return string.Join("|",new[]{"Cliente","Mascota","Reintegro"}.Select(t=>string.Join(",",new IntegridadTablasDAL().Leer(t).Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))));}
    static void SembrarReintegros()
    {
        Sql("INSERT dbo.Reintegro(idCliente,idMascota,fechaSolicitud,importe,estado,detalle,dvh) VALUES(1,1,'20261010 10:30',12500.50,'Pendiente','Consulta & medicación <prueba>',''),(1,2,'20261010 10:30',8000,'Aprobado',NULL,''),(2,4,'20261011',5000,'Rechazado','Solo otro cliente','');");
        Refrescar("Reintegro");
    }
    static void Main(string[] args)
    {
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);var cs=new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if(!cs.InitialCatalog.StartsWith("VitaPet_Verificacion_Reintegros",StringComparison.Ordinal))throw new Exception("Only isolated databases allowed");
        if(args.Length==1&&args[0]=="--refresh") {connection=cs.ConnectionString;Refrescar("Reintegro");return;}
        cs.InitialCatalog="VitaPet_Verificacion_Reintegros_"+Guid.NewGuid().ToString("N");connection=cs.ConnectionString;
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
            errors.AsegurarErroresReintegros();errors.AsegurarErroresReintegros();
            Check(new ErrorSistemaDAL().ObtenerPorCodigo("ER059")!=null&&sec.VerificarIntegridad(),"ER059 registrado con DVH, DVV y referencias");
            if(args.Contains("--web")){SembrarReintegros();File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"web-environment.txt"),new[]{cs.InitialCatalog,runtime});keep=true;return;}
            var consulta=new ConsultaReintegroBLL();
            foreach(int actor in new[]{0,2,3}){SessionAs(actor);bool denied=false;try{consulta.Listar();}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Listado exclusivo de Cliente: "+actor);}
            SessionAs(5);Error(()=>consulta.Listar(),"ER011");SessionAs(1);Error(()=>consulta.Listar(),"ER009");
            SembrarReintegros();var lista=consulta.Listar();Check(lista.Count==2&&lista[0].IdReintegro==2,"Listado propio ordenado por fecha e identificador descendente");
            string dominio=Dominio();int events=Count("Bitacora");var dato=consulta.Consultar(1);
            Check(dato.IdReintegro==1&&dato.Importe==12500.50m&&dato.Estado=="Pendiente","Detalle completo del reintegro propio");
            Check(Dominio()==dominio&&Count("Bitacora")==events+1,"Consulta de solo lectura, con auditoria");
            var audit=new BitacoraDAL().ObtenerParaIntegridad().Last();Check(audit.Accion=="Consulta de estado de reintegro"&&audit.IdUsuario==1&&audit.Modulo=="Reintegros"&&sec.VerificarIntegridad(),"Evento asociado al usuario y controles validos");
            Error(()=>consulta.Consultar(3),"ER011");Error(()=>consulta.ExportarXml(3),"ER011");Error(()=>consulta.Consultar(0),"ER011");
            using(var tx=new OperacionIntegridadDAL()){bool denied=false;try{new ReintegroDAL().Consultar(1,3);}catch(SqlException ex){denied=ex.Number==50011;}Check(denied,"SP rechaza reintegro ajeno");}
            events=Count("Bitacora");var xml=new System.Xml.XmlDocument{XmlResolver=null};xml.Load(new MemoryStream(consulta.ExportarXml(1)));
            Check(xml.DocumentElement.Name=="Reintegros"&&xml.SelectNodes("/Reintegros/Reintegro").Count==1,"XML con un unico reintegro");
            Check(xml.SelectSingleNode("//importe").InnerText=="12500.50"&&xml.SelectSingleNode("//detalle").InnerText=="Consulta & medicación <prueba>","Decimales invariantes y caracteres especiales preservados");
            Check(xml.SelectSingleNode("//dvh")==null&&xml.SelectSingleNode("//idCliente")==null&&Count("Bitacora")==events,"XML no expone controles internos ni altera la consulta");
            Sql("UPDATE dbo.Reintegro SET estado='Aprobado' WHERE idReintegro=1");Refrescar("Reintegro");xml.Load(new MemoryStream(consulta.ExportarXml(1)));Check(xml.SelectSingleNode("//estado").InnerText=="Aprobado","Exportacion reconsulta el estado actual");
            xml.Load(new MemoryStream(consulta.ExportarXml(2)));Check(xml.SelectSingleNode("//detalle").InnerText=="","Detalle nulo exportado sin error");
            Sql("CREATE TRIGGER dbo.TestReintegroAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50033,'Test audit failure',1;");events=Count("Bitacora");Error(()=>consulta.Consultar(1),"ER033");Sql("DROP TRIGGER dbo.TestReintegroAuditFail");Check(Count("Bitacora")==events&&sec.VerificarIntegridad(),"Falla de auditoria revierte sin perder integridad");
            Sql("CREATE OR ALTER PROCEDURE dbo.sp_Reintegro_ListarCliente @idUsuario INT AS THROW 50059,'Test read failure',1;");Error(()=>consulta.Listar(),"ER059");
            string migration;using(var reader=new StreamReader(typeof(DatabaseMigrator).Assembly.GetManifestResourceStream("VitaPet.DAL.Migrations.019_ConsultaReintegros.sql"))){migration=reader.ReadToEnd();}
            foreach(var batch in System.Text.RegularExpressions.Regex.Split(migration,@"^GO\s*$",System.Text.RegularExpressions.RegexOptions.Multiline|System.Text.RegularExpressions.RegexOptions.IgnoreCase))if(!string.IsNullOrWhiteSpace(batch))Sql(batch);
            Sql("UPDATE dbo.Reintegro SET detalle=CHAR(1) WHERE idReintegro=1");Refrescar("Reintegro");Error(()=>consulta.ExportarXml(1),"ER015");
            Sql("UPDATE dbo.Reintegro SET detalle='Restaurado' WHERE idReintegro=1");Refrescar("Reintegro");
            events=Count("Bitacora");Parallel.For(0,2,i=>{SessionAs(1);new ConsultaReintegroBLL().Consultar(1);});SessionAs(1);Check(Count("Bitacora")==events+2&&sec.VerificarIntegridad(),"Consultas concurrentes mantienen auditoria e integridad");
            SessionAs(4);Check(consulta.Listar().Count==1&&consulta.Consultar(3).IdReintegro==3,"Segunda cuenta solo obtiene sus reintegros");SessionAs(1);
            Sql("DELETE dbo.Reintegro WHERE idReintegro=2");Refrescar("Reintegro");Error(()=>consulta.ExportarXml(2),"ER011");
            Sql("UPDATE dbo.Reintegro SET estado='Alterado' WHERE idReintegro=1");Error(()=>consulta.Consultar(1),"ER023");Error(()=>consulta.ExportarXml(1),"ER023");
            Console.WriteLine("TOTAL: "+checks+" checks passed");
        }
        finally
        {
            if(!keep)
            {HttpContext.Current=null;SqlConnection.ClearAllPools();var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};using(var cn=new SqlConnection(master.ConnectionString))using(var cmd=cn.CreateCommand()){cn.Open();cmd.CommandText="ALTER DATABASE ["+cs.InitialCatalog+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+cs.InitialCatalog+"];";cmd.ExecuteNonQuery();}}
        }
    }
}
