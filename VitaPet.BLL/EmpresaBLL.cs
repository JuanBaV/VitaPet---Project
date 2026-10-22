using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public sealed class EmpresaException : Exception
    {
        public string Codigo { get; private set; }
        public EmpresaException(string codigo, Exception inner=null)
            : base("Operación de empresa (" + codigo + ").", inner) { Codigo=codigo; }
    }

    public sealed class EmpresaBLL
    {
        private static bool Conexion(SqlException ex)
        {
            return ex.Class >= 20 || new[] {-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number);
        }

        private static T Ejecutar<T>(Func<int,SecurityManager,T> accion, string fallo)
        {
            try
            {
                var seguridad = new SecurityManager();
                var actor = seguridad.ExigirAdministrador();
                using (var tx = new OperacionIntegridadDAL())
                {
                    seguridad.ExigirIntegridad();
                    var resultado = accion(actor.IdUsuario, seguridad);
                    seguridad.ExigirIntegridad();
                    tx.Complete(); return resultado;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number==50024) throw new UnauthorizedAccessException("Acceso exclusivo para Administrador.",ex);
                string codigo = Conexion(ex) ? "ER003" :
                    ex.Number==50011 ? "ER011" : ex.Number==50002 ? "ER002" : ex.Number==50025 ? "ER025" :
                    ex.Number==50068 || ex.Number==2601 || ex.Number==2627 ? "ER068" : fallo;
                throw new EmpresaException(codigo, ex);
            }
            catch (ConfigurationErrorsException ex) { throw new EmpresaException("ER003",ex); }
            catch (TransactionException ex) { throw new EmpresaException(fallo,ex); }
            catch (InvalidOperationException ex)
            {
                throw new EmpresaException(ex.Message.StartsWith("ER023",StringComparison.Ordinal) ? "ER023" : fallo,ex);
            }
        }

        public static DatosEmpresaBE Validar(DatosEmpresaBE datos)
        {
            if (datos==null || !new SecurityManager().ValidarDatosObligatorios(datos.RazonSocial,datos.Cuit,datos.Direccion,datos.Telefono))
                throw new EmpresaException("ER002");
            var d = new DatosEmpresaBE { RazonSocial=datos.RazonSocial.Trim(), Cuit=datos.Cuit.Trim(),
                Direccion=datos.Direccion.Trim(), Telefono=datos.Telefono.Trim() };
            if (d.RazonSocial.Length>100 || d.Cuit.Length>20 || d.Direccion.Length>150 || d.Telefono.Length>30 ||
                new[]{d.RazonSocial,d.Cuit,d.Direccion,d.Telefono}.Any(x=>x.Any(char.IsControl)) ||
                !Regex.IsMatch(d.Cuit,@"\A(?:[0-9]{11}|[0-9]{2}-[0-9]{8}-[0-9])\z") ||
                !Regex.IsMatch(d.Telefono,@"\A[0-9 +()\-]+\z") || !d.Telefono.Any(c=>c>='0'&&c<='9'))
                throw new EmpresaException("ER025");
            try
            {
                var encoding = Encoding.GetEncoding(1252,EncoderFallback.ExceptionFallback,DecoderFallback.ExceptionFallback);
                foreach (var value in new[]{d.RazonSocial,d.Direccion,d.Telefono}) encoding.GetBytes(value);
            }
            catch (EncoderFallbackException ex) { throw new EmpresaException("ER025",ex); }
            d.Cuit=d.Cuit.Replace("-",string.Empty);
            return d;
        }

        private static T ConsultarDatos<T>(Func<T> consulta)
        {
            try { return consulta(); }
            catch (SqlException ex)
            {
                if (ex.Number==50024) throw;
                throw new EmpresaException(Conexion(ex) ? "ER003" : ex.Number==50011 ? "ER011" : "ER067",ex);
            }
        }

        private static EmpresaBE Obtener(int usuario, int empresa)
        {
            if (empresa<=0) throw new EmpresaException("ER011");
            var resultado=ConsultarDatos(()=>new EmpresaDAL().Consultar(usuario,empresa));
            if (resultado==null) throw new EmpresaException("ER011");
            return resultado;
        }

        private static void ExigirCuitUnico(int usuario, DatosEmpresaBE datos, int empresa=0)
        {
            if (ConsultarDatos(()=>new EmpresaDAL().CuitRegistrado(usuario,datos.Cuit,empresa)))
                throw new EmpresaException("ER068");
        }

        private static void Auditar(SecurityManager seguridad,int actor,string evento,bool escritura=false)
        {
            try
            {
                seguridad.RegistrarBitacora(new BitacoraBE { IdUsuario=actor, Fecha=DateTime.Now,
                    Accion=evento, Modulo="Empresas", Criticidad="Informativa" });
            }
            catch (SqlException ex) { throw new EmpresaException(Conexion(ex) ? "ER003" : escritura ? "ER040" : "ER033",ex); }
            catch (ConfigurationErrorsException ex) { throw new EmpresaException("ER003",ex); }
            catch (Exception ex) when (ex is IOException || ex is XmlException || ex is UnauthorizedAccessException || ex is TransactionException)
            { throw new EmpresaException(escritura ? "ER040" : "ER033",ex); }
            catch (InvalidOperationException ex)
            { throw new EmpresaException(ex.Message.StartsWith("ER023",StringComparison.Ordinal) ? "ER023" : escritura ? "ER040" : "ER033",ex); }
        }

        public List<EmpresaBE> Listar()
        {
            return Ejecutar((actor,seguridad)=>{
                var lista=ConsultarDatos(()=>new EmpresaDAL().Listar(actor));
                if (lista.Count==0) throw new EmpresaException("ER017");
                Auditar(seguridad,actor,"Consulta de empresas asociadas");return lista;
            },"ER067");
        }

        public EmpresaBE Seleccionar(int empresa)
        { return Ejecutar((actor,seguridad)=>Obtener(actor,empresa),"ER067"); }

        public EmpresaBE Consultar(int empresa)
        {
            return Ejecutar((actor,seguridad)=>{
                var datos=Obtener(actor,empresa);Auditar(seguridad,actor,"Consulta de empresa asociada");return datos;
            },"ER067");
        }

        public EmpresaBE Registrar(DatosEmpresaBE datos)
        {
            return Ejecutar((actor,seguridad)=>{
                var d=Validar(datos);ExigirCuitUnico(actor,d);
                int id=new EmpresaDAL().Registrar(actor,d);IntegridadRegistroBLL.Actualizar("Empresa",id);
                Auditar(seguridad,actor,"Alta de empresa",true);
                return new EmpresaBE {IdEmpresa=id,RazonSocial=d.RazonSocial,Cuit=d.Cuit,Direccion=d.Direccion,Telefono=d.Telefono,Estado=true};
            },"ER069");
        }

        public void Actualizar(int empresa,DatosEmpresaBE datos)
        {
            Ejecutar((actor,seguridad)=>{
                var d=Validar(datos);Obtener(actor,empresa);ExigirCuitUnico(actor,d,empresa);
                new EmpresaDAL().Actualizar(actor,empresa,d);IntegridadRegistroBLL.Actualizar("Empresa",empresa);
                Auditar(seguridad,actor,"Modificación de empresa asociada",true);return true;
            },"ER070");
        }
    }
}
