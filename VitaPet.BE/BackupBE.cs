using System;
namespace VitaPet.BE
{
    public sealed class BackupBE
    {
        public string Id { get; set; }
        public string BaseDatos { get; set; }
        public DateTime FechaUtc { get; set; }
        public int IdOperador { get; set; }
        public string HashArchivo { get; set; }
        public string Esquema { get; set; }
        public bool Verificado { get; set; }
        public string Descripcion => FechaUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") + " | " + Id + (Verificado ? " | Verificado" : " | No apto");
    }
}
