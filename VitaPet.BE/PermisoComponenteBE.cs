using System;
using System.Collections.Generic;
using System.Linq;

namespace VitaPet.BE
{
    public abstract class PermisoComponenteBE
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public abstract IEnumerable<string> Pantallas();
    }

    public sealed class PermisoPantallaBE : PermisoComponenteBE
    {
        public string Pantalla { get; set; }
        public override IEnumerable<string> Pantallas() { yield return Pantalla; }
    }

    public sealed class PerfilBE : PermisoComponenteBE
    {
        private readonly List<PermisoComponenteBE> hijos = new List<PermisoComponenteBE>();
        public bool Sistema { get; set; }
        public string Version { get; set; }
        public IReadOnlyList<PermisoComponenteBE> Hijos { get { return hijos.AsReadOnly(); } }
        public void Agregar(PermisoComponenteBE hijo)
        {
            if (hijo == null) throw new ArgumentNullException(nameof(hijo));
            if (ReferenceEquals(this, hijo) || Contiene(hijo, this)) throw new InvalidOperationException("El perfil no puede contener ciclos.");
            if (!hijos.Any(h => h.Id == hijo.Id)) hijos.Add(hijo);
        }
        private static bool Contiene(PermisoComponenteBE raiz, PermisoComponenteBE buscado)
        {
            if (ReferenceEquals(raiz, buscado)) return true;
            var perfil = raiz as PerfilBE;
            return perfil != null && perfil.Hijos.Any(h => Contiene(h, buscado));
        }
        public override IEnumerable<string> Pantallas() { return hijos.SelectMany(h => h.Pantallas()).Distinct(StringComparer.OrdinalIgnoreCase); }
    }
}
