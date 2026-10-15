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
                {"Reintegro", "idReintegro"}, {"ErrorSistema", "codigo"}
            });
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
            var esperadas = new HashSet<string>(Claves.Keys, StringComparer.Ordinal);
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
