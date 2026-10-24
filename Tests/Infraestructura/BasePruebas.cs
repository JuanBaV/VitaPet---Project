using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.Pruebas
{
    // La preparación de datos sintéticos sólo existe en los ejecutables de pruebas.
    internal static class BasePruebas
    {
        private static SqlConnectionStringBuilder Destino(string nombre = null)
        {
            var b = new SqlConnectionStringBuilder(new ConexionDAL().CrearConexion().ConnectionString);
            if (nombre != null) b.InitialCatalog = nombre;
            if (!Regex.IsMatch(b.InitialCatalog, @"^VitaPet_Verificacion_(?:(?:Rework|Refactor)_)?[a-f0-9]{32}$"))
                throw new InvalidOperationException("La preparación sólo admite bases temporales de pruebas.");
            return b;
        }
        public static string LeerSql(string archivo)
        {
            using (var s = typeof(BasePruebas).Assembly.GetManifestResourceStream("VitaPet.Pruebas.Schema." + archivo))
            using (var r = new StreamReader(s)) return r.ReadToEnd().Replace("\r\n", "\n").Replace("\r", "\n");
        }
        private static void Ejecutar(SqlConnection cn, string script, SqlTransaction tx = null)
        {
            foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                if (!string.IsNullOrWhiteSpace(batch))
                    using (var cmd = new SqlCommand(batch, cn, tx)) { cmd.CommandTimeout = 120; cmd.ExecuteNonQuery(); }
        }
        public static void Crear(string nombre = null)
        {
            var b = Destino(nombre); string db = b.InitialCatalog; b.InitialCatalog = "master";
            using (var cn = new SqlConnection(b.ConnectionString))
            using (var cmd = new SqlCommand("CREATE DATABASE [" + db + "]", cn)) { cn.Open(); cmd.ExecuteNonQuery(); }
            b.InitialCatalog = db;
            using (var cn = new SqlConnection(b.ConnectionString))
            {
                cn.Open();
                Ejecutar(cn, "CREATE TABLE dbo.__SchemaMigrations(migrationId nvarchar(255) PRIMARY KEY,checksum char(64) NOT NULL,appliedUtc datetime2(0) NOT NULL DEFAULT SYSUTCDATETIME());");
                foreach (string recurso in typeof(BasePruebas).Assembly.GetManifestResourceNames()
                    .Where(n => n.StartsWith("VitaPet.Pruebas.Schema.") && !n.EndsWith(CifradoDatosDAL.VersionCifrado)).OrderBy(n => n, StringComparer.Ordinal))
                    using (var tx = cn.BeginTransaction())
                    {
                        string id = recurso.Substring("VitaPet.Pruebas.Schema.".Length), script = LeerSql(id);
                        Ejecutar(cn, script, tx);
                        using (var cmd = new SqlCommand("INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(@id,@checksum)", cn, tx))
                        { cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@checksum", new IntegridadBLL().CalcularHash(script)); cmd.ExecuteNonQuery(); }
                        tx.Commit();
                    }
            }
        }
        public static void ActualizarControles()
        {
            Destino();
            using (var op = new OperacionIntegridadDAL())
            {
                var h = new IntegridadBLL(); var usuarios = new UsuarioDAL(); var bitacora = new BitacoraDAL();
                foreach (var u in usuarios.ObtenerParaIntegridad()) usuarios.ActualizarDVH(u.IdUsuario, h.CalcularDVH(u));
                foreach (var r in bitacora.ObtenerParaIntegridad()) bitacora.ActualizarDVH(r.IdBitacora, h.CalcularDVH(r));
                var dvv = new DVVDAL();
                dvv.Guardar(new DVVBE { NombreTabla = "Usuario", ValorDVV = h.CalcularDVV(usuarios.ObtenerDVHs()) });
                dvv.Guardar(new DVVBE { NombreTabla = "Bitacora", ValorDVV = h.CalcularDVV(bitacora.ObtenerDVHs()) });
                var dal = new IntegridadTablasDAL(); var calculo = new IntegridadTablasBLL();
                foreach (var clave in IntegridadTablasDAL.Claves)
                {
                    var filas = dal.Leer(clave.Key);
                    foreach (DataRow fila in filas.Rows)
                    { string valor = calculo.CalcularDVH(fila); dal.GuardarDvh(clave.Key, fila[clave.Value], valor); fila["dvh"] = valor; }
                    dvv.Guardar(new DVVBE { NombreTabla = clave.Key, ValorDVV = h.CalcularDVV(filas.Rows.Cast<DataRow>().Select(r => (string)r["dvh"])) });
                }
                new ReferenciaIntegridadDAL().GuardarFormato("sha256-v2");
                typeof(ReferenciaIntegridadBLL).GetMethod("GuardarEstadoVerificado", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(new ReferenciaIntegridadBLL(), null);
                op.Complete();
            }
        }
        public static bool TieneCifrado => new BackupDAL().Esquema().Contains(CifradoDatosDAL.VersionCifrado + ":");
        public static void Cifrar()
        {
            Destino();
            var seguridad = new SecurityManager(); seguridad.ExigirIntegridad();
            if (TieneCifrado) return;
            using (var op = new OperacionIntegridadDAL())
            using (var cn = new ConexionDAL().CrearConexion())
            {
                cn.Open(); CifradoDatosDAL.Preparar(cn, true);
                string script = LeerSql(CifradoDatosDAL.VersionCifrado); Ejecutar(cn, script);
                using (var cmd = new SqlCommand("INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(@id,@checksum)", cn))
                { cmd.Parameters.AddWithValue("@id", CifradoDatosDAL.VersionCifrado); cmd.Parameters.AddWithValue("@checksum", new IntegridadBLL().CalcularHash(script)); cmd.ExecuteNonQuery(); }
                seguridad.ExigirIntegridad(); op.Complete();
            }
        }
    }
}
