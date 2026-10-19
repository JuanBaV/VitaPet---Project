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
    public sealed class GestionMascotaException : Exception
    {
        public string Codigo { get; private set; }
        public GestionMascotaException(string codigo,Exception inner=null):base("Gestión de mascota ("+codigo+").",inner){Codigo=codigo;}
    }
    public sealed class MascotaBLL
    {
        internal static bool Conexion(SqlException ex)
        {return ex.Class>=20||new[]{-2,2,20,53,64,233,4060,18456,10053,10054,10060}.Contains(ex.Number);}
        internal static T Ejecutar<T>(Func<int,SecurityManager,T> accion,string fallo)
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
                string code=Conexion(ex)?"ER003":ex.Number==50011?"ER011":ex.Number==50064?"ER064":
                    ex.Number==50002?"ER002":ex.Number==50025?"ER025":fallo;
                throw new GestionMascotaException(code,ex);
            }
            catch(ConfigurationErrorsException ex){throw new GestionMascotaException("ER003",ex);}
            catch(TransactionException ex){throw new GestionMascotaException(fallo,ex);}
            catch(InvalidOperationException ex){throw new GestionMascotaException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":fallo,ex);}
        }
        internal static void Auditar(SecurityManager seguridad,int usuario,string evento,bool escritura=false)
        {
            try{seguridad.RegistrarBitacora(new BitacoraBE {IdUsuario=usuario,Fecha=DateTime.Now,Accion=evento,Modulo="Mascotas",Criticidad="Informativa"});}
            catch(SqlException ex){throw new GestionMascotaException(Conexion(ex)?"ER003":escritura?"ER040":"ER033",ex);}
            catch(ConfigurationErrorsException ex){throw new GestionMascotaException("ER003",ex);}
            catch(Exception ex) when(ex is IOException||ex is XmlException||ex is UnauthorizedAccessException||ex is TransactionException)
            {throw new GestionMascotaException(escritura?"ER040":"ER033",ex);}
            catch(InvalidOperationException ex){throw new GestionMascotaException(ex.Message.StartsWith("ER023",StringComparison.Ordinal)?"ER023":escritura?"ER040":"ER033",ex);}
        }
        internal static MascotaBE Obtener(int usuario,int id)
        {
            if(id<=0)throw new GestionMascotaException("ER011");
            return new GestionMascotaDAL().Consultar(usuario,id);
        }
        public List<MascotaBE> Listar()
        {
            return Ejecutar((usuario,seguridad)=>{
                var lista=new GestionMascotaDAL().Listar(usuario);
                if(lista.Count==0)throw new GestionMascotaException("ER010");
                Auditar(seguridad,usuario,"Consulta de mascotas");return lista;
            },"ER061");
        }
        public MascotaBE Seleccionar(int id)
        {return Ejecutar((usuario,seguridad)=>Obtener(usuario,id),"ER061");}
        public MascotaBE Consultar(int id)
        {
            return Ejecutar((usuario,seguridad)=>{
                var mascota=Obtener(usuario,id);Auditar(seguridad,usuario,"Consulta de datos de mascota");return mascota;
            },"ER061");
        }
        public void Actualizar(int id,string version,RegistroMascotaBE datos)
        {
            Ejecutar((usuario,seguridad)=>{
                RegistroMascotaBE validos;
                try{validos=RegistroMascotaBLL.Validar(datos);}
                catch(RegistroMascotaException ex){throw new GestionMascotaException(ex.Codigo,ex);}
                MascotaBE actual;
                try{actual=Obtener(usuario,id);}
                catch(SqlException ex){throw new GestionMascotaException(Conexion(ex)?"ER003":ex.Number==50011?"ER011":"ER061",ex);}
                if(string.IsNullOrEmpty(version)||actual.DVH!=version)throw new GestionMascotaException("ER064");
                new GestionMascotaDAL().Actualizar(usuario,id,version,validos);
                var tablas=new IntegridadTablasDAL();var filas=tablas.Leer("Mascota");
                var fila=filas.Rows.Cast<DataRow>().Single(f=>(int)f["idMascota"]==id);
                string dvh=new IntegridadTablasBLL().CalcularDVH(fila);tablas.GuardarDvh("Mascota",id,dvh);fila["dvh"]=dvh;
                new DVVDAL().Guardar(new DVVBE {NombreTabla="Mascota",ValorDVV=seguridad.CalcularDVV(filas.Rows.Cast<DataRow>().Select(f=>(string)f["dvh"]))});
                new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                Auditar(seguridad,usuario,"Actualización de datos de mascota",true);return true;
            },"ER062");
        }
    }
    public sealed class ConsultaHistorialClinicoBLL
    {
        public ResultadoHistorialClinicoBE Consultar(int mascota)
        {
            return MascotaBLL.Ejecutar((usuario,seguridad)=>{
                var propia=MascotaBLL.Obtener(usuario,mascota);var lista=new GestionMascotaDAL().Historial(usuario,mascota);
                if(lista.Count==0)throw new GestionMascotaException("ER011");
                MascotaBLL.Auditar(seguridad,usuario,"Consulta de historial clínico");
                return new ResultadoHistorialClinicoBE {Mascota=propia,Historial=lista};
            },"ER063");
        }
    }
    public sealed class ConsultaDatosClinicosBLL
    {
        public ResultadoDatosClinicosBE Consultar(int mascota)
        {
            return MascotaBLL.Ejecutar((usuario,seguridad)=>{
                var propia=MascotaBLL.Obtener(usuario,mascota);var lista=new GestionMascotaDAL().DatosClinicos(usuario,mascota);
                if(lista.Count==0)throw new GestionMascotaException("ER011");
                MascotaBLL.Auditar(seguridad,usuario,"Consulta de datos clínicos");
                return new ResultadoDatosClinicosBE {Mascota=propia,Datos=lista};
            },"ER063");
        }
    }
}
