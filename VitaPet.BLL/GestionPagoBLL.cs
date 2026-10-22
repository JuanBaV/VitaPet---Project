using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using System.Security.Cryptography;
using VitaPet.SECURITY;
using VitaPet.SERVICES;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public sealed class GestionPagoException : Exception
    {
        public string Codigo { get; private set; }
        public GestionPagoException(string codigo,Exception inner=null):base("No se pudo completar la gestion de pago ("+codigo+").",inner){Codigo=codigo;}
    }

    public sealed class GestionPagoBLL
    {
        public const string Credito="Tarjeta de credito", Debito="Tarjeta de debito";
        public static bool EsPruebaLocal { get { return ConfigurationManager.AppSettings["PagoProcesador"]=="PruebaLocal"; } }

        private static T Ejecutar<T>(Func<SecurityManager,UsuarioBE,PagoDAL,T> action)
        {
            try
            {
                using(var tx=new OperacionIntegridadDAL())
                {
                    var s=new SecurityManager();var u=s.ExigirPermiso("GestionPago");s.ExigirIntegridad();
                    var result=action(s,u,new PagoDAL());tx.Complete();return result;
                }
            }
            catch(SqlException ex)
            {
                string code=ex.Class>=20||new[]{-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number)?"ER003":
                    ex.Number==50054?"ER054":ex.Number==50050?"ER050":ex.Number==50052?"ER052":ex.Number==50025?"ER025":"ER057";
                throw new GestionPagoException(code,ex);
            }
            catch(ConfigurationErrorsException ex){throw new GestionPagoException("ER003",ex);}
            catch(TransactionException ex){throw new GestionPagoException("ER057",ex);}
            catch(InvalidOperationException ex){throw new GestionPagoException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":"ER057",ex);}
        }

        public ResumenPagoBE Consultar(int suscripcion)
        {return Ejecutar((s,u,d)=>d.ConsultarPendiente(u.IdUsuario,suscripcion));}

        public PagoBE ObtenerComprobante(int suscripcion)
        {return CompletarFactura(Ejecutar((s,u,d)=>{var p=d.Comprobante(u.IdUsuario,suscripcion);if(p==null)throw new GestionPagoException("ER054");return p;}));}

        public CertificadoPagoBE ObtenerCertificado(int suscripcion)
        {
            var pago = Ejecutar((s,u,d)=>{var p=d.Comprobante(u.IdUsuario,suscripcion);if(p==null)throw new GestionPagoException("ER054");return p;});
            if (!EsPruebaLocal) throw new GestionPagoException("ER055");
            return Certificar(pago);
        }

        private static CertificadoPagoBE Certificar(PagoBE pago)
        {
            try
            {
                string carpeta = Path.Combine(Path.GetDirectoryName(EstadoAplicacion.ResolverRuta("RutaBitacoraXml", "~/App_Data/Bitacora.xml")), "CertificadosPago");
                return new CertificadoPagoService().Obtener(pago, carpeta);
            }
            catch(Exception ex) when(ex is CryptographicException || ex is IOException || ex is UnauthorizedAccessException)
            {throw new GestionPagoException("ER055",ex);}
        }

        public PagoBE Pagar(int suscripcion,decimal importeConfirmado,DatosPagoBE datos)
        {
            try
            {
                var resultado = CompletarFactura(Ejecutar((s,u,d)=>{
                    var anterior=d.Comprobante(u.IdUsuario,suscripcion);if(anterior!=null)return anterior;
                    var resumen=d.ConsultarPendiente(u.IdUsuario,suscripcion);
                    Validar(datos);
                    // Nunca aceptar el importe del navegador ni cobrar un resumen anterior.
                    if(importeConfirmado!=resumen.Importe)throw new GestionPagoException("ER025");
                    ProcesarPrueba(datos);
                    d.Registrar(u.IdUsuario,suscripcion,resumen.Importe,datos.MedioPago,datos.Titular,datos.CorreoFactura);
                    var pago=d.Comprobante(u.IdUsuario,suscripcion);
                    IntegridadRegistroBLL.Actualizar("Pago",pago.IdPago);
                    IntegridadRegistroBLL.Actualizar("DetallePago",pago.IdPago);
                    try
                    {
                        s.RegistrarBitacora(new BitacoraBE{IdUsuario=u.IdUsuario,Fecha=DateTime.Now,
                            Accion="Gestión de pago",Modulo="Pagos",Criticidad="Informativa"});
                    }
                    catch(Exception ex) when(ex is SqlException||ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException)
                    {throw new GestionPagoException("ER040",ex);}
                    s.ExigirIntegridad();return pago;
                }));
                if(EsPruebaLocal)Certificar(resultado);
                return resultado;
            }
            finally {if(datos!=null)datos.Limpiar();}
        }

        private static PagoBE CompletarFactura(PagoBE pago)
        {
            try{pago.Factura=new FacturaBLL().Emitir(pago.IdSuscripcion);}
            catch(GestionPagoException ex){if(ex.Codigo=="ER023")throw;pago.CodigoErrorFactura=ex.Codigo;}
            return pago;
        }

        private static void Validar(DatosPagoBE d)
        {
            if(d==null||new[]{d.MedioPago,d.Titular,d.NumeroTarjeta,d.Vencimiento,d.CodigoSeguridad,d.CorreoFactura}.Any(string.IsNullOrWhiteSpace))
                throw new GestionPagoException("ER002");
            d.CorreoFactura=d.CorreoFactura.Trim();
            string numero=d.NumeroTarjeta.Replace(" ","").Replace("-","");
            DateTime vencimiento;
            if(d.CorreoFactura.Length>100||!Regex.IsMatch(d.CorreoFactura,@"\A[^@\s]+@[^@\s]+\.[^@\s]+\z")||
                (d.MedioPago!=Credito&&d.MedioPago!=Debito)||d.Titular.Trim().Length<2||d.Titular.Length>100||
                d.NumeroTarjeta.Length>25||!Regex.IsMatch(numero,@"\A[0-9]{13,19}\z")||
                !Regex.IsMatch(d.CodigoSeguridad,@"\A[0-9]{3,4}\z")||
                !DateTime.TryParseExact(d.Vencimiento,"MM/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out vencimiento)||
                vencimiento<DateTime.Today.AddDays(1-DateTime.Today.Day)||
                (numero!="1111111111111111"||!EsPruebaLocal)&&!Luhn(numero))
                throw new GestionPagoException("ER025");
            d.NumeroTarjeta=numero;
        }
        private static bool Luhn(string n)
        {
            int sum=0;bool doble=false;
            for(int i=n.Length-1;i>=0;i--){int x=n[i]-'0';if(doble){x*=2;if(x>9)x-=9;}sum+=x;doble=!doble;}
            return sum%10==0;
        }
        private static void ProcesarPrueba(DatosPagoBE d)
        {
            // Simulador cerrado: no llama redes y solo aprueba tarjetas dummy conocidas.
            if(!EsPruebaLocal||d.NumeroTarjeta=="5555555555554444")throw new GestionPagoException("ER055");
            if(d.NumeroTarjeta!="1111111111111111"&&d.NumeroTarjeta!="4111111111111111")throw new GestionPagoException("ER006");
        }
    }
}
