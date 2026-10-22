using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VitaPet.BE;
using VitaPet.DAL;

namespace VitaPet.BLL.Seguridad
{
    public class SecurityManager
    {
        private const string TablaUsuario = "Usuario";
        private const string TablaBitacora = "Bitacora";
        private const string PatronContrasenaSegura =
            @"^(?=.{8,}$)(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s]).+$";

        private readonly UsuarioDAL _usuarioDAL;
        private readonly BitacoraDAL _bitacoraDAL;
        private readonly DVVDAL _dvvDAL;
        private readonly BitacoraBLL _bitacoraBLL;
        private readonly ErrorSistemaBLL _errorSistemaBLL;
        private readonly IntegridadBLL _integridadBLL;

        public SecurityManager()
        {
            _usuarioDAL = new UsuarioDAL();
            _bitacoraDAL = new BitacoraDAL();
            _dvvDAL = new DVVDAL();
            _bitacoraBLL = new BitacoraBLL();
            _errorSistemaBLL = new ErrorSistemaBLL();
            _integridadBLL = new IntegridadBLL();
        }

        public string CalcularHash(string valor)
        {
            return _integridadBLL.CalcularHash(valor);
        }

        public bool ValidarDatosObligatorios(params string[] valores)
        {
            return valores != null &&
                   valores.Length > 0 &&
                   valores.All(valor => !string.IsNullOrWhiteSpace(valor));
        }

        public bool CumpleRequisitosContrasena(
            string contrasena)
        {
            return !string.IsNullOrWhiteSpace(contrasena) &&
                   Regex.IsMatch(
                       contrasena,
                       PatronContrasenaSegura);
        }

        public bool VerificarIntegridad() => DiagnosticarIntegridad().EsValida;

        public ResultadoIntegridadBE DiagnosticarIntegridad(bool permitirMigracionesPendientes = false)
        {
            using(var op=new OperacionIntegridadDAL())
            {
                var resultado=DiagnosticarDigitos();
                string formato=new ReferenciaIntegridadDAL().ObtenerFormato();
                if(formato=="sha256-v2") new IntegridadTablasBLL().Verificar(resultado,permitirMigracionesPendientes);
                else if(formato!="sha256-v1") ReferenciaIntegridadBLL.Error(resultado,"__FormatoIntegridad","1","formato","Formato de integridad no inicializado o desconocido.");
                new ReferenciaIntegridadBLL().Verificar(resultado);
                op.Complete();
                return resultado;
            }
        }
        internal ResultadoIntegridadBE DiagnosticarDigitos()
        {
            var r=new ResultadoIntegridadBE();
            var usuarios=_usuarioDAL.ObtenerParaIntegridad();
            var bitacora=_bitacoraDAL.ObtenerParaIntegridad();
            foreach(var u in usuarios) if(!_integridadBLL.VerificarDVH(u))
                ReferenciaIntegridadBLL.Error(r,"Usuario",u.IdUsuario.ToString(),"dvh","DVH no coincide; consultar detalle por campo.");
            foreach(var b in bitacora) if(!_integridadBLL.VerificarDVH(b))
                ReferenciaIntegridadBLL.Error(r,"Bitacora",b.IdBitacora.ToString(),"dvh","DVH no coincide; consultar detalle por campo.");
            VerificarTabla(r,"Usuario",usuarios.Select(u=>u.DVH));
            VerificarTabla(r,"Bitacora",bitacora.Select(b=>b.DVH));
            return r;
        }
        private void VerificarTabla(ResultadoIntegridadBE r,string tabla,IEnumerable<string> dvhs)
        {
            var dvv=_dvvDAL.ObtenerPorTabla(tabla);
            if(dvv==null || !_integridadBLL.VerificarDVV(dvhs,dvv.ValorDVV))
                ReferenciaIntegridadBLL.Error(r,"DVV", "nombreTabla="+tabla,"valorDVV",dvv==null ? "Control ausente." : "DVV no coincide.");
        }
        public void ExigirIntegridad()
        {
            if(!VerificarIntegridad()) throw new InvalidOperationException("ER023: inconsistencia de integridad.");
        }
        public bool EsWebmasterConfiable(UsuarioBE usuario)
        {
            return usuario!=null && string.Equals(usuario.Rol,"Webmaster",StringComparison.OrdinalIgnoreCase)
                && new Estados.UsuarioStateBLL(usuario).PuedeIniciarSesion()
                && new ReferenciaIntegridadBLL().UsuarioConfiable(usuario);
        }
        public UsuarioBE AutenticarRecuperacion(string correo,string contrasena)
        {
            using(var op=new OperacionIntegridadDAL())
            {
                var u=_usuarioDAL.ObtenerPorCorreo(correo);
                bool valido=EsWebmasterConfiable(u) && string.Equals(u.Contrasena,CalcularHash(contrasena),StringComparison.OrdinalIgnoreCase);
                op.Complete();
                return valido ? u : null;
            }
        }
        public UsuarioBE ExigirCliente()
        {
            return ExigirGrupo("Cliente");
        }

