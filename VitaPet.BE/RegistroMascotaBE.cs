using System;
namespace VitaPet.BE
{
    public sealed class RegistroMascotaBE
    {
        public string Nombre { get; set; }
        public string Especie { get; set; }
        public string Raza { get; set; }
        public DateTime? FechaNacimiento { get; set; }
    }
}
