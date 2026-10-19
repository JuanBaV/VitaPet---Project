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
            Add(r,"Usuario",u.IdUsuario,"rol",u.Rol);
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
        public List<ReferenciaCampoBE> CamposActuales(bool ampliar = true)
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
                Add(r,"Bitacora",b.IdBitacora,"dvh",b.DVH);
            }
            if (ampliar && dal.ObtenerFormato() == "sha256-v2") r.AddRange(new IntegridadTablasBLL().CamposActuales());
            return r;
        }
        private static string Sello(IEnumerable<ReferenciaCampoBE> campos) => EstadoAplicacion.CalcularHuella(Pack("conjunto-v1",
            string.Concat(campos.OrderBy(c=>c.Clave,StringComparer.Ordinal).Select(c=>Pack(c.Clave,c.Huella)))));
        public bool UsuarioConfiable(UsuarioBE u)
        {
            if(u==null) return false;
            var refs=dal.Obtener().ToDictionary(c=>c.Clave,StringComparer.Ordinal);
            return CamposUsuario(u).All(c=> refs.ContainsKey(c.Clave) && refs[c.Clave].Huella==c.Huella);
        }
        public void Verificar(ResultadoIntegridadBE resultado, bool ampliar = true)
        {
            var stored=dal.Obtener();
            var refs=stored.Where(c=>c.Tabla!="#").ToDictionary(c=>c.Clave,StringComparer.Ordinal);
            var sello=stored.SingleOrDefault(c=>c.Clave=="#/0/sello");
            if(sello==null || sello.Huella!=Sello(refs.Values))
                Error(resultado,"ReferenciaIntegridad","—","huella","La referencia protegida está incompleta o alterada.");
            foreach(var c in CamposActuales(ampliar))
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
            var claves=new HashSet<string>(campos.Select(c=>c.Tabla+"/"+c.Fila),StringComparer.Ordinal);
            foreach(var old in dal.Obtener().Where(c=>c.Tabla!="#").GroupBy(c=>c.Tabla+"/"+c.Fila))
                if(!claves.Contains(old.Key)) dal.EliminarFila(old.First().Tabla,old.First().Fila);
            foreach(var c in campos) dal.Guardar(c);
            dal.Guardar(new ReferenciaCampoBE { Tabla="#",Fila="0",Columna="sello",Huella=Sello(campos) });
        }
        public void Inicializar()
        {
            using(var op=new OperacionIntegridadDAL())
            {
                string formato=dal.ObtenerFormato();
                if(formato=="sha256-v2") { op.Complete(); return; }
                if(formato!="pendiente" && formato!="sha256-v1") throw new InvalidOperationException("Formato de integridad desconocido o ausente.");
                if(formato=="pendiente" && dal.Obtener().Count != 0) throw new InvalidOperationException("ER023: no se reinicializan referencias existentes.");
                var result=new Seguridad.SecurityManager().DiagnosticarDigitos();
                if(formato=="sha256-v1") Verificar(result, false);
                if(!result.EsValida) throw new InvalidOperationException("ER023: no se migran referencias sobre una BD inconsistente.");
                new IntegridadTablasBLL().Inicializar();
                dal.GuardarFormato("sha256-v2");
                GuardarEstadoVerificado();
                op.Complete();
            }
        }
        internal static void Error(ResultadoIntegridadBE r,string tabla,string fila,string columna,string motivo)
        { r.Detalles.Add(new DetalleIntegridadBE { Tabla=tabla,Fila=fila,Columna=columna,Motivo=motivo }); }
    }
}
