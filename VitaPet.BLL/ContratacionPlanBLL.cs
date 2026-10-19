using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Xml;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public sealed class ContratacionPlanException : Exception
    {
        public string Codigo { get; private set; }
        public ContratacionPlanException(string codigo,Exception inner=null):base("No se pudo completar la operación de planes ("+codigo+").",inner){Codigo=codigo;}
    }
    public sealed class ContratacionPlanBLL
    {
        private static T Ejecutar<T>(Func<SecurityManager,UsuarioBE,PlanDAL,T> accion,string fallo)
        {
            try
            {
                using(var tx=new OperacionIntegridadDAL())
                {
                    var sec=new SecurityManager();var actor=sec.ExigirCliente();sec.ExigirIntegridad();
                    T valor=accion(sec,actor,new PlanDAL());tx.Complete();return valor;
                }
            }
            catch(SqlException ex)
            {
                string codigo=ex.Class>=20||new[]{-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number)?"ER003":
                    ex.Number==50005?"ER005":ex.Number==50011?"ER011":ex.Number==50050?"ER050":ex.Number==50052?"ER052":ex.Number==50053?"ER053":fallo;
                throw new ContratacionPlanException(codigo,ex);
            }
            catch(ConfigurationErrorsException ex){throw new ContratacionPlanException("ER003",ex);}
            catch(TransactionException ex){throw new ContratacionPlanException(fallo,ex);}
            catch(InvalidOperationException ex){throw new ContratacionPlanException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":fallo,ex);}
        }
        public List<PlanBE> ConsultarPlanes()
        {
            return Ejecutar((s,u,d)=>{var planes=d.Listar();if(planes.Count==0)throw new ContratacionPlanException("ER051");return planes;},"ER004");
        }
        private static CotizacionPlanBE Cotizacion(PlanDAL d,int usuario,int plan)
        {
            if(plan<=0||d.Obtener(plan)==null)throw new ContratacionPlanException("ER050");
            var c=d.Cotizar(usuario,plan);if(c==null)throw new ContratacionPlanException("ER011");return c;
        }
        public CotizacionPlanBE ConsultarDetalle(int plan){return Ejecutar((s,u,d)=>Cotizacion(d,u.IdUsuario,plan),"ER004");}
        public List<CoberturaBE> ConsultarCobertura(int plan)
        {return Ejecutar((s,u,d)=>{if(d.Obtener(plan)==null)throw new ContratacionPlanException("ER050");return d.Coberturas(plan);},"ER004");}
        public CotizacionPlanBE PrepararContratacion(int plan,string version,int cantidad)
        {
            return Ejecutar((s,u,d)=>{
                var cotizacion=Cotizacion(d,u.IdUsuario,plan);
                if(cotizacion.CantidadMascotas==0)throw new ContratacionPlanException("ER052");
                if(cotizacion.ImporteUSD>99999999.99m)throw new ContratacionPlanException("ER025");
                var resultado=d.Preparar(u.IdUsuario,plan,version??"",cantidad);
                cotizacion.IdSuscripcion=resultado.Item1;
                if(resultado.Item2)
                {
                    ActualizarIntegridad("Suscripcion",resultado.Item1);
                    new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                    try {s.RegistrarBitacora(new BitacoraBE {IdUsuario=u.IdUsuario,Fecha=DateTime.Now,Accion="Contratación de plan pendiente",Modulo="Planes",Criticidad="Informativa"});}
                    catch(SqlException ex){throw new ContratacionPlanException(ex.Class>=20?"ER003":"ER040",ex);}
                    catch(Exception ex)when(ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException){throw new ContratacionPlanException("ER040",ex);}
                    s.ExigirIntegridad();
                }
                return cotizacion;
            },"ER007");
        }
        public CotizacionPlanBE ObtenerPendiente(int suscripcion)
        {return Ejecutar((s,u,d)=>{var plan=d.PlanPendiente(u.IdUsuario,suscripcion);if(!plan.HasValue)throw new ContratacionPlanException("ER011");var c=Cotizacion(d,u.IdUsuario,plan.Value);c.IdSuscripcion=suscripcion;return c;},"ER007");}
        public void ConfirmarPago(int suscripcion)
        {
            Ejecutar((s,u,d)=>{
                if(new PagoDAL().Activar(u.IdUsuario,suscripcion))
                {
                    IntegridadRegistroBLL.Actualizar("Suscripcion",suscripcion);
                    try{s.RegistrarBitacora(new BitacoraBE{IdUsuario=u.IdUsuario,Fecha=DateTime.Now,
                        Accion="Contratación de plan",Modulo="Planes",Criticidad="Informativa"});}
                    catch(Exception ex) when(ex is SqlException||ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException)
                    {throw new ContratacionPlanException("ER040",ex);}
                    s.ExigirIntegridad();
                }
                return true;
            },"ER007");
        }
        private static void ActualizarIntegridad(string tabla,int id)
        {
            var dal=new IntegridadTablasDAL();var datos=dal.Leer(tabla);
            var fila=datos.Rows.Cast<DataRow>().Single(r=>(int)r[IntegridadTablasDAL.Claves[tabla]]==id);
            string dvh=new IntegridadTablasBLL().CalcularDVH(fila);dal.GuardarDvh(tabla,id,dvh);fila["dvh"]=dvh;
            new DVVDAL().Guardar(new DVVBE{NombreTabla=tabla,ValorDVV=new IntegridadBLL().CalcularDVV(datos.Rows.Cast<DataRow>().Select(r=>(string)r["dvh"]))});
        }
        // Catálogo inicial idempotente; no modifica planes ni coberturas existentes.
        public void AsegurarCatalogo()
        {
            using(var tx=new OperacionIntegridadDAL())
            {
                var sec=new SecurityManager();sec.ExigirIntegridad();var dal=new PlanDAL();
                string[] nombres={"Plan Esencial","Plan Preventivo","Plan Integral","Plan Corporativo"};
                string[] detalles={"Por mes y por mascota. Cobertura básica según el alcance definido para el plan.","Por mes y por mascota. Mayor orientación a controles programados y prevención.","Por mes y por mascota. Mayor alcance frente a urgencias, estudios y otras prestaciones.","Disponible mediante convenio corporativo. Precio mensual fijo por mascota; no contratable de manera particular."};
                decimal?[] precios={19m,28m,39m,16m};
                if(new AdministracionPlanDAL().NecesitaCatalogo())
                for(int i=0;i<nombres.Length;i++)
                {
                    var p=dal.AsegurarPlan(nombres[i],detalles[i],precios[i]);if(p.Item2)ActualizarIntegridad("Plan",p.Item1);
                    if(i<3){var c=dal.AsegurarCobertura(p.Item1,"Alcance del plan",detalles[i]);if(c.Item2)ActualizarIntegridad("Cobertura",c.Item1);}
                }
                int? corporativo=new AdministracionPlanDAL().InicializarCorporativo();
                if(corporativo.HasValue)ActualizarIntegridad("Plan",corporativo.Value);
                new ReferenciaIntegridadBLL().GuardarEstadoVerificado();sec.ExigirIntegridad();tx.Complete();
            }
        }
    }
}
