using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Transactions;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public sealed class ConsultaVirtualException : Exception
    {
        public string Codigo { get; private set; }
        public ConsultaVirtualException(string codigo) : base(codigo) { Codigo = codigo; }
    }

    public sealed class ConsultaVirtualBLL
    {
        public const int MaximoImagenBytes = 3 * 1024 * 1024;
        public const int MaximoTexto = 2000;

        // La identidad siempre sale de la sesión y el plan se relee antes de cada envío.
        // La transacción termina antes de esperar la respuesta de Gemini.
        public UsuarioBE ExigirAcceso() => VerificarAcceso(false);

        public UsuarioBE IniciarConsulta() => VerificarAcceso(true);

        private UsuarioBE VerificarAcceso(bool registrarInicio)
        {
            try
            {
                using (var operacion = new OperacionIntegridadDAL())
                {
                    var seguridad = new SecurityManager();
                    var usuario = seguridad.ExigirPermiso("Cliente");
                    if (new ConsultaCoberturaDAL().ObtenerVigente(usuario.IdUsuario) == null)
                        throw new ConsultaVirtualException("SinPlan");
                    if (registrarInicio)
                    {
                        try
                        {
                            seguridad.RegistrarBitacora(new BitacoraBE
                            {
                                IdUsuario = usuario.IdUsuario, Fecha = DateTime.Now,
                                Accion = "Inicio de consulta virtual", Modulo = "Consulta virtual",
                                Criticidad = "Informativa"
                            });
                            seguridad.ExigirIntegridad();
                        }
                        catch (SqlException) { throw new ConsultaVirtualException("Bitacora"); }
                        catch (Exception ex) when (ex is BitacoraException || ex is IOException ||
                            ex is XmlException || ex is UnauthorizedAccessException ||
                            ex is System.Security.SecurityException || ex is ConfigurationErrorsException)
                        { throw new ConsultaVirtualException("Bitacora"); }
                    }
                    operacion.Complete();
                    return usuario;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 50008 || ex.Number == 50011)
                    throw new ConsultaVirtualException("SinPlan");
                throw new ConsultaVirtualException("Verificacion");
            }
            catch (ConfigurationErrorsException) { throw new ConsultaVirtualException("Verificacion"); }
            catch (TransactionException) { throw new ConsultaVirtualException(registrarInicio ? "Bitacora" : "Verificacion"); }
            catch (InvalidOperationException) { throw new ConsultaVirtualException("Verificacion"); }
        }

        public MensajeConsultaVirtualBE PrepararMensaje(string texto, byte[] foto)
        {
            texto = (texto ?? "").Trim();
            if (texto.Length > MaximoTexto) throw new ConsultaVirtualException("TextoLargo");
            if (texto.Length == 0 && (foto == null || foto.Length == 0))
                throw new ConsultaVirtualException("Vacio");
            return new MensajeConsultaVirtualBE
            {
                Rol = "user", Texto = texto,
                ImagenBase64 = foto == null ? null : PrepararFoto(foto)
            };
        }

        private static string PrepararFoto(byte[] foto)
        {
            if (foto.Length == 0 || foto.Length > MaximoImagenBytes)
                throw new ConsultaVirtualException("ImagenGrande");
            try
            {
                using (var entrada = new MemoryStream(foto, false))
                using (var imagen = Image.FromStream(entrada, false, true))
                {
                    if (imagen.RawFormat.Guid != ImageFormat.Jpeg.Guid && imagen.RawFormat.Guid != ImageFormat.Png.Guid)
                        throw new ConsultaVirtualException("ImagenInvalida");
                    if (imagen.Width > 8000 || imagen.Height > 8000 || (long)imagen.Width * imagen.Height > 24000000)
                        throw new ConsultaVirtualException("ImagenGrande");
                    // Aplicar orientación antes de quitar metadatos (incluido GPS).
                    if (Array.IndexOf(imagen.PropertyIdList, 0x112) >= 0)
                    {
                        var orientacion = imagen.GetPropertyItem(0x112).Value[0];
                        var giros = new[] { RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipNone,
                            RotateFlipType.RotateNoneFlipX, RotateFlipType.Rotate180FlipNone, RotateFlipType.Rotate180FlipX,
                            RotateFlipType.Rotate90FlipX, RotateFlipType.Rotate90FlipNone, RotateFlipType.Rotate270FlipX,
                            RotateFlipType.Rotate270FlipNone };
                        if (orientacion < giros.Length) imagen.RotateFlip(giros[orientacion]);
                    }
                    double escala = Math.Min(1, 1600.0 / Math.Max(imagen.Width, imagen.Height));
                    using (var limpia = new Bitmap(Math.Max(1, (int)(imagen.Width * escala)), Math.Max(1, (int)(imagen.Height * escala))))
                    using (var salida = new MemoryStream())
                    {
                        using (var grafico = Graphics.FromImage(limpia))
                        {
                            grafico.Clear(Color.White);
                            grafico.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            grafico.DrawImage(imagen, 0, 0, limpia.Width, limpia.Height);
                        }
                        limpia.Save(salida, ImageFormat.Jpeg);
                        return Convert.ToBase64String(salida.ToArray());
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentException || ex is System.Runtime.InteropServices.ExternalException || ex is OutOfMemoryException)
            { throw new ConsultaVirtualException("ImagenInvalida"); }
        }
    }
}
