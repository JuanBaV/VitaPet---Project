using System;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;
namespace VitaPet.DAL
{
    public sealed class MascotaDAL
    {
        public int ObtenerClienteActivo(int usuario)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand("dbo.sp_Cliente_ObtenerActivoUsuario",cn))
            {
                cmd.CommandType=CommandType.StoredProcedure;cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;
                cn.Open();return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        public int Registrar(int usuario,RegistroMascotaBE datos)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand("dbo.sp_Mascota_Registrar",cn))
            {
                cmd.CommandType=CommandType.StoredProcedure;cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;
                cmd.Parameters.Add("@nombre",SqlDbType.VarChar,60).Value=datos.Nombre;
                cmd.Parameters.Add("@especie",SqlDbType.VarChar,40).Value=datos.Especie;
                cmd.Parameters.Add("@raza",SqlDbType.VarChar,60).Value=datos.Raza;
                cmd.Parameters.Add("@fechaNacimiento",SqlDbType.Date).Value=datos.FechaNacimiento.Value.Date;
                cn.Open();return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
