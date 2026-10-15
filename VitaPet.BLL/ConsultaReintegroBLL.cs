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
    public sealed class ConsultaReintegroException : Exception
    {
        public string Codigo { get; private set; }
        public ConsultaReintegroException(string codigo,Exception inner=null)
            :base("No se pudo completar la consulta de reintegros ("+codigo+").",inner){Codigo=codigo;}
    }
    public sealed class ConsultaReintegroBLL
    {
        private static bool Conexion(SqlException ex)
        {return ex.Class>=20||new[]{-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number);}
        private T Ejecutar<T>(Func<int,SecurityManager,T> accion)
        {
            try
            {
                using(var tx=new OperacionIntegridadDAL())
                {
                    var seguridad=new SecurityManager();var usuario=seguridad.ExigirCliente();seguridad.ExigirIntegridad();
                    var resultado=accion(usuario.IdUsuario,seguridad);
                    seguridad.ExigirIntegridad();tx.Complete();return resultado;
                }
            }
            catch(SqlException ex){throw new ConsultaReintegroException(Conexion(ex)?"ER003":ex.Number==50011?"ER011":"ER059",ex);}
            catch(ConfigurationErrorsException ex){throw new ConsultaReintegroException("ER003",ex);}
            catch(TransactionException ex){throw new ConsultaReintegroException("ER033",ex);}
            catch(InvalidOperationException ex){throw new ConsultaReintegroException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":"ER059",ex);}
        }
        public List<ReintegroBE> Listar()
        {
            return Ejecutar((usuario,seguridad)=>{
                var lista=new ReintegroDAL().Listar(usuario);
                if(lista.Count==0)throw new ConsultaReintegroException("ER009");
                return lista;
            });
        }
        private ReintegroBE Obtener(int usuario,int id)
        {
            if(id<=0)throw new ConsultaReintegroException("ER011");
            var dato=new ReintegroDAL().Consultar(usuario,id);
            if(dato==null)throw new ConsultaReintegroException("ER011");return dato;
        }
        public ReintegroBE Consultar(int id)
        {
            return Ejecutar((usuario,seguridad)=>{
                var dato=Obtener(usuario,id);
                try {seguridad.RegistrarBitacora(new BitacoraBE {IdUsuario=usuario,Fecha=DateTime.Now,
                    Accion="Consulta de estado de reintegro",Modulo="Reintegros",Criticidad="Informativa"});}
                catch(SqlException ex){throw new ConsultaReintegroException(Conexion(ex)?"ER003":"ER033",ex);}
                catch(Exception ex) when(ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException)
                {throw new ConsultaReintegroException("ER033",ex);}
                return dato;
            });
        }
        public byte[] ExportarXml(int id)
        {
            return Ejecutar((usuario,seguridad)=>{
                // Se vuelve a consultar el registro propio: nunca se exportan datos del ViewState o del navegador.
                var dato=Obtener(usuario,id);
                try
                {
                    XmlConvert.VerifyXmlChars(dato.Estado??"");XmlConvert.VerifyXmlChars(dato.Detalle??"");
                    using(var memoria=new MemoryStream())
                    {
                        using(var xml=new XmlTextWriter(memoria,new UTF8Encoding(false)))
                        {
                            xml.Formatting=Formatting.Indented;xml.WriteStartDocument();xml.WriteStartElement("Reintegros");xml.WriteStartElement("Reintegro");
                            xml.WriteElementString("idReintegro",XmlConvert.ToString(dato.IdReintegro));
                            xml.WriteElementString("fechaSolicitud",XmlConvert.ToString(dato.FechaSolicitud,XmlDateTimeSerializationMode.Unspecified));
                            xml.WriteElementString("importe",XmlConvert.ToString(dato.Importe));
                            xml.WriteElementString("estado",dato.Estado);xml.WriteElementString("detalle",dato.Detalle??"");
                            xml.WriteEndElement();xml.WriteEndElement();xml.WriteEndDocument();xml.Flush();return memoria.ToArray();
                        }
                    }
                }
                catch(Exception ex) when(ex is XmlException||ex is ArgumentException||ex is IOException)
                {throw new ConsultaReintegroException("ER015",ex);}
            });
        }
    }
}
