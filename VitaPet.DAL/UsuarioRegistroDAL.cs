using System;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public partial class UsuarioDAL
    {
        public int RegistrarCliente(UsuarioBE usuario)
        {
            using (var cn = _conexionDAL.CrearConexion())
            using (var cmd = new SqlCommand("dbo.sp_Usuario_RegistrarCliente", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@idRol", SqlDbType.Int).Value = usuario.IdRol;
                cmd.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = usuario.Nombre;
                cmd.Parameters.Add("@correo", SqlDbType.VarChar, 100).Value = usuario.CorreoElectronico;
                cmd.Parameters.Add("@contrasena", SqlDbType.VarChar, 255).Value = usuario.Contrasena;
                cn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
