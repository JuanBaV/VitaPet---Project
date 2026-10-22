using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
namespace VitaPet.DAL
{
    public sealed class IntegridadTablasDAL
    {
        public static readonly IReadOnlyDictionary<string, string> Claves =
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(new Dictionary<string, string> {
                {"Empresa", "idEmpresa"}, {"Cliente", "idCliente"}, {"Plan", "idPlan"},
                {"Cobertura", "idCobertura"}, {"Suscripcion", "idSuscripcion"}, {"Pago", "idPago"},
                {"Mascota", "idMascota"}, {"Turno", "idTurno"}, {"HistorialClinico", "idHistorial"},
                {"Reintegro", "idReintegro"}, {"ErrorSistema", "codigo"}, {"DatosClinicos", "idDatoClinico"}, {"AgendaTurno", "idAgendaTurno"}, {"DetallePago", "idPago"}, {"Factura", "idFactura"}
            });
        public IReadOnlyDictionary<string,string> ClavesEsquema
        {
            get
            {
                // Únicamente un respaldo temporal anterior a la migración puede omitir su tabla nueva y vacía.
                var claves=Claves.ToDictionary(k=>k.Key,k=>k.Value);
                if(ConexionDAL.EnBaseTemporal)
                foreach(var ampliacion in new[]{new[]{"DatosClinicos","021_GestionMascotas.sql"},new[]{"AgendaTurno","022_Turnos.sql"},new[]{"DetallePago","027_Facturacion.sql"},new[]{"Factura","027_Facturacion.sql"}})
                {
                    using(var cn=new ConexionDAL().CrearConexion())
                    using(var cmd=new SqlCommand("SELECT CASE WHEN OBJECT_ID(@tabla,'U') IS NULL AND NOT EXISTS(SELECT 1 FROM dbo.__SchemaMigrations WHERE migrationId=@migracion) AND NOT EXISTS(SELECT 1 FROM dbo.DVV WHERE nombreTabla=@nombre) THEN 1 ELSE 0 END",cn))
                    {
                        cmd.Parameters.AddWithValue("@tabla","dbo."+ampliacion[0]);cmd.Parameters.AddWithValue("@migracion",ampliacion[1]);cmd.Parameters.AddWithValue("@nombre",ampliacion[0]);
                        cn.Open();if((int)cmd.ExecuteScalar()==1)claves.Remove(ampliacion[0]);
                    }
                }
                return new System.Collections.ObjectModel.ReadOnlyDictionary<string,string>(claves);
            }
        }
        public DataTable Leer(string tabla)
        {
            if (!Claves.ContainsKey(tabla)) throw new ArgumentException("Tabla no supervisada.");
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("SELECT * FROM dbo.[" + tabla + "] ORDER BY [" + Claves[tabla] + "]", cn))
            using (var a = new SqlDataAdapter(cmd))
            { var datos = new DataTable(tabla); a.Fill(datos); return datos; }
        }
        public void GuardarDvh(string tabla, object id, string dvh)
        {
            if (!Claves.ContainsKey(tabla)) throw new ArgumentException("Tabla no supervisada.");
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("UPDATE dbo.[" + tabla + "] SET dvh=@dvh WHERE [" + Claves[tabla] + "]=@id", cn))
            {
                cmd.Parameters.Add("@dvh", SqlDbType.VarChar, 64).Value = dvh;
                if (tabla == "ErrorSistema") cmd.Parameters.Add("@id", SqlDbType.VarChar, 10).Value = id;
                else cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
                cn.Open(); if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Registro ausente.");
            }
        }
        public void ValidarCobertura()
        {
            var esperadas = new HashSet<string>(ClavesEsquema.Keys, StringComparer.Ordinal);
            esperadas.UnionWith(new[] {"Usuario", "Bitacora", "DVV", "ReferenciaIntegridad", "__FormatoIntegridad", "__SchemaMigrations"});
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo')", cn))
            {
                cn.Open(); using (var r = cmd.ExecuteReader()) while (r.Read())
                    if (!esperadas.Remove(r.GetString(0))) throw new InvalidOperationException("Tabla sin cobertura de integridad.");
            }
            if (esperadas.Count != 0) throw new InvalidOperationException("Faltan tablas supervisadas.");
        }
    }
}
