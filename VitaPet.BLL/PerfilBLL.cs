using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;

namespace VitaPet.BLL
{
    public sealed class PerfilBLL
    {
        public static readonly IReadOnlyDictionary<string, string> Catalogo = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                {"Cliente","Cliente"},{"Planes","Cliente"},{"ContratarPlan","Cliente"},{"GestionPago","Cliente"},
                {"ConsultaCobertura","Cliente"},{"Reintegros","Cliente"},{"RegistroMascota","Cliente"},{"Mascotas","Cliente"},
                {"ActualizarMascota","Cliente"},{"ConsultaMascota","Cliente"},{"HistorialClinico","Cliente"},{"DatosClinicos","Cliente"},{"Turnos","Cliente"},
                {"Empresas","Administrador"},{"AltaEmpresa","Administrador"},{"ConsultaEmpresa","Administrador"},{"ModificarEmpresa","Administrador"},
                {"RegistroClienteCorporativo","Administrador"},{"GestionPlanes","Administrador"},{"AltaPlan","Administrador"},{"ConsultaPlan","Administrador"},{"ModificarPlan","Administrador"},
                {"ConsultaBitacora","Webmaster"},{"BaseDatos","Webmaster"},{"GestionUsuarios","Webmaster"},{"ConsultarUsuario","Webmaster"},
                {"ModificarUsuario","Webmaster"},{"BloquearUsuario","Webmaster"},{"DesbloquearUsuario","Webmaster"},
                {"GestionPerfiles","Webmaster"},{"AltaUsuario","Webmaster"},{"ConsultarPerfiles","Administrador"}
            });
        private readonly PerfilDAL dal = new PerfilDAL();
        public static string NombrePantalla(string pantalla)
        {
            switch(pantalla)
            {
                case "Cliente": return "Mi perfil";
                case "GestionPago": return "Pago y factura";
                case "ConsultaCobertura": return "Consulta de cobertura";
                case "RegistroMascota": return "Registro de mascotas";
                case "ActualizarMascota": return "Modificar mascota";
                case "ConsultaMascota": return "Consultar mascota";
                case "HistorialClinico": return "Historial clínico";
                case "DatosClinicos": return "Datos clínicos";
                case "AltaEmpresa": return "Alta de empresas";
                case "ConsultaEmpresa": return "Consultar empresa";
                case "ModificarEmpresa": return "Modificar empresa";
                case "RegistroClienteCorporativo": return "Alta de clientes corporativos";
                case "GestionPlanes": return "Gestión de planes";
                case "AltaPlan": return "Alta de planes";
                case "ConsultaPlan": return "Consultar plan";
                case "ModificarPlan": return "Modificar plan";
                case "ConsultaBitacora": return "Bitácora";
                case "BaseDatos": return "Base de datos";
                case "GestionUsuarios": return "Gestión de usuarios";
                case "ConsultarUsuario": return "Consultar usuario";
                case "ModificarUsuario": return "Modificar usuario";
                case "BloquearUsuario": return "Bloquear usuario";
                case "DesbloquearUsuario": return "Desbloquear usuario";
                case "GestionPerfiles": return "Gestión de perfiles";
                case "AltaUsuario": return "Alta de usuarios";
                case "ConsultarPerfiles": return "Consultar perfiles";
                case "ContratarPlan": return "Contratar plan";
                default: return pantalla;
            }
        }
        internal List<PermisoComponenteBE> Leer() //lee los permisos y perfiles de la DB
        {
            var roles = dal.Roles().Rows.Cast<DataRow>().ToDictionary(r => (int)r["idRol"]);
            var todos = dal.Componentes().Rows.Cast<DataRow>().ToDictionary(r => (int)r["idComponente"], r =>
                (string)r["tipo"] == "Perfil" ? (PermisoComponenteBE)CrearPerfil(r,roles)
                    : new PermisoPantallaBE { Id=(int)r["idComponente"], Nombre=(string)r["nombre"], Pantalla=(string)r["pantalla"] });
            var permisosRoles = dal.PermisosRoles();
            foreach (DataRow r in permisosRoles.Rows)
            {
                var rol=todos[(int)r["idRol"]] as RolBE;
                if(rol==null) throw new InvalidOperationException("Un rol debe ser un componente compuesto.");
                rol.Agregar(todos[(int)r["idComponente"]]);
            }
            foreach(var perfil in todos.Values.OfType<PerfilBE>())
                perfil.Version=new IntegridadBLL().CalcularHash(perfil.Version+string.Concat(permisosRoles.Rows.Cast<DataRow>().Where(r => (int)r["idRol"]==perfil.Id).Select(r => (string)r["dvh"])));
            return todos.Values.ToList();
        }
        private static PerfilBE CrearPerfil(DataRow fila, Dictionary<int,DataRow> roles) //crea un perfil
        {
            int id=(int)fila["idComponente"];
            if (!roles.ContainsKey(id)) throw new InvalidOperationException("Un componente compuesto debe corresponder a un rol.");
            var perfil=new RolBE();
            perfil.Id=id; perfil.Nombre=(string)fila["nombre"]; perfil.Sistema=(bool)fila["sistema"];
            perfil.Version=(string)fila["dvh"]+(string)roles[id]["dvh"];
            return perfil;
        }
        public List<PermisoComponenteBE> Consultar() //consulta los permisos y perfiles de la DB, exigiendo seguridad e integridad
        {
            using (var tx = new OperacionIntegridadDAL())
            {
                var sec = new SecurityManager();
                sec.ExigirPermiso("ConsultarPerfiles", "GestionPerfiles", "AltaUsuario", "ModificarUsuario");
                sec.ExigirIntegridad(); var lista = Leer(); tx.Complete(); return lista;
            }
        }
        public bool PuedeAcceder(string pantalla) //verifica si el usuario actual puede acceder a una pantalla especifica, exigiendo integridad y seguridad
        {
            var actual=SessionManager.Instancia.UsuarioActual;
            if(actual==null || SessionManager.Instancia.HaySesionContingencia) return false;
            var sec=new SecurityManager();
            // La recuperación de integridad debe seguir disponible para un Webmaster confiable.
            if(pantalla=="BaseDatos" && sec.EsWebmasterConfiable(new UsuarioDAL().ObtenerPorId(actual.IdUsuario))) return true;
            sec.ExigirIntegridad();
            return PantallasActuales().Contains(pantalla,StringComparer.OrdinalIgnoreCase);
        }
        public IEnumerable<string> PantallasActuales() //obtiene el listado de pantallas a las que el usuario tiene acceso
        {
            var sesion = SessionManager.Instancia;
            if (sesion.HaySesionContingencia || sesion.UsuarioActual == null) return Enumerable.Empty<string>();
            var u = new UsuarioDAL().ObtenerPorId(sesion.UsuarioActual.IdUsuario);
            if (u == null || !new Estados.UsuarioStateBLL(u).PuedeIniciarSesion()) return Enumerable.Empty<string>();
            var seguridad=new SecurityManager();
            if(!seguridad.VerificarIntegridad())
                return seguridad.EsWebmasterConfiable(u) ? new[]{"BaseDatos"} : Enumerable.Empty<string>();
            var perfil = Leer().OfType<RolBE>().SingleOrDefault(p => p.Id == u.IdRol);
            if (perfil==null) return Enumerable.Empty<string>();
            return perfil.Pantallas().Concat(new[]{"Home","Default","Logout"}).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        internal void ActualizarIntegridad(params string[] tablas)
        {
            var d = new IntegridadTablasDAL(); var h = new IntegridadTablasBLL(); var sec = new SecurityManager();
            foreach (var tabla in tablas)
            {
                var filas = d.Leer(tabla);
                foreach (DataRow r in filas.Rows) { var dvh=h.CalcularDVH(r); d.GuardarDvh(tabla,r[IntegridadTablasDAL.Claves[tabla]],dvh); r["dvh"]=dvh; }
                new DVVDAL().Guardar(new DVVBE { NombreTabla=tabla, ValorDVV=sec.CalcularDVV(filas.Rows.Cast<DataRow>().Select(r => (string)r["dvh"])) });
            }
            new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
        }
        public void AsegurarIniciales() //asegura que los 3 perfiles basicos sigan existiendo en la DB (Webmaster, Administrador y Cliente)
        {
            using (var tx = new OperacionIntegridadDAL())
            {
                var sec = new SecurityManager(); sec.ExigirIntegridad();
                if (dal.Componentes().Rows.Cast<DataRow>().Any(r => (string)r["tipo"]=="Pantalla")) { tx.Complete(); return; }
                var existentes=dal.Componentes().Rows.Cast<DataRow>().Where(r => (string)r["tipo"]=="Perfil").ToDictionary(r => (string)r["nombre"],r => (int)r["idComponente"]);
                var roles = new[] {"Cliente","Administrador","Webmaster"}.ToDictionary(n => n, n => existentes.ContainsKey(n) ? existentes[n] : dal.Insertar(n,null,true));
                foreach (var p in Catalogo) dal.Vincular(roles[p.Value], dal.Insertar("Pantalla: " + p.Key,p.Key,true));
                dal.Vincular(roles["Webmaster"], dal.Componentes().Rows.Cast<DataRow>().Where(r => (string)(r["pantalla"] as string ?? "") == "ConsultarPerfiles").Select(r => (int)r["idComponente"]).Single());
                ActualizarIntegridad("PermisoComponente","Rol","RolPermiso"); sec.ExigirIntegridad(); tx.Complete();
            }
        }
        private static void ExigirGestion() 
        {
            var sec = new SecurityManager(); var u = sec.ExigirPermiso("GestionPerfiles");
            if (u.Rol != "Webmaster") throw new UnauthorizedAccessException("La gestión de perfiles corresponde al Webmaster.");
        }
        public void Guardar(int id, string version, string nombre, IEnumerable<int> hijos) //guardar un perfil, verificando que no se creen ciclos 
        {
            using (var tx = new OperacionIntegridadDAL())
            {
                ExigirGestion(); var sec=new SecurityManager(); sec.ExigirIntegridad();
                nombre=(nombre ?? "").Trim();
                if (nombre.Length==0 || nombre.Length>30 || nombre.Any(c => !char.IsLetterOrDigit(c) && c!=' ' && c!='_' && c!='-')) throw new ArgumentException("Ingresá un nombre de perfil de hasta 30 caracteres, usando letras, números, espacios o guiones.");
                var todos=Leer(); var perfil=todos.OfType<PerfilBE>().SingleOrDefault(p => p.Id==id);
                if (id!=0 && (perfil==null || perfil.Version!=version)) throw new ArgumentException("El perfil cambió. Volvé a seleccionarlo.");
                if (perfil!=null && perfil.Sistema && nombre!=perfil.Nombre) throw new ArgumentException("Los nombres de los tres perfiles iniciales se conservan.");
                if (todos.Any(p => p.Id!=id && string.Equals(p.Nombre,nombre,StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Ya existe ese nombre.");
                if (perfil!=null && perfil.Nombre!=nombre && new UsuarioDAL().ObtenerParaIntegridad().Any(u => u.IdRol==perfil.Id)) throw new ArgumentException("No se puede renombrar un perfil asignado a usuarios.");
                var seleccion=(hijos ?? Enumerable.Empty<int>()).Distinct().ToList();
                if (seleccion.Count==0) throw new ArgumentException("Seleccioná al menos una pantalla o perfil.");
                var prueba=new PerfilBE { Id=id, Nombre=nombre };
                foreach (int hijo in seleccion)
                {
                    var componente=todos.SingleOrDefault(p => p.Id==hijo);
                    if (componente==null || hijo==id || Alcanza(componente,id)) throw new ArgumentException("La combinación de perfiles contiene un ciclo o una selección inválida.");
                    prueba.Agregar(componente);
                }
                if (nombre!="Webmaster" && prueba.Pantallas().Any(p => p=="GestionPerfiles" || p=="AltaUsuario")) throw new ArgumentException("La gestión de perfiles y el alta de usuarios son exclusivas del Webmaster.");
                if (nombre=="Webmaster" && new[] {"GestionPerfiles","AltaUsuario","GestionUsuarios","ModificarUsuario","BaseDatos"}.Any(p => !prueba.Pantallas().Contains(p))) throw new ArgumentException("El Webmaster debe conservar sus accesos de gestión y recuperación.");
                if (id==0) id=dal.Insertar(nombre,null,false); else dal.Renombrar(id,nombre);
                dal.QuitarHijos(id); foreach(int hijo in seleccion) dal.Vincular(id,hijo);
                foreach(var f in Leer().OfType<PerfilBE>())
                {
                    var accesos=f.Pantallas().ToArray();
                    if(f.Nombre!="Webmaster" && accesos.Any(p => p=="GestionPerfiles" || p=="AltaUsuario")) throw new ArgumentException("Los accesos exclusivos del Webmaster no pueden heredarse en otro perfil.");
                    if(f.Nombre=="Webmaster" && new[]{"GestionPerfiles","AltaUsuario","GestionUsuarios","ModificarUsuario","BaseDatos"}.Any(p => !accesos.Contains(p))) throw new ArgumentException("El cambio quitaría accesos de gestión al Webmaster.");
                }
                ActualizarIntegridad("PermisoComponente","Rol","RolPermiso"); Auditar("Guardado de perfil: "+nombre); sec.ExigirIntegridad(); tx.Complete();
            }
        }
        private static bool Alcanza(PermisoComponenteBE p,int id) { var f=p as PerfilBE; return p.Id==id || f!=null && f.Hijos.Any(h => Alcanza(h,id)); } 
        public void Eliminar(int id,string version) //eliminar un perfil
        {
            using(var tx=new OperacionIntegridadDAL())
            {
                ExigirGestion(); var sec=new SecurityManager(); sec.ExigirIntegridad(); var todos=Leer(); var p=todos.OfType<PerfilBE>().SingleOrDefault(f => f.Id==id);
                if(p==null || p.Version!=version) throw new ArgumentException("El perfil cambió. Volvé a seleccionarlo.");
                if(p.Sistema) throw new ArgumentException("Los tres perfiles iniciales deben conservarse.");
                if(new UsuarioDAL().ObtenerParaIntegridad().Any(u => u.IdRol==p.Id) || todos.OfType<PerfilBE>().Any(f => f.Hijos.Any(h => h.Id==id))) throw new ArgumentException("El perfil está asignado a usuarios o incluido en otro perfil.");
                dal.Eliminar(id); ActualizarIntegridad("PermisoComponente","Rol","RolPermiso"); Auditar("Eliminación de perfil: "+p.Nombre); sec.ExigirIntegridad(); tx.Complete();
            }
        }
        private static void Auditar(string accion) { new SecurityManager().RegistrarBitacora(new BitacoraBE { IdUsuario=SessionManager.Instancia.UsuarioActual.IdUsuario, Fecha=DateTime.Now, Accion=accion, Modulo="Perfiles", Criticidad="Informativa" }); }
    }
}
