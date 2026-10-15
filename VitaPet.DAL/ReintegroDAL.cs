using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using VitaPet.BE;
namespace VitaPet.DAL
{
    public sealed class ReintegroDAL
    {
        private List<ReintegroBE> Leer(string sp,int usuario,int? reintegro=null)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand(sp,cn))
            using(var adapter=new SqlDataAdapter(cmd))
            {
                cmd.CommandType=CommandType.StoredProcedure;
                cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;
                if(reintegro.HasValue)cmd.Parameters.Add("@idReintegro",SqlDbType.Int).Value=reintegro.Value;
                var datos=new DataTable();adapter.Fill(datos);
                return datos.Rows.Cast<DataRow>().Select(f=>new ReintegroBE {
                    IdReintegro=(int)f["idReintegro"],FechaSolicitud=(DateTime)f["fechaSolicitud"],
                    Importe=(decimal)f["importe"],Estado=(string)f["estado"],
                    Detalle=f.IsNull("detalle")?null:(string)f["detalle"] }).ToList();
            }
        }
        public List<ReintegroBE> Listar(int usuario){return Leer("dbo.sp_Reintegro_ListarCliente",usuario);}
        public ReintegroBE Consultar(int usuario,int reintegro){return Leer("dbo.sp_Reintegro_ConsultarCliente",usuario,reintegro).SingleOrDefault();}
    }
}
