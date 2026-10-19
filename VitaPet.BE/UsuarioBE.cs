using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VitaPet.BE
{
    public class UsuarioBE
    {
        public int IdUsuario { get; set; }
        public string CorreoElectronico { get; set; }
        public string Contrasena { get; set; }
        public string Rol { get; set; }
        public bool Estado { get; set; }
        public string Nombre { get; set; }
        public byte IntentosFallidos { get; set; }
        public bool Bloqueado { get; set; }
        public string Idioma { get; set; }
        public bool RequiereRestablecerContrasena { get; set; }
        public string DVH { get; set; }


    }
}
