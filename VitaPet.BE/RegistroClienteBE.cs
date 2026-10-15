namespace VitaPet.BE
{
    // Datos del formulario público; el rol y el estado nunca provienen de la GUI.
    public sealed class RegistroClienteBE
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string CorreoElectronico { get; set; }
        public string Contrasena { get; set; }
        public string Telefono { get; set; }
    }
}
