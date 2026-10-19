using System;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;
namespace VitaPet.DAL
{
    public sealed class ClienteCorporativoDAL
    {
        private static SqlCommand Cmd(SqlConnection cn,string sp,int actor,string parametro="@idUsuario")
        {
            var cmd=new SqlCommand("dbo.sp_ClienteCorporativo_"+sp,cn){CommandType=CommandType.StoredProcedure};
            cmd.Parameters.Add(parametro,SqlDbType.Int).Value=actor;return cmd;
        }
        public EmpresaCorporativaBE ConsultarPlan(int actor)
        {
            using(var cn=new ConexionDAL().CrearConexion())using(var cmd=Cmd(cn,"ConsultarPlan",actor))
            {cn.Open();using(var r=cmd.ExecuteReader())return r.Read()?new EmpresaCorporativaBE{IdPlan=(int)r["idPlan"],Plan=(string)r["nombre"],Precio=(decimal)r["precio"]}:null;}
        }
        public bool CorreoRegistrado(int actor,string correo)
        {
            using(var cn=new ConexionDAL().CrearConexion())using(var cmd=Cmd(cn,"CorreoRegistrado",actor))
            {cmd.Parameters.Add("@correo",SqlDbType.VarChar,100).Value=correo;cn.Open();return(bool)cmd.ExecuteScalar();}
        }
        public int RegistrarUsuario(int actor,string nombre,string correo,string hash)
        {
            using(var cn=new ConexionDAL().CrearConexion())using(var cmd=Cmd(cn,"RegistrarUsuario",actor,"@idAdministrador"))
            {
                Texto(cmd,"@nombre",nombre);Texto(cmd,"@correo",correo);cmd.Parameters.Add("@hash",SqlDbType.VarChar,64).Value=hash;
                cn.Open();return(int)cmd.ExecuteScalar();
            }
        }
        public int RegistrarCliente(int actor,int usuario,DatosClienteCorporativoBE d)
        {
            using(var cn=new ConexionDAL().CrearConexion())using(var cmd=Cmd(cn,"RegistrarCliente",actor,"@idAdministrador"))
            {
                cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;cmd.Parameters.Add("@idEmpresa",SqlDbType.Int).Value=d.IdEmpresa;
                Texto(cmd,"@nombre",d.Nombre);Texto(cmd,"@apellido",d.Apellido);Texto(cmd,"@telefono",d.Telefono);
                cn.Open();return(int)cmd.ExecuteScalar();
            }
        }
        public int AsignarPlan(int actor,int cliente,int plan)
        {
            using(var cn=new ConexionDAL().CrearConexion())using(var cmd=Cmd(cn,"AsignarPlan",actor,"@idAdministrador"))
            {
                cmd.Parameters.Add("@idCliente",SqlDbType.Int).Value=cliente;cmd.Parameters.Add("@idPlan",SqlDbType.Int).Value=plan;
                cn.Open();return(int)cmd.ExecuteScalar();
            }
        }
        private static void Texto(SqlCommand cmd,string p,string value){cmd.Parameters.Add(p,SqlDbType.NVarChar,-1).Value=value;}
    }
}
