using System;
using System.Transactions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{



    public partial class UsuarioDAL
    {
        private readonly ConexionDAL _conexionDAL;

        private static UsuarioBE LeerUsuario(SqlDataReader lector)
        {
            return new UsuarioBE {
                IdUsuario = Convert.ToInt32(lector["idUsuario"]),
                CorreoElectronico = Convert.ToString(lector["correoElectronico"]),
                Contrasena = Convert.ToString(lector["contrasena"]),
                Nombre = Convert.ToString(lector["nombre"]),
                IdRol = Convert.ToInt32(lector["idRol"]), Rol = Convert.ToString(lector["rol"]),
                Estado = Convert.ToBoolean(lector["estado"]),
                IntentosFallidos = Convert.ToByte(lector["intentosFallidos"]),
                Bloqueado = Convert.ToBoolean(lector["bloqueado"]),
                Idioma = Convert.ToString(lector["idioma"]),
                RequiereRestablecerContrasena = Convert.ToBoolean(lector["requiereRestablecerContrasena"]),
                DVH = Convert.ToString(lector["dvh"])
            };
        }

        public UsuarioDAL()
        {
            _conexionDAL = new ConexionDAL();
        }

        public ResultadoConsultaUsuariosBE ListarUsuarios(FiltroUsuariosBE filtro)
        {
            var resultado = new ResultadoConsultaUsuariosBE();
            using (var conexion = _conexionDAL.CrearConexion())
            using (var comando = new SqlCommand("dbo.sp_Usuario_Listar", conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add("@nombre", SqlDbType.NVarChar, 100).Value = filtro.Nombre ?? string.Empty;
                comando.Parameters.Add("@estado", SqlDbType.Int).Value = (int)filtro.Estado;
                var hayUsuarios = comando.Parameters.Add("@hayUsuarios", SqlDbType.Bit);
                hayUsuarios.Direction = ParameterDirection.Output;
                conexion.Open();
                using (var lector = comando.ExecuteReader())
                    while (lector.Read())
                        resultado.Usuarios.Add(new UsuarioListadoBE {
                            IdUsuario = Convert.ToInt32(lector["idUsuario"]),
                            Nombre = Convert.ToString(lector["nombre"]),
                            CorreoElectronico = Convert.ToString(lector["correoElectronico"]),
                            IdRol = Convert.ToInt32(lector["idRol"]), Rol = Convert.ToString(lector["rol"]),
                            Estado = Convert.ToBoolean(lector["estado"]),
                            Bloqueado = Convert.ToBoolean(lector["bloqueado"])
                        });
                resultado.HayUsuariosRegistrados = Convert.ToBoolean(hayUsuarios.Value);
            }
            return resultado;
        }

        public List<UsuarioBE> ObtenerParaIntegridad()
        {
            List<UsuarioBE> usuarios = new List<UsuarioBE>();

            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand(
                "dbo.sp_Usuario_ObtenerParaIntegridad",
                conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                conexion.Open();

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        usuarios.Add(LeerUsuario(lector));
                    }
                }
            }

            return usuarios;
        }

        public UsuarioBE ObtenerPorCorreo(string correoElectronico)
        {
            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand("dbo.sp_Usuario_ObtenerPorCorreo", conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add("@correoElectronico", SqlDbType.VarChar, 100).Value = correoElectronico;


                conexion.Open();

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                    {
                        return null;
                    }

                    return LeerUsuario(lector);


                }


            }

        }


        public List<string> ObtenerDVHs()
        {
            List<string> valores = new List<string>();

            using (SqlConnection conexion =
                _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand(
                "dbo.sp_Usuario_ObtenerDvhs",
                conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                conexion.Open();

                using (SqlDataReader lector =
                    comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        valores.Add(
                            Convert.ToString(lector["dvh"]));
                    }
                }
            }

            return valores;
        }


        public void ActualizarDVH(int idUsuario, string dvh)
        {
            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand(
                "dbo.sp_Usuario_ActualizarDVH",
                conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add(
                    "@idUsuario",
                    SqlDbType.Int).Value = idUsuario;
                comando.Parameters.Add(
                    "@dvh",
                    SqlDbType.VarChar,
                    64).Value = dvh;

                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        public void ActualizarContrasena(
            int idUsuario,
            string contrasena,
            string dvh,
            DVVBE dvv)
        {
            using (SqlConnection conexion =
                _conexionDAL.CrearConexion())
            {
                using (TransactionScope transaccion =
                    new TransactionScope(TransactionScopeOption.Required))
                {
                    conexion.Open();
                    using (SqlCommand comandoUsuario =
                        new SqlCommand(
                            "dbo.sp_Usuario_ActualizarContrasena",
                            conexion))
                    {
                        comandoUsuario.CommandType =
                            CommandType.StoredProcedure;

                        comandoUsuario.Parameters.Add(
                            "@idUsuario",
                            SqlDbType.Int).Value = idUsuario;

                        comandoUsuario.Parameters.Add(
                            "@contrasena",
                            SqlDbType.VarChar,
                            255).Value = contrasena;

                        comandoUsuario.Parameters.Add(
                            "@dvh",
                            SqlDbType.VarChar,
                            64).Value = dvh;

                        comandoUsuario.ExecuteNonQuery();
                    }

                    DVVDAL.Guardar(conexion, dvv);

                    transaccion.Complete();
                }
            }
        }

        public void ActualizarEstadoSeguridad(
            int idUsuario,
            byte intentosFallidos,
            bool bloqueado,
            bool estado,
            string dvh,
            DVVBE dvv)
        {
            using (SqlConnection conexion =
                _conexionDAL.CrearConexion())
            {
                using (TransactionScope transaccion =
                    new TransactionScope(TransactionScopeOption.Required))
                {
                    conexion.Open();
                    using (SqlCommand comandoUsuario =
                        new SqlCommand(
                            "dbo.sp_Usuario_ActualizarEstadoSeguridad",
                            conexion))
                    {
                        comandoUsuario.CommandType =
                            CommandType.StoredProcedure;

                        comandoUsuario.Parameters.Add(
                            "@idUsuario",
                            SqlDbType.Int).Value = idUsuario;

                        comandoUsuario.Parameters.Add(
                            "@intentosFallidos",
                            SqlDbType.TinyInt).Value =
                            intentosFallidos;

                        comandoUsuario.Parameters.Add(
                            "@bloqueado",
                            SqlDbType.Bit).Value = bloqueado;

                        comandoUsuario.Parameters.Add(
                            "@estado",
                            SqlDbType.Bit).Value = estado;

                        comandoUsuario.Parameters.Add(
                            "@dvh",
                            SqlDbType.VarChar,
                            64).Value = dvh;

                        comandoUsuario.ExecuteNonQuery();
                    }

                    DVVDAL.Guardar(conexion, dvv);

                    transaccion.Complete();
                }
            }

        }
    }
}
