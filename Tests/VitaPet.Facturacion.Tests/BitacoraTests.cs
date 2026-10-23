using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.SessionState;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;

static class BitacoraTests
{
    static int checks;
    static readonly SecurityManager seguridad = new SecurityManager();
    static readonly IntegridadBLL integridad = new IntegridadBLL();
    static void Check(bool ok, string texto) { if (!ok) throw new Exception(texto); checks++; Console.WriteLine("PASS " + texto); }
    static void Sql(string sql)
    { using (var c = new ConexionDAL().CrearConexion()) using (var cmd = new SqlCommand(sql, c)) { c.Open(); cmd.ExecuteNonQuery(); } }
    static void Referencias() => typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(new ReferenciaIntegridadBLL(), null);
    static void Sesion(int id)
    {
        var ctx = new HttpContext(new HttpRequest("", "http://localhost/", ""), new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx, new HttpSessionStateContainer(Guid.NewGuid().ToString(), new SessionStateItemCollection(), new HttpStaticObjectsCollection(), 20, true, HttpCookieMode.UseCookies, SessionStateMode.InProc, false));
        HttpContext.Current = ctx;
        if (id > 0) SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }
    static void Error(Action accion, string codigo)
    { bool rechazo = false; try { accion(); } catch (BitacoraException e) { rechazo = e.Codigo == codigo; } Check(rechazo, "Rechaza " + codigo); }

