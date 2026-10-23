using System;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public sealed class MigracionCifradoBLL
    {
        public void Ejecutar()
        {
            if (DatabaseMigrator.EstaAplicada(CifradoDatosDAL.Migracion)) return;
            using (var tx = new OperacionIntegridadDAL())
            {
                var seguridad = new SecurityManager();
                seguridad.ExigirIntegridad();
                DatabaseMigrator.AplicarCifrado();
                // Deben conservarse los valores lógicos: jamás recalcular DVH/DVV para ocultar una pérdida de datos.
                seguridad.ExigirIntegridad();
                tx.Complete();
            }
        }
    }
}
