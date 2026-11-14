using System;
using System.Collections.Generic;

namespace VitaPet.BE
{
    [Serializable]
    public sealed class MensajeConsultaVirtualBE
    {
        public string Rol { get; set; }
        public string Texto { get; set; }
        public string ImagenBase64 { get; set; }
    }

    [Serializable]
    public sealed class ConversacionVirtualBE
    {
        public int IdUsuario { get; set; }
        public DateTime? UltimoEnvioUtc { get; set; }
        public List<MensajeConsultaVirtualBE> Mensajes { get; set; } = new List<MensajeConsultaVirtualBE>();
    }
}
