using System;
using System.Security.Cryptography;
using System.Text;
namespace VitaPet.SECURITY
{
    public static class ProteccionContingencia
    {
        private const int Iteraciones = 210000;
        public static string Aleatorio(int bytes)
        {
            var valor = new byte[bytes];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(valor);
            return Convert.ToBase64String(valor);
        }
        public static string HashContrasena(string valor, string sal)
        {
            using (var kdf = new Rfc2898DeriveBytes(valor, Convert.FromBase64String(sal), Iteraciones, HashAlgorithmName.SHA256))
                return Convert.ToBase64String(kdf.GetBytes(32));
        }
        public static bool Iguales(string a, string b)
        {
            if (a == null || b == null) return false;
            var x = Encoding.UTF8.GetBytes(a); var y = Encoding.UTF8.GetBytes(b);
            if (x.Length != y.Length) return false;
            int diferencia = 0;
            for (int i = 0; i < x.Length; i++) diferencia |= x[i] ^ y[i];
            return diferencia == 0;
        }
        public static string Firmar(string xml, string clave)
        {
            using (var h = new HMACSHA256(Convert.FromBase64String(clave)))
                return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(xml)));
        }
    }
}
