using System;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public class DVVDAL
    {
        private readonly ConexionDAL _conexionDAL;

        public DVVDAL()
        {
            _conexionDAL = new ConexionDAL();
        }

        public DVVBE ObtenerPorTabla(string nombreTabla)
        {
            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand(
                "dbo.sp_DVV_ObtenerPorTabla",
                conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add(
                    "@nombreTabla",
                    SqlDbType.VarChar,
                    60).Value = nombreTabla;

                conexion.Open();

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                    {
                        return null;
                    }

                    return new DVVBE
                    {
                        IdDVV = Convert.ToInt32(lector["idDVV"]),
                        NombreTabla = Convert.ToString(lector["nombreTabla"]),
                        ValorDVV = Convert.ToString(lector["valorDVV"])
                    };
                }
            }
        }

        public void Guardar(DVVBE dvv)
        {
            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand("dbo.sp_DVV_Guardar", conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add("@nombreTabla", SqlDbType.VarChar, 60).Value = dvv.NombreTabla;
                comando.Parameters.Add("@valorDVV", SqlDbType.VarChar, 255).Value = dvv.ValorDVV;

                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }
    }
}
