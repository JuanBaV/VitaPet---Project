using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VitaPet.DAL;

partial class Program
{
    // Comparación del contrato anterior y el nuevo en la base aislada de la suite.
    static void ProbarProcedimientosSimplificados()
    {
        var assembly = typeof(DatabaseMigrator).Assembly;
        var anteriores = new Dictionary<string, string>();
        foreach (string resource in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql") && string.CompareOrdinal(n, "VitaPet.DAL.Migrations.013_") < 0).OrderBy(n => n))
        using (var reader = new StreamReader(assembly.GetManifestResourceStream(resource)))
        foreach (string batch in Regex.Split(reader.ReadToEnd(), @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            var match = Regex.Match(batch, @"CREATE(?: OR ALTER)? PROCEDURE dbo\.(\w+)", RegexOptions.IgnoreCase);
            if (match.Success) anteriores[match.Groups[1].Value] = batch.Substring(match.Index);
        }
        Check(anteriores.Count == 21, "All 21 previous procedure contracts are available");
        foreach (var entry in anteriores)
            Sql(entry.Value.Replace("dbo." + entry.Key, "dbo." + entry.Key + "_Anterior"));
        try
        {
            using (var cn = new SqlConnection(connection))
            {
                cn.Open();
                foreach (string name in anteriores.Keys)
                {
                    using (var cmd = cn.CreateCommand())
                    {
                        cmd.CommandText = @"SELECT name, system_type_id, max_length, precision, scale, is_output
FROM sys.parameters WHERE object_id=OBJECT_ID(@proc) ORDER BY parameter_id";
                        cmd.Parameters.AddWithValue("@proc", "dbo." + name);
                        string actual = Filas(cmd);
                        cmd.Parameters[0].Value = "dbo." + name + "_Anterior";
                        Check(actual == Filas(cmd), "Parameter contract preserved: " + name);
                    }
                }
            }
            foreach (string nombre in new[] { null, "", "   ", "  JOSE PRUEBA  ", "%_[", "' OR 1=1 --", "No existe", "José" })
            foreach (int? estado in new int?[] { null, -1, 0, 1, 2, 3, 4, 5 })
            {
                Func<SqlParameter[]> args = () => new[] {
                    new SqlParameter("@nombre", SqlDbType.NVarChar,100) { Value=(object)nombre??DBNull.Value },
                    new SqlParameter("@estado", SqlDbType.Int) { Value=(object)estado??DBNull.Value },
                    new SqlParameter("@hayUsuarios", SqlDbType.Bit) { Direction=ParameterDirection.Output }
                };
                CompararProcedimiento("sp_Usuario_Listar", args, false);
            }
            var user = new UsuarioDAL().ObtenerPorCorreo("fixture0@test.local");
            foreach (string caso in new[] { "valido", "sintransaccion", "ausente", "version", "versionnull", "duplicado", "nombrenull", "correonull" })
            {
                Func<SqlParameter[]> args = () => new[] {
                    new SqlParameter("@idUsuario",SqlDbType.Int){Value=caso=="ausente"?-1:user.IdUsuario},
                    new SqlParameter("@version",SqlDbType.VarChar,64){Value=caso=="versionnull"?(object)DBNull.Value:caso=="version"?"desactualizada":user.DVH},
                    new SqlParameter("@nombre",SqlDbType.VarChar,100){Value=caso=="nombrenull"?(object)DBNull.Value:"Modificado"},
                    new SqlParameter("@correo",SqlDbType.VarChar,100){Value=caso=="correonull"?(object)DBNull.Value:caso=="duplicado"?"fixture1@test.local":"nuevo@test.local"},
                    new SqlParameter("@rol",SqlDbType.VarChar,30){Value="Cliente"},
                    new SqlParameter("@estado",SqlDbType.Bit){Value=true},
                    new SqlParameter("@bloqueado",SqlDbType.Bit){Value=false},
                    new SqlParameter("@intentos",SqlDbType.TinyInt){Value=0},
                    new SqlParameter("@dvh",SqlDbType.VarChar,64){Value=new string('a',64)}
                };
                CompararProcedimiento("sp_Usuario_GuardarAdministracion",args,caso!="sintransaccion");
            }
        }
        finally
        {
            foreach(string name in anteriores.Keys) Sql("DROP PROCEDURE dbo."+name+"_Anterior");
        }
    }
    static string Filas(SqlCommand cmd)
    {
        var text = new StringBuilder();
        using(var r=cmd.ExecuteReader())
        do {
            for(int i=0;i<r.FieldCount;i++) text.Append(r.GetName(i)).Append(':').Append(r.GetDataTypeName(i)).Append('|');
            while(r.Read()) for(int i=0;i<r.FieldCount;i++)
            {
                string value=r.IsDBNull(i)?"<NULL>":Convert.ToString(r.GetValue(i),System.Globalization.CultureInfo.InvariantCulture);
                text.Append(value.Length).Append(':').Append(value);
            }
        } while(r.NextResult());
        return text.ToString();
    }
    static string EjecutarContrato(string proc, SqlParameter[] parameters, bool transaction)
    {
        using(var cn=new SqlConnection(connection))
        {
            cn.Open();var tx=transaction?cn.BeginTransaction(IsolationLevel.Serializable):null;
            try
            {
                string result;
                using(var cmd=new SqlCommand("dbo."+proc,cn,tx))
                {
                    cmd.CommandType=CommandType.StoredProcedure;cmd.Parameters.AddRange(parameters);
                    try { result=Filas(cmd); }
                    catch(SqlException ex) { result="ERROR:"+ex.Number; }
                    foreach(SqlParameter p in parameters.Where(p=>p.Direction==ParameterDirection.Output))
                        result+="|OUTPUT:"+Convert.ToString(p.Value);
                }
                using(var cmd=new SqlCommand("SELECT * FROM dbo.Usuario ORDER BY idUsuario",cn,tx))
                    result+="|USUARIOS:"+Filas(cmd);
                return result;
            }
            finally { if(tx!=null){tx.Rollback();tx.Dispose();} }
        }
    }
    static void CompararProcedimiento(string name,Func<SqlParameter[]> args,bool transaction)
    {
        string anterior=EjecutarContrato(name+"_Anterior",args(),transaction);
        string actual=EjecutarContrato(name,args(),transaction);
        Check(anterior==actual,"Equivalent results/errors/data: "+name);
    }
}
