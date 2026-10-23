using System;
using System.Linq;
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
                var resultado = seguridad.DiagnosticarIntegridad();
                if (!resultado.EsValida)
                    throw new InvalidOperationException("ER023: el cifrado se revierte. " + string.Join("; ",resultado.Detalles.Select(d => d.Tabla+"."+d.Columna+": "+d.Motivo).Distinct()));
                tx.Complete();
            }
        }
    }
}
