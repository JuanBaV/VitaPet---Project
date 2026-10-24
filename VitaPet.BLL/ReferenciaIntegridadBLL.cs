using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VitaPet.BE;
using VitaPet.DAL;
using VitaPet.SECURITY;
namespace VitaPet.BLL
{
    public sealed class ReferenciaIntegridadBLL
    {
        private readonly ReferenciaIntegridadDAL dal = new ReferenciaIntegridadDAL();
        private static string Pack(params string[] values) => string.Concat(values.Select(v => v == null ? "-1:" : v.Length.ToString(CultureInfo.InvariantCulture)+":"+v));
        private static string Value(object value)
        {
            if(value==null) return null;
            if(value is bool) return (bool)value ? "1" : "0";
            if(value is DateTime) return ((DateTime)value).ToString("O",CultureInfo.InvariantCulture);
            return Convert.ToString(value,CultureInfo.InvariantCulture);
        }
        private static void Add(List<ReferenciaCampoBE> result,string tabla,int id,string columna,object value)
        {
            string fila=id.ToString(CultureInfo.InvariantCulture);
            result.Add(new ReferenciaCampoBE { Tabla=tabla,Fila=fila,Columna=columna,
                Huella=EstadoAplicacion.CalcularHuella(Pack("campo-v1",tabla,fila,columna,Value(value))) });
        }
        public List<ReferenciaCampoBE> CamposUsuario(UsuarioBE u)
        {
            var r=new List<ReferenciaCampoBE>();
            Add(r,"Usuario",u.IdUsuario,"idUsuario",u.IdUsuario);
            Add(r,"Usuario",u.IdUsuario,"correoElectronico",u.CorreoElectronico);
            Add(r,"Usuario",u.IdUsuario,"contrasena",u.Contrasena);
            Add(r,"Usuario",u.IdUsuario,"idRol",u.IdRol);
            Add(r,"Usuario",u.IdUsuario,"estado",u.Estado);
            Add(r,"Usuario",u.IdUsuario,"nombre",u.Nombre);
            Add(r,"Usuario",u.IdUsuario,"intentosFallidos",u.IntentosFallidos);
            Add(r,"Usuario",u.IdUsuario,"bloqueado",u.Bloqueado);
            Add(r,"Usuario",u.IdUsuario,"idioma",u.Idioma);
            // Ausencia canónica = falso: compatible con referencias previas a CU-005-005.
            if(u.RequiereRestablecerContrasena) Add(r,"Usuario",u.IdUsuario,"requiereRestablecerContrasena",true);
            Add(r,"Usuario",u.IdUsuario,"dvh",u.DVH);
            return r;
        }
        public List<ReferenciaCampoBE> CamposActuales()
        {
            var r=new List<ReferenciaCampoBE>();
            foreach(var u in new UsuarioDAL().ObtenerParaIntegridad()) r.AddRange(CamposUsuario(u));
            foreach(var b in new BitacoraDAL().ObtenerParaIntegridad())
            {
                Add(r,"Bitacora",b.IdBitacora,"idBitacora",b.IdBitacora);
                Add(r,"Bitacora",b.IdBitacora,"idUsuario",b.IdUsuario);
                Add(r,"Bitacora",b.IdBitacora,"fecha",b.Fecha);
                Add(r,"Bitacora",b.IdBitacora,"accion",b.Accion);
                Add(r,"Bitacora",b.IdBitacora,"modulo",b.Modulo);
                Add(r,"Bitacora",b.IdBitacora,"criticidad",b.Criticidad);
                if(b.Archivado) Add(r,"Bitacora",b.IdBitacora,"archivado",true);
                Add(r,"Bitacora",b.IdBitacora,"dvh",b.DVH);
            }
            r.AddRange(new IntegridadTablasBLL().CamposActuales());
            return r;
        }
        private static string Sello(IEnumerable<ReferenciaCampoBE> campos) => EstadoAplicacion.CalcularHuella(Pack("conjunto-v1",
            string.Concat(campos.OrderBy(c=>c.Clave,StringComparer.Ordinal).Select(c=>Pack(c.Clave,c.Huella)))));
        public bool UsuarioConfiable(UsuarioBE u)
        {
            if(u==null) return false;
            var refs=dal.Obtener().ToDictionary(c=>c.Clave,StringComparer.Ordinal);
            return CamposUsuario(u).All(c=> {
                ReferenciaCampoBE anterior;
                return refs.TryGetValue(c.Clave,out anterior) && anterior.Huella==c.Huella;
            });
        }
        public bool RolConfiable(int idRol)
        {
            var guardadas=dal.Obtener();
            var refs=guardadas.Where(c=>c.Tabla!="#").ToDictionary(c=>c.Clave,StringComparer.Ordinal);
            var sello=guardadas.SingleOrDefault(c=>c.Clave=="#/0/sello");
            if(sello==null || sello.Huella!=Sello(refs.Values)) return false;
            foreach(var tabla in new[]{"Rol","PermisoComponente"})
            {
                var datos=new IntegridadTablasDAL().Leer(tabla);
                var fila=datos.Rows.Cast<System.Data.DataRow>().SingleOrDefault(r=>(int)r[IntegridadTablasDAL.Claves[tabla]]==idRol);
                if(fila==null) return false;
                foreach(System.Data.DataColumn columna in datos.Columns)
                {
                    var clave=tabla+"/"+idRol.ToString(CultureInfo.InvariantCulture)+"/"+columna.ColumnName;
                    ReferenciaCampoBE anterior;
                    string huella=new IntegridadBLL().CalcularHash(Pack("campo-v1",tabla,idRol.ToString(CultureInfo.InvariantCulture),columna.ColumnName,IntegridadTablasBLL.Valor(fila[columna])));
                    if(!refs.TryGetValue(clave,out anterior) || anterior.Huella!=huella) return false;
                }
            }
            return true;
        }
        public void Verificar(ResultadoIntegridadBE resultado)
        {
            var stored=dal.Obtener();
            var refs=stored.Where(c=>c.Tabla!="#").ToDictionary(c=>c.Clave,StringComparer.Ordinal);
            var sello=stored.SingleOrDefault(c=>c.Clave=="#/0/sello");
            if(sello==null || sello.Huella!=Sello(refs.Values))
                Error(resultado,"ReferenciaIntegridad","—","huella","La referencia protegida está incompleta o alterada.");
            foreach(var c in CamposActuales())
            {
                ReferenciaCampoBE before;
                if(!refs.TryGetValue(c.Clave,out before)) Error(resultado,c.Tabla,c.Fila,c.Columna,"Campo o fila sin referencia confiable.");
                else if(c.Huella!=before.Huella) Error(resultado,c.Tabla,c.Fila,c.Columna,"El campo o su referencia protegida fue alterado.");
                refs.Remove(c.Clave);
            }
            foreach(var c in refs.Values) Error(resultado,c.Tabla,c.Fila,c.Columna,"Fila ausente o referencia inesperada.");
        }
        // Solo dentro de una escritura legítima cuya integridad previa ya fue comprobada.
        internal void GuardarEstadoVerificado()
        {
            var campos=CamposActuales();
            var guardadas=dal.Obtener();
            var claves=new HashSet<string>(campos.Select(c=>c.Tabla+"/"+c.Fila),StringComparer.Ordinal);
            foreach(var old in guardadas.Where(c=>c.Tabla!="#").GroupBy(c=>c.Tabla+"/"+c.Fila))
                if(!claves.Contains(old.Key)) dal.EliminarFila(old.First().Tabla,old.First().Fila);
            campos.Add(new ReferenciaCampoBE { Tabla="#",Fila="0",Columna="sello",Huella=Sello(campos) });
            var anteriores=guardadas.ToDictionary(c=>c.Clave,StringComparer.Ordinal);
            foreach(var c in campos)
            {
                ReferenciaCampoBE anterior;
                if(!anteriores.TryGetValue(c.Clave,out anterior) || anterior.Huella!=c.Huella)
                    dal.Guardar(c);
            }
        }
        internal static void Error(ResultadoIntegridadBE r,string tabla,string fila,string columna,string motivo)
        { r.Detalles.Add(new DetalleIntegridadBE { Tabla=tabla,Fila=fila,Columna=columna,Motivo=motivo }); }
    }
}
