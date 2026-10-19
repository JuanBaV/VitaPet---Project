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
    public sealed class TurnoException : Exception
    {
        public string Codigo { get; private set; }
        public TurnoException(string codigo,Exception inner=null):base("Gestión de turnos ("+codigo+").",inner){Codigo=codigo;}
    }
    internal static class OperacionTurnoBLL
    {
        internal static T Ejecutar<T>(Func<int,SecurityManager,T> accion,string fallo="ER065")
        {
            try
            {
                var seguridad=new SecurityManager();var usuario=seguridad.ExigirCliente();
                using(var tx=new OperacionIntegridadDAL())
                {
                    seguridad.ExigirIntegridad();var resultado=accion(usuario.IdUsuario,seguridad);
                    seguridad.ExigirIntegridad();tx.Complete();return resultado;
                }
            }
            catch(SqlException ex)
            {
                string codigo=MascotaBLL.Conexion(ex)?"ER003":ex.Number==50011?"ER011":ex.Number==50002?"ER002":
                    ex.Number==50025?"ER025":ex.Number==50013||ex.Number==2601||ex.Number==2627?"ER013":fallo;
                throw new TurnoException(codigo,ex);
            }
            catch(ConfigurationErrorsException ex){throw new TurnoException("ER003",ex);}
            catch(InvalidOperationException ex){throw new TurnoException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":fallo,ex);}
            catch(Exception ex) when(ex is TransactionException||ex is IOException||ex is XmlException)
            {throw new TurnoException(fallo,ex);}
        }
        internal static void ValidarSeleccion(int mascota,int agenda,DateTime fecha,string horario)
        {
            if(mascota<=0||agenda<=0||fecha==default(DateTime)||string.IsNullOrWhiteSpace(horario))throw new TurnoException("ER002");
            if(fecha.Year<1753||horario.Length!=5||!Regex.IsMatch(horario,@"\A(?:[01][0-9]|2[0-3]):[0-5][0-9]\z"))throw new TurnoException("ER025");
        }
        internal static void GuardarControles(string tabla,IEnumerable<int> nuevos,SecurityManager seguridad)
        {
            var ids=new HashSet<int>(nuevos);var dal=new IntegridadTablasDAL();var filas=dal.Leer(tabla);var hash=new IntegridadTablasBLL();
            foreach(DataRow fila in filas.Rows)
            {
                int id=(int)fila[IntegridadTablasDAL.Claves[tabla]];if(!ids.Contains(id))continue;
                string dvh=hash.CalcularDVH(fila);dal.GuardarDvh(tabla,id,dvh);fila["dvh"]=dvh;
            }
            new DVVDAL().Guardar(new DVVBE {NombreTabla=tabla,ValorDVV=seguridad.CalcularDVV(filas.Rows.Cast<DataRow>().Select(f=>(string)f["dvh"]))});
            new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
        }
        internal static void Auditar(SecurityManager seguridad,int usuario,bool escritura)
        {
            try
            {
                seguridad.RegistrarBitacora(new BitacoraBE {IdUsuario=usuario,Fecha=DateTime.Now,Modulo="Turnos",Criticidad="Informativa",
                    Accion=escritura?"Solicitud de turno veterinario":"Consulta de turnos"});
            }
            catch(SqlException ex){throw new TurnoException(MascotaBLL.Conexion(ex)?"ER003":escritura?"ER040":"ER033",ex);}
            catch(ConfigurationErrorsException ex){throw new TurnoException("ER003",ex);}
            catch(InvalidOperationException ex){throw new TurnoException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":escritura?"ER040":"ER033",ex);}
            catch(Exception ex) when(ex is IOException||ex is XmlException||ex is TransactionException||ex is UnauthorizedAccessException)
            {throw new TurnoException(escritura?"ER040":"ER033",ex);}
        }
    }
    public sealed class SolicitudTurnoBLL
    {
        private static void InicializarSiVacia(SecurityManager seguridad)
        {
            // También permite utilizar turnos después de restaurar un respaldo anterior a 022.
            // La integridad ya fue verificada: nunca se completan ni reparan agendas corruptas.
            var ids=new TurnoDAL().InicializarAgenda();
            if(ids.Count>0)OperacionTurnoBLL.GuardarControles("AgendaTurno",ids,seguridad);
        }
        public void AsegurarAgendaPrueba()
        {
            using(var tx=new OperacionIntegridadDAL())
            {
                var seguridad=new SecurityManager();seguridad.ExigirIntegridad();InicializarSiVacia(seguridad);
                seguridad.ExigirIntegridad();tx.Complete();
            }
        }
        public List<string> Zonas(){return OperacionTurnoBLL.Ejecutar((usuario,seguridad)=>{InicializarSiVacia(seguridad);return new TurnoDAL().Zonas(usuario);});}
        public List<DisponibilidadTurnoBE> Disponibilidad(int mascota,string zona)
        {
            return OperacionTurnoBLL.Ejecutar((usuario,seguridad)=>{
                if(mascota<=0||string.IsNullOrWhiteSpace(zona))throw new TurnoException("ER002");
                zona=zona.Trim();if(zona.Length>60)throw new TurnoException("ER025");
                InicializarSiVacia(seguridad);
                var datos=new TurnoDAL().Disponibilidad(usuario,mascota,zona);
                if(datos.Count==0)throw new TurnoException("ER012");return datos;
            });
        }
        public DisponibilidadTurnoBE Verificar(int mascota,int agenda,DateTime fecha,string horario)
        {
            return OperacionTurnoBLL.Ejecutar((usuario,seguridad)=>{
                OperacionTurnoBLL.ValidarSeleccion(mascota,agenda,fecha,horario);
                return new TurnoDAL().Verificar(usuario,mascota,agenda,fecha,horario);
            });
        }
        public TurnoBE Registrar(int mascota,int agenda,DateTime fecha,string horario)
        {
            return OperacionTurnoBLL.Ejecutar((usuario,seguridad)=>{
                OperacionTurnoBLL.ValidarSeleccion(mascota,agenda,fecha,horario);
                var turno=new TurnoDAL().Registrar(usuario,mascota,agenda,fecha,horario);
                OperacionTurnoBLL.GuardarControles("Turno",new[]{turno.IdTurno},seguridad);
                OperacionTurnoBLL.Auditar(seguridad,usuario,true);return turno;
            },"ER066");
        }
    }
    public sealed class ConsultaTurnosBLL
    {
        private static ResultadoConsultaTurnosBE Obtener(int usuario,int mascota)
        {
            if(mascota<=0)throw new TurnoException("ER011");
            var propia=new GestionMascotaDAL().Consultar(usuario,mascota);var turnos=new TurnoDAL().Consultar(usuario,mascota);
            if(turnos.Count==0)throw new TurnoException("ER014");
            return new ResultadoConsultaTurnosBE {Mascota=propia,Turnos=turnos};
        }
        public ResultadoConsultaTurnosBE Consultar(int mascota)
        {
            return OperacionTurnoBLL.Ejecutar((usuario,seguridad)=>{
                var datos=Obtener(usuario,mascota);OperacionTurnoBLL.Auditar(seguridad,usuario,false);return datos;
            });
        }
        public byte[] ExportarXml(int mascota)
        {
            return OperacionTurnoBLL.Ejecutar((usuario,seguridad)=>{
                // La exportación siempre vuelve a leer datos propios; no recibe filas del navegador.
                var datos=Obtener(usuario,mascota);
                try
                {
                    foreach(var turno in datos.Turnos)
                    {XmlConvert.VerifyXmlChars(turno.Prestacion);XmlConvert.VerifyXmlChars(turno.Horario);XmlConvert.VerifyXmlChars(turno.Zona);XmlConvert.VerifyXmlChars(turno.Estado);}
                    using(var memoria=new MemoryStream())
                    {
                        using(var xml=new XmlTextWriter(memoria,new UTF8Encoding(false)))
                        {
                            xml.Formatting=Formatting.Indented;xml.WriteStartDocument();xml.WriteStartElement("Turnos");
                            foreach(var turno in datos.Turnos)
                            {
                                xml.WriteStartElement("Turno");xml.WriteElementString("idTurno",XmlConvert.ToString(turno.IdTurno));
                                xml.WriteElementString("prestacion",turno.Prestacion);xml.WriteElementString("fecha",turno.Fecha.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture));
                                xml.WriteElementString("horario",turno.Horario);xml.WriteElementString("zona",turno.Zona);xml.WriteElementString("estado",turno.Estado);xml.WriteEndElement();
                            }
                            xml.WriteEndElement();xml.WriteEndDocument();xml.Flush();return memoria.ToArray();
                        }
                    }
                }
                catch(Exception ex) when(ex is XmlException||ex is ArgumentException||ex is IOException){throw new TurnoException("ER015",ex);}
            });
        }
    }
}
