using System;

namespace VitaPet.BE
{
    public sealed class ResumenPagoBE
    {
        public int IdSuscripcion { get; set; }
        public int IdPlan { get; set; }
        public string NombrePlan { get; set; }
        public decimal PrecioMascota { get; set; }
        public int CantidadMascotas { get; set; }
        public decimal Importe { get; set; }
    }

    public sealed class PagoBE
    {
        public int IdPago { get; set; }
        public int IdSuscripcion { get; set; }
        public string NombrePlan { get; set; }
        public decimal Importe { get; set; }
        public string MedioPago { get; set; }
        public DateTime FechaPago { get; set; }
        public string Estado { get; set; }
    }

    // Datos transitorios: no forman parte de Pago, ViewState, auditoria ni XML.
    public sealed class DatosPagoBE
    {
        public string MedioPago { get; set; }
        public string Titular { get; set; }
        public string NumeroTarjeta { get; set; }
        public string Vencimiento { get; set; }
        public string CodigoSeguridad { get; set; }
        public void Limpiar()
        {
            Titular = NumeroTarjeta = Vencimiento = CodigoSeguridad = null;
        }
    }
}
