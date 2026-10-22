using System;

namespace VitaPet.BE
{
    public sealed class CertificadoPagoBE
    {
        public string Emisor { get; set; }
        public string Serie { get; set; }
        public string Huella { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public byte[] Certificado { get; set; }
        public byte[] Autorizacion { get; set; }
    }
}
