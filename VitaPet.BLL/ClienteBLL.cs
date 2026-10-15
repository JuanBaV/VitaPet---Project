using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public sealed class ClienteBLL
    {
        public int RegistrarCliente(RegistroClienteBE datos)
        {
            try
            {
                var cliente = ValidarDatosRegistro(datos);
                var seguridad = new SecurityManager();
                using (var operacion = new OperacionIntegridadDAL())
                {
                    seguridad.ExigirIntegridad();
                    var usuarios = new UsuarioDAL();
                    if (usuarios.ObtenerPorCorreo(cliente.CorreoElectronico) != null)
                        throw new RegistroClienteException("ER001");
                    var usuario = new UsuarioBE {
                        Nombre = cliente.Nombre + " " + cliente.Apellido,
                        CorreoElectronico = cliente.CorreoElectronico,
                        Contrasena = seguridad.CalcularHash(cliente.Contrasena),
                        Rol = "Cliente", Estado = true, Bloqueado = false,
                        IntentosFallidos = 0, Idioma = "es"
                    };
                    var dal = new ClienteDAL();
                    usuario.IdUsuario = usuarios.RegistrarCliente(usuario);
                    // El DVH incluye el identificador generado por SQL Server.
                    usuario.DVH = seguridad.CalcularDVH(usuario);
                    usuarios.ActualizarDVH(usuario.IdUsuario, usuario.DVH);
                    int idCliente = dal.Registrar(usuario.IdUsuario, cliente);
                    var tablas = new IntegridadTablasDAL();
                    var filas = tablas.Leer("Cliente");
                    var fila = filas.Rows.Cast<DataRow>().Single(r => (int)r["idCliente"] == idCliente);
                    string dvh = new IntegridadTablasBLL().CalcularDVH(fila);
                    tablas.GuardarDvh("Cliente", idCliente, dvh);
                    fila["dvh"] = dvh;
                    var dvv = new DVVDAL();
                    dvv.Guardar(new DVVBE { NombreTabla = "Usuario", ValorDVV = seguridad.CalcularDVV(usuarios.ObtenerDVHs()) });
                    dvv.Guardar(new DVVBE { NombreTabla = "Cliente", ValorDVV = seguridad.CalcularDVV(filas.Rows.Cast<DataRow>().Select(r => (string)r["dvh"])) });
                    // RegistrarBitacora verifica la integridad nuevamente: publicar las
                    // referencias de la escritura legítima dentro de la misma transacción.
                    new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                    Auditar(seguridad, usuario.IdUsuario);
                    seguridad.ExigirIntegridad();
                    operacion.Complete();
                    return usuario.IdUsuario;
                }
            }
            catch (SqlException ex)
            {
                throw new RegistroClienteException(EsConexion(ex) ? "ER003" :
                    ex.Number == 50001 || ex.Number == 2601 || ex.Number == 2627 ? "ER001" : "ER049", ex);
            }
            catch (ConfigurationErrorsException ex) { throw new RegistroClienteException("ER003", ex); }
            catch (TransactionException ex) { throw new RegistroClienteException("ER049", ex); }
            catch (InvalidOperationException ex)
            { throw new RegistroClienteException(ex.Message.StartsWith("ER023", StringComparison.Ordinal) ? "ER023" : "ER049", ex); }
        }

        public static RegistroClienteBE ValidarDatosRegistro(RegistroClienteBE datos)
        {
            var seguridad = new SecurityManager();
            if (datos == null || !seguridad.ValidarDatosObligatorios(datos.Nombre, datos.Apellido,
                datos.CorreoElectronico, datos.Contrasena, datos.Telefono))
                throw new RegistroClienteException("ER002");
            var c = new RegistroClienteBE { Nombre = datos.Nombre.Trim(), Apellido = datos.Apellido.Trim(),
                CorreoElectronico = datos.CorreoElectronico.Trim(), Contrasena = datos.Contrasena, Telefono = datos.Telefono.Trim() };
            if (c.Nombre.Length > 60 || c.Apellido.Length > 60 || (c.Nombre + " " + c.Apellido).Length > 100 ||
                c.CorreoElectronico.Length > 100 || c.Contrasena.Length > 100 || c.Telefono.Length > 30 ||
                c.Nombre.Any(char.IsControl) || c.Apellido.Any(char.IsControl) || c.CorreoElectronico.Any(char.IsWhiteSpace) ||
                !Regex.IsMatch(c.CorreoElectronico, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") ||
                !Regex.IsMatch(c.Telefono, @"^\+?[0-9 ()\-\.]+$") || c.Telefono.Count(char.IsDigit) < 6 ||
                !seguridad.CumpleRequisitosContrasena(c.Contrasena))
                throw new RegistroClienteException("ER025");
            try
            {
                if (new MailAddress(c.CorreoElectronico).Address != c.CorreoElectronico)
                    throw new RegistroClienteException("ER025");
                // El esquema usa VARCHAR: rechazar valores que SQL convertiría con pérdida.
                var encoding = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                foreach (string valor in new[] { c.Nombre, c.Apellido, c.CorreoElectronico, c.Telefono }) encoding.GetBytes(valor);
            }
            catch (FormatException ex) { throw new RegistroClienteException("ER025", ex); }
            catch (EncoderFallbackException ex) { throw new RegistroClienteException("ER025", ex); }
            return c;
        }

        private static void Auditar(SecurityManager seguridad, int idUsuario)
        {
            try
            {
                seguridad.RegistrarBitacora(new BitacoraBE { IdUsuario = idUsuario, Fecha = DateTime.Now,
                    Accion = "Registro de cliente", Modulo = "Clientes", Criticidad = "Informativa" });
            }
            catch (SqlException ex) { throw new RegistroClienteException(EsConexion(ex) ? "ER003" : "ER040", ex); }
            catch (ConfigurationErrorsException ex) { throw new RegistroClienteException("ER003", ex); }
            catch (Exception ex) when (ex is IOException || ex is XmlException || ex is UnauthorizedAccessException || ex is TransactionException)
            { throw new RegistroClienteException("ER040", ex); }
            catch (InvalidOperationException ex)
            { throw new RegistroClienteException(ex.Message.StartsWith("ER023", StringComparison.Ordinal) ? "ER023" : "ER040", ex); }
        }

        private static bool EsConexion(SqlException ex)
        { return ex.Class >= 20 || new[] { -2, 2, 20, 53, 64, 233, 4060, 18456, 10053, 10054, 10060 }.Contains(ex.Number); }
    }
}
