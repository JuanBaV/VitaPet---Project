using System.Configuration;
using System.Data.SqlClient;

namespace VitaPet.DAL
{
    public class ConexionDAL
    {
        [System.ThreadStatic] private static string baseTemporal;
        public static System.IDisposable UsarBaseTemporal(string nombre)
        {
            if(!nombre.StartsWith("VitaPet_Verificacion_",System.StringComparison.Ordinal))
                throw new System.ArgumentException("Solo se permiten bases temporales de verificación.");
            return new DestinoTemporal(nombre);
        }
        private sealed class DestinoTemporal : System.IDisposable
        {
            private readonly string anterior;
            public DestinoTemporal(string nombre) { anterior=baseTemporal; baseTemporal=nombre; }
            public void Dispose() { baseTemporal=anterior; }
        }
        private readonly string _cadenaConexion;

        public ConexionDAL()
        {
            ConnectionStringSettings configuracion =
                ConfigurationManager.ConnectionStrings["VitaPetConnection"];

            if (configuracion == null ||
                string.IsNullOrWhiteSpace(configuracion.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "No se encontró la conexión VitaPetConnection.");
            }

            _cadenaConexion = configuracion.ConnectionString;
        }

        public SqlConnection CrearConexion()
        {
            var builder=new SqlConnectionStringBuilder(_cadenaConexion);
            if(baseTemporal!=null) builder.InitialCatalog=baseTemporal;
            var conexion = new SqlConnection(builder.ConnectionString);
            conexion.StateChange += (sender, e) => {
                if (e.CurrentState == System.Data.ConnectionState.Open)
                    CifradoDatosDAL.Preparar((SqlConnection)sender);
            };
            return conexion;
        }
    }
}
