using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using VitaPet.BE;
using System.Security.Cryptography;
using System.Text;

namespace VitaPet.DAL
{
    public static class CifradoDatosDAL
    {
        public const string VersionCifrado = "034_CifradoDatosPersonales.sql";
        public static readonly string[] Tablas = { "Usuario", "Cliente", "Empresa", "DetallePago", "Factura" };
        private static readonly Dictionary<string,string[]> Campos = new Dictionary<string,string[]> {
            { "Usuario", new[] { "correoElectronico", "nombre" } },
            { "Cliente", new[] { "nombre", "apellido", "telefono" } },
            { "Empresa", new[] { "razonSocial", "cuit", "direccion", "telefono" } },
            { "DetallePago", new[] { "titular", "correoFactura" } },
            { "Factura", new[] { "clienteNombre", "clienteCorreo", "titular" } }
        };

        public static List<DetalleIntegridadBE> Diagnosticar()
        {
            var errores = new List<DetalleIntegridadBE>();
            using (var c = new ConexionDAL().CrearConexion())
            using (var cmd = c.CreateCommand())
            {
                c.Open();
                cmd.CommandText = "SELECT COUNT(*) FROM sys.symmetric_keys WHERE name='VitaPet_DatosV1'";
                bool tieneClave = (int)cmd.ExecuteScalar() == 1;
                cmd.CommandText = "SELECT COUNT(*) FROM dbo.__SchemaMigrations WHERE migrationId=@id";
                cmd.Parameters.AddWithValue("@id", VersionCifrado);
                bool migrada = (int)cmd.ExecuteScalar() == 1;
                if (migrada != tieneClave)
                    errores.Add(new DetalleIntegridadBE { Tabla="__SchemaMigrations", Fila=VersionCifrado, Columna="migrationId", Motivo="El cifrado no coincide con el historial de migraciones." });
                if (!tieneClave) return errores;
                cmd.Parameters.Clear();
                cmd.CommandText = string.Join(" UNION ALL ", Campos.SelectMany(t => t.Value.Select(campo =>
                    "SELECT N'"+t.Key+"',N'"+campo+"' WHERE EXISTS(SELECT 1 FROM dbo.["+t.Key+"] WHERE ["+campo+"] IS NOT NULL AND dbo.fn_DescifrarDato(["+campo+"],N'"+t.Key+"."+campo+"') IS NULL)")));
                using (var r = cmd.ExecuteReader()) while (r.Read())
                    errores.Add(new DetalleIntegridadBE { Tabla=r.GetString(0), Fila="—", Columna=r.GetString(1), Motivo="Dato no cifrado o ciphertext inválido." });
            }
            return errores;
        }

        private static string Clave()
        {
            string ruta = Environment.GetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO");
            if (string.IsNullOrWhiteSpace(ruta))
                // LocalAppData puede virtualizarse para aplicaciones MSIX y resolver a archivos distintos.
                ruta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vitapet", "cifrado.key");
            try
            {
                string clave = File.ReadAllText(ruta).Trim();
                if (Convert.FromBase64String(clave).Length != 32) throw new FormatException();
                return clave;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is FormatException)
            {
                throw new ConfigurationErrorsException("No se pudo leer la clave de cifrado de VitaPet. Configure el archivo de clave original; no genere otra clave para una base ya cifrada.", ex);
            }
        }

        // La clave se abre por sesión SQL y no queda escrita en procedimientos ni en Web.config.
        public static void Preparar(SqlConnection conexion, bool creandoClave = false)
        {
            using (var cmd = conexion.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sys.symmetric_keys WHERE name='VitaPet_DatosV1'";
                bool cifrada = (int)cmd.ExecuteScalar() != 0;
                if (!cifrada && !creandoClave) return;
                string clave = Clave();
                cmd.Parameters.Add("@clave", SqlDbType.NVarChar, 128).Value = clave;
                using (var hmac = new HMACSHA256(Convert.FromBase64String(clave)))
                    cmd.Parameters.Add("@mac", SqlDbType.VarBinary, 32).Value = hmac.ComputeHash(Encoding.UTF8.GetBytes("VitaPet.Datos.MAC.v1"));
                cmd.CommandText = "EXEC sys.sp_set_session_context @key=N'VitaPetClave', @value=@clave; EXEC sys.sp_set_session_context @key=N'VitaPetMAC', @value=@mac;";
                if (cifrada)
                    cmd.CommandText += " IF EXISTS(SELECT 1 FROM sys.openkeys WHERE key_name='VitaPet_DatosV1') CLOSE SYMMETRIC KEY VitaPet_DatosV1; DECLARE @sql nvarchar(max)=N'OPEN SYMMETRIC KEY VitaPet_DatosV1 DECRYPTION BY PASSWORD = '+QUOTENAME(@clave,''''); EXEC sys.sp_executesql @sql;";
                try { cmd.ExecuteNonQuery(); }
                finally
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = "EXEC sys.sp_set_session_context @key=N'VitaPetClave', @value=NULL;";
                    // La reconstrucción de una base vacía necesita crear y abrir su clave SQL.
                    if (!creandoClave) cmd.ExecuteNonQuery();
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
