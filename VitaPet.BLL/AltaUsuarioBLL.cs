using System;
using System.Linq;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public sealed class AltaUsuarioBLL
    {
        public int Registrar(RegistroClienteBE datos, string perfil)
        {
            var sec=new SecurityManager();
            using(var tx=new OperacionIntegridadDAL())
            {
                var actor=sec.ExigirPermiso("AltaUsuario");
                if(actor.Rol!="Webmaster") throw new UnauthorizedAccessException("Solo el Webmaster puede dar de alta usuarios.");
                sec.ExigirIntegridad(); var d=ClienteBLL.ValidarDatosRegistro(datos);
                var permisos=new PerfilBLL(); var p=permisos.Leer().OfType<PerfilBE>().SingleOrDefault(f => f.Nombre==perfil);
                if(p==null) throw new ArgumentException("Seleccioná un perfil vigente.");
                var dal=new UsuarioDAL();
                if(dal.ObtenerPorCorreo(d.CorreoElectronico)!=null) throw new RegistroClienteException("ER001");
                var u=new UsuarioBE {Nombre=d.Nombre+" "+d.Apellido, CorreoElectronico=d.CorreoElectronico, Contrasena=sec.CalcularHash(d.Contrasena), Rol=perfil, Estado=true, Idioma="es"};
                u.IdUsuario=dal.RegistrarCliente(u); u.DVH=sec.CalcularDVH(u);
                // El alta existente fija Cliente: asignar el perfil validado antes de publicar sus controles.
                dal.GuardarAdministracion(u, "");
                if(p.Pantallas().Any(pantalla => PerfilBLL.Catalogo.ContainsKey(pantalla) && PerfilBLL.Catalogo[pantalla]=="Cliente"))
                { new ClienteDAL().Registrar(u.IdUsuario,d); permisos.ActualizarIntegridad("Cliente"); }
                new DVVDAL().Guardar(new DVVBE {NombreTabla="Usuario",ValorDVV=sec.CalcularDVV(dal.ObtenerDVHs())});
                new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                sec.RegistrarBitacora(new BitacoraBE {IdUsuario=actor.IdUsuario,Fecha=DateTime.Now,Accion="Alta de usuario (idUsuario="+u.IdUsuario+", perfil="+perfil+")",Modulo="Gestión de usuarios",Criticidad="Informativa"});
                sec.ExigirIntegridad();tx.Complete();return u.IdUsuario;
            }
        }
    }
}
