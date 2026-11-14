using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using VitaPet.BE;

namespace VitaPet.SERVICES
{
    public sealed class CorreoFacturaService
    {
        public void Enviar(FacturaBE factura, string carpeta, bool reintentar = false)
        {
            factura.EstadoCorreo = "Pendiente";
            factura.DestinatarioCorreo = factura.CorreoFactura;
            if(string.IsNullOrWhiteSpace(factura.DestinatarioCorreo))
            { factura.EstadoCorreo="SinDestinatario"; return; }
            try
            {
                using (var smtp = new SmtpClient())
                {
                    string remitente = ConfigurationManager.AppSettings["CorreoRemitente"] ?? "vitapet.noreply@gmail.com";
                    if (smtp.DeliveryMethod == SmtpDeliveryMethod.Network)
                    {
                        string protegida = ConfigurationManager.AppSettings["CorreoClaveProtegida"];
                        if (string.IsNullOrWhiteSpace(protegida) || string.IsNullOrWhiteSpace(smtp.Host))
                        { factura.EstadoCorreo = "SinConfigurar"; return; }
                        string clave = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(protegida), null, DataProtectionScope.CurrentUser));
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(remitente, clave);
                        smtp.Timeout = 30000;
                    }
                    byte[] pdf = new FacturaPdfService().Generar(factura);
                    string identificador;
                    using (var hash = SHA256.Create())
                        identificador = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(
                            remitente + "\n" + factura.DestinatarioCorreo + "\n" + Convert.ToBase64String(pdf)))).Replace("-", "");
                    Directory.CreateDirectory(carpeta);
                    string archivo = Path.Combine(carpeta, identificador + ".estado");
                    using (var bloqueo = new FileStream(archivo + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        if (File.Exists(archivo))
                        {
                            string estado;
                            try
                            {
                                estado = Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(archivo), null, DataProtectionScope.CurrentUser));
                            }
                            catch (CryptographicException)
                            {
                                if (!reintentar) return;
                                File.Copy(archivo, archivo + ".anterior-" + Guid.NewGuid().ToString("N"), false);
                                estado = null;
                            }
                            if (estado == "Enviado") { factura.EstadoCorreo = estado; return; }
                            if (!reintentar) return;
                        }
                        Guardar(archivo, "Pendiente");
                        using (var mensaje = CrearMensaje(factura, remitente, factura.DestinatarioCorreo, pdf))
                            smtp.Send(mensaje);
                        factura.EstadoCorreo = "Enviado";
                        Guardar(archivo, "Enviado");
                    }
                }
            }
            catch (Exception ex) when (ex is SmtpException || ex is IOException || ex is UnauthorizedAccessException ||
                ex is CryptographicException || ex is FormatException || ex is ArgumentException || ex is ConfigurationErrorsException || ex is InvalidOperationException)
            {
                // El pago y la factura ya están confirmados; el correo se reintenta sin volver a cobrar.
            }
        }

        private static MailMessage CrearMensaje(FacturaBE f, string remitente, string destinatario, byte[] pdf)
        {
            var mensaje = new MailMessage(new MailAddress(remitente, "VitaPet"), new MailAddress(destinatario));
            mensaje.Subject = "VitaPet - Factura " + f.Numero;
            mensaje.SubjectEncoding = mensaje.BodyEncoding = Encoding.UTF8;
            mensaje.Body = string.Join("\r\n", new[] {
                "Hola, " + f.Cliente + ".", "", "Te enviamos la factura de tu contratación en VitaPet.",
                "Factura: " + f.Numero, "Plan: " + f.Plan, "Concepto: " + f.Concepto,
                "Mascotas: " + f.CantidadMascotas.ToString(CultureInfo.InvariantCulture),
                "Importe: " + f.Moneda + " " + f.Importe.ToString("0.00", CultureInfo.InvariantCulture),
                "Medio de pago: " + f.MedioPago,
                "Período: " + f.PeriodoDesde.ToString("dd/MM/yyyy") + " al " + f.PeriodoHasta.ToString("dd/MM/yyyy"),
                "", "Adjuntamos el comprobante en PDF para que puedas guardarlo o imprimirlo.", "", "VitaPet" });
            mensaje.Attachments.Add(new Attachment(new MemoryStream(pdf, false), "Factura-VitaPet-" + f.Numero + ".pdf", "application/pdf"));
            return mensaje;
        }

        private static void Guardar(string archivo, string estado)
        {
            byte[] contenido = ProtectedData.Protect(Encoding.UTF8.GetBytes(estado), null, DataProtectionScope.CurrentUser);
            string temporal = archivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporal, contenido);
                if (File.Exists(archivo)) File.Replace(temporal, archivo, null); else File.Move(temporal, archivo);
            }
            finally { if (File.Exists(temporal)) File.Delete(temporal); }
        }
    }
}
