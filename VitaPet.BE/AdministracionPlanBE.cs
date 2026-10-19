using System;
using System.Collections.Generic;

namespace VitaPet.BE
{
    [Serializable]
    public sealed class DatosCoberturaPlanBE
    {
        public int IdCobertura { get; set; }
        public string Prestacion { get; set; }
        public string Detalle { get; set; }
        public bool? Estado { get; set; }
    }

    [Serializable]
    public sealed class DatosPlanBE
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal? Precio { get; set; }
        public bool? Estado { get; set; }
        public List<DatosCoberturaPlanBE> Coberturas { get; set; } = new List<DatosCoberturaPlanBE>();
    }

    [Serializable]
    public sealed class PlanAdministracionBE
    {
        public int IdPlan { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public decimal? Precio { get; set; }
        public bool Estado { get; set; }
        public bool EsCorporativo { get; set; }
        public List<DatosCoberturaPlanBE> Coberturas { get; set; } = new List<DatosCoberturaPlanBE>();
    }
}
