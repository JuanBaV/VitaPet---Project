using System;
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

        public byte[] ExportarXml()
        {
            // Consultar vuelve a autorizar y obtiene la cobertura de la cuenta vigente.
            var cobertura=Consultar();
            try
            {
                using(var memoria=new MemoryStream())
                {
                    using(var xml=XmlWriter.Create(memoria,new XmlWriterSettings {Encoding=new UTF8Encoding(false),Indent=true,CloseOutput=false}))
                    {
                        xml.WriteStartDocument();xml.WriteStartElement("Cobertura");
                        xml.WriteElementString("idSuscripcion",XmlConvert.ToString(cobertura.IdSuscripcion));
                        xml.WriteElementString("idPlan",XmlConvert.ToString(cobertura.IdPlan));
                        xml.WriteElementString("nombrePlan",cobertura.NombrePlan);
                        xml.WriteElementString("descripcionPlan",cobertura.DescripcionPlan);
                        xml.WriteElementString("estadoSuscripcion",cobertura.EstadoSuscripcion);
                        xml.WriteElementString("fechaAlta",XmlConvert.ToString(cobertura.FechaAlta,XmlDateTimeSerializationMode.Unspecified));
                        xml.WriteElementString("fechaVencimiento",XmlConvert.ToString(cobertura.FechaVencimiento,XmlDateTimeSerializationMode.Unspecified));
                        xml.WriteStartElement("Prestaciones");
                        foreach(var prestacion in cobertura.Coberturas)
                        {
                            xml.WriteStartElement("Prestacion");
                            xml.WriteElementString("nombre",prestacion.Prestacion);
                            xml.WriteElementString("detalle",prestacion.Detalle);
                            xml.WriteEndElement();
                        }
                        xml.WriteEndElement();xml.WriteEndElement();xml.WriteEndDocument();
                    }
                    return memoria.ToArray();
                }
            }
            catch(Exception ex) when(ex is XmlException||ex is ArgumentException||ex is IOException)
            {throw new ConsultaCoberturaException("ER015",ex);}
        }

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
