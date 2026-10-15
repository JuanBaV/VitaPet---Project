using System;
namespace VitaPet.BE
{
    [Serializable]
    public sealed class FiltroBitacoraBE
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public string Criticidad { get; set; }
    }
}
