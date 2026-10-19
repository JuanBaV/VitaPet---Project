using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Xml;
namespace VitaPet.DAL
{
    public sealed class ContingenciaDatosDAL
    {
        private static string Q(string value) => "[" + value.Replace("]", "]]") + "]";
        public string ExportarXml()
        {
            using (var c = new ConexionDAL().CrearConexion())
            {
                c.Open();
                var datos = new DataSet("VitaPetContingencia");
                foreach (string tabla in Tablas(c))
                    using (var a = new SqlDataAdapter("SELECT * FROM dbo." + Q(tabla), c)) a.Fill(datos, tabla);
                using (var w = new StringWriter()) { datos.WriteXml(w, XmlWriteMode.WriteSchema); return w.ToString(); }
            }
        }
        private static string[] Tablas(SqlConnection c)
        {
            var t = new DataTable();
            using (var a = new SqlDataAdapter("SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') ORDER BY name", c)) a.Fill(t);
            return t.Rows.Cast<DataRow>().Select(r => (string)r[0]).ToArray();
        }
        // Sólo se carga XML firmado en una base temporal recién creada con las migraciones vigentes.
        public void ImportarTemporal(string nombre, string xml, bool ampliarIntegridad = false, bool ampliarDatosClinicos = false)
        {
            ValidarTemporal(nombre);
            var datos = new DataSet();
            using (var s = new StringReader(xml))
            using (var r = XmlReader.Create(s, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null, MaxCharactersInDocument = 128L * 1024 * 1024 })) datos.ReadXml(r, XmlReadMode.ReadSchema);
            using (ConexionDAL.UsarBaseTemporal(nombre))
            using (var c = new ConexionDAL().CrearConexion())
            {
                c.Open(); var tablas = Tablas(c);
                if(ampliarDatosClinicos && !datos.Tables.Contains("DatosClinicos") && datos.Tables.Contains("DVV") &&
                    datos.Tables.Contains("__SchemaMigrations") &&
                    !datos.Tables["__SchemaMigrations"].Rows.Cast<DataRow>().Any(r=>(string)r["migrationId"]=="021_GestionMascotas.sql") &&
                    !datos.Tables["DVV"].Rows.Cast<DataRow>().Any(r=>(string)r["nombreTabla"]=="DatosClinicos"))
                {
                    using(var a=new SqlDataAdapter("SELECT TOP(0) * FROM dbo.DatosClinicos",c))a.Fill(datos,"DatosClinicos");
                    var fila=datos.Tables["DVV"].NewRow();
                    fila["idDVV"]=datos.Tables["DVV"].Rows.Cast<DataRow>().Select(r=>(int)r["idDVV"]).DefaultIfEmpty(0).Max()+1;
                    fila["nombreTabla"]="DatosClinicos";
                    fila["valorDVV"]="e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
                    datos.Tables["DVV"].Rows.Add(fila);
                }
                if(ampliarIntegridad && !datos.Tables.Contains("AgendaTurno") && datos.Tables.Contains("DVV") &&
                    datos.Tables.Contains("__SchemaMigrations") &&
                    !datos.Tables["__SchemaMigrations"].Rows.Cast<DataRow>().Any(r=>(string)r["migrationId"]=="022_Turnos.sql") &&
                    !datos.Tables["DVV"].Rows.Cast<DataRow>().Any(r=>(string)r["nombreTabla"]=="AgendaTurno"))
                {
                    using(var a=new SqlDataAdapter("SELECT TOP(0) * FROM dbo.AgendaTurno",c))a.Fill(datos,"AgendaTurno");
                    var fila=datos.Tables["DVV"].NewRow();
                    fila["idDVV"]=datos.Tables["DVV"].Rows.Cast<DataRow>().Select(r=>(int)r["idDVV"]).DefaultIfEmpty(0).Max()+1;
                    fila["nombreTabla"]="AgendaTurno";
                    fila["valorDVV"]="e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
                    datos.Tables["DVV"].Rows.Add(fila);
                }
                if (!tablas.OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(datos.Tables.Cast<DataTable>().Select(t => t.TableName).OrderBy(n => n, StringComparer.Ordinal)))
                    throw new InvalidDataException("Las tablas del XML no corresponden al esquema actual.");
                using (var tx = c.BeginTransaction())
                {
                    foreach (string tabla in tablas) Ejecutar(c, tx, "ALTER TABLE dbo." + Q(tabla) + " NOCHECK CONSTRAINT ALL;");
                    foreach (string tabla in tablas)
                    {
                        // Las migraciones y procedimientos provienen del código, nunca del archivo.
                        if (tabla == "__SchemaMigrations") continue;
                        Ejecutar(c, tx, "DELETE FROM dbo." + Q(tabla));
                        var expected = new DataTable();
                        using (var a = new SqlDataAdapter(new SqlCommand("SELECT TOP (0) * FROM dbo." + Q(tabla), c, tx))) a.Fill(expected);
                        var source = datos.Tables[tabla];
                        if (ampliarIntegridad && tabla == "ErrorSistema" && !source.Columns.Contains("dvh"))
                        {
                            source.Columns.Add("dvh", typeof(string));
                            foreach (DataRow row in source.Rows) row["dvh"] = "";
                        }
                        if (expected.Columns.Count != source.Columns.Count || expected.Columns.Cast<DataColumn>().Any(col =>
                            !source.Columns.Contains(col.ColumnName) || source.Columns[col.ColumnName].DataType != col.DataType))
                            throw new InvalidDataException("Columnas XML incompatibles.");
                        using (var bulk = new SqlBulkCopy(c, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.KeepNulls, tx))
                        {
                            bulk.DestinationTableName = "dbo." + Q(tabla); bulk.BulkCopyTimeout = 120;
                            foreach (DataColumn col in source.Columns) bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                            bulk.WriteToServer(source);
                        }
                    }
                    foreach (string tabla in tablas) Ejecutar(c, tx, "ALTER TABLE dbo." + Q(tabla) + " WITH CHECK CHECK CONSTRAINT ALL;");
                    tx.Commit();
                }
            }
        }
        private static void Ejecutar(SqlConnection c, SqlTransaction tx, string sql)
        { using (var cmd = new SqlCommand(sql, c, tx)) { cmd.CommandTimeout = 120; cmd.ExecuteNonQuery(); } }
        public static void ValidarTemporal(string nombre)
        {
            Guid id;
            const string prefijo = "VitaPet_Verificacion_";
            if (nombre == null || !nombre.StartsWith(prefijo, StringComparison.Ordinal) ||
                !Guid.TryParseExact(nombre.Substring(prefijo.Length), "N", out id)) throw new ArgumentException("Base temporal inválida.");
        }
    }
}
