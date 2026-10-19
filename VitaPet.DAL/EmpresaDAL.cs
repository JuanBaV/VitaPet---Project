using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public sealed class EmpresaDAL
    {
        private static SqlCommand Comando(SqlConnection cn, string nombre, int usuario)
        {
            var cmd = new SqlCommand("dbo." + nombre, cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@idUsuario", SqlDbType.Int).Value = usuario;
            return cmd;
        }

        private static EmpresaBE Leer(SqlDataReader r)
        {
            return new EmpresaBE {
                IdEmpresa=(int)r["idEmpresa"], RazonSocial=(string)r["razonSocial"],
                Cuit=(string)r["cuit"], Direccion=(string)r["direccion"],
                Telefono=(string)r["telefono"], Estado=(bool)r["estado"]
            };
        }

        public List<EmpresaBE> Listar(int usuario)
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = Comando(cn, "sp_Empresa_Listar", usuario))
            {
                cn.Open(); var lista = new List<EmpresaBE>();
                using (var r = cmd.ExecuteReader()) while (r.Read()) lista.Add(Leer(r));
                return lista;
            }
        }

        public EmpresaBE Consultar(int usuario, int empresa)
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = Comando(cn, "sp_Empresa_Consultar", usuario))
            {
                cmd.Parameters.Add("@idEmpresa", SqlDbType.Int).Value = empresa;
                cn.Open(); using (var r = cmd.ExecuteReader()) return r.Read() ? Leer(r) : null;
            }
        }

        public bool CuitRegistrado(int usuario, string cuit, int excluirEmpresa=0)
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = Comando(cn, "sp_Empresa_CuitRegistrado", usuario))
            {
                cmd.Parameters.Add("@cuit", SqlDbType.VarChar, 20).Value = cuit;
                cmd.Parameters.Add("@idEmpresa", SqlDbType.Int).Value = excluirEmpresa;
                cn.Open(); return (bool)cmd.ExecuteScalar();
            }
        }

        private static void Datos(SqlCommand cmd, DatosEmpresaBE datos)
        {
            cmd.Parameters.Add("@razonSocial", SqlDbType.NVarChar, -1).Value = datos.RazonSocial;
            cmd.Parameters.Add("@cuit", SqlDbType.NVarChar, -1).Value = datos.Cuit;
            cmd.Parameters.Add("@direccion", SqlDbType.NVarChar, -1).Value = datos.Direccion;
            cmd.Parameters.Add("@telefono", SqlDbType.NVarChar, -1).Value = datos.Telefono;
        }

        public int Registrar(int usuario, DatosEmpresaBE datos)
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = Comando(cn, "sp_Empresa_Registrar", usuario))
            {
                Datos(cmd, datos); cn.Open(); return (int)cmd.ExecuteScalar();
            }
        }

        public void Actualizar(int usuario, int empresa, DatosEmpresaBE datos)
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = Comando(cn, "sp_Empresa_Actualizar", usuario))
            {
                cmd.Parameters.Add("@idEmpresa", SqlDbType.Int).Value = empresa;
                Datos(cmd, datos); cn.Open(); cmd.ExecuteNonQuery();
            }
        }
    }
}
