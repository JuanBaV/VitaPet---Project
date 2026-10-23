using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace VitaPet.DAL
{
    public static class CifradoDatosDAL
    {
        public const string Migracion = "034_CifradoDatosPersonales.sql";
        public static readonly string[] Tablas = { "Usuario", "Cliente", "Empresa", "DetallePago", "Factura" };

        private static string Clave()
        {
            string ruta = Environment.GetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO");
            if (string.IsNullOrWhiteSpace(ruta))
                ruta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VitaPet", "cifrado.key");
            try
            {
                string clave = File.ReadAllText(ruta).Trim();
                if (Convert.FromBase64String(clave).Length != 32) throw new FormatException();
                return clave;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is FormatException)
            {
                throw new ConfigurationErrorsException("No se pudo leer la clave de cifrado de VitaPet. Consulte GUIA_CIFRADO.md; no genere otra clave para una base ya cifrada.", ex);
            }
        }

        // La clave se abre por sesión SQL y no queda escrita en procedimientos ni en Web.config.
        public static void Preparar(SqlConnection conexion, bool migrando = false)
        {
            using (var cmd = conexion.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sys.symmetric_keys WHERE name='VitaPet_DatosV1'";
                bool cifrada = (int)cmd.ExecuteScalar() != 0;
                if (!cifrada && !migrando) return;
                string clave = Clave();
                cmd.Parameters.Add("@clave", SqlDbType.NVarChar, 128).Value = clave;
                cmd.CommandText = "EXEC sys.sp_set_session_context @key=N'VitaPetClave', @value=@clave;";
                if (cifrada)
                    cmd.CommandText += " IF EXISTS(SELECT 1 FROM sys.openkeys WHERE key_name='VitaPet_DatosV1') CLOSE SYMMETRIC KEY VitaPet_DatosV1; DECLARE @sql nvarchar(max)=N'OPEN SYMMETRIC KEY VitaPet_DatosV1 DECRYPTION BY PASSWORD = '+QUOTENAME(@clave,''''); EXEC sys.sp_executesql @sql;";
                try { cmd.ExecuteNonQuery(); }
                finally
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = "EXEC sys.sp_set_session_context @key=N'VitaPetClave', @value=NULL;";
                    // La migración necesita la contraseña hasta crear y abrir la clave SQL.
                    if (!migrando) cmd.ExecuteNonQuery();
                }
            }
        }

        internal static string TablaLectura(string tabla, SqlConnection conexion)
        {
            if (Array.IndexOf(Tablas, tabla) < 0) return "dbo.[" + tabla + "]";
            using (var cmd = new SqlCommand("SELECT OBJECT_ID(@vista,'V')", conexion))
            {
                cmd.Parameters.AddWithValue("@vista", "dbo.vw_" + tabla + "Descifrado");
                return cmd.ExecuteScalar() == DBNull.Value ? "dbo.[" + tabla + "]" : "dbo.[vw_" + tabla + "Descifrado]";
            }
        }
    }
}
