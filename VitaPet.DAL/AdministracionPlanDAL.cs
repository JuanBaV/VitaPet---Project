using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public sealed class AdministracionPlanDAL
    {
        private static SqlCommand Comando(SqlConnection cn,string sp,int usuario)
        {
            var cmd=new SqlCommand("dbo."+sp,cn){CommandType=CommandType.StoredProcedure};
            cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;return cmd;
        }
        private static PlanAdministracionBE Leer(SqlDataReader r)
        {
            return new PlanAdministracionBE {IdPlan=(int)r["idPlan"],Nombre=(string)r["nombre"],
                Descripcion=(string)r["descripcion"],Precio=r["precio"]==DBNull.Value?(decimal?)null:(decimal)r["precio"],
                Estado=(bool)r["estado"],EsCorporativo=(bool)r["esCorporativo"]};
        }
        public List<PlanAdministracionBE> Listar(int usuario)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=Comando(cn,"sp_Plan_ListarAdministracion",usuario))
            {cn.Open();var list=new List<PlanAdministracionBE>();using(var r=cmd.ExecuteReader())while(r.Read())list.Add(Leer(r));return list;}
        }
        public PlanAdministracionBE Consultar(int usuario,int plan)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=Comando(cn,"sp_Plan_ConsultarAdministracion",usuario))
            {
                cmd.Parameters.Add("@idPlan",SqlDbType.Int).Value=plan;cn.Open();
                using(var r=cmd.ExecuteReader())
                {
                    if(!r.Read())return null;var result=Leer(r);r.NextResult();
                    while(r.Read())result.Coberturas.Add(new DatosCoberturaPlanBE {IdCobertura=(int)r["idCobertura"],
                        Prestacion=(string)r["prestacion"],Detalle=(string)r["detalle"],Estado=(bool)r["estado"]});return result;
                }
            }
        }
        private static void Datos(SqlCommand cmd,DatosPlanBE d)
        {
            cmd.Parameters.Add("@nombre",SqlDbType.NVarChar,-1).Value=d.Nombre;
            cmd.Parameters.Add("@descripcion",SqlDbType.NVarChar,-1).Value=d.Descripcion;
            cmd.Parameters.Add("@precio",SqlDbType.Decimal).Value=d.Precio.Value;cmd.Parameters["@precio"].Precision=28;cmd.Parameters["@precio"].Scale=10;
            cmd.Parameters.Add("@estado",SqlDbType.Bit).Value=d.Estado.Value;
        }
        public int Registrar(int usuario,DatosPlanBE d)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=Comando(cn,"sp_Plan_Registrar",usuario))
            {Datos(cmd,d);cn.Open();return (int)cmd.ExecuteScalar();}
        }
        public void Actualizar(int usuario,int plan,DatosPlanBE d)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=Comando(cn,"sp_Plan_Actualizar",usuario))
            {Datos(cmd,d);cmd.Parameters.Add("@idPlan",SqlDbType.Int).Value=plan;cn.Open();cmd.ExecuteNonQuery();}
        }
        public int GuardarCobertura(int usuario,int plan,DatosCoberturaPlanBE d)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=Comando(cn,"sp_Cobertura_GuardarAdministracion",usuario))
            {
                cmd.Parameters.Add("@idPlan",SqlDbType.Int).Value=plan;cmd.Parameters.Add("@idCobertura",SqlDbType.Int).Value=d.IdCobertura;
                cmd.Parameters.Add("@prestacion",SqlDbType.NVarChar,-1).Value=d.Prestacion;cmd.Parameters.Add("@detalle",SqlDbType.NVarChar,-1).Value=d.Detalle;
                cmd.Parameters.Add("@estado",SqlDbType.Bit).Value=d.Estado.Value;cn.Open();return(int)cmd.ExecuteScalar();
            }
        }
        public bool NecesitaCatalogo()
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand("dbo.sp_Plan_NecesitaCatalogo",cn){CommandType=CommandType.StoredProcedure})
            {cn.Open();return(bool)cmd.ExecuteScalar();}
        }
        public int? InicializarCorporativo()
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand("dbo.sp_Plan_InicializarCorporativo",cn){CommandType=CommandType.StoredProcedure})
            {cn.Open();var id=cmd.ExecuteScalar();return id==null||id==DBNull.Value?(int?)null:(int)id;}
        }
    }
}
