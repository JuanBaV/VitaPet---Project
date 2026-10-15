using System.Collections.Generic;

namespace VitaPet.BE
{
    public sealed class PlanBE
    {
        public int IdPlan { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string Version { get; set; }
    }
    public sealed class CotizacionPlanBE
    {
        public PlanBE Plan { get; set; }
        public int CantidadMascotas { get; set; }
        public decimal ImporteUSD { get { return Plan.Precio * CantidadMascotas; } }
        public int IdSuscripcion { get; set; }
    }
    public sealed class CoberturaBE
    {
        public string Prestacion { get; set; }
        public string Detalle { get; set; }
    }
    public sealed class ResultadoCoberturaBE
    {
        public List<CoberturaBE> Coberturas { get; set; } = new List<CoberturaBE>();
        public string CodigoError { get; set; }
    }
}
