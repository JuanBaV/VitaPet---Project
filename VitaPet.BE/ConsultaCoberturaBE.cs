using System;
using System.Collections.Generic;

namespace VitaPet.BE
{
    public sealed class ConsultaCoberturaBE
    {
        public int IdSuscripcion { get; set; }
        public int IdPlan { get; set; }
        public string NombrePlan { get; set; }
        public string DescripcionPlan { get; set; }
        public string EstadoSuscripcion { get; set; }
        public DateTime FechaAlta { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public List<CoberturaBE> Coberturas { get; set; } = new List<CoberturaBE>();
    }
    public sealed class ResultadoConsultaCoberturaBE
    {
        public ConsultaCoberturaBE Consulta { get; set; }
        public string CodigoError { get; set; }
    }
}
