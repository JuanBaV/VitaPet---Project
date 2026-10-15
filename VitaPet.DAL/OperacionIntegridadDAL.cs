using System;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
namespace VitaPet.DAL
{
    // Serializa lectores y escritores de controles antes de leer los DV.
    // Las conexiones secuenciales se enlistan en la misma transacción ambiente.
    public sealed class OperacionIntegridadDAL : IDisposable
    {
        private readonly TransactionScope scope;
        public OperacionIntegridadDAL()
        {
            scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel=System.Transactions.IsolationLevel.Serializable, Timeout=TimeSpan.FromMinutes(3) });
            try
            {
                using (var cn = new ConexionDAL().CrearConexion())
                using (var cmd = cn.CreateCommand())
                {
                    cn.Open();
                    cmd.CommandText="DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'VitaPet.Integridad', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=30000; SELECT @r;";
                    if (Convert.ToInt32(cmd.ExecuteScalar()) < 0) throw new InvalidOperationException("No se pudo bloquear la operación de integridad.");
                }
            }
            catch { scope.Dispose(); throw; }
        }
        public void Complete() { scope.Complete(); }
        public void Dispose() { scope.Dispose(); }
    }
}
