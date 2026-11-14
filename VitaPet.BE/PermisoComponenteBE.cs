using System;
using System.Collections.Generic;
using System.Linq;

namespace VitaPet.BE
{
    //COMPOSITE:
    //PermisoComponenteBE --> Componente base
    //PermisoPantallaBE --> Hoja
    //PerfilBE --> Compuesto, agurpa otros perfiles y permisos a pantallas

    public abstract class PermisoComponenteBE //Clase base para representar un componente de permisos, puede ser un perfil o un permiso a una pantalla
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public abstract IEnumerable<string> Pantallas();
    }

    public sealed class PermisoPantallaBE : PermisoComponenteBE  //Hoja del arbol de permisos, representa un permiso a una pantalla especifica
    {
        public string Pantalla { get; set; }
        public override IEnumerable<string> Pantallas() { yield return Pantalla; }
    }

    public sealed class RolBE : PerfilBE { } //Clase que representa un rol, hereda de PerfilBE para poder contener permisos y otros perfiles

    public class PerfilBE : PermisoComponenteBE //Clase que representa un perfil, puede contener otros perfiles y permisos a pantallas
    {
        private readonly List<PermisoComponenteBE> hijos = new List<PermisoComponenteBE>();
        public bool Sistema { get; set; }
        public string Version { get; set; }
        public IReadOnlyList<PermisoComponenteBE> Hijos { get { return hijos.AsReadOnly(); } }
        public void Agregar(PermisoComponenteBE hijo) //Metodo para agregar un hijo al perfil, verifica que no se creen ciclos en la jerarquia de permisos
        {
            if (hijo == null) throw new ArgumentNullException(nameof(hijo));
            if (ReferenceEquals(this, hijo) || Contiene(hijo, this)) throw new InvalidOperationException("El perfil no puede contener ciclos.");
            if (!hijos.Any(h => h.Id == hijo.Id)) hijos.Add(hijo);
        }
        private static bool Contiene(PermisoComponenteBE raiz, PermisoComponenteBE buscado) //Metodo recursivo para verificar si un perfil contiene a otro perfil en su jerarquia de permisos
        {
            if (ReferenceEquals(raiz, buscado)) return true;
            var perfil = raiz as PerfilBE;
            return perfil != null && perfil.Hijos.Any(h => Contiene(h, buscado));
        }

        //Metodo para obtener todas las pantallas a las que tiene acceso el perfil, incluyendo las de sus hijos, sin duplicados
        public override IEnumerable<string> Pantallas() { return hijos.SelectMany(h => h.Pantallas()).Distinct(StringComparer.OrdinalIgnoreCase); } 
    }
}
