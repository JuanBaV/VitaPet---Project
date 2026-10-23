using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.SessionState;
using System.Xml;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;

static class XmlTests
{
    static int checks;
    static void Check(bool ok,string texto){if(!ok)throw new Exception(texto);checks++;Console.WriteLine("PASS "+texto);}
    static void Sesion(int id)
    {
        var ctx=new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current=ctx;if(id>0)SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }
    static XmlDocument Documento(byte[] bytes,string raiz)
    {
        string texto=new UTF8Encoding(false,true).GetString(bytes);
        Check(texto.StartsWith("<?xml")&&texto.Contains("encoding=\"utf-8\""),raiz+": declaración UTF-8 sin BOM");
        var doc=new XmlDocument {XmlResolver=null};
        using(var reader=XmlReader.Create(new StringReader(texto),new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))doc.Load(reader);
        Check(doc.DocumentElement.Name==raiz&&doc.SelectNodes("//html|//form").Count==0,raiz+": documento XML completo, sin HTML de la página");return doc;
    }
    static void Error(Action accion,string codigo,string texto)
    {
        try{accion();}catch(ConsultaReintegroException e){Check(e.Codigo==codigo,texto);return;}
        catch(TurnoException e){Check(e.Codigo==codigo,texto);return;}
        catch(ConsultaCoberturaException e){Check(e.Codigo==codigo,texto);return;}
        throw new Exception("No se rechazó: "+texto);
    }
    static void Deniega(Action accion,string texto)
    {try{accion();}catch(UnauthorizedAccessException){Check(true,texto);return;}throw new Exception("No se rechazó: "+texto);}
    public static void Run()
    {
        var cultura=CultureInfo.CurrentCulture;var culturaUi=CultureInfo.CurrentUICulture;
        var reintegros=new ConsultaReintegroBLL();var turnos=new ConsultaTurnosBLL();var cobertura=new ConsultaCoberturaBLL();
        try
        {
            foreach(int id in new[]{0,2,3})
            {
                Sesion(id);Deniega(()=>reintegros.ExportarXml(1),"Reintegros: deniega al usuario "+id);
                Deniega(()=>turnos.ExportarXml(1),"Turnos: deniega al usuario "+id);
                Deniega(()=>cobertura.ExportarXml(),"Cobertura: deniega al usuario "+id);
            }
            Sesion(1);byte[][] referencia=null;
            foreach(string idioma in new[]{"es-AR","en-US"})
            {
                CultureInfo.CurrentCulture=new CultureInfo(idioma);CultureInfo.CurrentUICulture=new CultureInfo(idioma);
                var propio=reintegros.Consultar(1);var citas=turnos.Consultar(1).Turnos;var plan=cobertura.Consultar();
                byte[][] contenido={reintegros.ExportarXml(1),turnos.ExportarXml(1),cobertura.ExportarXml()};
                var r=Documento(contenido[0],"Reintegros");var item=r.SelectSingleNode("/Reintegros/Reintegro");
                Check(r.SelectNodes("/Reintegros/Reintegro").Count==1,"Reintegros "+idioma+": exporta solo el registro seleccionado");
                Check(item.ChildNodes.Cast<XmlNode>().Select(n=>n.Name).SequenceEqual(new[]{"idReintegro","fechaSolicitud","importe","estado","detalle"}),"Reintegros: conserva los cinco campos de negocio");
                Check(int.Parse(item["idReintegro"].InnerText)==propio.IdReintegro&&XmlConvert.ToDecimal(item["importe"].InnerText)==propio.Importe,"Reintegros: identificador e importe coinciden con la consulta");
                Check(item["importe"].InnerText=="1234.56"&&item["detalle"].InnerText=="Revisión & análisis <Luna>","Reintegros: decimal invariante y caracteres especiales sin pérdida");
                Check(XmlConvert.ToDateTime(item["fechaSolicitud"].InnerText,XmlDateTimeSerializationMode.Unspecified)==propio.FechaSolicitud&&item["estado"].InnerText==propio.Estado,"Reintegros: fecha y estado coinciden");
                var t=Documento(contenido[1],"Turnos");var filas=t.SelectNodes("/Turnos/Turno").Cast<XmlNode>().ToArray();
                Check(filas.Length==citas.Count&&filas.Length==2,"Turnos "+idioma+": incluye todos los turnos de Luna");
                foreach(var fila in filas)
                {
                    var cita=citas.Single(x=>x.IdTurno==int.Parse(fila["idTurno"].InnerText));
                    Check(fila.ChildNodes.Cast<XmlNode>().Select(n=>n.Name).SequenceEqual(new[]{"idTurno","prestacion","fecha","horario","zona","estado"}),"Turnos: conserva los seis campos del turno "+cita.IdTurno);
                    Check(fila["prestacion"].InnerText==cita.Prestacion&&fila["zona"].InnerText==cita.Zona&&fila["horario"].InnerText==cita.Horario&&fila["estado"].InnerText==cita.Estado,"Turnos: contenido coincide con el registro "+cita.IdTurno);
                    Check(fila["fecha"].InnerText==cita.Fecha.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),"Turnos: fecha ISO sin depender del idioma");
                }
                Check(Encoding.UTF8.GetString(contenido[1]).Contains("&amp;")&&Encoding.UTF8.GetString(contenido[1]).Contains("&lt;Luna&gt;"),"Turnos: escapa ampersand y etiquetas del texto de negocio");
                var c=Documento(contenido[2],"Cobertura");var root=c.DocumentElement;
                Check(root.ChildNodes.Cast<XmlNode>().Select(n=>n.Name).SequenceEqual(new[]{"idSuscripcion","idPlan","nombrePlan","descripcionPlan","estadoSuscripcion","fechaAlta","fechaVencimiento","Prestaciones"}),"Cobertura: contrato con plan, vigencia y prestaciones");
                Check(XmlConvert.ToInt32(root["idSuscripcion"].InnerText)==plan.IdSuscripcion&&XmlConvert.ToInt32(root["idPlan"].InnerText)==plan.IdPlan&&root["nombrePlan"].InnerText==plan.NombrePlan,"Cobertura: corresponde a la contratación del cliente");
                Check(root["descripcionPlan"].InnerText==plan.DescripcionPlan&&root["estadoSuscripcion"].InnerText==plan.EstadoSuscripcion,"Cobertura: descripción y estado coinciden con el negocio");
                Check(XmlConvert.ToDateTime(root["fechaAlta"].InnerText,XmlDateTimeSerializationMode.Unspecified)==plan.FechaAlta&&XmlConvert.ToDateTime(root["fechaVencimiento"].InnerText,XmlDateTimeSerializationMode.Unspecified)==plan.FechaVencimiento,"Cobertura: vigencia completa en formato ISO");
                var prestaciones=c.SelectNodes("/Cobertura/Prestaciones/Prestacion").Cast<XmlNode>().ToArray();
                Check(prestaciones.Length==plan.Coberturas.Count&&prestaciones.Zip(plan.Coberturas,(n,p)=>n["nombre"].InnerText==p.Prestacion&&n["detalle"].InnerText==p.Detalle).All(x=>x),"Cobertura: exporta todas las prestaciones y sus detalles");
                Check(prestaciones.Any(n=>n["detalle"].InnerText=="Cobertura & atención <preventiva>"),"Cobertura: conserva acentos y caracteres especiales");
                if(referencia!=null)Check(contenido.Zip(referencia,(x,y)=>x.SequenceEqual(y)).All(x=>x),"Los tres archivos conservan el mismo contrato y contenido en español e inglés");
                referencia=contenido;
            }
            var vacio=Documento(reintegros.ExportarXml(2),"Reintegros");Check(vacio.SelectSingleNode("/Reintegros/Reintegro/detalle").InnerText=="","Reintegros: un detalle NULL se exporta vacío");
            Error(()=>reintegros.ExportarXml(0),"ER011","Reintegros: no exporta una selección inválida");
            Error(()=>reintegros.ExportarXml(3),"ER011","Reintegros: no exporta datos del otro cliente");
            Error(()=>turnos.ExportarXml(4),"ER011","Turnos: no exporta la mascota de otro cliente");
            Error(()=>turnos.ExportarXml(2),"ER014","Turnos: no genera un archivo si la mascota no tiene turnos");
            Sesion(4);Error(()=>cobertura.ExportarXml(),"ER008","Cobertura: una cuenta sin contratación no obtiene la del otro cliente");
            Error(()=>reintegros.ExportarXml(1),"ER011","Reintegros: tampoco permite el acceso inverso entre clientes");
            Error(()=>turnos.ExportarXml(1),"ER011","Turnos: tampoco permite el acceso inverso entre clientes");
            Check(Documento(reintegros.ExportarXml(3),"Reintegros").SelectSingleNode("/Reintegros/Reintegro/detalle").InnerText=="Ajeno reservado","El segundo cliente puede exportar su reintegro propio");
            Check(Documento(turnos.ExportarXml(4),"Turnos").SelectNodes("/Turnos/Turno").Count==1,"El segundo cliente solo exporta sus propios turnos");
            Check(new SecurityManager().VerificarIntegridad(),"Las tres salidas de negocio conservan DVH, DVV y referencias");
            Console.WriteLine("TOTAL XML BLL: "+checks+" comprobaciones");
        }
        finally{CultureInfo.CurrentCulture=cultura;CultureInfo.CurrentUICulture=culturaUi;}
    }
}
