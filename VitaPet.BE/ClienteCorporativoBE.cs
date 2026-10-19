using System;
namespace VitaPet.BE
{
    public sealed class DatosClienteCorporativoBE
    {
        public int IdEmpresa { get; set; }
        public string Correo { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Telefono { get; set; }
    }
    public sealed class EmpresaCorporativaBE
    {
        public EmpresaBE Empresa { get; set; }
        public decimal Precio { get; set; }
        public int IdPlan { get; set; }
        public string Plan { get; set; }
    }
    public sealed class AltaClienteCorporativoBE
    {
        public int IdCliente { get; set; }
        public int IdUsuario { get; set; }
        public string Correo { get; set; }
        public string Empresa { get; set; }
        public string Plan { get; set; }
        public DateTime Vencimiento { get; set; }
    }
}
