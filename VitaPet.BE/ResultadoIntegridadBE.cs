using System;
using System.Collections.Generic;
namespace VitaPet.BE
{
    public sealed class DetalleIntegridadBE
    {
        public string Tabla { get; set; }
        public string Fila { get; set; }
        public string Columna { get; set; }
        public string Motivo { get; set; }
    }
    public sealed class ResultadoIntegridadBE
    {
        public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
        public List<DetalleIntegridadBE> Detalles { get; } = new List<DetalleIntegridadBE>();
        public bool EsValida => Detalles.Count == 0;
    }
    public sealed class ReferenciaCampoBE
    {
        public string Tabla { get; set; }
        public string Fila { get; set; }
        public string Columna { get; set; }
        public string Huella { get; set; }
        public string Clave => Tabla + "/" + Fila + "/" + Columna;
    }
}