        public UsuarioBE ExigirPermiso(params string[] pantallas)
        {
            var sesion=VitaPet.SECURITY.SessionManager.Instancia;
            if(sesion.UsuarioActual==null || sesion.HaySesionContingencia) throw new UnauthorizedAccessException("Se requiere una sesión.");
            ExigirIntegridad();
            var accesos=new HashSet<string>(new PerfilBLL().PantallasActuales(),StringComparer.OrdinalIgnoreCase);
            if (!pantallas.Any(accesos.Contains)) throw new UnauthorizedAccessException("La cuenta no tiene permiso para esta pantalla.");
            var u = _usuarioDAL.ObtenerPorId(VitaPet.SECURITY.SessionManager.Instancia.UsuarioActual.IdUsuario);
            return u;
        }

        private UsuarioBE ExigirGrupo(string grupo)
        {
            return ExigirPermiso(PerfilBLL.Catalogo.Where(p => p.Value==grupo).Select(p => p.Key).ToArray());
        }

        public UsuarioBE ExigirAdministrador()
        {
            return ExigirGrupo("Administrador");
        }

        public UsuarioBE ExigirWebmaster(string pantalla = null)
        {
            var actual=VitaPet.SECURITY.SessionManager.Instancia.UsuarioActual;
            if(actual==null) throw new UnauthorizedAccessException("Se requiere una sesión.");
            if(actual.Rol!="Webmaster")
            {
                if(pantalla!=null) return ExigirPermiso(pantalla);
                throw new UnauthorizedAccessException("Acceso exclusivo para Webmaster.");
            }
            var vigente=_usuarioDAL.ObtenerPorCorreo(actual.CorreoElectronico);
            if(vigente==null || vigente.IdUsuario!=actual.IdUsuario || !EsWebmasterConfiable(vigente))
                throw new UnauthorizedAccessException("No se pudo verificar la identidad del Webmaster.");
            return vigente;
        }

        public string CalcularDVH(UsuarioBE usuario)
        {
            return _integridadBLL.CalcularDVH(usuario);
        }

        public string CalcularDVH(BitacoraBE registro)
        {
            return _integridadBLL.CalcularDVH(registro);
        }

        public string CalcularDVV(IEnumerable<string> digitosHorizontales)
        {
            return _integridadBLL.CalcularDVV(digitosHorizontales);
        }

        public bool VerificarDVH(UsuarioBE usuario)
        {
            return _integridadBLL.VerificarDVH(usuario);
        }

        public bool VerificarDVH(BitacoraBE registro)
        {
            return _integridadBLL.VerificarDVH(registro);
        }

        public bool VerificarDVV(
            IEnumerable<string> digitosHorizontales,
            string dvvAlmacenado)
        {
            return _integridadBLL.VerificarDVV(
                digitosHorizontales,
                dvvAlmacenado);
        }

        public int RegistrarBitacora(BitacoraBE registro)
        {
            using(var operacion = new OperacionIntegridadDAL())
            {
                ExigirIntegridad();

            if (registro == null)
            {
                throw new ArgumentNullException(nameof(registro));
            }

            // SQL DATETIME redondea a 1/300 s; calcular el DVH sobre el valor que realmente se persiste.
            registro.Fecha = new System.Data.SqlTypes.SqlDateTime(registro.Fecha).Value;
            registro.DVH =
                _integridadBLL.CalcularDVH(registro);

            List<string> dvhs =
                _bitacoraDAL.ObtenerDVHs();

            dvhs.Add(registro.DVH);

            DVVBE dvv = new DVVBE
            {
                NombreTabla = TablaBitacora,
                ValorDVV = _integridadBLL.CalcularDVV(dvhs)
            };

            int resultado = _bitacoraBLL.Registrar(
                registro,
                dvv);

                new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                operacion.Complete();
                return resultado;
            }
        }

        public ErrorSistemaBE ObtenerError(string codigo)
        {
            return _errorSistemaBLL.ObtenerPorCodigo(codigo);
        }
    }
}
