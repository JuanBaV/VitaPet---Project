using System;
using System.IO;
using System.Linq;
using System.Configuration;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

partial class Program
{
    static CambiosUsuarioBE Cambios(UsuarioListadoBE u)
    { return new CambiosUsuarioBE { Nombre=u.Nombre, CorreoElectronico=u.CorreoElectronico, Rol=u.Rol, Estado=u.Estado }; }
    static string Foto()
    {
        return string.Join("|",new UsuarioDAL().ObtenerParaIntegridad().Select(u=>u.IdUsuario+":"+u.DVH)) + ";" +
            string.Join("|",new BitacoraDAL().ObtenerParaIntegridad().Select(b=>b.IdBitacora+":"+b.DVH));
    }
    static void ProbarExtensiones(UsuarioBLL bll, UsuarioDAL dal, SecurityManager sec, UsuarioBE webmaster)
    {
        int id=dal.ObtenerPorCorreo("fixture0@test.local").IdUsuario;
        var original=dal.ObtenerPorId(id);
        var detalle=bll.PrepararOperacionUsuario(id,OperacionUsuario.Consultar);
        Check(detalle.Usuario.IdUsuario==id && dal.ObtenerPorId(id).DVH==original.DVH,"CU005: detalle sin modificar usuario");
        var evento=new BitacoraDAL().ObtenerParaIntegridad().Last();
        Check(evento.Accion.Contains("Consulta de usuario (idUsuario="+id+")") && evento.IdUsuario==webmaster.IdUsuario,"CU005: audita actor y usuario consultado");
        foreach(var tipo in new[]{OperacionUsuario.Modificar,OperacionUsuario.Bloquear})
        {
            string foto=Foto();bll.PrepararOperacionUsuario(id,tipo);
            Check(Foto()==foto,"Abrir/cancelar "+tipo+" no escribe ni audita");
        }
        foreach(OperacionUsuario tipo in Enum.GetValues(typeof(OperacionUsuario)))
            Error(()=>bll.PrepararOperacionUsuario(int.MaxValue,tipo),"ER036");
        var edicion=bll.PrepararOperacionUsuario(id,OperacionUsuario.Modificar);
        var cambios=Cambios(edicion.Usuario);cambios.Nombre="  Nombre actualizado  ";cambios.CorreoElectronico="editado@test.local";cambios.Rol="Administrador";cambios.Estado=false;
        bll.ModificarUsuario(id,edicion.Version,cambios);
        var actualizado=dal.ObtenerPorId(id);
        Check(actualizado.Nombre=="Nombre actualizado"&&actualizado.CorreoElectronico=="editado@test.local"&&actualizado.Rol=="Administrador"&&!actualizado.Estado,"CU006: guarda los cuatro campos editables");
        Check(actualizado.Bloqueado==original.Bloqueado && actualizado.Contrasena==original.Contrasena && actualizado.IntentosFallidos==original.IntentosFallidos && actualizado.Idioma==original.Idioma,"CU006: conserva campos restantes");
        Check(sec.VerificarIntegridad(),"CU006: DVH/DVV y referencias válidas");
        string antes=Foto();Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER038");Check(Foto()==antes,"ER038 no modifica ni audita");
        edicion=bll.PrepararOperacionUsuario(id,OperacionUsuario.Modificar);
        cambios=Cambios(edicion.Usuario);cambios.Nombre="";Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER002");
        cambios.Nombre=new string('a',101);Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER025");
        cambios.Nombre="Nombre";cambios.CorreoElectronico="sin-correo";Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER025");
        cambios.CorreoElectronico="cliente@vitapet.local";Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER001");
        cambios.CorreoElectronico="editado@test.local";cambios.Rol="Superusuario";Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER025");
        cambios.Rol="Cliente";cambios.Estado=null;Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER002");
        Check(Foto()==antes,"Validaciones rechazadas no escriben ni auditan");
        Error(()=>bll.BloquearUsuarioAdministrado(id),"ER037");
        cambios.Estado=true;bll.ModificarUsuario(id,edicion.Version,cambios);
        // Intento fallido legítimo para verificar que bloquear conserva el contador.
        bll.RegistrarIntentoFallido(dal.ObtenerPorId(id));
        var previo=dal.ObtenerPorId(id);
        bll.BloquearUsuarioAdministrado(id);var bloqueado=dal.ObtenerPorId(id);
        Check(bloqueado.Bloqueado&&bloqueado.Estado&&bloqueado.IntentosFallidos==previo.IntentosFallidos,"CU007: bloquea conservando estado e intentos");
        Error(()=>bll.BloquearUsuarioAdministrado(id),"ER037");
        Check(sec.VerificarIntegridad(),"CU007: integridad completa");
        bll.DesbloquearUsuarioAdministrado(id);var libre=dal.ObtenerPorId(id);
        Check(!libre.Bloqueado&&libre.Estado&&libre.IntentosFallidos==0,"CU008: desbloquea y reinicia intentos");
        Error(()=>bll.DesbloquearUsuarioAdministrado(id),"ER037");
        int inactivo=dal.ObtenerPorCorreo("fixture3@test.local").IdUsuario;
        antes=Foto();bll.PrepararOperacionUsuario(inactivo,OperacionUsuario.Desbloquear);Check(Foto()==antes,"Abrir/cancelar desbloqueo no audita");
        bll.DesbloquearUsuarioAdministrado(inactivo);
        Check(!dal.ObtenerPorId(inactivo).Estado&&!dal.ObtenerPorId(inactivo).Bloqueado,"CU008: conserva Inactivo");
        // El estado se vuelve a leer al confirmar.
        bll.PrepararOperacionUsuario(id,OperacionUsuario.Bloquear);bll.BloquearUsuarioAdministrado(id);
        Error(()=>bll.BloquearUsuarioAdministrado(id),"ER037");bll.DesbloquearUsuarioAdministrado(id);
        edicion=bll.PrepararOperacionUsuario(id,OperacionUsuario.Modificar);cambios=Cambios(edicion.Usuario);cambios.Nombre="No debe guardarse";
        antes=Foto();
        Sql("CREATE TRIGGER dbo.TR_PruebaFalloUsuario ON dbo.Usuario AFTER UPDATE AS THROW 51039, 'Fallo de prueba', 1;");
        try { Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER039"); }
        finally { Sql("DROP TRIGGER dbo.TR_PruebaFalloUsuario"); }
        Check(Foto()==antes && sec.VerificarIntegridad(),"ER039 revierte usuario, DV y auditoría");
        Sql("CREATE TRIGGER dbo.TR_PruebaFalloAuditoria ON dbo.Bitacora AFTER INSERT AS THROW 51040, 'Fallo de auditoria de prueba', 1;");
        try
        {
            Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER040");
            Error(()=>bll.BloquearUsuarioAdministrado(id),"ER040");
            Error(()=>bll.PrepararOperacionUsuario(id,OperacionUsuario.Consultar),"ER033");
        }
        finally { Sql("DROP TRIGGER dbo.TR_PruebaFalloAuditoria"); }
        Check(Foto()==antes&&sec.VerificarIntegridad(),"ER040/ER033: rollback completo ante falla SQL de auditoría");
        var xml=ConfigurationManager.AppSettings["RutaBitacoraXml"];
        using(var archivo=new FileStream(xml,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
        { Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER040"); }
        Check(Foto()==antes&&sec.VerificarIntegridad(),"ER040: archivo histórico inaccesible revierte SQL");
        foreach(var role in new[]{"cliente","administrador",null})
        {
            SessionAs(role==null?null:dal.ObtenerPorCorreo(role+"@vitapet.local"));
            foreach(Action accion in new Action[]{()=>bll.PrepararOperacionUsuario(id,OperacionUsuario.Consultar),()=>bll.ModificarUsuario(id,edicion.Version,cambios),()=>bll.BloquearUsuarioAdministrado(id),()=>bll.DesbloquearUsuarioAdministrado(id)})
            {
                bool denied=false;try{accion();}catch(UnauthorizedAccessException){denied=true;}
                Check(denied,"Extensiones denegadas a "+(role??"anónimo"));
            }
        }
        SessionAs(webmaster);
        Check(Foto()==antes && sec.VerificarIntegridad(),"Permisos rechazados conservan datos e integridad");
        foreach(var code in new[]{"ER036","ER037","ER038","ER039","ER040"}) Check(sec.ObtenerError(code)!=null,"Catálogo "+code);
        // Integridad corrupta al abrir y confirmar cada extensión.
        string nombre=dal.ObtenerPorId(id).Nombre;
        Sql("UPDATE dbo.Usuario SET nombre='Alterado' WHERE idUsuario="+id);
        foreach(OperacionUsuario tipo in Enum.GetValues(typeof(OperacionUsuario))) Error(()=>bll.PrepararOperacionUsuario(id,tipo),"ER023");
        Error(()=>bll.ModificarUsuario(id,edicion.Version,cambios),"ER023");
        Error(()=>bll.BloquearUsuarioAdministrado(id),"ER023");Error(()=>bll.DesbloquearUsuarioAdministrado(id),"ER023");
        Sql("UPDATE dbo.Usuario SET nombre='"+nombre.Replace("'","''")+"' WHERE idUsuario="+id);
        Check(sec.VerificarIntegridad(),"Los flujos rechazados no reparan ni encubren corrupción");
    }
}
