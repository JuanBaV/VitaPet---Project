using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public sealed class ConsultaCoberturaDAL
    {
        private static DataTable Leer(string procedimiento,int usuario,int? suscripcion=null)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand(procedimiento,cn))
            using(var adapter=new SqlDataAdapter(cmd))
            {
                cmd.CommandType=CommandType.StoredProcedure;
                cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;
                if(suscripcion.HasValue)cmd.Parameters.Add("@idSuscripcion",SqlDbType.Int).Value=suscripcion.Value;
                var t=new DataTable();adapter.Fill(t);return t;
            }
        }
        public ConsultaCoberturaBE ObtenerVigente(int usuario)
        {
            var t=Leer("dbo.sp_Suscripcion_ObtenerVigenteCliente",usuario);
            if(t.Rows.Count==0)return null;
            var r=t.Rows[0];return new ConsultaCoberturaBE{IdSuscripcion=(int)r["idSuscripcion"],IdPlan=(int)r["idPlan"],
                NombrePlan=(string)r["nombre"],DescripcionPlan=(string)r["descripcion"],EstadoSuscripcion=(string)r["estado"],
                FechaAlta=(DateTime)r["fechaAlta"],FechaVencimiento=(DateTime)r["fechaVencimiento"]};
        }
        public List<CoberturaBE> ObtenerPrestaciones(int usuario,int suscripcion)
        {
            return Leer("dbo.sp_Cobertura_ConsultarCuenta",usuario,suscripcion).Rows.Cast<DataRow>()
                .Select(r=>new CoberturaBE{Prestacion=(string)r["prestacion"],Detalle=(string)r["detalle"]}).ToList();
        }
    }
}
