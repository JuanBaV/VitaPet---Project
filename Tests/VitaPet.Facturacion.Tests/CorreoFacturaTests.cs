using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Net.Mail;
using System.Text;
using VitaPet.BE;
using VitaPet.SERVICES;

static class CorreoFacturaTests
{
    static int checks;
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    public static void Run()
    {
        string dir=Path.Combine(Path.GetTempPath(),"VitaPet_Correo_"+Guid.NewGuid().ToString("N"));
        string salida=Path.Combine(dir,"salida"),estado=Path.Combine(dir,"estados");
        Directory.CreateDirectory(salida);
        var cfg=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        byte[] original=File.ReadAllBytes(cfg.FilePath);
        try
        {
            var smtp=(SmtpSection)cfg.GetSection("system.net/mailSettings/smtp");
            smtp.DeliveryMethod=SmtpDeliveryMethod.SpecifiedPickupDirectory;
            smtp.SpecifiedPickupDirectory.PickupDirectoryLocation=salida;
            smtp.Network.EnableSsl=false;
            cfg.AppSettings.Settings.Remove("CorreoDestinoPrueba");
            cfg.AppSettings.Settings.Add("CorreoDestinoPrueba","fijo-que-no-debe-usarse@example.test");
            cfg.Save();ConfigurationManager.RefreshSection("system.net/mailSettings/smtp");ConfigurationManager.RefreshSection("appSettings");
            var f=new FacturaBE {IdFactura=1,IdPago=1,IdSuscripcion=1,Numero="0001-0000000001",Cliente="Cliente de prueba",
                Correo="cliente@example.test",CorreoFactura="destino@example.test",Plan="Plan Esencial",Concepto="Suscripción mensual",PeriodoDesde=new DateTime(2026,10,22),
                PeriodoHasta=new DateTime(2026,11,21),FechaEmision=new DateTime(2026,10,22),PrecioMascota=19,PrecioMascotaAdicional=15.2m,
                CantidadMascotas=2,Importe=34.2m,Moneda="USD",MedioPago="Tarjeta de credito",Titular="Prueba Prueba"};
            var servicio=new CorreoFacturaService();servicio.Enviar(f,estado);
            Check(f.EstadoCorreo=="Enviado","Entrega SMTP de prueba aceptada sin contactar proveedores externos");
            string archivo=Directory.GetFiles(salida).Single();string mensaje=File.ReadAllText(archivo);
            Check(mensaje.Contains("vitapet.noreply@gmail.com")&&mensaje.Contains("destino@example.test"),"Remitente y destinatario correctos");
            Check(!mensaje.Contains("fijo-que-no-debe-usarse@example.test"),"Ignora el destinatario fijo de prueba anterior y usa el correo del pago");
            Check(mensaje.Contains("application/pdf")&&mensaje.Contains("Factura-VitaPet-0001-0000000001.pdf"),"Factura PDF adjunta con nombre y tipo correctos");
            Check(mensaje.Replace("\r","").Replace("\n","").Contains(Convert.ToBase64String(new FacturaPdfService().Generar(f))),"Adjunto coincide exactamente con el PDF descargable");
            Check(!mensaje.Contains("CorreoClaveProtegida"),"Mensaje no expone configuración ni credenciales");
            servicio.Enviar(f,estado);servicio.Enviar(f,estado,true);
            Check(Directory.GetFiles(salida).Length==1&&f.EstadoCorreo=="Enviado","Reconsulta y reintento no duplican un correo aceptado");
            File.Delete(archivo);Directory.Delete(salida);
            f.IdFactura=2;f.Numero="0001-0000000002";servicio.Enviar(f,estado);
            Check(f.EstadoCorreo=="Pendiente"&&f.Importe==34.2m,"Fallo de entrega conserva los datos de la factura");
            Directory.CreateDirectory(salida);servicio.Enviar(f,estado);
            Check(Directory.GetFiles(salida).Length==0,"No repite automáticamente un envío cuyo resultado no fue confirmado");
            servicio.Enviar(f,estado,true);
            Check(f.EstadoCorreo=="Enviado"&&Directory.GetFiles(salida).Length==1,"Reintento explícito entrega la factura pendiente sin volver a cobrar");
            Console.WriteLine("TOTAL: "+checks+" verificaciones de correo (captura local; no demuestra recepción real)");
        }
        finally
        {
            File.WriteAllBytes(cfg.FilePath,original);
            ConfigurationManager.RefreshSection("system.net/mailSettings/smtp");
            if(!Path.GetFullPath(dir).StartsWith(Path.GetFullPath(Path.GetTempPath())+"VitaPet_Correo_",StringComparison.OrdinalIgnoreCase))throw new Exception("Ruta temporal inesperada");
            Directory.Delete(dir,true);
        }
    }
}
