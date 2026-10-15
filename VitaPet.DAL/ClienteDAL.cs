using System;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public sealed class ClienteDAL
    {
        public int Registrar(int idUsuario, RegistroClienteBE cliente)
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("dbo.sp_Cliente_Registrar", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@idUsuario", SqlDbType.Int).Value = idUsuario;
                cmd.Parameters.Add("@nombre", SqlDbType.VarChar, 60).Value = cliente.Nombre;
                cmd.Parameters.Add("@apellido", SqlDbType.VarChar, 60).Value = cliente.Apellido;
                cmd.Parameters.Add("@telefono", SqlDbType.VarChar, 30).Value = cliente.Telefono;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
