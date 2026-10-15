using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

class Program
{
    static string connection;
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; Console.WriteLine("PASS " + message); }
    static void Sql(string sql) { using (var cn = new SqlConnection(connection)) using (var cmd = new SqlCommand(sql, cn)) { cn.Open(); cmd.ExecuteNonQuery(); } }
    static int Count(string table) { using (var cn = new SqlConnection(connection)) using (var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.["+table+"]", cn)) { cn.Open(); return (int)cmd.ExecuteScalar(); } }
    static RegistroClienteBE Datos(string correo) { return new RegistroClienteBE { Nombre=" José ", Apellido=" O'Connor ", CorreoElectronico=correo, Contrasena="Prueba2026!", Telefono="+54 (11) 5555-1234" }; }
    static void Error(Action action, string code)
    {
        try { action(); throw new Exception("Expected " + code); }
        catch (RegistroClienteException ex) { Check(ex.Codigo == code, "Error centralizado " + code); }
    }
    static void Main(string[] args)
    {
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var cs=new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if (!cs.InitialCatalog.StartsWith("VitaPet_Verificacion_Registro", StringComparison.Ordinal)) throw new Exception("Only isolated databases allowed");
        cs.InitialCatalog="VitaPet_Verificacion_Registro_"+Guid.NewGuid().ToString("N"); connection=cs.ConnectionString;
        var runtime=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"runtime",cs.InitialCatalog); Directory.CreateDirectory(runtime);
        cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=connection;
        cfg.AppSettings.Settings["RutaRecuperacion"].Value=Path.Combine(runtime,"recuperacion");
        cfg.AppSettings.Settings["RutaBitacoraXml"].Value=Path.Combine(runtime,"Bitacora.xml");
        cfg.Save(); ConfigurationManager.RefreshSection("connectionStrings"); ConfigurationManager.RefreshSection("appSettings");
        bool keep=false;
        try
        {
            DatabaseMigrator.Migrate(); DatabaseMigrator.Migrate(); new ReferenciaIntegridadBLL().Inicializar();
            var errors=new ErrorSistemaBLL();errors.AsegurarErroresRegistroCliente();errors.AsegurarErroresRegistroCliente();
            var sec=new SecurityManager();var bll=new ClienteBLL();var usuarios=new UsuarioDAL();
            Check(sec.VerificarIntegridad(),"Integridad inicial y alta idempotente de ER049");
            if(args.Contains("--web"))
            {
                File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"web-environment.txt"),new[]{cs.InitialCatalog,runtime});
                keep=true;return;
            }
            int before=Count("Usuario"),clients=Count("Cliente"),audit=Count("Bitacora");
            Error(()=>bll.RegistrarCliente(null),"ER002");
            var datos=Datos("registro@test.local");datos.Nombre=" ";Error(()=>bll.RegistrarCliente(datos),"ER002");
            foreach(var cambiar in new Action<RegistroClienteBE>[] {
                c=>c.CorreoElectronico="bad", c=>c.CorreoElectronico=new string('x',101)+"@test.local",
                c=>c.Contrasena="weak",c=>c.Contrasena=new string('A',101)+"1!",c=>c.Nombre=new string('a',61),
                c=>c.Apellido=new string('b',61),c=>{c.Nombre=new string('a',60);c.Apellido=new string('b',60);},
                c=>c.Telefono="no phone",c=>c.Nombre="😀",c=>c.CorreoElectronico="test @test.local" })
            { datos=Datos("invalid@test.local");cambiar(datos);Error(()=>bll.RegistrarCliente(datos),"ER025"); }
            Check(Count("Usuario")==before && Count("Cliente")==clients && Count("Bitacora")==audit,"Validaciones no escriben datos");
            int id=bll.RegistrarCliente(Datos("  registro@test.local  "));var u=usuarios.ObtenerPorCorreo("registro@test.local");
            Check(u.IdUsuario==id && u.Rol=="Cliente" && u.Estado && !u.Bloqueado && u.IntentosFallidos==0 && u.Idioma=="es","Cuenta habilitada con valores fijos del servidor");
            Check(u.Nombre=="José O'Connor" && u.Contrasena==sec.CalcularHash("Prueba2026!"),"Datos normalizados; hash compatible con Login");
            var fila=new IntegridadTablasDAL().Leer("Cliente").Rows.Cast<System.Data.DataRow>().Single(r=>(int)r["idUsuario"]==id);
            Check((string)fila["nombre"]=="José" && (string)fila["apellido"]=="O'Connor" && fila.IsNull("idEmpresa") && (string)fila["tipoCliente"]=="Individual","Cliente vinculado y SQL parametrizado");
            Check(sec.VerificarIntegridad(),"DVH, DVV y referencias válidos tras el alta");
            var evento=new BitacoraDAL().ObtenerParaIntegridad().Last();
            Check(evento.IdUsuario==id && evento.Accion=="Registro de cliente" && evento.Modulo=="Clientes" && evento.Criticidad=="Informativa","Registro auditado con la nueva cuenta");
            Error(()=>bll.RegistrarCliente(Datos("REGISTRO@test.local")),"ER001");
            Check(Count("Usuario")==before+1 && Count("Cliente")==clients+1 && Count("Bitacora")==audit+1,"Correo duplicado no deja altas parciales");
            var results=new string[2];
            Parallel.For(0,2,i=>{try{new ClienteBLL().RegistrarCliente(Datos("race@test.local"));results[i]="OK";}catch(RegistroClienteException ex){results[i]=ex.Codigo;}});
            Check(results.Count(r=>r=="OK")==1 && results.Count(r=>r=="ER001")==1,"Dos registros simultáneos no duplican el correo");
            Check(sec.VerificarIntegridad(),"Concurrencia conserva integridad");
            before=Count("Usuario");clients=Count("Cliente");audit=Count("Bitacora");
            Sql("CREATE TRIGGER dbo.TestRegistroClienteFail ON dbo.Cliente AFTER INSERT AS THROW 50049, 'Test client failure', 1;");
            Error(()=>bll.RegistrarCliente(Datos("rollback@test.local")),"ER049");Sql("DROP TRIGGER dbo.TestRegistroClienteFail;");
            Check(Count("Usuario")==before && Count("Cliente")==clients && Count("Bitacora")==audit && sec.VerificarIntegridad(),"Falla en Cliente revierte Usuario y verificadores");
            Sql("CREATE TRIGGER dbo.TestRegistroAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040, 'Test audit failure', 1;");
            Error(()=>bll.RegistrarCliente(Datos("auditfail@test.local")),"ER040");Sql("DROP TRIGGER dbo.TestRegistroAuditFail;");
            Check(Count("Usuario")==before && Count("Cliente")==clients && Count("Bitacora")==audit && sec.VerificarIntegridad(),"Falla en bitácora revierte todo el registro");
            // Fallo real de conexión en un puerto local sin SQL; restaurar la
            // configuración aislada antes de consultar o eliminar la base.
            var unavailable=new SqlConnectionStringBuilder(connection){DataSource="localhost,1",ConnectTimeout=1};
            try
            {
                cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=unavailable.ConnectionString;
                cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");
                Error(()=>bll.RegistrarCliente(Datos("connectionfail@test.local")),"ER003");
            }
            finally
            {
                cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=connection;
                cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");
            }
            Check(Count("Usuario")==before && Count("Cliente")==clients,"Fallo de conexión no crea datos parciales");
            Sql("UPDATE dbo.Cliente SET telefono='555555' WHERE idUsuario="+id);
            Error(()=>bll.RegistrarCliente(Datos("tamper@test.local")),"ER023");
            Check(Count("Usuario")==before,"Integridad alterada impide nuevas cuentas");
            Console.WriteLine("TOTAL: "+checks+" checks passed");
        }
        finally
        {
            if(!keep)
            {
                SqlConnection.ClearAllPools();var master=new SqlConnectionStringBuilder(connection){InitialCatalog="master"};
                using(var cn=new SqlConnection(master.ConnectionString))using(var cmd=cn.CreateCommand())
                {cn.Open();cmd.CommandText="ALTER DATABASE ["+cs.InitialCatalog+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ["+cs.InitialCatalog+"];";cmd.ExecuteNonQuery();}
            }
        }
    }
}
