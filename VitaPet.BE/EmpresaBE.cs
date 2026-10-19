namespace VitaPet.BE
{
    public sealed class DatosEmpresaBE
    {
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
    }

    public sealed class EmpresaBE
    {
        public int IdEmpresa { get; set; }
        public string RazonSocial { get; set; }
        public string Cuit { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }
        public bool Estado { get; set; }
    }
}
