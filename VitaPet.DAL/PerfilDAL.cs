using System;
using System.Data;
using System.Data.SqlClient;

namespace VitaPet.DAL
{
    public sealed class PerfilDAL
    {
        public DataTable Componentes() { return new IntegridadTablasDAL().Leer("PermisoComponente"); }
        public DataTable Relaciones() { return new IntegridadTablasDAL().Leer("PermisoRelacion"); }
        private object Ejecutar(string sql, params SqlParameter[] parametros)
        {
            if (System.Transactions.Transaction.Current == null) throw new InvalidOperationException("Se requiere una transacción de integridad.");
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand(sql, cn)) { cmd.Parameters.AddRange(parametros); cn.Open(); return cmd.ExecuteScalar(); }
        }
        public int Insertar(string nombre, string pantalla, bool sistema)
        {
            return Convert.ToInt32(Ejecutar("INSERT dbo.PermisoComponente(nombre,tipo,pantalla,sistema,dvh) OUTPUT INSERTED.idComponente VALUES(@nombre,@tipo,@pantalla,@sistema,'')",
                new SqlParameter("@nombre", nombre), new SqlParameter("@tipo", pantalla == null ? "Perfil" : "Pantalla"),
                new SqlParameter("@pantalla", (object)pantalla ?? DBNull.Value), new SqlParameter("@sistema", sistema)));
        }
        public void Renombrar(int id, string nombre) { Ejecutar("UPDATE dbo.PermisoComponente SET nombre=@nombre WHERE idComponente=@id", new SqlParameter("@id", id), new SqlParameter("@nombre", nombre)); }
        public void Vincular(int padre, int hijo) { Ejecutar("INSERT dbo.PermisoRelacion(idPadre,idHijo,dvh) VALUES(@padre,@hijo,'')", new SqlParameter("@padre", padre), new SqlParameter("@hijo", hijo)); }
        public void QuitarHijos(int id) { Ejecutar("DELETE dbo.PermisoRelacion WHERE idPadre=@id", new SqlParameter("@id", id)); }
        public void Eliminar(int id) { QuitarHijos(id); Ejecutar("DELETE dbo.PermisoComponente WHERE idComponente=@id", new SqlParameter("@id", id)); }
    }
}
