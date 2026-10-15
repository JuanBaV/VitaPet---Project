using System;
namespace VitaPet.BE
{
    public enum OperacionUsuario { Consultar, Modificar, Bloquear, Desbloquear }

    [Serializable]
    public sealed class EdicionUsuarioBE
    {
        public UsuarioListadoBE Usuario { get; set; }
        // Conservar exclusivamente en sesión del servidor, nunca en campos ocultos.
        public string Version { get; set; }
    }

    public sealed class CambiosUsuarioBE
    {
        public string Nombre { get; set; }
        public string CorreoElectronico { get; set; }
        public string Rol { get; set; }
        public bool? Estado { get; set; }
    }
}
