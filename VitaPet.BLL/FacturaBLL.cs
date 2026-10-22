using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Transactions;
using System.Xml;
using VitaPet.BE;
using VitaPet.DAL;
using VitaPet.BLL.Seguridad;
using VitaPet.SERVICES;
using VitaPet.SECURITY;
namespace VitaPet.BLL
{
    public sealed class FacturaBLL
    {
        private static T Ejecutar<T>(Func<SecurityManager,UsuarioBE,FacturaDAL,T> action,string fallo)
        {
            try
            {
                using(var tx=new OperacionIntegridadDAL())
                {
                    var s=new SecurityManager();var u=s.ExigirCliente();s.ExigirIntegridad();var r=action(s,u,new FacturaDAL());tx.Complete();return r;
                }
            }
            catch(SqlException ex){throw new GestionPagoException(ex.Class>=20||new[]{-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number)?"ER003":ex.Number==50054?"ER054":ex.Number==50079?"ER079":fallo,ex);}
            catch(ConfigurationErrorsException ex){throw new GestionPagoException("ER003",ex);}
            catch(TransactionException ex){throw new GestionPagoException(fallo,ex);}
            catch(InvalidOperationException ex){throw new GestionPagoException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":fallo,ex);}
        }
        public List<ConsultaFacturaBE> ListarPropias(){return Ejecutar((s,u,d)=>d.Listar(u.IdUsuario),"ER079");}
        public FacturaBE Emitir(int suscripcion)
        {
            var factura = Ejecutar((s,u,d)=>{
                var r=d.Emitir(u.IdUsuario,suscripcion);
                if(r.Item2)
                {
                    IntegridadRegistroBLL.Actualizar("Factura",r.Item1);
                    try{s.RegistrarBitacora(new BitacoraBE {IdUsuario=u.IdUsuario,Fecha=DateTime.Now,Accion="Emisión de factura",Modulo="Pagos",Criticidad="Informativa"});}
                    catch(Exception ex)when(ex is SqlException||ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException){throw new GestionPagoException("ER040",ex);}
                    s.ExigirIntegridad();
                }
                return d.Obtener(u.IdUsuario,suscripcion);
            },"ER080");
            EnviarCorreo(factura, false);
            return factura;
        }
        public FacturaBE ReintentarCorreo(int suscripcion)
        {
            var factura=Ejecutar((s,u,d)=>{var f=d.Obtener(u.IdUsuario,suscripcion);if(f==null)throw new GestionPagoException("ER079");return f;},"ER079");
            EnviarCorreo(factura,true);
            return factura;
        }
        private static void EnviarCorreo(FacturaBE factura,bool reintentar)
        {
            string carpeta=Path.Combine(Path.GetDirectoryName(EstadoAplicacion.ResolverRuta("RutaBitacoraXml","~/App_Data/Bitacora.xml")),"CorreoFacturas");
            new CorreoFacturaService().Enviar(factura,carpeta,reintentar);
        }
        public byte[] Descargar(int suscripcion)
        {
            var f=Ejecutar((s,u,d)=>{var r=d.Obtener(u.IdUsuario,suscripcion);if(r==null)throw new GestionPagoException("ER081");return r;},"ER081");
            try{return new FacturaPdfService().Generar(f);}
            catch(Exception ex)when(ex is IOException||ex is ArgumentException||ex is InvalidOperationException){throw new GestionPagoException("ER081",ex);}
        }
    }
}
