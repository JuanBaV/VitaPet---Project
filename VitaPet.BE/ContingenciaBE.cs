using System;
using System.Collections.Generic;
namespace VitaPet.BE
{
    public sealed class CuentaContingenciaBE
    {
        public int VersionFormato { get; set; }
        public string Id { get; set; }
        public string Correo { get; set; }
        public string Sal { get; set; }
        public string Hash { get; set; }
        public string ClaveFirma { get; set; }
        public int Intentos { get; set; }
        public DateTime BloqueadaHastaUtc { get; set; }
    }
    public sealed class RespaldoContingenciaBE
    {
        public string Id { get; set; }
        public string CuentaId { get; set; }
        public string BaseDatos { get; set; }
        public string Servidor { get; set; }
        public DateTime FechaUtc { get; set; }
        public string Esquema { get; set; }
        public string DatosXml { get; set; }
        public string HistoricoXml { get; set; }
        public string Firma { get; set; }
        public string Descripcion => FechaUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") + " - " + Id;
    }
}
