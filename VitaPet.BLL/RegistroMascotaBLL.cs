using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
namespace VitaPet.BLL
{
    public sealed class RegistroMascotaException : Exception
    {
        public string Codigo { get; private set; }
        public RegistroMascotaException(string codigo,Exception inner=null)
            :base("No se pudo registrar la mascota ("+codigo+").",inner){Codigo=codigo;}
    }
    public sealed class RegistroMascotaBLL
    {
        private static bool Conexion(SqlException ex)
        {return ex.Class>=20||new[]{-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number);}
        public static RegistroMascotaBE Validar(RegistroMascotaBE datos)
        {
            if(datos==null||!new SecurityManager().ValidarDatosObligatorios(datos.Nombre,datos.Especie,datos.Raza)||!datos.FechaNacimiento.HasValue)
                throw new RegistroMascotaException("ER002");
            var d=new RegistroMascotaBE {Nombre=datos.Nombre.Trim(),Especie=datos.Especie.Trim(),Raza=datos.Raza.Trim(),FechaNacimiento=datos.FechaNacimiento.Value.Date};
            if(d.Nombre.Length>60||d.Especie.Length>40||d.Raza.Length>60||d.FechaNacimiento.Value>DateTime.Today||
                new[]{d.Nombre,d.Especie,d.Raza}.Any(x=>x.Any(char.IsControl)))throw new RegistroMascotaException("ER025");
            try
            {
                var encoding=Encoding.GetEncoding(1252,EncoderFallback.ExceptionFallback,DecoderFallback.ExceptionFallback);
                foreach(var texto in new[]{d.Nombre,d.Especie,d.Raza})encoding.GetBytes(texto);
            }
            catch(EncoderFallbackException ex){throw new RegistroMascotaException("ER025",ex);}
            return d;
        }
        public int Registrar(RegistroMascotaBE datos)
        {
            try
            {
                var seguridad=new SecurityManager();var usuario=seguridad.ExigirCliente();var d=Validar(datos);
                using(var op=new OperacionIntegridadDAL())
                {
                    seguridad.ExigirIntegridad();var dal=new MascotaDAL();dal.ObtenerClienteActivo(usuario.IdUsuario);
                    int id=dal.Registrar(usuario.IdUsuario,d);
                    IntegridadRegistroBLL.Actualizar("Mascota",id);
                    Auditar(seguridad,usuario.IdUsuario);seguridad.ExigirIntegridad();op.Complete();return id;
                }
            }
            catch(SqlException ex){throw new RegistroMascotaException(Conexion(ex)?"ER003":ex.Number==50011?"ER011":ex.Number==50002?"ER002":ex.Number==50025?"ER025":"ER060",ex);}
            catch(ConfigurationErrorsException ex){throw new RegistroMascotaException("ER003",ex);}
            catch(TransactionException ex){throw new RegistroMascotaException("ER060",ex);}
            catch(InvalidOperationException ex){throw new RegistroMascotaException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":"ER060",ex);}
        }
        private void Auditar(SecurityManager seguridad,int usuario)
        {
            try {seguridad.RegistrarBitacora(new BitacoraBE {IdUsuario=usuario,Fecha=DateTime.Now,Accion="Registro de mascota",Modulo="Mascotas",Criticidad="Informativa"});}
            catch(SqlException ex){throw new RegistroMascotaException(Conexion(ex)?"ER003":"ER040",ex);}
            catch(ConfigurationErrorsException ex){throw new RegistroMascotaException("ER003",ex);}
            catch(Exception ex) when(ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException)
            {throw new RegistroMascotaException("ER040",ex);}
            catch(InvalidOperationException ex){throw new RegistroMascotaException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":"ER040",ex);}
        }
    }
}
