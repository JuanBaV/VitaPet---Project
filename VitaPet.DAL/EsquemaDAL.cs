using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VitaPet.DAL
{
    public static class EsquemaDAL
    {
        private static string Recurso(string nombre)
        {
            using (var stream = typeof(EsquemaDAL).Assembly.GetManifestResourceStream("VitaPet.DAL." + nombre))
            using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
        }

        public static string VersionActual => string.Concat(Recurso("EsquemaActual.manifest")
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(linea => linea.Trim()));

        public static IReadOnlyList<string> Inconsistencias()
        {
            var esperadas = Recurso("EsquemaActual.manifest").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(linea => linea.Trim().TrimEnd(';').Split(':')).ToDictionary(p => p[0], p => p[1], StringComparer.Ordinal);
            var aplicadas = new Dictionary<string, string>(StringComparer.Ordinal);
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("SELECT migrationId,checksum FROM dbo.__SchemaMigrations", cn))
            {
                cn.Open(); using (var r = cmd.ExecuteReader()) while (r.Read()) aplicadas.Add(r.GetString(0), r.GetString(1));
            }
            var errores = aplicadas.Where(p => !esperadas.ContainsKey(p.Key) ||
                !string.Equals(p.Value, esperadas[p.Key], StringComparison.OrdinalIgnoreCase)).Select(p => p.Key).ToList();
            errores.AddRange(esperadas.Keys.Where(id => id != CifradoDatosDAL.VersionCifrado && !aplicadas.ContainsKey(id)));
            return errores;
        }

        // La recuperación XML crea una base vacía con el esquema vigente; nunca actualiza una base existente.
        public static void CrearTemporal(string nombre)
        {
            ContingenciaDatosDAL.ValidarTemporal(nombre);
            var builder = new SqlConnectionStringBuilder(new ConexionDAL().CrearConexion().ConnectionString);
            builder.InitialCatalog = "master";
            using (var cn = new SqlConnection(builder.ConnectionString))
            using (var cmd = new SqlCommand("IF DB_ID(@nombre) IS NOT NULL THROW 50003,'La base temporal ya existe.',1; CREATE DATABASE [" + nombre + "];", cn))
            {
                cmd.Parameters.Add("@nombre", SqlDbType.NVarChar, 128).Value = nombre;
                cn.Open(); cmd.ExecuteNonQuery();
            }
            builder.InitialCatalog = nombre;
            try
            {
                using (var cn = new SqlConnection(builder.ConnectionString))
                {
                    cn.Open(); CifradoDatosDAL.Preparar(cn, true);
                    using (var tx = cn.BeginTransaction())
                    {
                        foreach (string batch in Regex.Split(Recurso("EsquemaActual.sql"), @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                            if (!string.IsNullOrWhiteSpace(batch))
                                using (var cmd = new SqlCommand(batch, cn, tx)) { cmd.CommandTimeout = 120; cmd.ExecuteNonQuery(); }
                        tx.Commit();
                    }
                }
            }
            catch { new BackupDAL().EliminarTemporal(nombre); throw; }
        }
    }
}
