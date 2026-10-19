using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
partial class Program
{
    static void RefrescarDatosClinicosFixture()
    {
        var dal=new IntegridadTablasDAL();var datos=dal.Leer("DatosClinicos");var hash=new IntegridadTablasBLL();
        foreach(DataRow fila in datos.Rows)
        {string dvh=hash.CalcularDVH(fila);dal.GuardarDvh("DatosClinicos",fila["idDatoClinico"],dvh);fila["dvh"]=dvh;}
        new DVVDAL().Guardar(new DVVBE {NombreTabla="DatosClinicos",ValorDVV=new IntegridadBLL().CalcularDVV(datos.Rows.Cast<DataRow>().Select(f=>(string)f["dvh"]))});
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(new ReferenciaIntegridadBLL(),null);
    }
    static void SembrarTablas()
    {
        Sql(@"INSERT dbo.Empresa(razonSocial,cuit,direccion,telefono,estado,dvh) VALUES('Prueba','20-test','D','T',1,'');
INSERT dbo.Cliente(idUsuario,idEmpresa,nombre,apellido,telefono,tipoCliente,estado,dvh) VALUES(1,1,'Nombre','Apellido','T','Individual',1,'');
INSERT dbo.[Plan](nombre,descripcion,precio,estado,dvh) VALUES('Plan','Detalle',123.45,1,'');
INSERT dbo.Cobertura(idPlan,prestacion,detalle,estado,dvh) VALUES(1,'Consulta','Detalle',1,'');
INSERT dbo.Suscripcion(idCliente,idPlan,fechaAlta,fechaVencimiento,estado,dvh) VALUES(1,1,'20260101','20261231','Activa','');
INSERT dbo.Pago(idSuscripcion,importe,medioPago,fechaPago,estado,dvh) VALUES(1,123.45,'Tarjeta','20260101','Pagado','');
INSERT dbo.Mascota(idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh) VALUES(1,'Mascota','Perro','Raza','20200101',1,'');
INSERT dbo.Turno(idMascota,fecha,horario,prestacion,zona,estado,dvh) VALUES(1,'20261201','10:00','Consulta','Sur','Pendiente','');
INSERT dbo.HistorialClinico(idMascota,fechaRegistro,diagnostico,tratamiento,observaciones,dvh) VALUES(1,'20260101','Diagnostico','Tratamiento',NULL,'');
INSERT dbo.Reintegro(idCliente,idMascota,fechaSolicitud,importe,estado,detalle,dvh) VALUES(1,1,'20260101',12.34,'Pendiente',NULL,'');
INSERT dbo.AgendaTurno(zona,prestacion,horaInicio,horaFin,duracionMinutos,horizonteDias,estado,dvh) VALUES('Sur','Consulta','09:00','18:00',30,30,1,'');");
    }
    static object Escalar(string sql)
    { using(var c=new SqlConnection(connection)) using(var cmd=new SqlCommand(sql,c)) { c.Open(); return cmd.ExecuteScalar(); } }
    static void ActualizarCampo(string tabla,string campo,object valor)
    {
        using(var c=new SqlConnection(connection)) using(var cmd=new SqlCommand("UPDATE dbo.["+tabla+"] SET ["+campo+"]=@valor",c))
        { cmd.Parameters.Add("@valor",SqlDbType.NVarChar,4000).Value=valor??DBNull.Value; c.Open();cmd.ExecuteNonQuery(); }
    }
    static void ProbarIntegridadTablas()
    {
        var sec=new SecurityManager();
        Sql("INSERT dbo.DatosClinicos(idMascota,peso,temperatura,frecuenciaCardiaca,observaciones,dvh) VALUES(1,12.5,38.4,90,'Control','');");
        RefrescarDatosClinicosFixture();
        Check(sec.VerificarIntegridad() && Convert.ToInt32(Escalar("SELECT COUNT(*) FROM dbo.DVV"))==IntegridadTablasDAL.Claves.Count+2,"All supervised functional tables have valid DVV");
        string[] campos={"razonSocial","nombre","nombre","detalle","estado","medioPago","nombre","prestacion","diagnostico","detalle","descripcion","observaciones","prestacion"};
        int n=0;
        foreach(string tabla in IntegridadTablasDAL.Claves.Keys)
        {
            // ErrorSistema has multiple records: preserve each independently via a snapshot table.
            string campo=campos[n++];
            DataTable original=new IntegridadTablasDAL().Leer(tabla);
            ActualizarCampo(tabla,campo,"Alterado");
            var result=sec.DiagnosticarIntegridad();
            Check(!result.EsValida && result.Detalles.Any(d=>d.Tabla==tabla && d.Columna==campo),"Field tampering detected and located: "+tabla);
            bool rejected=false;try{sec.ExigirIntegridad();}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"Inconsistent table blocks normal operations: "+tabla);
            using(var c=new SqlConnection(connection))
            {
                c.Open();foreach(DataRow row in original.Rows)
                using(var cmd=new SqlCommand("UPDATE dbo.["+tabla+"] SET ["+campo+"]=@v WHERE ["+IntegridadTablasDAL.Claves[tabla]+"]=@id",c))
                { cmd.Parameters.AddWithValue("@v",row[campo]);cmd.Parameters.AddWithValue("@id",row[IntegridadTablasDAL.Claves[tabla]]);cmd.ExecuteNonQuery(); }
            }
            Check(sec.VerificarIntegridad(),"Restoring original field restores integrity: "+tabla);
            string dvv=(string)Escalar("SELECT valorDVV FROM dbo.DVV WHERE nombreTabla='"+tabla+"'");
            Sql("DELETE dbo.DVV WHERE nombreTabla='"+tabla+"'");
            Check(!sec.VerificarIntegridad(),"Missing DVV rejected: "+tabla);
            new DVVDAL().Guardar(new DVVBE{NombreTabla=tabla,ValorDVV=dvv});
        }
        Sql("INSERT dbo.Empresa(razonSocial,cuit,direccion,telefono,estado,dvh) VALUES('Intruso','extra','D','T',1,'x')");
        Check(!sec.VerificarIntegridad(),"Unexpected row detected");Sql("DELETE dbo.Empresa WHERE cuit='extra'");
        Sql("CREATE TABLE dbo.TablaSinCobertura(id int)");
        Check(!sec.VerificarIntegridad(),"New table cannot remain silently outside coverage");Sql("DROP TABLE dbo.TablaSinCobertura");
        var fila=new IntegridadTablasDAL().Leer("Plan").Rows[0]; var bll=new IntegridadTablasBLL();
        var previo=CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture=new CultureInfo("es-AR");string h=bll.CalcularDVH(fila);
            CultureInfo.CurrentCulture=new CultureInfo("en-US");Check(h==bll.CalcularDVH(fila),"Decimal hashes do not depend on regional settings"); }
        finally { CultureInfo.CurrentCulture=previo; }
        fila=new IntegridadTablasDAL().Leer("Reintegro").Rows[0];string nullHash=bll.CalcularDVH(fila);fila["detalle"]="";
        Check(nullHash!=bll.CalcularDVH(fila),"Null and empty text produce different extended hashes");
        Sql("UPDATE dbo.__FormatoIntegridad SET formato='pendiente'");
        bool deniedReset=false;try{new ReferenciaIntegridadBLL().Inicializar();}catch(InvalidOperationException){deniedReset=true;}
        Check(deniedReset,"Existing references cannot be reinitialized as a fresh database");
        Sql("UPDATE dbo.__FormatoIntegridad SET formato='sha256-v2'");
        Check(sec.VerificarIntegridad(),"All-table checks leave fixture intact");
    }
    static void ProbarBackupAntesDeSimplificar()
    {
        string actual = new BackupDAL().Esquema();
        string previo = actual.Substring(0, actual.IndexOf("013_ProcedimientosSimples.sql:", StringComparison.Ordinal));
        var metodo = typeof(BackupBLL).GetMethod("EsquemaCompatible", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        Func<string,string,bool> compatible = (a,b) => (bool)metodo.Invoke(null, new object[] {a,b});
        Check(compatible(previo,actual), "Known migration chain accepts intact historical schema");
        Check(!compatible(previo,actual + "999_Desconocida.sql:" + new string('a',64) + ";"), "Unknown migration cannot enable historical restore");
        Check(!compatible(previo.Replace("001_InitialSchema.sql:", "001_Alterada.sql:"),actual), "Changed migration identity is rejected");
        Check(!compatible(previo.TrimEnd(';'),actual), "Incomplete migration metadata is rejected");
        var sec = new SecurityManager();
        var app = new ContingenciaWebMasterBLL();
        var users = new UsuarioDAL();
        var assembly = typeof(DatabaseMigrator).Assembly;
        using (var reader = new System.IO.StreamReader(assembly.GetManifestResourceStream("VitaPet.DAL.Migrations.010_GestionUsuarios.sql")))
        {
            string script = reader.ReadToEnd();
            Sql(script.Substring(script.IndexOf("CREATE OR ALTER PROCEDURE", StringComparison.Ordinal)));
        }
        Sql("DROP TABLE dbo.AgendaTurno; DROP INDEX UX_Turno_Confirmado ON dbo.Turno; DROP INDEX IX_Turno_Mascota ON dbo.Turno; DELETE dbo.DVV WHERE nombreTabla='AgendaTurno'; DROP TABLE dbo.DatosClinicos; DROP INDEX IX_HistorialClinico_Mascota ON dbo.HistorialClinico; DELETE dbo.DVV WHERE nombreTabla='DatosClinicos'; DELETE dbo.__SchemaMigrations WHERE migrationId >= '013_';");
        string xml; BackupBE backup;
        using(ConexionDAL.UsarBaseTemporal(new SqlConnectionStringBuilder(connection).InitialCatalog))
        {
            typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(new ReferenciaIntegridadBLL(),null);
            xml=app.CrearRespaldo();backup=new BackupBLL().Crear();
        }
        DatabaseMigrator.Migrate();
        Check(sec.VerificarIntegridad(), "Procedure update preserves all-table integrity");
        new BackupBLL().Restaurar(backup.Id);
        Check(sec.VerificarIntegridad() && Convert.ToInt32(Escalar("SELECT COUNT(*) FROM dbo.__SchemaMigrations WHERE migrationId='013_ProcedimientosSimples.sql'")) == 1,
            "SQL backup from schema 012 restores with current procedures");
        Check(((string)Escalar("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.sp_Usuario_Listar'))")).Contains("SET @nombre ="),
            "SQL restore reapplies simplified procedure definition");
        Sql("UPDATE dbo.Usuario SET nombre='Alterado' WHERE idUsuario=3");
        SessionAs();
        Check(app.Autenticar(Correo, Clave), "Emergency login for schema 012 XML");
        app.Restaurar(xml);
        Check(sec.VerificarIntegridad() && ((string)Escalar("SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.sp_Usuario_Listar'))")).Contains("SET @nombre ="),
            "Signed XML from schema 012 restores with current procedures");
        SessionAs(users.ObtenerPorCorreo("webmaster@vitapet.local"));
    }
    static void ProbarMigracionAnterior()
    {
        var sec=new SecurityManager(); var app=new ContingenciaWebMasterBLL(); var users=new UsuarioDAL();
        // Recreate the immediately preceding schema only inside this isolated fixture.
        Sql("DROP TABLE dbo.AgendaTurno; DROP INDEX UX_Turno_Confirmado ON dbo.Turno; DROP INDEX IX_Turno_Mascota ON dbo.Turno; DROP TABLE dbo.DatosClinicos; DROP INDEX IX_HistorialClinico_Mascota ON dbo.HistorialClinico; DELETE dbo.DVV WHERE nombreTabla NOT IN ('Usuario','Bitacora'); DELETE dbo.ReferenciaIntegridad; UPDATE dbo.__FormatoIntegridad SET formato='sha256-v1'; ALTER TABLE dbo.ErrorSistema DROP CONSTRAINT DF_ErrorSistema_dvh; ALTER TABLE dbo.ErrorSistema DROP COLUMN dvh; DELETE dbo.__SchemaMigrations WHERE migrationId >= '012_';");
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Invoke(new ReferenciaIntegridadBLL(),null);
        Check(sec.VerificarIntegridad(),"Legacy fixture retains valid original controls");
        string oldXml=app.CrearRespaldo(); var oldBak=new BackupBLL().Crear();
        string originalName=(string)Escalar("SELECT nombre FROM dbo.Usuario WHERE idUsuario=3");
        Sql("UPDATE dbo.Usuario SET nombre='Alterado' WHERE idUsuario=3");
        DatabaseMigrator.Migrate();
        bool rejected=false;try{new ReferenciaIntegridadBLL().Inicializar();}catch(InvalidOperationException){rejected=true;}
        Check(rejected && (string)Escalar("SELECT formato FROM dbo.__FormatoIntegridad")=="sha256-v1", "Upgrade refuses corrupted legacy data without blessing changes");
        using(var c=new SqlConnection(connection)) using(var cmd=new SqlCommand("UPDATE dbo.Usuario SET nombre=@n WHERE idUsuario=3",c))
        {cmd.Parameters.AddWithValue("@n",originalName);c.Open();cmd.ExecuteNonQuery();}
        new ReferenciaIntegridadBLL().Inicializar();
        Check(sec.VerificarIntegridad() && (string)Escalar("SELECT formato FROM dbo.__FormatoIntegridad")=="sha256-v2","Healthy legacy database upgrades to all-table integrity");
        new BackupBLL().Restaurar(oldBak.Id);
        Check(sec.VerificarIntegridad() && (string)Escalar("SELECT formato FROM dbo.__FormatoIntegridad")=="sha256-v2","Legacy SQL backup restores and upgrades safely");
        Sql("UPDATE dbo.Usuario SET nombre='Alterado' WHERE idUsuario=3");SessionAs();
        Check(app.Autenticar(Correo,Clave),"Emergency login supports legacy XML recovery");
        app.Restaurar(oldXml);
        Check(sec.VerificarIntegridad() && (string)Escalar("SELECT formato FROM dbo.__FormatoIntegridad")=="sha256-v2","Legacy signed XML restores with expanded integrity");
        SessionAs(users.ObtenerPorCorreo("webmaster@vitapet.local"));
    }

}
