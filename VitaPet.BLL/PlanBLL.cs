using System;
using System.Collections.Generic;
using System.Configuration;
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
    public sealed class PlanException:Exception
    {
        public string Codigo {get;private set;}
        public PlanException(string codigo,Exception inner=null):base("Administración de planes ("+codigo+").",inner){Codigo=codigo;}
    }
    public sealed class PlanBLL
    {
        private static bool Conexion(SqlException ex){return ex.Class>=20||new[]{-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number);}
        private static T Ejecutar<T>(Func<int,SecurityManager,T> action,string fallo)
        {
            try
            {
                var sec=new SecurityManager();var actor=sec.ExigirAdministrador();
                using(var tx=new OperacionIntegridadDAL())
                {sec.ExigirIntegridad();var result=action(actor.IdUsuario,sec);sec.ExigirIntegridad();tx.Complete();return result;}
            }
            catch(SqlException ex)
            {
                if(ex.Number==50024)throw new UnauthorizedAccessException("Acceso exclusivo para Administrador.",ex);
                throw new PlanException(Conexion(ex)?"ER003":ex.Number==50011?"ER011":ex.Number==50002?"ER002":
                    ex.Number==50025?"ER025":ex.Number==50078||ex.Number==2601||ex.Number==2627?"ER078":fallo,ex);
            }
            catch(ConfigurationErrorsException ex){throw new PlanException("ER003",ex);}
            catch(TransactionException ex){throw new PlanException(fallo,ex);}
            catch(InvalidOperationException ex){throw new PlanException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":fallo,ex);}
        }
        private static T Consulta<T>(Func<T> action,string fallo="ER075")
        {
            try{return action();}
            catch(SqlException ex){if(ex.Number==50024)throw;throw new PlanException(Conexion(ex)?"ER003":ex.Number==50011?"ER011":fallo,ex);}
        }
        private static string Texto(string value,int max,bool multilinea=false)
        {
            if(string.IsNullOrWhiteSpace(value))throw new PlanException("ER002");value=value.Trim();
            if(value.Length>max||value.Any(c=>char.IsControl(c)&&!(multilinea&&(c=='\r'||c=='\n'||c=='\t'))))throw new PlanException("ER025");
            try{Encoding.GetEncoding(1252,EncoderFallback.ExceptionFallback,DecoderFallback.ExceptionFallback).GetBytes(value);}
            catch(EncoderFallbackException ex){throw new PlanException("ER025",ex);}return value;
        }
        public static DatosPlanBE Validar(DatosPlanBE d)
        {
            if(d==null||!new SecurityManager().ValidarDatosObligatorios(d.Nombre,d.Descripcion)||!d.Precio.HasValue||!d.Estado.HasValue||d.Coberturas==null||d.Coberturas.Count==0)throw new PlanException("ER002");
            var result=new DatosPlanBE {Nombre=Texto(d.Nombre,80),Descripcion=Texto(d.Descripcion,255,true),Precio=d.Precio,Estado=d.Estado};
            if(d.Precio<=0||d.Precio>99999999.99m||decimal.Round(d.Precio.Value,2)!=d.Precio)throw new PlanException("ER025");
            var ids=new HashSet<int>();
            foreach(var c in d.Coberturas)
            {
                if(c==null||!c.Estado.HasValue)throw new PlanException("ER002");
                if(c.IdCobertura<0||(c.IdCobertura>0&&!ids.Add(c.IdCobertura)))throw new PlanException("ER025");
                result.Coberturas.Add(new DatosCoberturaPlanBE {IdCobertura=c.IdCobertura,Prestacion=Texto(c.Prestacion,100),Detalle=Texto(c.Detalle,255,true),Estado=c.Estado});
            }
            return result;
        }
        private static PlanAdministracionBE Obtener(int usuario,int plan)
        {
            if(plan<=0)throw new PlanException("ER011");var result=Consulta(()=>new AdministracionPlanDAL().Consultar(usuario,plan));
            if(result==null)throw new PlanException("ER011");return result;
        }
        private static void Auditar(SecurityManager sec,int actor,string evento,bool escritura=false)
        {
            try{sec.RegistrarBitacora(new BitacoraBE {IdUsuario=actor,Fecha=DateTime.Now,Accion=evento,Modulo="Planes",Criticidad="Informativa"});}
            catch(SqlException ex){throw new PlanException(Conexion(ex)?"ER003":escritura?"ER040":"ER033",ex);}
            catch(ConfigurationErrorsException ex){throw new PlanException("ER003",ex);}
            catch(Exception ex)when(ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException)
            {throw new PlanException(escritura?"ER040":"ER033",ex);}
            catch(InvalidOperationException ex){throw new PlanException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":escritura?"ER040":"ER033",ex);}
        }
        public void PrepararAlta(){new SecurityManager().ExigirPermiso("AltaPlan");Ejecutar((actor,sec)=>true,"ER075");}
        public List<PlanAdministracionBE> Listar()
        {return Ejecutar((actor,sec)=>{var list=Consulta(()=>new AdministracionPlanDAL().Listar(actor),"ER004");if(list.Count==0)throw new PlanException("ER016");Auditar(sec,actor,"Consulta de planes");return list;},"ER004");}
        public PlanAdministracionBE Seleccionar(int plan){return Ejecutar((actor,sec)=>Obtener(actor,plan),"ER075");}
        public PlanAdministracionBE Consultar(int plan)
        {return Ejecutar((actor,sec)=>{var p=Obtener(actor,plan);Auditar(sec,actor,"Consulta de plan");return p;},"ER075");}
        public int Registrar(DatosPlanBE d)
        {
            return Ejecutar((actor,sec)=>{
                var datos=Validar(d);if(datos.Coberturas.Any(c=>c.IdCobertura!=0))throw new PlanException("ER025");
                var dal=new AdministracionPlanDAL();int id=dal.Registrar(actor,datos);IntegridadRegistroBLL.Actualizar("Plan",id);
                foreach(var c in datos.Coberturas)IntegridadRegistroBLL.Actualizar("Cobertura",dal.GuardarCobertura(actor,id,c));
                Auditar(sec,actor,"Alta de plan",true);return id;
            },"ER076");
        }
        public void Actualizar(int plan,DatosPlanBE d)
        {
            Ejecutar((actor,sec)=>{
                var datos=Validar(d);var actual=Obtener(actor,plan);
                if(!new HashSet<int>(actual.Coberturas.Select(c=>c.IdCobertura)).SetEquals(datos.Coberturas.Where(c=>c.IdCobertura>0).Select(c=>c.IdCobertura)))throw new PlanException("ER025");
                var dal=new AdministracionPlanDAL();dal.Actualizar(actor,plan,datos);IntegridadRegistroBLL.Actualizar("Plan",plan);
                foreach(var c in datos.Coberturas)IntegridadRegistroBLL.Actualizar("Cobertura",dal.GuardarCobertura(actor,plan,c));
                Auditar(sec,actor,"Modificación de plan",true);return true;
            },"ER077");
        }
    }
}
