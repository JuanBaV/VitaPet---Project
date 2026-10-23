using System;
using System.Globalization;
using VitaPet.BE;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public sealed class MigracionRolesBLL
    {
        public void Ejecutar()
        {
            DatabaseMigrator.Migrate("031_AutorizacionPorPantallas.sql");
            bool usaRoles=new PerfilDAL().UsaRoles();
            if (DatabaseMigrator.EstaAplicada("033_EliminarPermisoRelacion.sql")) { DatabaseMigrator.Migrate(); return; }

            using (var tx = new OperacionIntegridadDAL())
            {
                var referencias = new ReferenciaIntegridadBLL();
                referencias.Inicializar();
                var seguridad = new SecurityManager();
                if (!seguridad.DiagnosticarIntegridad(true).EsValida)
                    throw new InvalidOperationException("ER023: no se migran roles sobre datos inconsistentes.");

                DatabaseMigrator.Migrate();
                if (!usaRoles)
                {
                    var usuarios = new UsuarioDAL();
                    foreach (var usuario in usuarios.ObtenerParaIntegridad())
                    {
                        usuarios.ActualizarDVH(usuario.IdUsuario, seguridad.CalcularDVH(usuario));
                        // La referencia de rol por nombre se sustituye por la FK idRol.
                        new ReferenciaIntegridadDAL().EliminarFila("Usuario", usuario.IdUsuario.ToString(CultureInfo.InvariantCulture));
                    }
                    new DVVDAL().Guardar(new DVVBE {
                        NombreTabla = "Usuario", ValorDVV = seguridad.CalcularDVV(usuarios.ObtenerDVHs())
                    });
                }
                new PerfilBLL().ActualizarIntegridad("PermisoComponente", "Rol", "RolPermiso");
                seguridad.ExigirIntegridad();
                tx.Complete();
            }
        }
    }
}
