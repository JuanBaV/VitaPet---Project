using System;
namespace VitaPet.BLL
{
    public sealed class ConsultaUsuariosException : Exception
    {
        public string Codigo { get; private set; }
        public ConsultaUsuariosException(string codigo, string mensaje, Exception inner = null)
            : base(mensaje, inner) { Codigo = codigo; }
    }
}
