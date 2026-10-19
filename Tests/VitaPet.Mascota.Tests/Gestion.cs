using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Protocols;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.GUI.WebServices;
partial class Program
{
    static object Scalar(string sql)
    {using(var c=new SqlConnection(connection))using(var cmd=new SqlCommand(sql,c)){c.Open();return cmd.ExecuteScalar();}}
    static void GestionError(Action accion,string codigo)
    {try{accion();throw new Exception("Expected "+codigo);}catch(GestionMascotaException ex){Check(ex.Codigo==codigo,"Error gestion "+codigo);}}
    static void FalloSp(string sp,string codigo,Action verificar)
    {
        string original=(string)Scalar("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo."+sp+"'))");
        try{Sql("ALTER PROCEDURE dbo."+sp+" @idUsuario int,@idMascota int AS THROW 50061,'Test read failure',1;");verificar();}
        finally{Sql(System.Text.RegularExpressions.Regex.Replace(original,@"\bCREATE(?:\s+OR\s+ALTER)?\s+PROCEDURE\b","ALTER PROCEDURE",System.Text.RegularExpressions.RegexOptions.IgnoreCase));}
    }
    static void ProbarGestionMascotas()
    {
        var b=new MascotaBLL();var sec=new SecurityManager();var ws=new HistorialClinicoService();
        Check((int)Scalar("SELECT COUNT(*) FROM dbo.__SchemaMigrations WHERE migrationId='021_GestionMascotas.sql'")==1,"Migracion 021 idempotente");
        Check((int)Scalar("SELECT COUNT(*) FROM sys.procedures WHERE name IN ('sp_Mascota_ListarUsuario','sp_Mascota_ConsultarUsuario','sp_Mascota_Actualizar','sp_HistorialClinico_ConsultarMascota','sp_DatosClinicos_ConsultarMascota')")==5,"Cinco SP instalados");
        Check(new ErrorSistemaDAL().ObtenerPorCodigo("ER064")!=null&&sec.VerificarIntegridad(),"ER061 a ER064 registrados sin alterar integridad");
        foreach(int actor in new[]{0,2,3})
        {
            SessionAs(actor);
            foreach(Action accion in new Action[]{()=>b.Listar(),()=>b.Seleccionar(1),()=>b.Consultar(1),()=>b.Actualizar(1,"x",Datos()),()=>new ConsultaHistorialClinicoBLL().Consultar(1),()=>new ConsultaDatosClinicosBLL().Consultar(1)})
            {bool denied=false;try{accion();}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Operacion exclusiva de Cliente: "+actor);}
            bool soapDenied=false;try{ws.ObtenerDatosClinicos(1);}catch(SoapException){soapDenied=true;}Check(soapDenied,"Webservice rechaza acceso no autorizado: "+actor);
        }
        SessionAs(6);GestionError(()=>b.Listar(),"ER010");SessionAs(5);GestionError(()=>b.Listar(),"ER011");SessionAs(1);
        int audit=Count("Bitacora");var lista=b.Listar();
        Check(lista.Count==3&&lista.All(x=>x.IdCliente==1)&&lista.Any(x=>!x.Estado),"Selector solo lista mascotas propias, incluyendo inactivas");
        Check(Count("Bitacora")==audit+1&&new BitacoraDAL().ObtenerParaIntegridad().Last().Accion=="Consulta de mascotas","Listado auditado");
        audit=Count("Bitacora");var m=b.Seleccionar(1);Check(Count("Bitacora")==audit&&m.Nombre=="Luna","Seleccion conserva contexto sin auditoria duplicada");
        Check(b.Consultar(1).Nombre=="Luna"&&Count("Bitacora")==audit+1,"Consulta de datos propia auditada sin modificar mascota");
        foreach(int id in new[]{-1,0,4,9999})
        {
            GestionError(()=>b.Seleccionar(id),"ER011");GestionError(()=>b.Consultar(id),"ER011");
            GestionError(()=>b.Actualizar(id,m.DVH,Datos()),"ER011");
            Check(ws.ObtenerHistorialClinico(id).CodigoError=="ER011"&&ws.ObtenerDatosClinicos(id).CodigoError=="ER011","Webservice oculta datos ajenos o inexistentes: "+id);
        }
        var historial=ws.ObtenerHistorialClinico(1);var clinicos=ws.ObtenerDatosClinicos(1);
        Check(historial.CodigoError==null&&historial.Mascota.IdMascota==1&&historial.Historial.Count==1&&historial.Historial[0].Diagnostico=="Control general","Webservice devuelve historial de la mascota propia");
        Check(clinicos.CodigoError==null&&clinicos.Datos.Count==1&&clinicos.Datos[0].Peso==12.5m&&clinicos.Datos[0].Temperatura==38.4m&&clinicos.Datos[0].FrecuenciaCardiaca==90,"Webservice devuelve mediciones clinicas propias");
        Check(ws.ObtenerHistorialClinico(2).CodigoError=="ER011"&&ws.ObtenerDatosClinicos(2).CodigoError=="ER011","Sin registros clinicos informa ER011");
        GestionError(()=>b.Actualizar(1,m.DVH,null),"ER002");var datos=Datos();datos.Nombre=" ";GestionError(()=>b.Actualizar(1,m.DVH,datos),"ER002");
        datos=Datos();datos.FechaNacimiento=null;GestionError(()=>b.Actualizar(1,m.DVH,datos),"ER002");
        datos=Datos();datos.FechaNacimiento=DateTime.Today.AddDays(1);GestionError(()=>b.Actualizar(1,m.DVH,datos),"ER025");
        datos=Datos();datos.Nombre=new string('n',61);GestionError(()=>b.Actualizar(1,m.DVH,datos),"ER025");
        audit=Count("Bitacora");b.Actualizar(1,m.DVH,Datos());var actualizado=b.Seleccionar(1);
        Check(actualizado.Nombre=="Nube"&&actualizado.Especie=="Perro"&&actualizado.Raza=="Mestizo"&&actualizado.FechaNacimiento==new DateTime(2020,1,2)&&actualizado.IdCliente==1&&actualizado.Estado,"Actualizacion normaliza campos y conserva propietario y estado");
        Check(Count("Bitacora")==audit+1&&new BitacoraDAL().ObtenerParaIntegridad().Last().Accion=="Actualización de datos de mascota"&&sec.VerificarIntegridad(),"Actualizacion y auditoria con DVH, DVV y referencias");
        audit=Count("Bitacora");GestionError(()=>b.Actualizar(1,m.DVH,Datos()),"ER064");Check(Count("Bitacora")==audit&&b.Seleccionar(1).DVH==actualizado.DVH,"Version vencida no sobrescribe ni registra actualizacion");
        var inactiva=b.Seleccionar(3);b.Actualizar(3,inactiva.DVH,Datos());Check(!b.Seleccionar(3).Estado,"Actualizar no activa mascotas inactivas");
        audit=Count("Bitacora");string version=b.Seleccionar(1).DVH;
        Sql("CREATE TRIGGER dbo.TestGestionMascotaFail ON dbo.Mascota AFTER UPDATE AS THROW 50062,'Test update failure',1;");
        try{GestionError(()=>b.Actualizar(1,version,Datos()),"ER062");}finally{Sql("DROP TRIGGER dbo.TestGestionMascotaFail");}
        Check(b.Seleccionar(1).DVH==version&&Count("Bitacora")==audit&&sec.VerificarIntegridad(),"Falla de actualizacion revierte datos y verificadores");
        Sql("CREATE TRIGGER dbo.TestGestionAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");
        try
        {
            GestionError(()=>b.Actualizar(1,version,Datos()),"ER040");GestionError(()=>b.Listar(),"ER033");GestionError(()=>b.Consultar(1),"ER033");
            Check(ws.ObtenerHistorialClinico(1).CodigoError=="ER033"&&ws.ObtenerDatosClinicos(1).CodigoError=="ER033","Consultas clinicas propagan error de auditoria");
        }
        finally{Sql("DROP TRIGGER dbo.TestGestionAuditFail");}
        Check(b.Seleccionar(1).DVH==version&&Count("Bitacora")==audit&&sec.VerificarIntegridad(),"Falla de auditoria revierte toda la operacion");
        FalloSp("sp_Mascota_ConsultarUsuario","ER061",()=>GestionError(()=>b.Seleccionar(1),"ER061"));
        FalloSp("sp_HistorialClinico_ConsultarMascota","ER063",()=>Check(ws.ObtenerHistorialClinico(1).CodigoError=="ER063","Falla de historial informa ER063"));
        FalloSp("sp_DatosClinicos_ConsultarMascota","ER063",()=>Check(ws.ObtenerDatosClinicos(1).CodigoError=="ER063","Falla de mediciones informa ER063"));
        int exitos=0,conflictos=0;
        Parallel.For(0,2,i=>{SessionAs(1);var d=Datos();d.Nombre="Concurrente "+i;try{new MascotaBLL().Actualizar(1,version,d);Interlocked.Increment(ref exitos);}catch(GestionMascotaException ex){if(ex.Codigo!="ER064")throw;Interlocked.Increment(ref conflictos);}});
        SessionAs(1);Check(exitos==1&&conflictos==1&&sec.VerificarIntegridad(),"Dos actualizaciones concurrentes no se sobrescriben");
        Sql("UPDATE dbo.DatosClinicos SET peso=13.5 WHERE idDatoClinico=1");GestionError(()=>b.Listar(),"ER023");
        Check(ws.ObtenerHistorialClinico(1).CodigoError=="ER023"&&ws.ObtenerDatosClinicos(1).CodigoError=="ER023","Corrupcion clinica bloquea las consultas");
        Sql("UPDATE dbo.DatosClinicos SET peso=12.5 WHERE idDatoClinico=1");Check(sec.VerificarIntegridad(),"Restaurar el valor original conserva integridad sin recalcular");
        SessionAs(4);Check(ws.ObtenerDatosClinicos(4).Datos.Single().Observaciones=="Dato ajeno"&&ws.ObtenerHistorialClinico(1).CodigoError=="ER011","Segunda cuenta solo consulta sus registros clinicos");
    }
}
