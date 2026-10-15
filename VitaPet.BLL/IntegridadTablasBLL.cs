using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using VitaPet.BE;
using VitaPet.DAL;
namespace VitaPet.BLL
{
    public sealed class IntegridadTablasBLL
    {
        private readonly IntegridadTablasDAL dal = new IntegridadTablasDAL();
        private readonly IntegridadBLL hash = new IntegridadBLL();
        internal static string Valor(object valor)
        {
            if (valor == null || valor == DBNull.Value) return null;
            if (valor is bool) return (bool)valor ? "1" : "0";
            if (valor is DateTime) return ((DateTime)valor).ToString("O", CultureInfo.InvariantCulture);
            if (valor is decimal) return ((decimal)valor).ToString("G29", CultureInfo.InvariantCulture);
            return Convert.ToString(valor, CultureInfo.InvariantCulture);
        }
        internal static string Pack(string valor) => valor == null ? "-1:" : valor.Length.ToString(CultureInfo.InvariantCulture) + ":" + valor;
        public string CalcularDVH(DataRow fila)
        {
            return hash.CalcularHash(string.Concat(fila.Table.Columns.Cast<DataColumn>()
                .Where(c => c.ColumnName != "dvh").Select(c => Pack(Valor(fila[c])))));
        }
        internal void Inicializar()
        {
            dal.ValidarCobertura();
            foreach (var tabla in IntegridadTablasDAL.Claves.Keys)
            {
                var datos = dal.Leer(tabla); var valores = new List<string>();
                foreach (DataRow fila in datos.Rows)
                {
                    string dvh = CalcularDVH(fila); valores.Add(dvh);
                    dal.GuardarDvh(tabla, fila[IntegridadTablasDAL.Claves[tabla]], dvh);
                }
                new DVVDAL().Guardar(new DVVBE { NombreTabla=tabla, ValorDVV=hash.CalcularDVV(valores) });
            }
        }
        internal void Verificar(ResultadoIntegridadBE resultado)
        {
            try { dal.ValidarCobertura(); }
            catch (InvalidOperationException ex) { ReferenciaIntegridadBLL.Error(resultado,"Esquema","—","tabla",ex.Message); return; }
            foreach (var tabla in IntegridadTablasDAL.Claves.Keys)
            {
                var datos=dal.Leer(tabla); var valores=new List<string>();
                foreach (DataRow fila in datos.Rows)
                {
                    string guardado=Convert.ToString(fila["dvh"],CultureInfo.InvariantCulture); valores.Add(guardado);
                    if (!string.Equals(CalcularDVH(fila),guardado,StringComparison.OrdinalIgnoreCase))
                        ReferenciaIntegridadBLL.Error(resultado,tabla,Valor(fila[IntegridadTablasDAL.Claves[tabla]]),"dvh","DVH no coincide.");
                }
                var dvv=new DVVDAL().ObtenerPorTabla(tabla);
                if(dvv==null || !hash.VerificarDVV(valores,dvv.ValorDVV))
                    ReferenciaIntegridadBLL.Error(resultado,"DVV","nombreTabla="+tabla,"valorDVV","DVV ausente o inconsistente.");
            }
        }
        internal List<ReferenciaCampoBE> CamposActuales()
        {
            var result=new List<ReferenciaCampoBE>();
            foreach(var tabla in IntegridadTablasDAL.Claves.Keys)
            {
                var datos=dal.Leer(tabla);
                foreach(DataRow fila in datos.Rows)
                foreach(DataColumn col in datos.Columns)
                {
                    string id=Valor(fila[IntegridadTablasDAL.Claves[tabla]]);
                    string entrada=Pack("campo-v1")+Pack(tabla)+Pack(id)+Pack(col.ColumnName)+Pack(Valor(fila[col]));
                    result.Add(new ReferenciaCampoBE { Tabla=tabla,Fila=id,Columna=col.ColumnName,Huella=hash.CalcularHash(entrada) });
                }
            }
            return result;
        }
    }
}
