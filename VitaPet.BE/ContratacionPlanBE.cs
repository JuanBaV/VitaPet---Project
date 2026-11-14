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
        public decimal PrecioMascotaAdicional {get{return decimal.Round(Plan.Precio*0.9m,2,System.MidpointRounding.AwayFromZero);}}
        public decimal ImporteUSD { get { return CantidadMascotas<=0?0:Plan.Precio+PrecioMascotaAdicional*(CantidadMascotas-1); } }
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
