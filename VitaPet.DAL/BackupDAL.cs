using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
namespace VitaPet.DAL
{
    public sealed class BackupDAL
    {
        private SqlConnectionStringBuilder Builder => new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString);
        public string BaseDatos => Builder.InitialCatalog;
        private SqlConnection Administrativa()
        {
            var configured=ConfigurationManager.ConnectionStrings["VitaPetAdministracion"];
            var b=configured==null ? Builder : new SqlConnectionStringBuilder(configured.ConnectionString);
            if(!string.Equals(b.DataSource,Builder.DataSource,StringComparison.OrdinalIgnoreCase)) throw new ConfigurationErrorsException("La conexión administrativa debe usar la misma instancia SQL.");
            b.InitialCatalog="master"; b.Pooling=false;
            return new SqlConnection(b.ConnectionString);
        }
        private static string Q(string name) => "["+name.Replace("]","]]")+"]";
        private static string Literal(string value) => "N'"+value.Replace("'","''")+"'";
        public void Crear(string archivo)
        {
            using(var c=Administrativa()) using(var cmd=c.CreateCommand())
            {
                cmd.CommandTimeout=600;
                cmd.CommandText="BACKUP DATABASE "+Q(BaseDatos)+" TO DISK=@archivo WITH COPY_ONLY, CHECKSUM, INIT;";
                cmd.Parameters.Add("@archivo",SqlDbType.NVarChar,4000).Value=archivo;
                c.Open(); cmd.ExecuteNonQuery();
            }
        }
        public void VerificarArchivo(string archivo)
        {
            using(var c=Administrativa()) using(var cmd=c.CreateCommand())
            {
                cmd.CommandTimeout=600; cmd.CommandText="RESTORE VERIFYONLY FROM DISK=@archivo WITH CHECKSUM;";
                cmd.Parameters.Add("@archivo",SqlDbType.NVarChar,4000).Value=archivo;
                c.Open(); cmd.ExecuteNonQuery();
            }
        }
        public string Esquema()
        {
            using(var c=new ConexionDAL().CrearConexion()) using(var cmd=c.CreateCommand())
            {
                cmd.CommandText="SELECT migrationId,checksum FROM dbo.__SchemaMigrations ORDER BY migrationId;";
                c.Open(); var s=new StringBuilder(); using(var r=cmd.ExecuteReader()) while(r.Read()) s.Append(r.GetString(0)).Append(":").Append(r.GetString(1)).Append(";");
                return s.ToString();
            }
        }
        public string CrearTemporal(string archivo)
        {
            string nombre="VitaPet_Verificacion_"+Guid.NewGuid().ToString("N");
            using(var c=Administrativa())
            {
                c.Open();
                string dataPath;
                using(var cmd=c.CreateCommand()) { cmd.CommandText="SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'));"; dataPath=Convert.ToString(cmd.ExecuteScalar()); }
                if(string.IsNullOrWhiteSpace(dataPath)) throw new InvalidOperationException("No se pudo determinar el directorio SQL para verificar el backup.");
                var files=new DataTable();
                using(var cmd=c.CreateCommand())
                { cmd.CommandText="RESTORE FILELISTONLY FROM DISK=@archivo"; cmd.Parameters.Add("@archivo",SqlDbType.NVarChar,4000).Value=archivo; using(var a=new SqlDataAdapter(cmd)) a.Fill(files); }
                var moves=files.Rows.Cast<DataRow>().Select((r,i)=>"MOVE "+Literal((string)r["LogicalName"])+" TO "+Literal(System.IO.Path.Combine(dataPath,nombre+"_"+i.ToString(CultureInfo.InvariantCulture)+((string)r["Type"]=="L" ? ".ldf" : ".mdf"))));
                try
                {
                    using(var cmd=c.CreateCommand()) { cmd.CommandTimeout=600; cmd.CommandText="RESTORE DATABASE "+Q(nombre)+" FROM DISK=@archivo WITH RECOVERY, CHECKSUM, "+string.Join(",",moves); cmd.Parameters.Add("@archivo",SqlDbType.NVarChar,4000).Value=archivo; cmd.ExecuteNonQuery(); }
                    return nombre;
                }
                catch { EliminarTemporal(nombre); throw; }
            }
        }
        public void EliminarTemporal(string nombre)
        {
            Guid id;
            if(!nombre.StartsWith("VitaPet_Verificacion_",StringComparison.Ordinal) || !Guid.TryParseExact(nombre.Substring("VitaPet_Verificacion_".Length),"N",out id))
                throw new ArgumentException("Nombre temporal inválido.");
            using(var c=Administrativa()) using(var cmd=c.CreateCommand())
            {
                cmd.CommandText="IF DB_ID(@nombre) IS NOT NULL BEGIN ALTER DATABASE "+Q(nombre)+" SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE "+Q(nombre)+"; END;";
                cmd.Parameters.Add("@nombre",SqlDbType.NVarChar,128).Value=nombre;
                c.Open(); cmd.ExecuteNonQuery();
            }
        }
        public void CrearDesdeTemporal(string nombre, string archivo)
        {
            ContingenciaDatosDAL.ValidarTemporal(nombre);
            using (var c = Administrativa()) using (var cmd = c.CreateCommand())
            {
                cmd.CommandTimeout = 600;
                cmd.CommandText = "BACKUP DATABASE " + Q(nombre) + " TO DISK=@archivo WITH COPY_ONLY, CHECKSUM, INIT;";
                cmd.Parameters.Add("@archivo", SqlDbType.NVarChar, 4000).Value = archivo;
                c.Open(); cmd.ExecuteNonQuery();
            }
        }
        public void RestaurarReconstruccionXml(string archivo)
        {
            using (var c = Administrativa())
            {
                c.Open(); SqlConnection.ClearAllPools();
                var files = new DataTable();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "RESTORE FILELISTONLY FROM DISK=@archivo";
                    cmd.Parameters.Add("@archivo", SqlDbType.NVarChar, 4000).Value = archivo;
                    using (var a = new SqlDataAdapter(cmd)) a.Fill(files);
                }
                // Archivos propios del destino: nunca los archivos físicos de la base temporal.
                var paths = new DataTable();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT type,physical_name FROM sys.master_files WHERE database_id=DB_ID(@base) ORDER BY file_id";
                    cmd.Parameters.Add("@base", SqlDbType.NVarChar, 128).Value = BaseDatos;
                    using (var a = new SqlDataAdapter(cmd)) a.Fill(paths);
                }
                string folder;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'))";
                    folder = Convert.ToString(cmd.ExecuteScalar());
                }
                if (string.IsNullOrWhiteSpace(folder)) throw new InvalidOperationException("Directorio SQL no disponible.");
                string unique = "VitaPet_Recuperada_" + Guid.NewGuid().ToString("N");
                var moves = files.Rows.Cast<DataRow>().Select((r, i) =>
                {
                    int tipo = (string)r["Type"] == "L" ? 1 : 0;
                    var destino = paths.Rows.Cast<DataRow>().FirstOrDefault(p => Convert.ToInt32(p["type"], CultureInfo.InvariantCulture) == tipo);
                    string path = destino == null ? System.IO.Path.Combine(folder, unique + "_" + i + (tipo == 1 ? ".ldf" : ".mdf")) : (string)destino["physical_name"];
                    if (destino != null) paths.Rows.Remove(destino);
                    return "MOVE " + Literal((string)r["LogicalName"]) + " TO " + Literal(path);
                }).ToArray();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandTimeout = 600;
                    cmd.CommandText = "IF DB_ID(@base) IS NOT NULL ALTER DATABASE " + Q(BaseDatos) + " SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                        "RESTORE DATABASE " + Q(BaseDatos) + " FROM DISK=@archivo WITH REPLACE, RECOVERY, CHECKSUM, " + string.Join(",", moves) + "; ALTER DATABASE " + Q(BaseDatos) + " SET MULTI_USER;";
                    cmd.Parameters.Add("@base", SqlDbType.NVarChar, 128).Value = BaseDatos;
                    cmd.Parameters.Add("@archivo", SqlDbType.NVarChar, 4000).Value = archivo;
                    try { cmd.ExecuteNonQuery(); }
                    catch
                    {
                        try { cmd.CommandText = "IF DB_ID(@base) IS NOT NULL ALTER DATABASE " + Q(BaseDatos) + " SET MULTI_USER;"; cmd.ExecuteNonQuery(); }
                        catch (SqlException) { }
                        throw;
                    }
                    finally { SqlConnection.ClearAllPools(); }
                }
            }
        }
        public void Restaurar(string archivo)
        {
            using(var c=Administrativa())
            {
                c.Open(); SqlConnection.ClearAllPools();
                using(var cmd=c.CreateCommand())
                {
                    cmd.CommandTimeout=600;
                    cmd.CommandText="ALTER DATABASE "+Q(BaseDatos)+" SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE "+Q(BaseDatos)+" FROM DISK=@archivo WITH REPLACE, RECOVERY, CHECKSUM; ALTER DATABASE "+Q(BaseDatos)+" SET MULTI_USER;";
                    cmd.Parameters.Add("@archivo",SqlDbType.NVarChar,4000).Value=archivo;
                    try { cmd.ExecuteNonQuery(); }
                    catch
                    {
                        try { cmd.Parameters.Clear(); cmd.CommandText="ALTER DATABASE "+Q(BaseDatos)+" SET MULTI_USER;"; cmd.ExecuteNonQuery(); } catch(SqlException) { }
                        throw;
                    }
                    finally { SqlConnection.ClearAllPools(); }
                }
            }
        }
    }
}
