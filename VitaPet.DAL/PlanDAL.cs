using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public sealed class PlanDAL
    {
        private DataTable Leer(string sp, params SqlParameter[] parametros)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand(sp,cn))
            using(var adapter=new SqlDataAdapter(cmd))
            {
                cmd.CommandType=CommandType.StoredProcedure;cmd.Parameters.AddRange(parametros);
                var datos=new DataTable();adapter.Fill(datos);return datos;
            }
        }
        private static SqlParameter Id(string nombre,int valor) { return new SqlParameter(nombre,SqlDbType.Int){Value=valor}; }
        private static PlanBE Plan(DataRow r) { return new PlanBE { IdPlan=(int)r["idPlan"],Nombre=(string)r["nombre"],Descripcion=(string)r["descripcion"],Precio=(decimal)r["precio"],Version=(string)r["dvh"] }; }
        public List<PlanBE> Listar() { return Leer("dbo.sp_Plan_ListarContratables").Rows.Cast<DataRow>().Select(Plan).ToList(); }
        public PlanBE Obtener(int id) { var t=Leer("dbo.sp_Plan_ObtenerContratable",Id("@idPlan",id));return t.Rows.Count==0?null:Plan(t.Rows[0]); }
        public CotizacionPlanBE Cotizar(int usuario,int plan)
        {
            var t=Leer("dbo.sp_Cliente_CotizarPlan",Id("@idUsuario",usuario),Id("@idPlan",plan));
            return t.Rows.Count==0?null:new CotizacionPlanBE { Plan=Plan(t.Rows[0]),CantidadMascotas=(int)t.Rows[0]["cantidadMascotas"] };
        }
        public Tuple<int,bool> Preparar(int usuario,int plan,string version,int cantidad)
        {
            var t=Leer("dbo.sp_Suscripcion_PrepararContratacion",Id("@idUsuario",usuario),Id("@idPlan",plan),
                new SqlParameter("@versionPlan",SqlDbType.VarChar,64){Value=version},Id("@cantidadEsperada",cantidad));
            return Tuple.Create((int)t.Rows[0]["id"],(bool)t.Rows[0]["nuevo"]);
        }
        public int? PlanPendiente(int usuario,int suscripcion)
        {
            var t=Leer("dbo.sp_Suscripcion_ObtenerPendiente",Id("@idUsuario",usuario),Id("@idSuscripcion",suscripcion));
            return t.Rows.Count==0?(int?)null:(int)t.Rows[0]["idPlan"];
        }
        public List<CoberturaBE> Coberturas(int id)
        { return Leer("dbo.sp_Cobertura_ObtenerPorPlan",Id("@idPlan",id)).Rows.Cast<DataRow>().Select(r=>new CoberturaBE{Prestacion=(string)r["prestacion"],Detalle=(string)r["detalle"]}).ToList(); }
        public Tuple<int,bool> AsegurarPlan(string nombre,string descripcion,decimal? precio)
        {
            var t=Leer("dbo.sp_Plan_AsegurarCatalogo",new SqlParameter("@nombre",SqlDbType.VarChar,80){Value=nombre},
                new SqlParameter("@descripcion",SqlDbType.VarChar,255){Value=descripcion},new SqlParameter("@precio",SqlDbType.Decimal){Precision=10,Scale=2,Value=(object)precio??DBNull.Value});
            return Tuple.Create((int)t.Rows[0]["id"],(bool)t.Rows[0]["nuevo"]);
        }
        public Tuple<int,bool> AsegurarCobertura(int plan,string prestacion,string detalle)
        {
            var t=Leer("dbo.sp_Cobertura_AsegurarCatalogo",Id("@idPlan",plan),new SqlParameter("@prestacion",SqlDbType.VarChar,100){Value=prestacion},new SqlParameter("@detalle",SqlDbType.VarChar,255){Value=detalle});
            return Tuple.Create((int)t.Rows[0]["id"],(bool)t.Rows[0]["nuevo"]);
        }
    }
}
