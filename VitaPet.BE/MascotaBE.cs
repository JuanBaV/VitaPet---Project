using System;
using System.Collections.Generic;
namespace VitaPet.BE
{
    public sealed class MascotaBE
    {
        public int IdMascota { get; set; }
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Especie { get; set; }
        public string Raza { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public bool Estado { get; set; }
        public string DVH { get; set; }
    }
    public sealed class HistorialClinicoBE
    {
        public int IdHistorial { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Diagnostico { get; set; }
        public string Tratamiento { get; set; }
        public string Observaciones { get; set; }
    }
    public sealed class DatosClinicosBE
    {
        public int IdDatoClinico { get; set; }
        public decimal Peso { get; set; }
        public decimal Temperatura { get; set; }
        public int FrecuenciaCardiaca { get; set; }
        public string Observaciones { get; set; }
    }
    public sealed class ResultadoHistorialClinicoBE
    {
        public string CodigoError { get; set; }
        public MascotaBE Mascota { get; set; }
        public List<HistorialClinicoBE> Historial { get; set; }
    }
    public sealed class ResultadoDatosClinicosBE
    {
        public string CodigoError { get; set; }
        public MascotaBE Mascota { get; set; }
        public List<DatosClinicosBE> Datos { get; set; }
    }
}
