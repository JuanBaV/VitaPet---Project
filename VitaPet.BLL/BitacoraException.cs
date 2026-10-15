using System;
namespace VitaPet.BLL
{
    public sealed class BitacoraException : Exception
    {
        public string Codigo { get; private set; }
        public BitacoraException(string codigo, string mensaje, Exception inner = null)
            : base(mensaje, inner) { Codigo = codigo; }
    }
}
