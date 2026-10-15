using System;
namespace VitaPet.BE
{
    public sealed class ReintegroBE
    {
        public int IdReintegro { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public decimal Importe { get; set; }
        public string Estado { get; set; }
        public string Detalle { get; set; }
    }
}
