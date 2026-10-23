using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;

class Program
{
    static string connection;
    static int checks;
    static SecurityManager seguridad;
    static void Check(bool ok,string mensaje)
    { if(!ok)throw new Exception(mensaje);checks++;Console.WriteLine("PASS "+mensaje); }
    static void Sql(string sql)
    { using(var cn=new SqlConnection(connection))using(var cmd=new SqlCommand(sql,cn)){cn.Open();cmd.ExecuteNonQuery();} }
    static string Valor(string sql)
    { using(var cn=new SqlConnection(connection))using(var cmd=new SqlCommand(sql,cn)){cn.Open();return Convert.ToString(cmd.ExecuteScalar(),CultureInfo.InvariantCulture);} }
    internal static void Sesion(int id)
    {
        var contexto=new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(contexto,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current=contexto;
        if(id>0)SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }
    static void Referencias()
    { typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(new ReferenciaIntegridadBLL(),null); }
    static void Falla<T>(Action accion,string mensaje) where T:Exception
    {
        try { accion(); }
        catch(T){Check(true,mensaje);return;}
        throw new Exception("No se produjo el error: "+mensaje);
    }
    static string DatosUsuario(UsuarioBE u) { return ContingenciaArchivoDAL.Serializar(u); }
    static RegistroMascotaBE Mascota(string nombre="Luna")
    { return new RegistroMascotaBE {Nombre=nombre,Especie="Perro",Raza="Mestizo",FechaNacimiento=new DateTime(2020,1,1)}; }

    static void Usuarios()
    {
        var dal=new UsuarioDAL();var bll=new UsuarioBLL();
        foreach(var u in dal.ObtenerParaIntegridad())
            Check(DatosUsuario(u)==DatosUsuario(dal.ObtenerPorId(u.IdUsuario))&&DatosUsuario(u)==DatosUsuario(dal.ObtenerPorCorreo(u.CorreoElectronico)),"Las tres lecturas preservan todos los campos de usuario "+u.IdUsuario);
        Check(dal.ObtenerPorId(-1)==null&&dal.ObtenerPorCorreo("ausente@test.local")==null,"Lecturas de usuario ausente conservan null");
        var usuario=dal.ObtenerPorId(1);
        bll.RegistrarIntentoFallido(usuario);Check(usuario.IntentosFallidos==1&&!usuario.Bloqueado&&seguridad.VerificarIntegridad(),"Primer intento y verificadores");
        bll.RegistrarIngresoExitoso(usuario);Check(usuario.IntentosFallidos==0&&seguridad.VerificarIntegridad(),"Ingreso exitoso reinicia intentos");
        for(int i=1;i<=4;i++)
        {
            bll.RegistrarIntentoFallido(usuario);
            Check(usuario.IntentosFallidos==Math.Min(i,3)&&usuario.Bloqueado==(i>=3)&&seguridad.VerificarIntegridad(),"State conserva límite y bloqueo: intento "+i);
        }
        bll.RegistrarIngresoExitoso(usuario);Check(usuario.Bloqueado&&usuario.IntentosFallidos==3,"Ingreso sobre estado bloqueado conserva bloqueo");
        bll.DesbloquearUsuario(1);Check(!dal.ObtenerPorId(1).Bloqueado&&seguridad.VerificarIntegridad(),"Desbloqueo conserva DVH, DVV y referencias");
        Falla<ArgumentNullException>(()=>bll.RegistrarIntentoFallido(null),"Intento sin usuario conserva validación");
        Falla<ArgumentOutOfRangeException>(()=>bll.DesbloquearUsuario(0),"Desbloqueo inválido conserva validación");
        Falla<InvalidOperationException>(()=>bll.RegistrarIngresoExitoso(new UsuarioBE {IdUsuario=-1}),"Ingreso de usuario inexistente conserva error");
        Sesion(1);
        Sql("CREATE TRIGGER dbo.RefactorReferenciasSinCambios ON dbo.ReferenciaIntegridad AFTER UPDATE AS IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON i.tabla=d.tabla AND i.fila=d.fila AND i.columna=d.columna WHERE i.huella=d.huella) THROW 50901,'Se reescribio una referencia sin cambios.',1;");
        try { bll.ActualizarIdioma(1,"en");Check(dal.ObtenerPorId(1).Idioma=="en"&&seguridad.VerificarIntegridad(),"Cambio de idioma actualiza solo referencias modificadas");bll.ActualizarIdioma(1,"es"); }
        finally { Sql("DROP TRIGGER dbo.RefactorReferenciasSinCambios"); }
        Sql("CREATE TRIGGER dbo.RefactorReferenciasInmutables ON dbo.ReferenciaIntegridad AFTER INSERT,UPDATE,DELETE AS THROW 50902,'Se escribieron referencias sin cambios.',1;");
        try { using(var tx=new OperacionIntegridadDAL()){Referencias();tx.Complete();}Check(seguridad.VerificarIntegridad(),"Sin cambios: cero escrituras de referencias, incluido el sello"); }
        finally { Sql("DROP TRIGGER dbo.RefactorReferenciasInmutables"); }
        string anterior=DatosUsuario(dal.ObtenerPorId(1));string logs=Valor("SELECT COUNT(*) FROM dbo.Bitacora");
        Sql("CREATE TRIGGER dbo.RefactorFallaDVV ON dbo.DVV AFTER INSERT,UPDATE AS THROW 50903,'Fallo de DVV.',1;");
        try
        {
            Falla<SqlException>(()=>bll.RegistrarIntentoFallido(dal.ObtenerPorId(1)),"Fallo de DVV propaga error de estado");
            Falla<SqlException>(()=>new RecuperacionContrasenaBLL().ActualizarContrasena(1,"Otra2026!"),"Fallo de DVV propaga error de contraseña");
            Falla<SqlException>(()=>seguridad.RegistrarBitacora(new BitacoraBE {Fecha=DateTime.Now,Accion="Rollback",Modulo="Prueba",Criticidad="Informativa"}),"Fallo de DVV propaga error de bitácora");
        }
        finally { Sql("DROP TRIGGER dbo.RefactorFallaDVV"); }
        Check(anterior==DatosUsuario(dal.ObtenerPorId(1))&&logs==Valor("SELECT COUNT(*) FROM dbo.Bitacora")&&seguridad.VerificarIntegridad(),"Las tres fallas revierten datos y verificadores en la conexión original");
    }

    static void Modulos()
    {
        Sesion(1);
        var mascotas=new MascotaBLL();int id=new RegistroMascotaBLL().Registrar(Mascota());
        var original=mascotas.Seleccionar(id);mascotas.Actualizar(id,original.DVH,Mascota("Luna nueva"));
        Check(mascotas.Consultar(id).Nombre=="Luna nueva"&&seguridad.VerificarIntegridad(),"Registro y actualización de mascota reutilizan controles");
        Falla<GestionMascotaException>(()=>mascotas.Actualizar(id,original.DVH,Mascota()),"Edición desactualizada de mascota sigue rechazada");
        int otro=new ClienteBLL().RegistrarCliente(new RegistroClienteBE {Nombre="Otro",Apellido="Cliente",CorreoElectronico="otro@test.local",Telefono="55555555",Contrasena="Prueba2026!"});
        Sesion(otro);Falla<GestionMascotaException>(()=>mascotas.Consultar(id),"Mascota ajena sigue protegida");
        Sesion(2);
        var empresas=new EmpresaBLL();var datos=new DatosEmpresaBE {RazonSocial="Empresa prueba",Cuit="30-12345678-9",Direccion="Calle 123",Telefono="55555555"};
        var empresa=empresas.Registrar(datos);datos.Direccion="Calle 456";empresas.Actualizar(empresa.IdEmpresa,datos);
        Check(empresas.Consultar(empresa.IdEmpresa).Direccion=="Calle 456"&&seguridad.VerificarIntegridad(),"Alta y modificación de empresa conservan integridad");
        var corporativo=new ClienteCorporativoBLL().Registrar(new DatosClienteCorporativoBE {IdEmpresa=empresa.IdEmpresa,Nombre="Cliente",Apellido="Empresa",Correo="corp@test.local",Telefono="55555555"});
        Check(new UsuarioDAL().ObtenerPorId(corporativo.IdUsuario).RequiereRestablecerContrasena&&seguridad.VerificarIntegridad(),"Alta corporativa conserva marca, suscripción y controles");
        Falla<UnauthorizedAccessException>(()=>Sesion(corporativo.IdUsuario),"Cuenta corporativa exige restablecimiento");
        new RecuperacionContrasenaBLL().ActualizarContrasena(corporativo.IdUsuario,"Prueba2026!");
        var recuperado=new UsuarioDAL().ObtenerPorId(corporativo.IdUsuario);
        Check(!recuperado.RequiereRestablecerContrasena&&!recuperado.Bloqueado&&seguridad.VerificarIntegridad(),"Restablecimiento elimina referencia opcional y conserva formato histórico");
        Sesion(2);var planes=new PlanBLL();var plan=new DatosPlanBE {Nombre="Plan de prueba",Descripcion="Cobertura de prueba",Precio=22m,Estado=true};
        plan.Coberturas.Add(new DatosCoberturaPlanBE {Prestacion="Consulta",Detalle="Detalle",Estado=true});
        int idPlan=planes.Registrar(plan);var actual=planes.Seleccionar(idPlan);plan.Precio=23m;plan.Coberturas=actual.Coberturas;
        planes.Actualizar(idPlan,plan);Check(planes.Consultar(idPlan).Precio==23m&&seguridad.VerificarIntegridad(),"Administración de plan y coberturas conserva verificadores");
        Sesion(1);var turnos=new SolicitudTurnoBLL();var zona=turnos.Zonas()[0];var disponible=turnos.Disponibilidad(id,zona)[0];
        var turno=turnos.Registrar(id,disponible.IdAgendaTurno,disponible.Fecha,disponible.Horario);
        Check(new ConsultaTurnosBLL().Consultar(id).Turnos.Any(t=>t.IdTurno==turno.IdTurno)&&seguridad.VerificarIntegridad(),"Agenda, reserva y consulta de turnos conservan integridad");
        Check(Encoding.UTF8.GetString(new ConsultaTurnosBLL().ExportarXml(id)).Contains("<Turnos>"),"Exportación de turnos conserva XML");
    }

    static void Procedimientos()
    {
        var assembly=typeof(DatabaseMigrator).Assembly;
        foreach(string recurso in new[]{"026_SimplificacionProcedimientos.sql","027_Facturacion.sql"})
        using(var reader=new StreamReader(assembly.GetManifestResourceStream("VitaPet.DAL.Migrations."+recurso)))
        foreach(string batch in Regex.Split(reader.ReadToEnd(),@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
        {
            var match=Regex.Match(batch,@"CREATE OR ALTER PROCEDURE dbo\.(sp_Mascota_Actualizar|sp_Factura_Emitir)\b");
            if(match.Success)Sql(batch.Substring(match.Index).Replace("dbo."+match.Groups[1].Value,"dbo."+match.Groups[1].Value+"_Anterior"));
        }
        foreach(string proc in new[]{"sp_Mascota_Actualizar","sp_Factura_Emitir"})
        {
            string contrato="SELECT STRING_AGG(CONCAT(name,':',system_type_id,':',max_length,':',precision,':',scale,':',is_output),';') WITHIN GROUP(ORDER BY parameter_id) FROM sys.parameters WHERE object_id=OBJECT_ID('dbo.";
            Check(Valor(contrato+proc+"')")==Valor(contrato+proc+"_Anterior')"),"Contrato de parámetros SQL preservado: "+proc);
        }
        int mascota=int.Parse(Valor("SELECT TOP(1) idMascota FROM dbo.Mascota ORDER BY idMascota"));
        string version=Valor("SELECT dvh FROM dbo.Mascota WHERE idMascota="+mascota);
        foreach(string caso in new[]{"valido","sintransaccion","ajeno","ausente","version","versionnull","incompleto","futuro"})
        {
            string args=(caso=="ajeno"?2:1)+","+(caso=="ausente"?-1:mascota)+","+(caso=="versionnull"?"NULL":"'"+(caso=="version"?"viejo":version)+"'")+","+(caso=="incompleto"?"NULL":"' Otra '")+",' Perro ',' Mestizo ',"+(caso=="futuro"?"'20991231'":"'20200101'");
            Comparar("sp_Mascota_Actualizar",args,caso!="sintransaccion");
        }
        Sesion(1);var contratacion=new ContratacionPlanBLL();var cotizacion=contratacion.ConsultarDetalle(contratacion.ConsultarPlanes()[0].IdPlan);
        cotizacion=contratacion.PrepararContratacion(cotizacion.Plan.IdPlan,cotizacion.Plan.Version,cotizacion.CantidadMascotas);
        var pago=new GestionPagoBLL().Pagar(cotizacion.IdSuscripcion,cotizacion.ImporteUSD,new DatosPagoBE {MedioPago=GestionPagoBLL.Credito,Titular="Prueba Prueba",NumeroTarjeta="1111111111111111",Vencimiento="12/2030",CodigoSeguridad="111",CorreoFactura="factura@example.test"});
        Check(pago.Factura!=null&&seguridad.VerificarIntegridad(),"Procedimiento de factura nuevo emite y protege un comprobante");
        foreach(int usuario in new[]{1,2,-1})foreach(bool transaccion in new[]{false,true})
            Comparar("sp_Factura_Emitir",usuario+","+cotizacion.IdSuscripcion,transaccion);
        string refs=ContingenciaArchivoDAL.Serializar(new ReferenciaIntegridadDAL().Obtener());
        Sql("DELETE dbo.__SchemaMigrations WHERE migrationId='028_OptimizarLecturas.sql'");
        DatabaseMigrator.Migrate();DatabaseMigrator.Migrate();
        Check(refs==ContingenciaArchivoDAL.Serializar(new ReferenciaIntegridadDAL().Obtener())&&seguridad.VerificarIntegridad()&&Valor("SELECT COUNT(*) FROM dbo.__SchemaMigrations WHERE migrationId='028_OptimizarLecturas.sql'")=="1","Migración nueva sobre base poblada es idempotente y conserva referencias");
        var compatible=typeof(BackupBLL).GetMethod("EsquemaCompatible",BindingFlags.Static|BindingFlags.NonPublic);
        Check((bool)compatible.Invoke(null,new object[]{"027_Facturacion.sql:hash;","027_Facturacion.sql:hash;028_OptimizarLecturas.sql:hash;"}),"Respaldos anteriores admiten la migración de procedimientos");
        Check(!(bool)compatible.Invoke(null,new object[]{"027_Facturacion.sql:hash;","027_Facturacion.sql:hash;999_Desconocida.sql:hash;"}),"Migraciones desconocidas siguen rechazadas al restaurar");
        Sql("DROP PROCEDURE dbo.sp_Mascota_Actualizar_Anterior;DROP PROCEDURE dbo.sp_Factura_Emitir_Anterior;");
    }
    static string Ejecutar(string proc,string args,bool transaccion)
    {
        using(var cn=new SqlConnection(connection))
        {
            cn.Open();using(var tx=transaccion?cn.BeginTransaction(System.Data.IsolationLevel.Serializable):null)
            {
                try
                {
                    var salida=new StringBuilder();
                    using(var cmd=new SqlCommand("EXEC dbo."+proc+" "+args,cn,tx))
                    try
                    {
                        using(var r=cmd.ExecuteReader())do
                        {
                            for(int i=0;i<r.FieldCount;i++)salida.Append(r.GetName(i)).Append(':').Append(r.GetDataTypeName(i)).Append('|');
                            while(r.Read())for(int i=0;i<r.FieldCount;i++)salida.Append(Convert.ToString(r[i],CultureInfo.InvariantCulture)).Append('|');
                        }while(r.NextResult());
                    }
                    catch(SqlException ex){salida.Append("ERROR:").Append(ex.Number).Append(':').Append(ex.Message).Append(':').Append(ex.State);}
                    using(var cmd=new SqlCommand("SELECT * FROM dbo.Mascota ORDER BY idMascota FOR XML PATH('Mascota')",cn,tx))salida.Append(cmd.ExecuteScalar());
                    return salida.ToString();
                }
                finally {if(tx!=null&&tx.Connection!=null)tx.Rollback();}
            }
        }
    }
    static void Comparar(string proc,string args,bool transaccion)
    { Check(Ejecutar(proc+"_Anterior",args,transaccion)==Ejecutar(proc,args,transaccion),"Resultados, errores y datos equivalentes: "+proc+" "+args+" tx="+transaccion); }

    static void Bitacora()
    {
        for(int i=0;i<55;i++)seguridad.RegistrarBitacora(new BitacoraBE {IdUsuario=i%2==0?(int?)null:1,Fecha=DateTime.Now,Accion="Evento "+i,Modulo="Prueba",Criticidad="Informativa"});
        var dal=new BitacoraDAL();var registros=dal.ObtenerParaIntegridad();
        Check(registros.Count==50&&registros.Any(r=>r.IdUsuario==null)&&seguridad.VerificarIntegridad(),"Rotación a XML conserva 50 activos y usuarios nulos");
        var excedentes=dal.ObtenerExcedentes(0);
        Check(registros.All(r=>ContingenciaArchivoDAL.Serializar(r)==ContingenciaArchivoDAL.Serializar(excedentes.Single(e=>e.IdBitacora==r.IdBitacora))),"Ambas lecturas de bitácora preservan todos sus campos");
        Sesion(3);var historico=new ConsultaBitacoraBLL().Consultar(new FiltroBitacoraBE());
        Check(historico.Count>=55&&seguridad.VerificarIntegridad(),"Consulta reúne histórico y activos tras la poda de referencias");
        using(var tx=new OperacionIntegridadDAL())
        {Sql("UPDATE dbo.Mascota SET nombre='Alterado' WHERE idMascota=(SELECT MIN(idMascota) FROM dbo.Mascota)");Check(!seguridad.VerificarIntegridad(),"Optimización conserva detección de datos alterados");}
        using(var tx=new OperacionIntegridadDAL())
        {Sql("UPDATE dbo.ReferenciaIntegridad SET huella=REPLICATE('0',64) WHERE tabla='Usuario' AND fila='1' AND columna='nombre'");Check(!seguridad.VerificarIntegridad(),"Optimización conserva detección de referencias alteradas");}
        Check(seguridad.VerificarIntegridad(),"Rollback de pruebas de alteración restaura estado íntegro");
    }

    static void Main(string[] args)
    {
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var destino=new SqlConnectionStringBuilder(cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString);
        if(!destino.InitialCatalog.StartsWith("VitaPet_Verificacion_Refactor",StringComparison.Ordinal))throw new Exception("Solo se permiten bases de pruebas.");
        destino.InitialCatalog="VitaPet_Verificacion_Refactor_"+Guid.NewGuid().ToString("N");connection=destino.ConnectionString;
        string runtime=Path.Combine(Path.GetTempPath(),destino.InitialCatalog);Directory.CreateDirectory(runtime);
        cfg.ConnectionStrings.ConnectionStrings["VitaPetConnection"].ConnectionString=connection;
        cfg.AppSettings.Settings["RutaRecuperacion"].Value=Path.Combine(runtime,"recuperacion");cfg.AppSettings.Settings["RutaBitacoraXml"].Value=Path.Combine(runtime,"Bitacora.xml");
        cfg.Save();ConfigurationManager.RefreshSection("connectionStrings");ConfigurationManager.RefreshSection("appSettings");
        seguridad=new SecurityManager();
        try
        {
            DatabaseMigrator.Migrate("031_AutorizacionPorPantallas.sql");
            Sql("INSERT dbo.Cliente(idUsuario,nombre,apellido,telefono,tipoCliente,estado,dvh) VALUES(1,'Cliente','Prueba','55555555','Individual',1,'');");
            new MigracionRolesBLL().Ejecutar();new PerfilBLL().AsegurarIniciales();new ContratacionPlanBLL().AsegurarCatalogo();
            Check(seguridad.VerificarIntegridad(),"Base aislada inicializada");
            if(args.Contains("--integridad-todas") || args.Contains("--integridad-login"))
            {
                IntegridadTodasLasTablas.Run(connection,args.Contains("--integridad-login"));
                return;
            }
            Usuarios();Modulos();Procedimientos();Bitacora();
            Console.WriteLine("TOTAL: "+checks+" verificaciones de refactorización");
        }
        finally
        {
            HttpContext.Current=null;SqlConnection.ClearAllPools();
            destino.InitialCatalog="master";
            using(var cn=new SqlConnection(destino.ConnectionString))using(var cmd=cn.CreateCommand())
            {cn.Open();string nombre=new SqlConnectionStringBuilder(connection).InitialCatalog;cmd.CommandText="IF DB_ID(@nombre) IS NOT NULL BEGIN ALTER DATABASE ["+nombre+"] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE ["+nombre+"];END";cmd.Parameters.AddWithValue("@nombre",nombre);cmd.ExecuteNonQuery();}
        }
    }
}
