using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using VitaPet.BE;
using VitaPet.SERVICES;

static class CertificadosPagoTests
{
    static int checks;
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static void Rechaza(Action accion,string text)
    {bool fallo=false;try{accion();}catch(CryptographicException){fallo=true;}Check(fallo,text);}
    public static void Run()
    {
        string dir=Path.Combine(Path.GetTempPath(),"VitaPet_Certificados_"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var pago=new PagoBE{IdPago=7,IdSuscripcion=12,Importe=34.20m,Estado="Aprobado",MedioPago="Tarjeta de credito",FechaPago=new DateTime(2026,10,22,15,0,0)};
            var servicio=new CertificadoPagoService();var certificado=servicio.Obtener(pago,dir);
            using(var publico=new X509Certificate2(certificado.Certificado))
            {
                Check(!publico.HasPrivateKey,"Descarga CER sin clave privada");
                Check(publico.Subject==publico.Issuer,"Certificado autofirmado del emisor local");
                Check(publico.NotBefore<DateTime.Now&&publico.NotAfter>DateTime.Now,"Certificado vigente");
            }
            servicio.Verificar(pago,certificado.Autorizacion,certificado.Certificado);
            Check(true,"Autorización CMS con firma RSA y SHA-256 verificable");
            var repetido=servicio.Obtener(pago,dir);
            Check(certificado.Autorizacion.SequenceEqual(repetido.Autorizacion)&&certificado.Huella==repetido.Huella,"Reconsulta conserva certificado y firma originales");
            pago.Importe=99;Rechaza(()=>servicio.Verificar(pago,certificado.Autorizacion,certificado.Certificado),"Rechaza importe alterado");pago.Importe=34.20m;
            pago.IdSuscripcion++;Rechaza(()=>servicio.Verificar(pago,certificado.Autorizacion,certificado.Certificado),"Rechaza autorización de otra suscripción");pago.IdSuscripcion--;
            var cms=new SignedCms();cms.Decode(certificado.Autorizacion);
            byte[] alterado=(byte[])certificado.Autorizacion.Clone();byte[] contenido=cms.ContentInfo.Content;
            int indice=Enumerable.Range(0,alterado.Length-contenido.Length).First(i=>alterado.Skip(i).Take(contenido.Length).SequenceEqual(contenido));
            alterado[indice]^=1;
            Rechaza(()=>servicio.Verificar(pago,alterado,certificado.Certificado),"Rechaza contenido CMS alterado conservando la firma");
            File.WriteAllBytes(Directory.GetFiles(dir,"pago-7-*.p7m").Single(),alterado);
            Rechaza(()=>servicio.Obtener(pago,dir),"No vuelve a firmar una autorización almacenada adulterada");
            var otro=servicio.Obtener(pago,Path.Combine(dir,"otro"));
            Rechaza(()=>servicio.Verificar(pago,certificado.Autorizacion,otro.Certificado),"Rechaza certificado de otro emisor");
            pago.Importe=48;
            var restaurado=servicio.Obtener(pago,dir);
            servicio.Verificar(pago,restaurado.Autorizacion,restaurado.Certificado);
            Check(!restaurado.Autorizacion.SequenceEqual(certificado.Autorizacion),"Una restauración con ID reutilizado conserva autorizaciones distintas por datos del pago");
            Console.WriteLine("TOTAL: "+checks+" verificaciones de certificados");
        }
        finally{Directory.Delete(dir,true);}
    }
}
