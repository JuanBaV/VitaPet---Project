using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Configuration;
using System.IO;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.Pruebas;

static class IntegridadTodasLasTablas
{
    static string connection;
    static int checks;
    static bool audit;
    static readonly SecurityManager security = new SecurityManager();
    static void Check(bool ok,string text)
    { if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text); }
    static int Sql(string sql)
    { using(var c=new SqlConnection(connection))using(var cmd=new SqlCommand(sql,c)){c.Open();return cmd.ExecuteNonQuery();} }
    static DataTable Read(string sql)
    { using(var c=new SqlConnection(connection))using(var a=new SqlDataAdapter(sql,c)){var t=new DataTable();a.Fill(t);return t;} }
    static void Seed()
    {
        // Datos sintéticos exclusivos de la base que crea y elimina la suite.
        Sql(@"
DECLARE @n int=1;
WHILE @n<=3 BEGIN
 INSERT dbo.Usuario(correoElectronico,contrasena,nombre,idRol,estado,dvh) VALUES(CONCAT('integridad',@n,'@test.local'),'hash','Cliente prueba',(SELECT idRol FROM dbo.Rol r JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol WHERE p.nombre='Cliente' AND p.sistema=1),1,'');
 DECLARE @u int=SCOPE_IDENTITY();
 INSERT dbo.PermisoComponente(nombre,tipo,pantalla,sistema,dvh) VALUES(CONCAT('Rol integridad ',@n),'Perfil',NULL,0,'');
 DECLARE @rol int=SCOPE_IDENTITY();
 INSERT dbo.Rol(idRol,dvh) VALUES(@rol,'');
 INSERT dbo.RolPermiso(idRol,idComponente,dvh) VALUES(@rol,(SELECT TOP(1) idComponente FROM dbo.PermisoComponente WHERE tipo='Pantalla' ORDER BY idComponente),'');
 INSERT dbo.Empresa(razonSocial,cuit,direccion,telefono,estado,dvh) VALUES(CONCAT('Empresa ',@n),CONCAT('3000000000',@n),'Calle 123','55555555',1,'');
 DECLARE @e int=SCOPE_IDENTITY();
 INSERT dbo.Cliente(idUsuario,idEmpresa,nombre,apellido,telefono,tipoCliente,estado,dvh) VALUES(@u,@e,'Cliente','Prueba','55555555','Individual',1,'');
 DECLARE @c int=SCOPE_IDENTITY();
 INSERT dbo.[Plan](nombre,descripcion,precio,estado,dvh) VALUES(CONCAT('Plan integridad ',@n),'Descripcion',10,1,'');
 DECLARE @p int=SCOPE_IDENTITY();
 INSERT dbo.Cobertura(idPlan,prestacion,detalle,estado,dvh) VALUES(@p,CONCAT('Prestacion ',@n),'Detalle',1,'');
 INSERT dbo.Mascota(idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh) VALUES(@c,'Mascota','Perro','Mestizo','20200101',1,'');
 DECLARE @m int=SCOPE_IDENTITY();
 INSERT dbo.Suscripcion(idCliente,idPlan,fechaAlta,fechaVencimiento,estado,dvh) VALUES(@c,@p,'20260101','20261231','Activa','');
 DECLARE @s int=SCOPE_IDENTITY();
 INSERT dbo.Pago(idSuscripcion,importe,medioPago,fechaPago,estado,dvh) VALUES(@s,10,'Tarjeta de credito','20261022','Aprobado','');
 DECLARE @pago int=SCOPE_IDENTITY();
 INSERT dbo.DetallePago(idPago,precioMascota,precioMascotaAdicional,cantidadMascotas,titular,dvh) VALUES(@pago,10,8,1,'Titular','');
 INSERT dbo.Factura(numeroFactura,idPago,idCliente,idSuscripcion,idPlan,clienteNombre,clienteCorreo,planNombre,concepto,periodoDesde,periodoHasta,fechaEmision,precioMascota,precioMascotaAdicional,cantidadMascotas,titular,importe,moneda,medioPago,estado,dvh)
 VALUES(CONCAT('TEST-',@n),@pago,@c,@s,@p,'Cliente Prueba',CONCAT('integridad',@n,'@test.local'),'Plan prueba','Concepto','20261001','20261031',SYSDATETIME(),10,8,1,'Titular',10,'USD','Tarjeta de credito','Emitida','');
 INSERT dbo.Turno(idMascota,fecha,horario,prestacion,zona,estado,dvh) VALUES(@m,'20261201','10:00','Consulta',CONCAT('Zona ',@n),'Confirmado','');
 INSERT dbo.HistorialClinico(idMascota,fechaRegistro,diagnostico,tratamiento,observaciones,dvh) VALUES(@m,GETDATE(),'Diagnostico','Tratamiento','Observacion','');
 INSERT dbo.DatosClinicos(idMascota,peso,temperatura,frecuenciaCardiaca,observaciones,dvh) VALUES(@m,10,38,80,'Observacion','');
 INSERT dbo.Reintegro(idCliente,idMascota,fechaSolicitud,importe,estado,detalle,dvh) VALUES(@c,@m,GETDATE(),10,'Pendiente','Detalle','');
 INSERT dbo.AgendaTurno(zona,prestacion,horaInicio,horaFin,duracionMinutos,horizonteDias,estado,dvh) VALUES(CONCAT('Zona ',@n),'Consulta','09:00','18:00',30,30,1,'');
 INSERT dbo.Bitacora(idUsuario,fecha,accion,modulo,criticidad,dvh) VALUES(@u,GETDATE(),'Evento de prueba','Prueba','Informativa','');
 SET @n+=1;
END;");
        var hash=new IntegridadBLL();var users=new UsuarioDAL();
        foreach(var user in users.ObtenerParaIntegridad()) users.ActualizarDVH(user.IdUsuario,hash.CalcularDVH(user));
        new DVVDAL().Guardar(new DVVBE{NombreTabla="Usuario",ValorDVV=hash.CalcularDVV(users.ObtenerDVHs())});
        var logs=new BitacoraDAL();
        foreach(var log in logs.ObtenerParaIntegridad())Sql("UPDATE dbo.Bitacora SET dvh='"+hash.CalcularDVH(log)+"' WHERE idBitacora="+log.IdBitacora);
        new DVVDAL().Guardar(new DVVBE{NombreTabla="Bitacora",ValorDVV=hash.CalcularDVV(logs.ObtenerDVHs())});
        BasePruebas.ActualizarControles();
        typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(new ReferenciaIntegridadBLL(),null);
    }
    static void CorruptThree(string table,string key,string field)
    {
        using(var transaction=new OperacionIntegridadDAL())
        {
            string filtro=table=="Usuario" || table=="Rol" ? " WHERE idRol<>(SELECT idRol FROM dbo.Usuario WHERE idUsuario=3)" : table=="PermisoComponente" ? " WHERE idComponente<>(SELECT idRol FROM dbo.Usuario WHERE idUsuario=3)" : "";
            var rows=Read("SELECT TOP(3) ["+key+"] FROM dbo.["+table+"]"+filtro+" ORDER BY ["+key+"]");
            Check(rows.Rows.Count==3,table+": tres registros de prueba disponibles");
            foreach(DataRow row in rows.Rows)
            {
                using(var c=new SqlConnection(connection))using(var cmd=new SqlCommand("UPDATE dbo.["+table+"] SET ["+field+"]=@value WHERE ["+key+"]=@id",c))
                {cmd.Parameters.AddWithValue("@value","ALTERADO_P4_"+row[0]);cmd.Parameters.AddWithValue("@id",row[0]);c.Open();Check(cmd.ExecuteNonQuery()==1,table+": fila alterada "+row[0]);}
            }
            var result=security.DiagnosticarIntegridad();
            Check(!result.EsValida,table+": rechaza los datos alterados");
            foreach(DataRow row in rows.Rows)
                Check(result.Detalles.Any(d=>d.Tabla==table&&d.Fila==Convert.ToString(row[0])&&d.Columna==field),table+": identifica fila "+row[0]+" y campo "+field);
            if(audit)
            {
                if(table=="PermisoComponente" || table=="Rol" || table=="RolPermiso")
                    Check(new PerfilBLL().PantallasActuales().SequenceEqual(new[]{"BaseDatos"}),table+": menú de recuperación sigue disponible sin recorrer el Composite alterado");
                var snapshot=new ContingenciaDatosDAL().ExportarXml();
                var saved=new BitacoraBLL().RegistrarErroresIntegridad();
                var consulta=new ConsultaBitacoraBLL();
                var events=consulta.Consultar(new FiltroBitacoraBE { Criticidad="Grave" });
                Check(consulta.SoloHistoricoIntegridad&&events.All(e=>e.Modulo=="Integridad"&&security.VerificarDVH(e)),table+": consulta únicamente el diagnóstico XML verificado");
                foreach(DataRow row in rows.Rows)
                    Check(events.Any(e=>e.IdUsuario==3&&e.Accion.Contains("Tabla="+table+"; Fila="+row[0]+"; Campo="+field+";")),table+": error de fila "+row[0]+" guardado en bitácora");
                Check(!saved.EsValida&&snapshot==new ContingenciaDatosDAL().ExportarXml(),table+": registrar el diagnóstico no escribe ni repara datos o verificadores SQL");
                Check(!new ContingenciaWebMasterBLL().RequiereAccesoExterno(),table+": conserva el ingreso normal del Webmaster confiable aunque haya cuenta de emergencia");
                if(table=="Bitacora")
                {
                    string path=ConfigurationManager.AppSettings["RutaBitacoraXml"];
                    string original=File.ReadAllText(path);
                    try
                    {
                        File.WriteAllText(path,original.Replace("ER023","ER099"));
                        bool rejected=false;
                        try{new BitacoraBLL().RegistrarErroresIntegridad();}catch(BitacoraException){rejected=true;}
                        Check(rejected,"No se agrega ni se presenta como verificado un histórico XML alterado");
                    }
                    finally{File.WriteAllText(path,original);}
                }
            }
        }
        Check(security.VerificarIntegridad(),table+": rollback conserva la base sana");
    }
    static void Control(string table,string sql)
    {
        using(var transaction=new OperacionIntegridadDAL())
        {
            Sql(sql);
            var result=security.DiagnosticarIntegridad();
            Check(!result.EsValida&&result.Detalles.Any(d=>d.Tabla==table),table+": alteración de control detectada");
        }
        Check(security.VerificarIntegridad(),table+": rollback del control conserva integridad");
    }
    public static void Run(string target,bool comprobarBitacora=false)
    {
        connection=target;
        audit=comprobarBitacora;
        if(!new SqlConnectionStringBuilder(target).InitialCatalog.StartsWith("VitaPet_Verificacion_Refactor_",StringComparison.Ordinal))throw new Exception("Solo se permite la base desechable de la suite.");
        Seed();Check(security.VerificarIntegridad(),"Fixture con datos sanos para todas las tablas");
        if(audit)
        {
            Program.Sesion(3);
            new ContingenciaWebMasterBLL().Configurar("emergencia@test.local","Emergencia2026!");
            Program.Sesion(3);
            Check(!new ContingenciaWebMasterBLL().RequiereAccesoExterno(),"Una cuenta de emergencia configurada no desplaza al Webmaster confiable");
        }
        var fields=new Dictionary<string,string>{
            {"Empresa","direccion"},{"Cliente","apellido"},{"Plan","descripcion"},{"Cobertura","detalle"},
            {"Suscripcion","estado"},{"Pago","medioPago"},{"Mascota","nombre"},{"Turno","prestacion"},
            {"HistorialClinico","observaciones"},{"Reintegro","detalle"},{"ErrorSistema","descripcion"},
            {"DatosClinicos","observaciones"},{"AgendaTurno","prestacion"},{"DetallePago","titular"},{"Factura","concepto"},
            {"Usuario","nombre"},{"Bitacora","accion"},{"PermisoComponente","nombre"},
            {"Rol","dvh"},{"RolPermiso","dvh"}};
        var keys=IntegridadTablasDAL.Claves.ToDictionary(p=>p.Key,p=>p.Value);keys.Add("Usuario","idUsuario");keys.Add("Bitacora","idBitacora");
        var controls=new[]{"DVV","ReferenciaIntegridad","__FormatoIntegridad","__SchemaMigrations"};
        var actual=Read("SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo')").Rows.Cast<DataRow>().Select(r=>(string)r[0]).OrderBy(n=>n).ToArray();
        Check(actual.SequenceEqual(keys.Keys.Concat(controls).OrderBy(n=>n)),"Inventario SQL completo: "+keys.Count+" tablas de datos y 4 de control");
        foreach(var table in keys.Keys)CorruptThree(table,keys[table],fields[table]);
        using(var transaction=new OperacionIntegridadDAL())
        {
            foreach(var table in new[]{"Usuario","Mascota","Factura"})
                Check(Sql("UPDATE dbo.["+table+"] SET ["+fields[table]+"]='ALTERADO_CONJUNTO' WHERE ["+keys[table]+"] IN (SELECT TOP(3) ["+keys[table]+"] FROM dbo.["+table+"] ORDER BY ["+keys[table]+"])")==3,table+": tres filas alteradas en la misma prueba");
            var result=security.DiagnosticarIntegridad();
            foreach(var table in new[]{"Usuario","Mascota","Factura"})
                Check(!result.EsValida&&result.Detalles.Count(d=>d.Tabla==table&&d.Columna==fields[table])==3,table+": identifica sus tres filas entre nueve alteraciones simultáneas");
        }
        Check(security.VerificarIntegridad(),"Rollback de nueve alteraciones simultáneas conserva integridad");
        Control("DVV","UPDATE TOP(3) dbo.DVV SET valorDVV=REPLICATE('0',64)");
        Control("ReferenciaIntegridad","UPDATE TOP(3) dbo.ReferenciaIntegridad SET huella=REPLICATE('0',64)");
        Control("__FormatoIntegridad","UPDATE dbo.__FormatoIntegridad SET formato='alterado'");
        Control("__SchemaMigrations","UPDATE TOP(3) dbo.__SchemaMigrations SET checksum=REPLICATE('0',64)");
        Control("__SchemaMigrations","DELETE FROM dbo.__SchemaMigrations WHERE migrationId='028_OptimizarLecturas.sql'");
        Control("__SchemaMigrations","INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES('999_Desconocida.sql',REPLICATE('0',64))");
        using(var transaction=new OperacionIntegridadDAL())
        {Sql("CREATE TABLE dbo.TablaSinCobertura(id int NOT NULL)");Check(!security.VerificarIntegridad(),"Tabla nueva sin cobertura no pasa inadvertida");}
        Check(security.VerificarIntegridad(),"Base íntegra al finalizar todas las pruebas");
        if(audit)
        {
            Program.Sesion(1);
            bool denied=false;
            try{new BitacoraBLL().RegistrarErroresIntegridad();}catch(UnauthorizedAccessException){denied=true;}
            Check(denied,"Un Cliente no puede generar un diagnóstico privilegiado");
            Program.Sesion(3);
            using(var transaction=new OperacionIntegridadDAL())
            {Sql("UPDATE dbo.Usuario SET nombre='Webmaster alterado' WHERE idUsuario=3");Check(new ContingenciaWebMasterBLL().RequiereAccesoExterno(),"Webmaster no confiable deriva al acceso externo");}
            var finalEvents=new ConsultaBitacoraBLL().Consultar(new FiltroBitacoraBE { Criticidad="Grave" });
            Check(finalEvents.Any(e=>e.Modulo=="Integridad")&&security.VerificarIntegridad(),"Los diagnósticos persisten y se integran a la consulta normal al recuperar la integridad");
        }
        Console.WriteLine("TOTAL: "+checks+" verificaciones de cobertura de integridad");
    }
}
