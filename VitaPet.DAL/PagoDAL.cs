using System;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public sealed class PagoDAL
    {
        private static DataTable Leer(string sp,int usuario,int suscripcion,params SqlParameter[] otros)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand(sp,cn))
            using(var adapter=new SqlDataAdapter(cmd))
            {
                cmd.CommandType=CommandType.StoredProcedure;
                cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;
                cmd.Parameters.Add("@idSuscripcion",SqlDbType.Int).Value=suscripcion;
                cmd.Parameters.AddRange(otros);
                var t=new DataTable();adapter.Fill(t);return t;
            }
        }
        public ResumenPagoBE ConsultarPendiente(int usuario,int suscripcion)
        {
            var t=Leer("dbo.sp_Pago_ConsultarPendiente",usuario,suscripcion);
            var r=t.Rows[0];
            return new ResumenPagoBE{IdSuscripcion=(int)r["idSuscripcion"],IdPlan=(int)r["idPlan"],NombrePlan=(string)r["nombre"],
                PrecioMascota=(decimal)r["precio"],CantidadMascotas=(int)r["cantidadMascotas"],Importe=(decimal)r["importe"]};
        }
        public int Registrar(int usuario,int suscripcion,decimal importe,string medio,string titular,string correoFactura)
        {
            var t=Leer("dbo.sp_Pago_Registrar",usuario,suscripcion,
                new SqlParameter("@importe",SqlDbType.Decimal){Precision=10,Scale=2,Value=importe},
                new SqlParameter("@medioPago",SqlDbType.VarChar,40){Value=medio},new SqlParameter("@titular",SqlDbType.NVarChar,100){Value=titular.Trim()},
                new SqlParameter("@correoFactura",SqlDbType.VarChar,100){Value=correoFactura});
            return (int)t.Rows[0][0];
        }
        public PagoBE Comprobante(int usuario,int suscripcion)
        {
            var t=Leer("dbo.sp_Pago_ObtenerComprobante",usuario,suscripcion);
            if(t.Rows.Count==0)return null;
            var r=t.Rows[0];return new PagoBE{IdPago=(int)r["idPago"],IdSuscripcion=(int)r["idSuscripcion"],
                NombrePlan=(string)r["nombre"],Importe=(decimal)r["importe"],MedioPago=(string)r["medioPago"],
                FechaPago=(DateTime)r["fechaPago"],Estado=(string)r["estado"]};
        }
        public bool Activar(int usuario,int suscripcion)
        {
            return (bool)Leer("dbo.sp_Suscripcion_ActivarPagada",usuario,suscripcion).Rows[0][0];
        }
    }
}
