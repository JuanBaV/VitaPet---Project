using System;
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Web.Compilation;

namespace VitaPet.GUI.Resources
{
    // Las expresiones Resources:Textos utilizan la misma DLL que los codebehind.
    public sealed class VitaPetResourceProviderFactory : ResourceProviderFactory
    {
        public override IResourceProvider CreateGlobalResourceProvider(string classKey)
        {
            if (!string.Equals(classKey, "Textos", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Grupo de recursos no registrado.", "classKey");
            return new ProveedorTextos();
        }

        public override IResourceProvider CreateLocalResourceProvider(string virtualPath)
        {
            return new ProveedorTextos();
        }

        private sealed class ProveedorTextos : IResourceProvider
        {
            public object GetObject(string resourceKey, CultureInfo culture)
            {
                return Textos.ResourceManager.GetObject(resourceKey,
                    culture ?? CultureInfo.CurrentUICulture);
            }

            public IResourceReader ResourceReader
            {
                get
                {
                    return new LectorRecursos(Textos.ResourceManager.GetResourceSet(
                        CultureInfo.InvariantCulture, true, true));
                }
            }
        }

        private sealed class LectorRecursos : IResourceReader
        {
            private readonly Hashtable valores = new Hashtable();
            public LectorRecursos(ResourceSet recursos)
            {
                foreach (DictionaryEntry entry in recursos) valores.Add(entry.Key, entry.Value);
            }
            public IDictionaryEnumerator GetEnumerator() { return valores.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
            public void Close() { }
            public void Dispose() { }
        }
    }
}
