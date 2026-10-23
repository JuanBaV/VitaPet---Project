using System;
using System.Data;
using System.Data.SqlClient;

namespace VitaPet.DAL
{
    public sealed class PerfilDAL
    {
        public DataTable Componentes() { return new IntegridadTablasDAL().Leer("PermisoComponente"); }
        public DataTable Relaciones() { return new IntegridadTablasDAL().Leer("PermisoRelacion"); }
        public DataTable Roles() { return new IntegridadTablasDAL().Leer("Rol"); }
        public DataTable PermisosRoles() { return new IntegridadTablasDAL().Leer("RolPermiso"); }
        public bool UsaRoles()
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand("SELECT CASE WHEN COL_LENGTH('dbo.Usuario','idRol') IS NULL THEN 0 ELSE 1 END",cn))
            { cn.Open(); return (int)cmd.ExecuteScalar()==1; }
        }
        private object Ejecutar(string sql, params SqlParameter[] parametros)
        {
            if (System.Transactions.Transaction.Current == null) throw new InvalidOperationException("Se requiere una transacción de integridad.");
            using (var cn = new ConexionDAL().CrearConexion())
            using (var cmd = new SqlCommand(sql, cn)) { cmd.Parameters.AddRange(parametros); cn.Open(); return cmd.ExecuteScalar(); }
        }
        public int Insertar(string nombre, string pantalla, bool sistema)
        {
            int id = Convert.ToInt32(Ejecutar("INSERT dbo.PermisoComponente(nombre,tipo,pantalla,sistema,dvh) OUTPUT INSERTED.idComponente VALUES(@nombre,@tipo,@pantalla,@sistema,'')",
                new SqlParameter("@nombre", nombre), new SqlParameter("@tipo", pantalla == null ? "Perfil" : "Pantalla"),
                new SqlParameter("@pantalla", (object)pantalla ?? DBNull.Value), new SqlParameter("@sistema", sistema)));
            if (pantalla==null) Ejecutar("INSERT dbo.Rol(idRol,dvh) VALUES(@id,'')",new SqlParameter("@id",id));
            return id;
        }
        public void Renombrar(int id, string nombre) { Ejecutar("UPDATE dbo.PermisoComponente SET nombre=@nombre WHERE idComponente=@id", new SqlParameter("@id", id), new SqlParameter("@nombre", nombre)); }
        public void Vincular(int padre, int hijo) { Ejecutar("IF EXISTS(SELECT 1 FROM dbo.Rol WHERE idRol=@padre) INSERT dbo.RolPermiso(idRol,idComponente,dvh) VALUES(@padre,@hijo,''); ELSE INSERT dbo.PermisoRelacion(idPadre,idHijo,dvh) VALUES(@padre,@hijo,'');", new SqlParameter("@padre", padre), new SqlParameter("@hijo", hijo)); }
        public void QuitarHijos(int id) { Ejecutar("DELETE dbo.RolPermiso WHERE idRol=@id; DELETE dbo.PermisoRelacion WHERE idPadre=@id;", new SqlParameter("@id", id)); }
        public void Eliminar(int id) { QuitarHijos(id); Ejecutar("DELETE dbo.Rol WHERE idRol=@id; DELETE dbo.PermisoComponente WHERE idComponente=@id;", new SqlParameter("@id", id)); }
    }
}
