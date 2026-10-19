using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;
namespace VitaPet.DAL
{
    public sealed class GestionMascotaDAL
    {
        private List<T> Leer<T>(string sp,int usuario,int? mascota,Func<SqlDataReader,T> convertir)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand(sp,cn))
            {
                cmd.CommandType=CommandType.StoredProcedure;
                cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;
                if(mascota.HasValue)cmd.Parameters.Add("@idMascota",SqlDbType.Int).Value=mascota.Value;
                cn.Open();var lista=new List<T>();
                using(var r=cmd.ExecuteReader())while(r.Read())lista.Add(convertir(r));
                return lista;
            }
        }
        private static MascotaBE Mascota(SqlDataReader r)
        {
            return new MascotaBE {IdMascota=(int)r["idMascota"],IdCliente=(int)r["idCliente"],Nombre=(string)r["nombre"],
                Especie=(string)r["especie"],Raza=(string)r["raza"],FechaNacimiento=(DateTime)r["fechaNacimiento"],
                Estado=(bool)r["estado"],DVH=(string)r["dvh"]};
        }
        public List<MascotaBE> Listar(int usuario){return Leer("dbo.sp_Mascota_ListarUsuario",usuario,null,Mascota);}
        public MascotaBE Consultar(int usuario,int mascota)
        {return Leer("dbo.sp_Mascota_ConsultarUsuario",usuario,mascota,Mascota)[0];}
        public List<HistorialClinicoBE> Historial(int usuario,int mascota)
        {
            return Leer("dbo.sp_HistorialClinico_ConsultarMascota",usuario,mascota,r=>new HistorialClinicoBE {
                IdHistorial=(int)r["idHistorial"],FechaRegistro=(DateTime)r["fechaRegistro"],Diagnostico=(string)r["diagnostico"],
                Tratamiento=(string)r["tratamiento"],Observaciones=r["observaciones"] as string});
        }
        public List<DatosClinicosBE> DatosClinicos(int usuario,int mascota)
        {
            return Leer("dbo.sp_DatosClinicos_ConsultarMascota",usuario,mascota,r=>new DatosClinicosBE {
                IdDatoClinico=(int)r["idDatoClinico"],Peso=(decimal)r["peso"],Temperatura=(decimal)r["temperatura"],
                FrecuenciaCardiaca=(int)r["frecuenciaCardiaca"],Observaciones=r["observaciones"] as string});
        }
        public void Actualizar(int usuario,int mascota,string version,RegistroMascotaBE datos)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand("dbo.sp_Mascota_Actualizar",cn))
            {
                cmd.CommandType=CommandType.StoredProcedure;
                cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;
                cmd.Parameters.Add("@idMascota",SqlDbType.Int).Value=mascota;
                cmd.Parameters.Add("@dvhOriginal",SqlDbType.VarChar,64).Value=(object)version??DBNull.Value;
                cmd.Parameters.Add("@nombre",SqlDbType.VarChar,60).Value=datos.Nombre;
                cmd.Parameters.Add("@especie",SqlDbType.VarChar,40).Value=datos.Especie;
                cmd.Parameters.Add("@raza",SqlDbType.VarChar,60).Value=datos.Raza;
                cmd.Parameters.Add("@fechaNacimiento",SqlDbType.Date).Value=datos.FechaNacimiento.Value.Date;
                cn.Open();cmd.ExecuteNonQuery();
            }
        }
    }
}
