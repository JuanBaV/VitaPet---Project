using System;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;

namespace VitaPet.DAL
{
    public partial class UsuarioDAL
    {
        // La BLL calcula los verificadores y conserva la transacción de integridad.
        public void ActualizarIdioma(int idUsuario, string nuevoIdioma, string dvh)
        {
            if (Transaction.Current == null)
                throw new InvalidOperationException("La actualización requiere una transacción de integridad.");
            using (var conexion = _conexionDAL.CrearConexion())
            using (var comando = new SqlCommand("dbo.sp_Usuario_ActualizarIdioma", conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add("@idUsuario", SqlDbType.Int).Value = idUsuario;
                comando.Parameters.Add("@idioma", SqlDbType.VarChar, 2).Value = nuevoIdioma;
                comando.Parameters.Add("@dvh", SqlDbType.VarChar, 64).Value = dvh;
                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }
    }
}
