using System;
using System.IO;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            var seguridad = new SecurityManager();
            seguridad.ExigirIntegridad();
            if (DatabaseMigrator.EstaAplicada(CifradoDatosDAL.Migracion))
            {
                Console.WriteLine("La base ya está cifrada y su integridad es válida."); return 0;
            }
            if (args.Length != 2 || args[0] != "--aplicar")
                throw new ArgumentException("Uso: VitaPet.Cifrado.exe --aplicar <ruta-absoluta-del-backup-previo.bak>");
            string respaldo = args[1];
            if (!Path.IsPathRooted(respaldo) || File.Exists(respaldo))
                throw new ArgumentException("El respaldo debe ser una ruta absoluta nueva; no se sobrescriben archivos.");
            var backup = new BackupDAL();
            Console.WriteLine("Creando y restaurando una copia de seguridad previa de " + backup.BaseDatos + "...");
            backup.Crear(respaldo);
            backup.VerificarArchivo(respaldo);
            string temporal = backup.CrearTemporal(respaldo);
            try
            {
                using (ConexionDAL.UsarBaseTemporal(temporal)) seguridad.ExigirIntegridad();
            }
            finally { backup.EliminarTemporal(temporal); }
            Console.WriteLine("Respaldo restaurable e íntegro. Aplicando cifrado dentro de una transacción...");
            new MigracionCifradoBLL().Ejecutar();
            seguridad.ExigirIntegridad();
            Console.WriteLine("Cifrado aplicado. Datos lógicos, DVH, DVV y referencias conservados.");
            Console.WriteLine("Conserve el archivo de clave y proteja el backup previo: contiene los datos anteriores al cifrado.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("No se completó la operación: " + ex.Message);
            return 1;
        }
    }
}