    // Simula el paso del tiempo sobre una base exclusiva de pruebas, sin alterar el reloj del equipo.
    public static void Semillar()
    {
        string db = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString).InitialCatalog;
        if (!db.StartsWith("VitaPet_Verificacion_Rework_", StringComparison.Ordinal)) throw new Exception("Sólo base aislada.");
        using (var op = new OperacionIntegridadDAL())
        {
            seguridad.ExigirIntegridad();
            var dal = new BitacoraDAL();
            for (int i = 0; i < 65; i++)
            {
                var r = new BitacoraBE { IdUsuario = i % 2 == 0 ? (int?)null : 3,
                    Fecha = new System.Data.SqlTypes.SqlDateTime(DateTime.Now.AddDays(i < 5 ? -31 : i < 10 ? -29 : 0).AddSeconds(i)).Value,
                    Accion = "P26 Evento " + i + " & revisión <histórica>", Modulo = "Prueba P26",
                    Criticidad = new[] { "Informativa", "Advertencia", "Grave", "Crítica" }[i % 4] };
                r.DVH = integridad.CalcularDVH(r);
                dal.Registrar(r, new DVVBE { NombreTabla = "Bitacora", ValorDVV = integridad.CalcularDVV(dal.ObtenerDVHs().Concat(new[] { r.DVH })) });
            }
            Referencias(); op.Complete();
        }
    }

    static void CompatibilidadXml(string xml, bool antiguo)
    {
        string temporal = "VitaPet_Verificacion_" + Guid.NewGuid().ToString("N");
        try
        {
            DatabaseMigrator.CrearTemporalContingencia(temporal);
            new ContingenciaDatosDAL().ImportarTemporal(temporal, xml);
            using (ConexionDAL.UsarBaseTemporal(temporal))
                Check(seguridad.VerificarIntegridad() && (antiguo ? new BitacoraDAL().ObtenerParaIntegridad().All(r => !r.Archivado) : new BitacoraDAL().ObtenerParaIntegridad().Count(r => r.Archivado) == 6),
                    "Recuperación XML " + (antiguo ? "anterior a 035 conserva verificadores" : "conserva bajas lógicas e integridad"));
        }
        finally { new BackupDAL().EliminarTemporal(temporal); }
    }

    static void CompatibilidadBackup(string xmlAnterior = null)
    {
        var backup = new BackupDAL();
        string origen = "VitaPet_Verificacion_" + Guid.NewGuid().ToString("N"), restaurada = null;
        string archivo = Path.Combine(Directory.GetCurrentDirectory(), "VitaPet Rework", "App_Data", "Backups", origen + ".bak");
        try
        {
            if (xmlAnterior != null)
            {
                DatabaseMigrator.CrearTemporalContingencia(origen);
                new ContingenciaDatosDAL().ImportarTemporal(origen, xmlAnterior);
                using (ConexionDAL.UsarBaseTemporal(origen))
                {
                    Sql("ALTER TABLE dbo.Bitacora DROP CONSTRAINT DF_Bitacora_Archivado; ALTER TABLE dbo.Bitacora DROP COLUMN archivado; DELETE dbo.__SchemaMigrations WHERE migrationId='035_BitacoraBajaLogica.sql';");
                    using (var reader = new StreamReader(typeof(DatabaseMigrator).Assembly.GetManifestResourceStream("VitaPet.DAL.Migrations.013_ProcedimientosSimples.sql")))
                    foreach (string batch in Regex.Split(reader.ReadToEnd(), @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                    {
                        var match = Regex.Match(batch, @"CREATE OR ALTER PROCEDURE dbo\.sp_Bitacora_(Eliminar|ObtenerParaIntegridad|ObtenerExcedentes)\b");
                        if (match.Success) Sql(batch.Substring(match.Index));
                    }
                }
                backup.CrearDesdeTemporal(origen, archivo);
            }
            else backup.Crear(archivo);
            backup.VerificarArchivo(archivo); restaurada = backup.CrearTemporal(archivo);
            using (ConexionDAL.UsarBaseTemporal(restaurada))
            {
                Check(seguridad.DiagnosticarIntegridad(true).EsValida, "Backup SQL " + (xmlAnterior != null ? "anterior a 035" : "actual") + " íntegro antes de migrar");
                new MigracionRolesBLL().Ejecutar();
                Check(seguridad.VerificarIntegridad() && (xmlAnterior != null ? new BitacoraDAL().ObtenerParaIntegridad().All(r => !r.Archivado) : new BitacoraDAL().ObtenerParaIntegridad().Count(r => r.Archivado) == 6), "Backup SQL restaurado admite la migración y conserva el estado de las bajas");
            }
        }
        finally
        {
            if (restaurada != null) backup.EliminarTemporal(restaurada);
            if (xmlAnterior != null) backup.EliminarTemporal(origen);
            if (File.Exists(archivo)) File.Delete(archivo);
        }
    }

    public static void Run(string runtime)
    {
        Semillar(); Sesion(3);
        var dal = new BitacoraDAL(); var bll = new BitacoraBLL(); var consulta = new ConsultaBitacoraBLL();
        int total = dal.ObtenerParaIntegridad().Count;
        var datos = new DataSet();
        using (var reader = new StringReader(new ContingenciaDatosDAL().ExportarXml())) datos.ReadXml(reader, XmlReadMode.ReadSchema);
        datos.Tables["Bitacora"].Columns.Remove("archivado");
        foreach (DataRow row in datos.Tables["__SchemaMigrations"].Rows.Cast<DataRow>().Where(r => (string)r["migrationId"] == "035_BitacoraBajaLogica.sql").ToArray()) datos.Tables["__SchemaMigrations"].Rows.Remove(row);
        using (var writer = new StringWriter())
        {
            datos.WriteXml(writer, XmlWriteMode.WriteSchema); string anterior = writer.ToString();
            CompatibilidadXml(anterior, true); CompatibilidadBackup(anterior);
        }
        Check(bll.Mantener() == 5, "Archiva automáticamente sólo los cinco eventos mayores a 30 días");
        var registros = dal.ObtenerParaIntegridad();
        Check(registros.Count == total && registros.Count(r => r.Archivado) == 5, "Baja lógica: no elimina físicamente ningún evento SQL");
        Check(registros.Count(r => !r.Archivado && r.Modulo == "Prueba P26") == 60, "Conserva más de 50 eventos de los últimos 30 días");
        Check(seguridad.VerificarIntegridad() && registros.All(seguridad.VerificarDVH), "Baja, DVH, DVV y referencias permanecen íntegros");
        string archivo = ConfigurationManager.AppSettings["RutaBitacoraXml"];
        var xml = new XmlDocument(); xml.Load(archivo);
        Check(xml.SelectNodes("/Bitacora/Registro").Count == 5 && xml.InnerText.Contains("& revisión <histórica>"), "Histórico XML completo y caracteres especiales conservados");
        Check(bll.Mantener() == 0 && dal.ObtenerParaIntegridad().Count == total, "Mantenimiento repetido es idempotente");
        var todos = consulta.Consultar(new FiltroBitacoraBE());
        Check(todos.Count(r => r.Modulo == "Prueba P26") == 65, "Consulta reúne activos e histórico sin duplicar SQL y XML");
        Check(todos.Where(r => r.Modulo == "Prueba P26").Select(r => r.IdBitacora).Distinct().Count() == 65, "Identificadores de eventos se conservan tras la baja");
        Check(todos.SequenceEqual(todos.OrderByDescending(r => r.Fecha).ThenByDescending(r => r.IdBitacora)), "Consulta ordenada por fecha e identificador descendentes");
        foreach (string nivel in new[] { "Informativa", "Advertencia", "Grave", "Crítica" })
            Check(consulta.Consultar(new FiltroBitacoraBE { Criticidad = nivel }).All(r => r.Criticidad == nivel), "Filtro de criticidad: " + nivel);
        DateTime dia = DateTime.Today.AddDays(-31);
        var antiguos = consulta.Consultar(new FiltroBitacoraBE { FechaDesde = dia, FechaHasta = dia });
        Check(antiguos.Count == 5 && antiguos.All(r => r.Fecha.Date == dia && r.Archivado), "Filtro inclusivo de un día permite consultar los cinco históricos");
        Check(consulta.Consultar(new FiltroBitacoraBE { FechaDesde = dia, FechaHasta = dia, Criticidad = "Grave" }).Count == 1, "Combina fechas y criticidad en el histórico");
        Check(consulta.Consultar(new FiltroBitacoraBE { FechaDesde = DateTime.Today.AddYears(1) }).Count == 0, "Filtro sin coincidencias devuelve vacío");
        Error(() => consulta.Consultar(new FiltroBitacoraBE { FechaDesde = DateTime.Today, FechaHasta = dia }), "ER027");
        Error(() => consulta.Consultar(new FiltroBitacoraBE { Criticidad = "Inventada" }), "ER025");
        var descarga = new XmlDocument(); descarga.Load(new MemoryStream(consulta.ExportarXml(antiguos)));
        Check(descarga.SelectNodes("/Bitacora/Registro[Archivado='true']").Count == 5, "Exporta todo el histórico filtrado con su marca de baja");
        string respaldo = File.ReadAllText(archivo);
        try
        {
            File.WriteAllBytes(archivo, consulta.ExportarXml(antiguos));
            Check(consulta.Consultar(new FiltroBitacoraBE { FechaDesde = dia, FechaHasta = dia }).Count == 5, "XML exportado puede releerse sin perder integridad ni duplicar");
            File.WriteAllText(archivo, respaldo.Replace("P26 Evento", "Alteración"));
            Error(() => consulta.Consultar(new FiltroBitacoraBE()), "ER023");
            File.WriteAllText(archivo, "<!DOCTYPE Bitacora [<!ENTITY prueba SYSTEM 'file:///no-leer'>]><Bitacora>&prueba;</Bitacora>");
            Error(() => consulta.Consultar(new FiltroBitacoraBE()), "ER031");
        }
        finally { File.WriteAllText(archivo, respaldo); }
        var archivado = registros.First(r => r.Archivado);
        using (var op = new OperacionIntegridadDAL())
        { Sql("UPDATE dbo.Bitacora SET archivado=0 WHERE idBitacora=" + archivado.IdBitacora); Check(!seguridad.VerificarIntegridad(), "Detecta alteración de la marca de baja lógica"); }
        Check(seguridad.VerificarIntegridad(), "Rollback conserva las referencias previas");
        foreach (int id in new[] { 0, 1, 2 })
        {
            Sesion(id); bool rechazo = false; try { consulta.Consultar(new FiltroBitacoraBE()); } catch (UnauthorizedAccessException) { rechazo = true; }
            Check(rechazo, "Rechaza consulta sin privilegio Webmaster: " + id);
        }
        Sesion(3);
        // Un fallo SQL posterior a escribir el XML debe permitir reintentar sin duplicaciones.
        using (var op = new OperacionIntegridadDAL())
        {
            var r = dal.ObtenerParaIntegridad().First(x => !x.Archivado && x.Modulo == "Prueba P26");
            r.Fecha = r.Fecha.AddDays(-40); r.DVH = integridad.CalcularDVH(r);
            using (var c = new ConexionDAL().CrearConexion()) using (var cmd = new SqlCommand("UPDATE dbo.Bitacora SET fecha=@fecha,dvh=@dvh WHERE idBitacora=@id", c))
            { cmd.Parameters.AddWithValue("@fecha", r.Fecha); cmd.Parameters.AddWithValue("@dvh", r.DVH); cmd.Parameters.AddWithValue("@id", r.IdBitacora); c.Open(); cmd.ExecuteNonQuery(); }
            new DVVDAL().Guardar(new DVVBE { NombreTabla = "Bitacora", ValorDVV = integridad.CalcularDVV(dal.ObtenerDVHs()) }); Referencias(); op.Complete();
        }
        respaldo = File.ReadAllText(archivo);
        try
        {
            File.WriteAllText(archivo, respaldo.Replace("P26 Evento", "Alteración"));
            Error(() => bll.Mantener(), "ER023");
            Check(dal.ObtenerParaIntegridad().Count(r => r.Archivado) == 5 && seguridad.VerificarIntegridad(), "XML alterado impide la baja sin modificar SQL ni sobrescribir evidencia");
        }
        finally { File.WriteAllText(archivo, respaldo); }
        File.Move(archivo, archivo + ".previo"); Directory.CreateDirectory(archivo);
        try
        {
            Error(() => bll.Mantener(), "ER031");
            Check(dal.ObtenerParaIntegridad().Count(r => r.Archivado) == 5 && seguridad.VerificarIntegridad(), "Histórico inaccesible conserva los eventos activos e integridad");
        }
        finally { Directory.Delete(archivo); File.Move(archivo + ".previo", archivo); }
        using (var bloqueo = new FileStream(archivo, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Error(() => bll.Mantener(), "ER033");
            Check(dal.ObtenerParaIntegridad().Count(r => r.Archivado) == 5 && seguridad.VerificarIntegridad(), "Fallo de reemplazo atómico conserva el XML anterior y las filas activas");
        }
        Sql("CREATE TRIGGER dbo.P26FallaArchivo ON dbo.Bitacora AFTER UPDATE AS IF UPDATE(archivado) THROW 50050,'Falla P26',1;");
        try { bool fallo = false; try { bll.Mantener(); } catch (SqlException) { fallo = true; } Check(fallo && seguridad.VerificarIntegridad(), "Fallo SQL revierte baja, DVH, DVV y referencias"); }
        finally { Sql("DROP TRIGGER dbo.P26FallaArchivo"); }
        int antes = dal.ObtenerParaIntegridad().Count;
        Check(bll.Mantener() == 1 && seguridad.VerificarIntegridad(), "Reintento posterior a rollback completa la baja");
        Check(consulta.Consultar(new FiltroBitacoraBE()).Count(r => r.Modulo == "Prueba P26") == 65, "XML de intento fallido no duplica eventos");
        Check(dal.ObtenerParaIntegridad().Count >= antes, "Reintento conserva todas las filas SQL");
        string actual = new ContingenciaDatosDAL().ExportarXml();
        CompatibilidadXml(actual, false);
        CompatibilidadBackup();
        Console.WriteLine("TOTAL: " + checks + " verificaciones de tratamiento de bitácora");
    }
}
