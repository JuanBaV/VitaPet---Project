using System;

namespace VitaPet.BLL
{
    public sealed class RegistroClienteException : Exception
    {
        public string Codigo { get; private set; }
        public RegistroClienteException(string codigo, Exception inner = null)
            : base("No se pudo completar el registro (" + codigo + ").", inner)
        { Codigo = codigo; }
    }
}
