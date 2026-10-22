using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using VitaPet.BE;

namespace VitaPet.SERVICES
{
    public sealed class CertificadoPagoService
    {
        private static readonly object bloqueo = new object();

        public CertificadoPagoBE Obtener(PagoBE pago, string carpeta)
        {
            if (pago == null || pago.IdPago <= 0 || pago.Estado != "Aprobado")
                throw new InvalidOperationException("El pago no está aprobado.");
            lock (bloqueo)
            {
                Directory.CreateDirectory(carpeta);
                string clave = Path.Combine(carpeta, "emisor.pfx");
                if (!File.Exists(clave))
                {
                    // Clave local del simulador; nunca se distribuye ni se instala como autoridad de confianza.
                    using (var rsa = RSA.Create(2048))
                    {
                        var solicitud = new CertificateRequest("CN=VitaPet Autorizacion Pagos, O=VitaPet", rsa,
                            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                        solicitud.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
                        using (var certificado = solicitud.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(5)))
                            Guardar(clave, certificado.Export(X509ContentType.Pfx));
                    }
                }
                using (var certificado = new X509Certificate2(File.ReadAllBytes(clave), (string)null, X509KeyStorageFlags.EphemeralKeySet))
                {
                    // Una restauración puede reutilizar IDs; la autorización identifica también los datos del pago.
                    string archivo = Path.Combine(carpeta, "pago-" + pago.IdPago.ToString(CultureInfo.InvariantCulture) + "-" + Huella(Contenido(pago)) + ".p7m");
                    if (!File.Exists(archivo))
                    {
                        if (DateTime.Now < certificado.NotBefore || DateTime.Now > certificado.NotAfter)
                            throw new CryptographicException("El certificado no está vigente para firmar.");
                        var firma = new SignedCms(new ContentInfo(Contenido(pago)), false);
                        var firmante = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificado);
                        firmante.DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1");
                        firmante.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.UtcNow));
                        firma.ComputeSignature(firmante);
                        Guardar(archivo, firma.Encode());
                    }
                    byte[] autorizacion = File.ReadAllBytes(archivo);
                    Verificar(pago, autorizacion, certificado.RawData);
                    return new CertificadoPagoBE {
                        Emisor = certificado.Issuer, Serie = certificado.SerialNumber,
                        Huella = Huella(certificado.RawData),
                        Desde = certificado.NotBefore, Hasta = certificado.NotAfter,
                        Certificado = certificado.RawData, Autorizacion = autorizacion
                    };
                }
            }
        }

        public void Verificar(PagoBE pago, byte[] autorizacion, byte[] certificadoEsperado)
        {
            var firma = new SignedCms();
            firma.Decode(autorizacion);
            // La confianza es local al simulador; no se afirma validación por una entidad bancaria.
            firma.CheckSignature(true);
            if (firma.SignerInfos.Count != 1 || firma.SignerInfos[0].Certificate == null ||
                !firma.SignerInfos[0].Certificate.RawData.SequenceEqual(certificadoEsperado) ||
                !firma.ContentInfo.Content.SequenceEqual(Contenido(pago)))
                throw new CryptographicException("La autorización no corresponde al pago o al emisor.");
        }

        private static byte[] Contenido(PagoBE pago)
        {
            return Encoding.UTF8.GetBytes("VitaPet Autorizacion de pago v1\n" +
                pago.IdPago.ToString(CultureInfo.InvariantCulture) + "\n" +
                pago.IdSuscripcion.ToString(CultureInfo.InvariantCulture) + "\n" +
                pago.Importe.ToString("0.00", CultureInfo.InvariantCulture) + "\nUSD\n" +
                pago.FechaPago.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + "\n" +
                pago.MedioPago + "\n" + pago.Estado);
        }

        private static string Huella(byte[] datos)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(datos)).Replace("-", "");
        }

        private static void Guardar(string archivo, byte[] datos)
        {
            string temporal = archivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(temporal, datos); File.Move(temporal, archivo); }
            finally { if (File.Exists(temporal)) File.Delete(temporal); }
        }
    }
}
