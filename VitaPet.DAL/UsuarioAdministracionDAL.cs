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
                    return new UsuarioBE {
                        IdUsuario = Convert.ToInt32(r["idUsuario"]), Nombre = Convert.ToString(r["nombre"]),
                        CorreoElectronico = Convert.ToString(r["correoElectronico"]), Rol = Convert.ToString(r["rol"]),
                        Estado = Convert.ToBoolean(r["estado"]), Bloqueado = Convert.ToBoolean(r["bloqueado"]),
                        IntentosFallidos = Convert.ToByte(r["intentosFallidos"]), Idioma = Convert.ToString(r["idioma"]),
                        Contrasena = Convert.ToString(r["contrasena"]), DVH = Convert.ToString(r["dvh"])
                    };
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
                cmd.Parameters.Add("@rol", SqlDbType.VarChar, 30).Value = u.Rol;
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
