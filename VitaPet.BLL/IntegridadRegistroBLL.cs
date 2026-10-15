using System.Data;
using System.Linq;
using VitaPet.BE;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    internal static class IntegridadRegistroBLL
    {
        internal static void Actualizar(string tabla,int id)
        {
            var dal=new IntegridadTablasDAL();var t=dal.Leer(tabla);
            var r=t.Rows.Cast<DataRow>().Single(f=>(int)f[IntegridadTablasDAL.Claves[tabla]]==id);
            string dvh=new IntegridadTablasBLL().CalcularDVH(r);dal.GuardarDvh(tabla,id,dvh);r["dvh"]=dvh;
            new DVVDAL().Guardar(new DVVBE{NombreTabla=tabla,
                ValorDVV=new IntegridadBLL().CalcularDVV(t.Rows.Cast<DataRow>().Select(f=>(string)f["dvh"]))});
            new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
        }
    }
}
