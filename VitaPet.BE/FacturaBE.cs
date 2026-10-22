using System;
namespace VitaPet.BE
{
    public sealed class FacturaBE
    {
        public int IdFactura {get;set;}
        public int IdPago {get;set;}
        public int IdCliente {get;set;}
        public int IdSuscripcion {get;set;}
        public string Numero {get;set;}
        public string Cliente {get;set;}
        public string Correo {get;set;}
        public string CorreoFactura {get;set;}
        public string Plan {get;set;}
        public string Concepto {get;set;}
        public string Titular {get;set;}
        public DateTime PeriodoDesde {get;set;}
        public DateTime PeriodoHasta {get;set;}
        public DateTime FechaEmision {get;set;}
        public decimal PrecioMascota {get;set;}
        public decimal PrecioMascotaAdicional {get;set;}
        public int CantidadMascotas {get;set;}
        public decimal Importe {get;set;}
        public string Moneda {get;set;}
        public string MedioPago {get;set;}
        public string EstadoCorreo {get;set;}
        public string DestinatarioCorreo {get;set;}
    }
    public sealed class ConsultaFacturaBE
    {
        public int IdSuscripcion {get;set;}
        public string Plan {get;set;}
        public string NumeroFactura {get;set;}
        public DateTime FechaPago {get;set;}
        public decimal Importe {get;set;}
    }
}
