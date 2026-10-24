using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using VitaPet.BE;

namespace VitaPet.BLL
{
    public class IntegridadBLL
    {
        public string CalcularHash(string valor)
        {
            if (valor == null)
            {
                throw new ArgumentNullException(nameof(valor));
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(valor);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder resultado = new StringBuilder(hash.Length * 2);

                foreach (byte byteHash in hash)
                {
                    resultado.Append(byteHash.ToString("x2"));
                }

                return resultado.ToString();
            }
        }

        public string CalcularDVH(UsuarioBE usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException(nameof(usuario));
            }

            string cadena = ConstruirCadena(
                usuario.IdUsuario.ToString(CultureInfo.InvariantCulture),
                usuario.CorreoElectronico,
                usuario.Contrasena,
                usuario.IdRol.ToString(CultureInfo.InvariantCulture),
                ConvertirBooleano(usuario.Estado),
                usuario.Nombre,
                usuario.IntentosFallidos.ToString(CultureInfo.InvariantCulture),
                ConvertirBooleano(usuario.Bloqueado),
                usuario.Idioma);

            // Cero es el valor canónico implícito de las cuentas anteriores.
            // El valor verdadero amplía el hash sin invalidar DVH históricos válidos.
            if (usuario.RequiereRestablecerContrasena)
                cadena += ConstruirCadena("requiereRestablecerContrasena", "1");
            return CalcularHash(cadena);
        }

        public string CalcularDVH(BitacoraBE registro)
        {
            if (registro == null)
            {
                throw new ArgumentNullException(nameof(registro));
            }

            string cadena = ConstruirCadena(
                registro.IdUsuario.HasValue
                    ? registro.IdUsuario.Value.ToString(CultureInfo.InvariantCulture)
                    : string.Empty,
                registro.Fecha.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                registro.Accion,
                registro.Modulo,
                registro.Criticidad);

            // Falso conserva los DVH anteriores a la baja lógica.
            if (registro.Archivado) cadena += ConstruirCadena("archivado", "1");
            return CalcularHash(cadena);
        }

        public string CalcularDVV(IEnumerable<string> digitosHorizontales)
        {
            if (digitosHorizontales == null)
            {
                throw new ArgumentNullException(nameof(digitosHorizontales));
            }

            StringBuilder cadena = new StringBuilder();

            foreach (string digitoHorizontal in digitosHorizontales)
            {
                AgregarValor(cadena, digitoHorizontal);
            }

            return CalcularHash(cadena.ToString());
        }

        public bool VerificarDVH(UsuarioBE usuario)
        {
            if (usuario == null)
            {
                throw new ArgumentNullException(nameof(usuario));
            }

            string dvhCalculado = CalcularDVH(usuario);

            return string.Equals(
                dvhCalculado,
                usuario.DVH,
                StringComparison.OrdinalIgnoreCase);
        }

        public bool VerificarDVH(BitacoraBE registro)
        {
            if (registro == null)
            {
                throw new ArgumentNullException(nameof(registro));
            }

            string dvhCalculado = CalcularDVH(registro);

            return string.Equals(
                dvhCalculado,
                registro.DVH,
                StringComparison.OrdinalIgnoreCase);
        }

        public bool VerificarDVV(
            IEnumerable<string> digitosHorizontales,
            string dvvAlmacenado)
        {
            if (digitosHorizontales == null)
            {
                throw new ArgumentNullException(nameof(digitosHorizontales));
            }

            string dvvCalculado = CalcularDVV(digitosHorizontales);

            return string.Equals(
                dvvCalculado,
                dvvAlmacenado,
                StringComparison.OrdinalIgnoreCase);
        }

        private string ConstruirCadena(params string[] valores)
        {
            StringBuilder cadena = new StringBuilder();

            foreach (string valor in valores)
            {
                AgregarValor(cadena, valor);
            }

            return cadena.ToString();
        }

        private void AgregarValor(StringBuilder cadena, string valor)
        {
            string valorNormalizado = valor ?? string.Empty;

            cadena.Append(
                valorNormalizado.Length.ToString(CultureInfo.InvariantCulture));
            cadena.Append(':');
            cadena.Append(valorNormalizado);
        }

        private string ConvertirBooleano(bool valor)
        {
            return valor ? "1" : "0";
        }
    }
}
