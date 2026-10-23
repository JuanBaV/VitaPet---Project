using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace VitaPet.DAL
{
    public static class DatabaseMigrator
    {
        private const string ConnectionStringName = "VitaPetConnection";
        private const string ResourcePrefix = "VitaPet.DAL.Migrations.";
        private const string MigrationLockName = "VitaPet.DatabaseMigrations";

        private static readonly Regex BatchSeparator = new Regex(
            @"^\s*GO\s*(?:--.*)?$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        public static void Migrate(string hasta = null)
        {
            using(var cn=new ConexionDAL().CrearConexion()) MigrarConexion(cn.ConnectionString,hasta);
        }

        public static IReadOnlyList<string> MigracionesInconsistentes(bool incluirPendientes = true)
        {
            using (var connection = new ConexionDAL().CrearConexion())
            {
                connection.Open();
                var applied = LoadAppliedMigrations(connection);
                var expected = LoadMigrations().ToDictionary(m => m.Id, m => m.Checksum, StringComparer.OrdinalIgnoreCase);
                var errors = applied.Where(m => !expected.ContainsKey(m.Key) ||
                    !string.Equals(m.Value, expected[m.Key], StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.Key).ToList();
                // Los respaldos anteriores se verifican antes de aplicar sus migraciones pendientes.
                if (incluirPendientes && !ConexionDAL.EnBaseTemporal)
                    errors.AddRange(expected.Keys.Where(id => !applied.ContainsKey(id)));
                return errors;
            }
        }

        public static void CrearTemporalContingencia(string nombre, string hasta = null)
        {
            ContingenciaDatosDAL.ValidarTemporal(nombre);
            var builder = new SqlConnectionStringBuilder(GetConnectionString()) { InitialCatalog = nombre };
            MigrarConexion(builder.ConnectionString,hasta);
        }

        public static void MigrarTemporalExistente(string nombre, string hasta = null)
        {
            ContingenciaDatosDAL.ValidarTemporal(nombre);
            var builder = new SqlConnectionStringBuilder(GetConnectionString()) { InitialCatalog = nombre };
            MigrarConexion(builder.ConnectionString,hasta);
        }

        private static void MigrarConexion(string connectionString, string hasta = null)
        {
            SqlConnectionStringBuilder targetBuilder =
                new SqlConnectionStringBuilder(connectionString);

            string databaseName = targetBuilder.InitialCatalog;

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ConfigurationErrorsException(
                    "La conexión VitaPetConnection debe indicar Initial Catalog o Database.");
            }

            if (System.Transactions.Transaction.Current==null) EnsureDatabaseExists(targetBuilder, databaseName);

            using (SqlConnection connection =
                new SqlConnection(targetBuilder.ConnectionString))
            {
                connection.Open();
                AcquireMigrationLock(connection);

                try
                {
                    EnsureMigrationHistoryExists(connection);
                    ApplyPendingMigrations(connection, LoadMigrations().Where(m => hasta==null || string.CompareOrdinal(m.Id,hasta)<=0));
                }
                finally
                {
                    ReleaseMigrationLock(connection);
                }
            }
        }

        private static string GetConnectionString()
        {
            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings[ConnectionStringName];

            if (settings == null ||
                string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "No se encontró la conexión VitaPetConnection.");
            }

            return settings.ConnectionString;
        }

        private static void EnsureDatabaseExists(
            SqlConnectionStringBuilder targetBuilder,
            string databaseName)
        {
            SqlConnectionStringBuilder masterBuilder =
                new SqlConnectionStringBuilder(targetBuilder.ConnectionString)
                {
                    InitialCatalog = "master"
                };

            using (SqlConnection connection =
                new SqlConnection(masterBuilder.ConnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
IF DB_ID(@databaseName) IS NULL
BEGIN
    DECLARE @statement NVARCHAR(MAX) =
        N'CREATE DATABASE ' + QUOTENAME(@databaseName);

    EXEC sys.sp_executesql @statement;
END;";
                command.Parameters.Add("@databaseName", SqlDbType.NVarChar, 128)
                    .Value = databaseName;
                command.CommandTimeout = 60;

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static void AcquireMigrationLock(SqlConnection connection)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
DECLARE @result INT;

EXEC @result = sys.sp_getapplock
    @Resource = @resource,
    @LockMode = 'Exclusive',
    @LockOwner = 'Session',
    @LockTimeout = 60000;

SELECT @result;";
                command.Parameters.Add("@resource", SqlDbType.NVarChar, 255)
                    .Value = MigrationLockName;
                command.CommandTimeout = 65;

                int result = Convert.ToInt32(
                    command.ExecuteScalar(),
                    CultureInfo.InvariantCulture);

                if (result < 0)
                {
                    throw new InvalidOperationException(
                        "No se pudo obtener el bloqueo para ejecutar las migraciones.");
                }
            }
        }

        private static void ReleaseMigrationLock(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                return;
            }

            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
EXEC sys.sp_releaseapplock
    @Resource = @resource,
    @LockOwner = 'Session';";
                command.Parameters.Add("@resource", SqlDbType.NVarChar, 255)
                    .Value = MigrationLockName;
                command.ExecuteNonQuery();
            }
        }

        private static void EnsureMigrationHistoryExists(
            SqlConnection connection)
        {
            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__SchemaMigrations
    (
        migrationId NVARCHAR(255) NOT NULL,
        checksum CHAR(64) NOT NULL,
        appliedUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_SchemaMigrations_appliedUtc
            DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SchemaMigrations PRIMARY KEY (migrationId)
    );
END;";
                command.ExecuteNonQuery();
            }
        }

        private static void ApplyPendingMigrations(
            SqlConnection connection,
            IEnumerable<Migration> migrations)
        {
            Dictionary<string, string> applied =
                LoadAppliedMigrations(connection);

            foreach (Migration migration in migrations)
            {
                string storedChecksum;

                if (applied.TryGetValue(migration.Id, out storedChecksum))
                {
                    if (!string.Equals(
                        storedChecksum,
                        migration.Checksum,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "La migración aplicada '" + migration.Id +
                            "' fue modificada. Cree una migración nueva en lugar de editarla.");
                    }

                    continue;
                }

                ApplyMigration(connection, migration);
            }
        }

        private static Dictionary<string, string> LoadAppliedMigrations(
            SqlConnection connection)
        {
            Dictionary<string, string> applied =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT migrationId, checksum
FROM dbo.__SchemaMigrations;";

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        applied.Add(
                            Convert.ToString(reader["migrationId"], CultureInfo.InvariantCulture),
                            Convert.ToString(reader["checksum"], CultureInfo.InvariantCulture));
                    }
                }
            }

            return applied;
        }

        private static void ApplyMigration(
            SqlConnection connection,
            Migration migration)
        {
            using (SqlTransaction transaction = System.Transactions.Transaction.Current==null ? connection.BeginTransaction() : null)
            {
                try
                {
                    foreach (string batch in SplitBatches(migration.Script))
                    {
                        using (SqlCommand command =
                            new SqlCommand(batch, connection, transaction))
                        {
                            command.CommandTimeout = 120;
                            command.ExecuteNonQuery();
                        }
                    }

                    using (SqlCommand command =
                        new SqlCommand(@"
INSERT INTO dbo.__SchemaMigrations (migrationId, checksum)
VALUES (@migrationId, @checksum);", connection, transaction))
                    {
                        command.Parameters.Add("@migrationId", SqlDbType.NVarChar, 255)
                            .Value = migration.Id;
                        command.Parameters.Add("@checksum", SqlDbType.Char, 64)
                            .Value = migration.Checksum;
                        command.ExecuteNonQuery();
                    }

                    if(transaction!=null) transaction.Commit();
                }
                catch
                {
                    if(transaction!=null) transaction.Rollback();
                    throw;
                }
            }
        }

        private static IEnumerable<string> SplitBatches(string script)
        {
            return BatchSeparator.Split(script)
                .Select(batch => batch.Trim())
                .Where(batch => batch.Length > 0);
        }

        private static IEnumerable<Migration> LoadMigrations()
        {
            Assembly assembly = typeof(DatabaseMigrator).Assembly;
            string[] resources = assembly.GetManifestResourceNames()
                .Where(name =>
                    name.StartsWith(ResourcePrefix, StringComparison.Ordinal) &&
                    name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            if (resources.Length == 0)
            {
                throw new InvalidOperationException(
                    "No se encontraron migraciones SQL embebidas.");
            }

            foreach (string resourceName in resources)
            {
                string script;

                using (Stream stream =
                    assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new InvalidOperationException(
                            "No se pudo leer la migración " + resourceName + ".");
                    }

                    using (StreamReader reader =
                        new StreamReader(stream, Encoding.UTF8, true))
                    {
                        script = reader.ReadToEnd();
                    }
                }

                script = script.Replace("\r\n", "\n").Replace("\r", "\n");

                yield return new Migration(
                    resourceName.Substring(ResourcePrefix.Length),
                    script,
                    CalculateChecksum(script));
            }
        }

        private static string CalculateChecksum(string script)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(script));
                StringBuilder result = new StringBuilder(hash.Length * 2);

                foreach (byte value in hash)
                {
                    result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return result.ToString();
            }
        }

        private sealed class Migration
        {
            public Migration(string id, string script, string checksum)
            {
                Id = id;
                Script = script;
                Checksum = checksum;
            }

            public string Id { get; private set; }
            public string Script { get; private set; }
            public string Checksum { get; private set; }
        }
    }
}
