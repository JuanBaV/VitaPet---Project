using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;
namespace VitaPet.DAL
{
    public sealed class ReferenciaIntegridadDAL
    {
        public string ObtenerFormato()
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("SELECT formato FROM dbo.__FormatoIntegridad WHERE id=1", cn))
            {
                cn.Open();
                return cmd.ExecuteScalar() as string;
            }
        }
        public void GuardarFormato(string formato)
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("UPDATE dbo.__FormatoIntegridad SET formato=@formato WHERE id=1", cn))
            {
                cmd.Parameters.Add("@formato", SqlDbType.VarChar, 20).Value=formato;
                cn.Open();
                if(cmd.ExecuteNonQuery()!=1) throw new InvalidOperationException("Falta el control de formato de integridad.");
            }
        }
        public List<ReferenciaCampoBE> Obtener()
        {
            var result = new List<ReferenciaCampoBE>();
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("dbo.sp_ReferenciaIntegridad_Obtener", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read())
                    result.Add(new ReferenciaCampoBE { Tabla=(string)r["tabla"], Fila=(string)r["fila"], Columna=(string)r["columna"], Huella=(string)r["huella"] });
            }
            return result;
        }
        public void Guardar(ReferenciaCampoBE item)
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("dbo.sp_ReferenciaIntegridad_Guardar", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@tabla", SqlDbType.VarChar,60).Value=item.Tabla;
                cmd.Parameters.Add("@fila", SqlDbType.VarChar,30).Value=item.Fila;
                cmd.Parameters.Add("@columna", SqlDbType.VarChar,60).Value=item.Columna;
                cmd.Parameters.Add("@huella", SqlDbType.Char,64).Value=item.Huella;
                cn.Open(); cmd.ExecuteNonQuery();
            }
        }
        public void EliminarFila(string tabla, string fila)
        {
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand("dbo.sp_ReferenciaIntegridad_EliminarFila", cn))
            {
                cmd.CommandType=CommandType.StoredProcedure;
                cmd.Parameters.Add("@tabla",SqlDbType.VarChar,60).Value=tabla;
                cmd.Parameters.Add("@fila",SqlDbType.VarChar,30).Value=fila;
                cn.Open(); cmd.ExecuteNonQuery();
            }
        }
    }
}
