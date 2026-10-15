using System;
using System.Data.SqlClient;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.SECURITY;

partial class Program
{
    static void ProbarIdioma(UsuarioBLL bll, UsuarioDAL dal, SecurityManager sec, UsuarioBE webmaster)
    {
        SessionAs(null);
        Check(SessionManager.Instancia.ObtenerIdiomaUsuario() == "es", "Idioma: sesión anónima usa español");
        var cliente = dal.ObtenerPorCorreo("cliente@vitapet.local");
        SessionAs(cliente);
        Check(SessionManager.Instancia.ObtenerIdiomaUsuario() == cliente.Idioma, "Idioma: login conserva preferencia");
        try { bll.ActualizarIdioma(cliente.IdUsuario, "fr"); throw new Exception("Idioma inválido aceptado"); }
        catch (ArgumentException) { Check(true, "Idioma: rechaza códigos no admitidos"); }
        try { bll.ActualizarIdioma(webmaster.IdUsuario, "en"); throw new Exception("Usuario ajeno aceptado"); }
        catch (UnauthorizedAccessException) { Check(true, "Idioma: solo modifica al usuario autenticado"); }
        string dvhAnterior = cliente.DVH;
        bll.ActualizarIdioma(cliente.IdUsuario, "en");
        var actualizado = dal.ObtenerPorId(cliente.IdUsuario);
        Check(actualizado.Idioma == "en" && actualizado.DVH != dvhAnterior && sec.VerificarIntegridad(), "Idioma: actualiza preferencia, DVH, DVV y referencias");
        Check(actualizado.Nombre == cliente.Nombre && actualizado.Contrasena == cliente.Contrasena
            && actualizado.Rol == cliente.Rol && actualizado.Estado == cliente.Estado
            && actualizado.Bloqueado == cliente.Bloqueado && actualizado.IntentosFallidos == cliente.IntentosFallidos
            && actualizado.CorreoElectronico == cliente.CorreoElectronico, "Idioma: conserva los demás campos");
        Check(SessionManager.Instancia.ObtenerIdiomaUsuario() == "en"
            && SessionManager.Instancia.UsuarioActual.DVH == actualizado.DVH, "Idioma: sesión recibe el usuario confirmado");
        SessionAs(actualizado);
        Check(SessionManager.Instancia.ObtenerIdiomaUsuario() == "en", "Idioma: preferencia persiste en otro login");
        Sql("CREATE TRIGGER dbo.TR_PruebaFalloIdioma ON dbo.DVV AFTER UPDATE AS THROW 51041, 'Fallo de prueba', 1;");
        try
        {
            try { bll.ActualizarIdioma(cliente.IdUsuario, "es"); throw new Exception("Fallo SQL ignorado"); }
            catch (SqlException) { Check(true, "Idioma: propaga fallo al guardar DVV"); }
        }
        finally { Sql("DROP TRIGGER dbo.TR_PruebaFalloIdioma"); }
        Check(dal.ObtenerPorId(cliente.IdUsuario).Idioma == "en"
            && SessionManager.Instancia.ObtenerIdiomaUsuario() == "en" && sec.VerificarIntegridad(), "Idioma: rollback conserva base, integridad y sesión");
        bll.ActualizarIdioma(cliente.IdUsuario, "es");
        Check(sec.VerificarIntegridad(), "Idioma: regreso a español conserva integridad");
        SessionAs(webmaster);
    }
}
