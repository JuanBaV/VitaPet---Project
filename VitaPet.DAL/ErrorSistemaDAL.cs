using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public class ErrorSistemaDAL
    {
        private readonly ConexionDAL _conexionDAL;

        public ErrorSistemaDAL()
        {
            _conexionDAL = new ConexionDAL();
        }

        public void Agregar(ErrorSistemaBE error)
        {
            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand(
                "INSERT dbo.ErrorSistema(codigo, descripcion, criticidad) VALUES(@codigo, @descripcion, @criticidad)", conexion))
            {
                comando.Parameters.Add("@codigo", SqlDbType.VarChar, 10).Value = error.Codigo;
                comando.Parameters.Add("@descripcion", SqlDbType.VarChar, 255).Value = error.Descripcion;
                comando.Parameters.Add("@criticidad", SqlDbType.VarChar, 30).Value = error.Criticidad;
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        public bool Eliminar(string codigo)
        {
            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand("DELETE dbo.ErrorSistema WHERE codigo=@codigo", conexion))
            {
                comando.Parameters.Add("@codigo", SqlDbType.VarChar, 10).Value = codigo;
                conexion.Open();
                return comando.ExecuteNonQuery() > 0;
            }
        }

        public List<ErrorSistemaBE> ObtenerTodos()
        {
            var errores = new List<ErrorSistemaBE>();
            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand(
                "SELECT codigo, descripcion, criticidad FROM dbo.ErrorSistema ORDER BY codigo", conexion))
            {
                conexion.Open();
                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                        errores.Add(new ErrorSistemaBE {
                            Codigo = Convert.ToString(lector["codigo"]),
                            Descripcion = Convert.ToString(lector["descripcion"]),
                            Criticidad = Convert.ToString(lector["criticidad"])
                        });
                }
            }
            return errores;
        }

        public ErrorSistemaBE ObtenerPorCodigo(string codigo)
        {
            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand("dbo.sp_ErrorSistema_ObtenerPorCodigo", conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add("@codigo", SqlDbType.VarChar, 10).Value = codigo;

                conexion.Open();

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                    {
                        return null;
                    }

                    return new ErrorSistemaBE
                    {
                        Codigo = Convert.ToString(lector["codigo"]),
                        Descripcion = Convert.ToString(lector["descripcion"]),
                        Criticidad = Convert.ToString(lector["criticidad"])
                    };
                }
            }
        }
    }
}
