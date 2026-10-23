using System;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public partial class UsuarioDAL
    {
        public UsuarioBE ObtenerPorId(int idUsuario)
        {
            using (var cn = _conexionDAL.CrearConexion())
            using (var cmd = new SqlCommand("dbo.sp_Usuario_ObtenerPorId", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@idUsuario", SqlDbType.Int).Value = idUsuario;
                cn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return null;
                    return LeerUsuario(r);
                }
            }
        }

        // Se ejecuta dentro de OperacionIntegridadDAL, junto con DVV, referencias y auditoría.
        public void GuardarAdministracion(UsuarioBE u, string version)
        {
            if (System.Transactions.Transaction.Current == null)
                throw new InvalidOperationException("La actualización requiere una transacción de integridad.");
            using (var cn = _conexionDAL.CrearConexion())
            using (var cmd = new SqlCommand("dbo.sp_Usuario_GuardarAdministracion", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@idUsuario", SqlDbType.Int).Value = u.IdUsuario;
                cmd.Parameters.Add("@version", SqlDbType.VarChar, 64).Value = version;
                cmd.Parameters.Add("@nombre", SqlDbType.VarChar, 100).Value = u.Nombre;
                cmd.Parameters.Add("@correo", SqlDbType.VarChar, 100).Value = u.CorreoElectronico;
                cmd.Parameters.Add("@idRol", SqlDbType.Int).Value = u.IdRol;
                cmd.Parameters.Add("@estado", SqlDbType.Bit).Value = u.Estado;
                cmd.Parameters.Add("@bloqueado", SqlDbType.Bit).Value = u.Bloqueado;
                cmd.Parameters.Add("@intentos", SqlDbType.TinyInt).Value = u.IntentosFallidos;
                cmd.Parameters.Add("@dvh", SqlDbType.VarChar, 64).Value = u.DVH;
                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
