using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

partial class Program
{
    static DatosEmpresaBE EmpresaDatos(string cuit="20-12345678-9",string nombre="  Empresa Álamo  ")
    { return new DatosEmpresaBE {RazonSocial=nombre,Cuit=cuit,Direccion="  Av. Prueba 100  ",Telefono=" +54 (11) 5555-0101 "}; }
    static void EmpresaError(Action action,string code)
    {
        try {action();throw new Exception("Expected "+code);}
        catch(EmpresaException ex) {Check(ex.Codigo==code,"Empresas: "+code);}
    }
    static void AdministradorEstado(bool activo)
    {
        Sql("UPDATE dbo.Usuario SET estado="+(activo?"1":"0")+" WHERE idUsuario=2");
        var dal=new UsuarioDAL();var h=new IntegridadBLL();var u=dal.ObtenerPorId(2);dal.ActualizarDVH(2,h.CalcularDVH(u));
        new DVVDAL().Guardar(new DVVBE {NombreTabla="Usuario",ValorDVV=h.CalcularDVV(dal.ObtenerDVHs())});
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(new ReferenciaIntegridadBLL(),null);
    }
    static void ProbarEmpresas()
    {
        var bll=new EmpresaBLL();var sec=new SecurityManager();
        foreach(int id in new[]{0,1,3})
        {
            SessionAs(id);
            foreach(Action accion in new Action[]{()=>bll.Listar(),()=>bll.Consultar(1),()=>bll.Registrar(EmpresaDatos()),()=>bll.Actualizar(1,EmpresaDatos())})
            {bool denied=false;try{accion();}catch(UnauthorizedAccessException){denied=true;}Check(denied,"Empresas exclusivas para Administrador, actor "+id);}
        }
        SessionAs(2);EmpresaError(()=>bll.Listar(),"ER017");
        Check(new ErrorSistemaDAL().ObtenerPorCodigo("ER067")!=null&&new ErrorSistemaDAL().ObtenerPorCodigo("ER070")!=null&&new ErrorSistemaDAL().ObtenerPorCodigo("ER071")==null&&sec.VerificarIntegridad(),"Errores 067 a 070 registrados sin ER071 y con integridad");
        EmpresaError(()=>bll.Registrar(null),"ER002");
        foreach(string field in new[]{"RazonSocial","Cuit","Direccion","Telefono"})
        {var d=EmpresaDatos();typeof(DatosEmpresaBE).GetProperty(field).SetValue(d," ");EmpresaError(()=>bll.Registrar(d),"ER002");}
        foreach(var pair in new[]{new[]{"RazonSocial","101"},new[]{"Direccion","151"},new[]{"Telefono","31"},new[]{"Cuit","21"}})
        {var d=EmpresaDatos();typeof(DatosEmpresaBE).GetProperty(pair[0]).SetValue(d,new string('1',int.Parse(pair[1])));EmpresaError(()=>bll.Registrar(d),"ER025");}
        foreach(string cuit in new[]{"123","123456789012","AA-12345678-9","20 12345678 9","2-0123456789","٢٠١٢٣٤٥٦٧٨٩"})
            EmpresaError(()=>bll.Registrar(EmpresaDatos(cuit)),"ER025");
        foreach(string telefono in new[]{"sin numero","++++()","5555\n0000"})
        {var d=EmpresaDatos();d.Telefono=telefono;EmpresaError(()=>bll.Registrar(d),"ER025");}
        var invalido=EmpresaDatos();invalido.RazonSocial="Empresa🐕";EmpresaError(()=>bll.Registrar(invalido),"ER025");
        int eventos=Count("Bitacora");var empresa=bll.Registrar(EmpresaDatos());int idEmpresa=empresa.IdEmpresa;
        var fila=new IntegridadTablasDAL().Leer("Empresa").Rows.Cast<DataRow>().Single(r=>(int)r["idEmpresa"]==idEmpresa);
        Check(empresa.Estado&&(string)fila["cuit"]=="20123456789"&&(string)fila["razonSocial"]=="Empresa Álamo"&&(string)fila["direccion"]=="Av. Prueba 100","Alta normalizada, CUIT sin guiones y estado activo");
        Check(Count("Bitacora")==eventos+1&&new BitacoraDAL().ObtenerParaIntegridad().Last().Modulo=="Empresas"&&sec.VerificarIntegridad(),"Alta y bitacora atomicas con DVH, DVV y referencias");
        EmpresaError(()=>bll.Registrar(EmpresaDatos("20123456789")),"ER068");
        var segunda=bll.Registrar(EmpresaDatos("30123456789","Zeta"));
        var limite=EmpresaDatos("30987654321",new string('R',100));limite.Direccion=new string('D',150);limite.Telefono=new string('1',30);bll.Registrar(limite);Check(sec.VerificarIntegridad(),"Admite longitudes maximas de Empresa");
        var lista=bll.Listar();Check(lista.Count==3&&lista.First().IdEmpresa==idEmpresa,"Listado ordenado y con todas las empresas");
        eventos=Count("Bitacora");bll.Seleccionar(idEmpresa);Check(Count("Bitacora")==eventos,"Seleccionar no registra una consulta adicional");
        bll.Consultar(idEmpresa);Check(Count("Bitacora")==eventos+1&&new BitacoraDAL().ObtenerParaIntegridad().Last().Accion=="Consulta de empresa asociada","Consulta detallada auditada");
        EmpresaError(()=>bll.Consultar(0),"ER011");EmpresaError(()=>bll.Seleccionar(int.MaxValue),"ER011");EmpresaError(()=>bll.Actualizar(int.MaxValue,EmpresaDatos()),"ER011");
        Sql("UPDATE dbo.Empresa SET estado=0 WHERE idEmpresa="+idEmpresa);Refrescar("Empresa");
        Sql("UPDATE dbo.Cliente SET idEmpresa="+idEmpresa+" WHERE idCliente=1");Refrescar("Cliente");
        bll.Actualizar(idEmpresa,EmpresaDatos("20-12345678-9","Nombre nuevo"));
        fila=new IntegridadTablasDAL().Leer("Empresa").Rows.Cast<DataRow>().Single(r=>(int)r["idEmpresa"]==idEmpresa);
        Check(!(bool)fila["estado"]&&(string)fila["razonSocial"]=="Nombre nuevo"&&new IntegridadTablasDAL().Leer("Cliente").Rows.Cast<DataRow>().Any(r=>(int)r["idCliente"]==1&&(int)r["idEmpresa"]==idEmpresa),"Modificacion conserva estado, identificador y asociacion de cliente");
        EmpresaError(()=>bll.Actualizar(idEmpresa,EmpresaDatos(segunda.Cuit)),"ER068");
        bll.Actualizar(idEmpresa,EmpresaDatos("20123456789","Primera edicion"));bll.Actualizar(idEmpresa,EmpresaDatos("20123456789","Segunda edicion"));
        Check(bll.Consultar(idEmpresa).RazonSocial=="Segunda edicion"&&sec.VerificarIntegridad(),"Modifica sin parametro ni control de version");
        int antes=Count("Empresa");eventos=Count("Bitacora");
        Sql("CREATE TRIGGER dbo.TestEmpresaInsertFail ON dbo.Empresa AFTER INSERT AS THROW 50069,'Test insert failure',1;");
        try{EmpresaError(()=>bll.Registrar(EmpresaDatos("30777777777")),"ER069");}finally{Sql("DROP TRIGGER dbo.TestEmpresaInsertFail");}
        Check(Count("Empresa")==antes&&Count("Bitacora")==eventos&&sec.VerificarIntegridad(),"Falla de alta revierte empresa y controles");
        Sql("CREATE TRIGGER dbo.TestEmpresaUpdateFail ON dbo.Empresa AFTER UPDATE AS THROW 50070,'Test update failure',1;");
        try{EmpresaError(()=>bll.Actualizar(idEmpresa,EmpresaDatos("20123456789","No guardar")),"ER070");}finally{Sql("DROP TRIGGER dbo.TestEmpresaUpdateFail");}
        Check(bll.Seleccionar(idEmpresa).RazonSocial=="Segunda edicion"&&sec.VerificarIntegridad(),"Falla de actualizacion no deja datos parciales");
        Sql("CREATE TRIGGER dbo.TestEmpresaAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");
        try
        {
            EmpresaError(()=>bll.Registrar(EmpresaDatos("30777777777")),"ER040");
            EmpresaError(()=>bll.Actualizar(idEmpresa,EmpresaDatos("20123456789","No guardar")),"ER040");
            EmpresaError(()=>bll.Listar(),"ER033");EmpresaError(()=>bll.Consultar(idEmpresa),"ER033");
        }
        finally{Sql("DROP TRIGGER dbo.TestEmpresaAuditFail");}
        Check(Count("Empresa")==antes&&Count("Bitacora")==eventos&&bll.Seleccionar(idEmpresa).RazonSocial=="Segunda edicion"&&sec.VerificarIntegridad(),"Falla de auditoria revierte altas, cambios, bitacora e integridad");
        Sql("EXEC sp_rename 'dbo.sp_Empresa_CuitRegistrado','TestEmpresaCuit';");
        try{EmpresaError(()=>bll.Registrar(EmpresaDatos("30777777777")),"ER067");}finally{Sql("EXEC sp_rename 'dbo.TestEmpresaCuit','sp_Empresa_CuitRegistrado';");}
        Sql("EXEC sp_rename 'dbo.sp_Empresa_Consultar','TestEmpresaConsultar';");
        try{EmpresaError(()=>bll.Consultar(idEmpresa),"ER067");}finally{Sql("EXEC sp_rename 'dbo.TestEmpresaConsultar','sp_Empresa_Consultar';");}
        int exitos=0,duplicados=0;
        Parallel.For(0,2,i=>{SessionAs(2);try{bll.Registrar(EmpresaDatos("30555555555"));System.Threading.Interlocked.Increment(ref exitos);}catch(EmpresaException ex){if(ex.Codigo!="ER068")throw;System.Threading.Interlocked.Increment(ref duplicados);}});
        SessionAs(2);Check(exitos==1&&duplicados==1&&Count("Empresa")==antes+1&&sec.VerificarIntegridad(),"Altas simultaneas de un CUIT generan una unica empresa");
        AdministradorEstado(false);bool denegado=false;try{bll.Actualizar(idEmpresa,EmpresaDatos());}catch(UnauthorizedAccessException){denegado=true;}
        Check(denegado,"Administrador desactivado despues del login no puede operar");AdministradorEstado(true);
        using(var op=new OperacionIntegridadDAL())
        {bool rechazo=false;try{new EmpresaDAL().Listar(1);}catch(SqlException ex){rechazo=ex.Number==50024;}Check(rechazo,"SP comprueba rol del actor");}
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);string original=cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString;
        try{cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=new SqlConnectionStringBuilder(original){InitialCatalog="VitaPet_Verificacion_Ausente"}.ConnectionString;cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");EmpresaError(()=>bll.Listar(),"ER003");}
        finally{cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=original;cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");}
        Sql("UPDATE dbo.Empresa SET razonSocial='Alterada sin DVH' WHERE idEmpresa="+idEmpresa);
        EmpresaError(()=>bll.Consultar(idEmpresa),"ER023");EmpresaError(()=>bll.Actualizar(idEmpresa,EmpresaDatos()),"ER023");
    }
}
