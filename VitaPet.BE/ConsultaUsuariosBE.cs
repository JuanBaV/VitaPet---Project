using System;
using System.Collections.Generic;

namespace VitaPet.BE
{
    public enum EstadoFiltroUsuario { Todos, Activo, Inactivo, Bloqueado, Desbloqueado }

    [Serializable]
    public sealed class FiltroUsuariosBE
    {
        public string Nombre { get; set; }
        public EstadoFiltroUsuario Estado { get; set; }
    }

    // Only public listing fields: never send credential hashes or integrity data to the UI.
    [Serializable]
    public sealed class UsuarioListadoBE
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string CorreoElectronico { get; set; }
        public int IdRol { get; set; }
        public string Rol { get; set; }
        public bool Estado { get; set; }
        public bool Bloqueado { get; set; }
        public bool PuedeBloquear { get { return Estado && !Bloqueado; } }
        public bool PuedeDesbloquear { get { return Bloqueado; } }
    }

    [Serializable]
    public sealed class ResultadoConsultaUsuariosBE
    {
        public List<UsuarioListadoBE> Usuarios { get; set; } = new List<UsuarioListadoBE>();
        public bool HayUsuariosRegistrados { get; set; }
    }
}
