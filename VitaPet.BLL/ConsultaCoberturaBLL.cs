using System;
using System.Configuration;
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
    public sealed class ConsultaCoberturaException : Exception
    {
        public string Codigo { get; private set; }
        public ConsultaCoberturaException(string codigo,Exception inner=null)
            :base("No se pudo consultar la cobertura ("+codigo+").",inner){Codigo=codigo;}
    }
    public sealed class ConsultaCoberturaBLL
    {
        private static bool Conexion(SqlException ex)
        {return ex.Class>=20||new[]{-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number);}

        public ConsultaCoberturaBE Consultar()
        {
            try
            {
                using(var tx=new OperacionIntegridadDAL())
                {
                    var seguridad=new SecurityManager();var cliente=seguridad.ExigirCliente();seguridad.ExigirIntegridad();
                    var dal=new ConsultaCoberturaDAL();var consulta=dal.ObtenerVigente(cliente.IdUsuario);
                    if(consulta==null)throw new ConsultaCoberturaException("ER008");
                    consulta.Coberturas=dal.ObtenerPrestaciones(cliente.IdUsuario,consulta.IdSuscripcion);
                    if(consulta.Coberturas.Count==0)throw new ConsultaCoberturaException("ER008");
                    try
                    {
                        seguridad.RegistrarBitacora(new BitacoraBE{IdUsuario=cliente.IdUsuario,Fecha=DateTime.Now,
                            Accion="Consulta de cobertura",Modulo="Planes",Criticidad="Informativa"});
                    }
                    catch(SqlException ex){throw new ConsultaCoberturaException(Conexion(ex)?"ER003":"ER033",ex);}
                    catch(Exception ex) when(ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException)
                    {throw new ConsultaCoberturaException("ER033",ex);}
                    try {seguridad.ExigirIntegridad();tx.Complete();}
                    catch(TransactionException ex){throw new ConsultaCoberturaException("ER033",ex);}
                    return consulta;
                }
            }
            catch(SqlException ex)
            {throw new ConsultaCoberturaException(Conexion(ex)?"ER003":ex.Number==50011?"ER011":ex.Number==50008?"ER008":"ER058",ex);}
            catch(ConfigurationErrorsException ex){throw new ConsultaCoberturaException("ER003",ex);}
            catch(TransactionException ex){throw new ConsultaCoberturaException("ER033",ex);}
            catch(InvalidOperationException ex)
            {throw new ConsultaCoberturaException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":"ER058",ex);}
        }
    }
}
