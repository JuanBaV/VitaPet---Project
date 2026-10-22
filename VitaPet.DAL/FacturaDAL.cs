using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using VitaPet.BE;
namespace VitaPet.DAL
{
    public sealed class FacturaDAL
    {
        private DataTable Leer(string sp,int usuario,int? suscripcion=null)
        {
            using(var cn=new ConexionDAL().CrearConexion())using(var cmd=new SqlCommand(sp,cn))using(var a=new SqlDataAdapter(cmd))
            {
                cmd.CommandType=CommandType.StoredProcedure;cmd.Parameters.Add("@idUsuario",SqlDbType.Int).Value=usuario;
                if(suscripcion.HasValue)cmd.Parameters.Add("@idSuscripcion",SqlDbType.Int).Value=suscripcion.Value;
                var t=new DataTable();a.Fill(t);return t;
            }
        }
        public Tuple<int,bool> Emitir(int usuario,int suscripcion)
        {var r=Leer("dbo.sp_Factura_Emitir",usuario,suscripcion).Rows[0];return Tuple.Create((int)r["id"],(bool)r["nuevo"]);}
        public FacturaBE Obtener(int usuario,int suscripcion)
        {
            var t=Leer("dbo.sp_Factura_Obtener",usuario,suscripcion);if(t.Rows.Count==0)return null;var r=t.Rows[0];
            return new FacturaBE {IdFactura=(int)r["idFactura"],IdPago=(int)r["idPago"],IdCliente=(int)r["idCliente"],IdSuscripcion=suscripcion,Numero=(string)r["numeroFactura"],
                Cliente=(string)r["clienteNombre"],Correo=(string)r["clienteCorreo"],CorreoFactura=r.IsNull("correoFactura")?null:(string)r["correoFactura"],Plan=(string)r["planNombre"],Concepto=(string)r["concepto"],Titular=r.IsNull("titular")?null:(string)r["titular"],
                PeriodoDesde=(DateTime)r["periodoDesde"],PeriodoHasta=(DateTime)r["periodoHasta"],FechaEmision=(DateTime)r["fechaEmision"],Importe=(decimal)r["importe"],Moneda=(string)r["moneda"],MedioPago=(string)r["medioPago"],
                CantidadMascotas=r.IsNull("cantidadMascotas")?0:(int)r["cantidadMascotas"],PrecioMascota=r.IsNull("precioMascota")?0:(decimal)r["precioMascota"],PrecioMascotaAdicional=r.IsNull("precioMascotaAdicional")?0:(decimal)r["precioMascotaAdicional"]};
        }
        public List<ConsultaFacturaBE> Listar(int usuario)
        {return Leer("dbo.sp_Factura_ListarPropias",usuario).Rows.Cast<DataRow>().Select(r=>new ConsultaFacturaBE {IdSuscripcion=(int)r["idSuscripcion"],Plan=(string)r["planNombre"],Importe=(decimal)r["importe"],FechaPago=(DateTime)r["fechaPago"],NumeroFactura=r.IsNull("numeroFactura")?null:(string)r["numeroFactura"]}).ToList();}
    }
}
