using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
namespace VitaPet.BLL
{
    public sealed class ClienteCorporativoException:Exception
    {
        public string Codigo {get;private set;}
        public ClienteCorporativoException(string codigo,Exception inner=null):base("Registro corporativo ("+codigo+").",inner){Codigo=codigo;}
    }
    public sealed class ClienteCorporativoBLL
    {
        private static bool Conexion(SqlException ex){return ex.Class>=20||new[]{-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number);}
        private static T Ejecutar<T>(Func<int,SecurityManager,T> accion,string fallo)
        {
            try
            {
                var sec=new SecurityManager();var actor=sec.ExigirAdministrador();
                using(var tx=new OperacionIntegridadDAL())
                {sec.ExigirIntegridad();var result=accion(actor.IdUsuario,sec);sec.ExigirIntegridad();tx.Complete();return result;}
            }
            catch(SqlException ex)
            {
                if(ex.Number==50024)throw new UnauthorizedAccessException("Acceso exclusivo para Administrador.",ex);
                string code=Conexion(ex)?"ER003":ex.Number==50001?"ER001":ex.Number==50002?"ER002":ex.Number==50025?"ER025":
                    ex.Number==50011?"ER011":ex.Number==50072?"ER072":ex.Number==50073?"ER073":fallo;
                throw new ClienteCorporativoException(code,ex);
            }
            catch(ConfigurationErrorsException ex){throw new ClienteCorporativoException("ER003",ex);}
            catch(TransactionException ex){throw new ClienteCorporativoException(fallo,ex);}
            catch(CryptographicException ex){throw new ClienteCorporativoException("ER049",ex);}
            catch(InvalidOperationException ex){throw new ClienteCorporativoException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":fallo,ex);}
        }
        private static T Consulta<T>(Func<T> action,string fallo)
        {
            try{return action();}
            catch(SqlException ex)
            {
                if(ex.Number==50024)throw;
                throw new ClienteCorporativoException(Conexion(ex)?"ER003":ex.Number==50011?"ER011":fallo,ex);
            }
        }
        private static EmpresaCorporativaBE Preparar(int actor,int id)
        {
            if(id<=0)throw new ClienteCorporativoException("ER011");
            var empresa=Consulta(()=>new EmpresaDAL().Consultar(actor,id),"ER067");
            if(empresa==null)throw new ClienteCorporativoException("ER011");
            if(!empresa.Estado)throw new ClienteCorporativoException("ER072");
            var plan=Consulta(()=>new ClienteCorporativoDAL().ConsultarPlan(actor),"ER074");
            if(plan==null)throw new ClienteCorporativoException("ER073");
            plan.Empresa=empresa;return plan;
        }
        public List<EmpresaBE> ListarEmpresas()
        {
            return Ejecutar((actor,sec)=>{
                var list=Consulta(()=>new EmpresaDAL().Listar(actor),"ER067");
                if(list.Count==0)throw new ClienteCorporativoException("ER017");return list;
            },"ER067");
        }
        public EmpresaCorporativaBE SeleccionarEmpresa(int empresa){return Ejecutar((actor,sec)=>Preparar(actor,empresa),"ER067");}
        public static string NormalizarCorreo(string correo)
        {
            if(string.IsNullOrWhiteSpace(correo))throw new ClienteCorporativoException("ER002");
            correo=correo.Trim();
            if(correo.Length>100||correo.Any(char.IsWhiteSpace)||!Regex.IsMatch(correo,@"\A[^@\s]+@[^@\s]+\.[^@\s]+\z"))throw new ClienteCorporativoException("ER025");
            try
            {
                if(new MailAddress(correo).Address!=correo)throw new ClienteCorporativoException("ER025");
                Encoding.GetEncoding(1252,EncoderFallback.ExceptionFallback,DecoderFallback.ExceptionFallback).GetBytes(correo);
            }
            catch(Exception ex)when(ex is FormatException||ex is EncoderFallbackException){throw new ClienteCorporativoException("ER025",ex);}
            return correo;
        }
        public string ValidarCorreo(int empresa,string correo)
        {
            return Ejecutar((actor,sec)=>{
                var mail=NormalizarCorreo(correo);Preparar(actor,empresa);Unico(actor,mail);return mail;
            },"ER074");
        }
        private static void Unico(int actor,string correo)
        {
            if(Consulta(()=>new ClienteCorporativoDAL().CorreoRegistrado(actor,correo),"ER074"))throw new ClienteCorporativoException("ER001");
        }
        public static DatosClienteCorporativoBE Validar(DatosClienteCorporativoBE d)
        {
            if(d==null)throw new ClienteCorporativoException("ER002");
            try
            {
                // Reutiliza las mismas reglas de datos personales del registro particular.
                // Esta contraseña solo permite ejecutar el validador; nunca se persiste.
                var r=ClienteBLL.ValidarDatosRegistro(new RegistroClienteBE{Nombre=d.Nombre,Apellido=d.Apellido,CorreoElectronico=d.Correo,Telefono=d.Telefono,Contrasena="Validacion1!"});
                return new DatosClienteCorporativoBE{IdEmpresa=d.IdEmpresa,Nombre=r.Nombre,Apellido=r.Apellido,Correo=r.CorreoElectronico,Telefono=r.Telefono};
            }
            catch(RegistroClienteException ex){throw new ClienteCorporativoException(ex.Codigo,ex);}
        }
        private static string HashAutomatico(SecurityManager sec)
        {
            var bytes=new byte[32];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(bytes);
            return sec.CalcularHash(Convert.ToBase64String(bytes)+"Aa1!");
        }
        private static DataRow Integridad(string tabla,int id,SecurityManager sec)
        {
            var dal=new IntegridadTablasDAL();var data=dal.Leer(tabla);var fila=data.Rows.Cast<DataRow>().Single(r=>(int)r[IntegridadTablasDAL.Claves[tabla]]==id);
            var hash=new IntegridadTablasBLL().CalcularDVH(fila);dal.GuardarDvh(tabla,id,hash);fila["dvh"]=hash;
            new DVVDAL().Guardar(new DVVBE{NombreTabla=tabla,ValorDVV=sec.CalcularDVV(data.Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))});return fila;
        }
        public AltaClienteCorporativoBE Registrar(DatosClienteCorporativoBE datos)
        {
            return Ejecutar((actor,sec)=>{
                var d=Validar(datos);var prep=Preparar(actor,d.IdEmpresa);Unico(actor,d.Correo);
                var dal=new ClienteCorporativoDAL();int usuario=dal.RegistrarUsuario(actor,d.Nombre+" "+d.Apellido,d.Correo,HashAutomatico(sec));
                var usuarios=new UsuarioDAL();var u=usuarios.ObtenerPorId(usuario);usuarios.ActualizarDVH(usuario,sec.CalcularDVH(u));
                int cliente=dal.RegistrarCliente(actor,usuario,d);Integridad("Cliente",cliente,sec);
                int suscripcion;
                try{suscripcion=dal.AsignarPlan(actor,cliente,prep.IdPlan);}
                catch(SqlException ex){if(ex.Number==50024||ex.Number==50072||ex.Number==50073)throw;throw new ClienteCorporativoException(Conexion(ex)?"ER003":"ER007",ex);}
                var sub=Integridad("Suscripcion",suscripcion,sec);
                new DVVDAL().Guardar(new DVVBE{NombreTabla="Usuario",ValorDVV=sec.CalcularDVV(usuarios.ObtenerDVHs())});
                new ReferenciaIntegridadBLL().GuardarEstadoVerificado();Auditar(sec,actor);
                return new AltaClienteCorporativoBE{IdUsuario=usuario,IdCliente=cliente,Correo=d.Correo,Empresa=prep.Empresa.RazonSocial,Plan=prep.Plan,Vencimiento=(DateTime)sub["fechaVencimiento"]};
            },"ER049");
        }
        private static void Auditar(SecurityManager sec,int actor)
        {
            try{sec.RegistrarBitacora(new BitacoraBE{IdUsuario=actor,Fecha=DateTime.Now,Accion="Registro de cliente corporativo",Modulo="Clientes corporativos",Criticidad="Informativa"});}
            catch(SqlException ex){throw new ClienteCorporativoException(Conexion(ex)?"ER003":"ER040",ex);}
            catch(ConfigurationErrorsException ex){throw new ClienteCorporativoException("ER003",ex);}
            catch(Exception ex)when(ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException){throw new ClienteCorporativoException("ER040",ex);}
            catch(InvalidOperationException ex){throw new ClienteCorporativoException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":"ER040",ex);}
        }
    }
}
