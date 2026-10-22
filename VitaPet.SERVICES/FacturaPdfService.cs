using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using VitaPet.BE;

namespace VitaPet.SERVICES
{
    // PDF de una página, sin dependencias de escritorio ni archivos temporales.
    // Los datos proceden de la factura registrada, nunca del formulario o del plan actual.
    public sealed class FacturaPdfService
    {
        private readonly StringBuilder contenido=new StringBuilder();
        private static readonly Encoding Codificacion=Encoding.GetEncoding(1252);
        private static string N(decimal valor){return valor.ToString("N2",CultureInfo.GetCultureInfo("en-US"));}
        private static string Escapar(string valor)
        {return (valor??"").Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)").Replace("\r"," ").Replace("\n"," ");}
        private void Texto(int x,int y,string texto,int tamano=10,bool negrita=false,string color="0.12 0.19 0.18")
        {
            contenido.AppendFormat(CultureInfo.InvariantCulture,"BT /{0} {1} Tf {2} rg 1 0 0 1 {3} {4} Tm ({5}) Tj ET\n",negrita?"F2":"F1",tamano,color,x,y,Escapar(texto));
        }
        private void Caja(int x,int y,int ancho,int alto,string color)
        {contenido.AppendFormat(CultureInfo.InvariantCulture,"{0} rg {1} {2} {3} {4} re f\n",color,x,y,ancho,alto);}
        private void Linea(int y){contenido.AppendFormat(CultureInfo.InvariantCulture,"0.83 0.88 0.87 RG 0.6 w 42 {0} m 553 {0} l S\n",y);}
        private void Parrafo(int x,int y,string texto,int maximo,int tamano=10)
        {
            string linea="";
            var palabras=(texto??"").Split(' ').SelectMany(p=>Enumerable.Range(0,(p.Length+maximo-1)/maximo).Select(i=>p.Substring(i*maximo,Math.Min(maximo,p.Length-i*maximo))));
            foreach(string palabra in palabras)
            {
                if(linea.Length>0&&linea.Length+palabra.Length+1>maximo){Texto(x,y,linea,tamano);y-=15;linea="";}
                linea+=(linea.Length==0?"":" ")+palabra;
            }
            if(linea.Length>0)Texto(x,y,linea,tamano);
        }
        public byte[] Generar(FacturaBE f)
        {
            if(f==null||f.IdFactura<=0)throw new ArgumentException("Factura ausente.");
            contenido.Clear();Caja(0,746,595,96,"0.055 0.35 0.32");
            Texto(42,795,"VitaPet",29,true,"1 1 1");Texto(43,774,"Cuidado y bienestar para tus mascotas",10,false,"0.87 0.97 0.94");
            Texto(388,798,"FACTURA",22,true,"1 1 1");Texto(389,777,"N° "+f.Numero,12,false,"1 1 1");
            Texto(42,717,"ORIGINAL",9,true);Texto(388,717,"Emisión: "+f.FechaEmision.ToString("dd/MM/yyyy"),10);
            Texto(42,686,"DATOS DEL CLIENTE",9,true,"0.05 0.42 0.37");
            for(int i=0;i<f.Cliente.Length;i+=51)Texto(42,665-(i/51)*15,f.Cliente.Substring(i,Math.Min(51,f.Cliente.Length-i)),10);
            for(int i=0;i<f.Correo.Length;i+=67)Texto(42,615-(i/67)*15,f.Correo.Substring(i,Math.Min(67,f.Correo.Length-i)),8);
            Texto(388,686,"Suscripción: "+f.IdSuscripcion,9);
            Linea(590);Texto(42,568,"PERÍODO FACTURADO",9,true,"0.05 0.42 0.37");
            Texto(42,548,f.PeriodoDesde.ToString("dd/MM/yyyy")+" al "+f.PeriodoHasta.ToString("dd/MM/yyyy"),12);
            Texto(388,568,"CONDICIÓN DE PAGO",9,true,"0.05 0.42 0.37");Texto(388,548,"Abonado · "+f.Moneda,12,true);
            Caja(42,500,511,32,"0.90 0.95 0.93");
            Texto(52,512,"Descripción",10,true);Texto(300,512,"Cant.",10,true);Texto(365,512,"P. unitario",10,true);Texto(470,512,"Importe",10,true);
            Parrafo(52,475,f.Concepto,38);Texto(307,475,"1");
            Texto(365,475,N(f.CantidadMascotas>0?f.PrecioMascota:f.Importe));Texto(463,475,N(f.CantidadMascotas>0?f.PrecioMascota:f.Importe));
            Texto(52,426,f.CantidadMascotas>0?"Primera mascota · precio completo":"Importe del pago registrado",9,false,"0.38 0.46 0.44");
            if(f.CantidadMascotas>1)
            {
                Texto(52,397,"Mascotas adicionales · descuento 20 %",10);
                Texto(307,397,(f.CantidadMascotas-1).ToString(CultureInfo.InvariantCulture));
                Texto(365,397,N(f.PrecioMascotaAdicional));Texto(463,397,N(f.PrecioMascotaAdicional*(f.CantidadMascotas-1)));
            }
            Linea(374);Texto(355,348,"Subtotal USD",10);Texto(463,348,N(f.Importe));
            Caja(333,286,220,42,"0.055 0.35 0.32");Texto(347,302,"TOTAL USD",12,true,"1 1 1");Texto(449,302,N(f.Importe),14,true,"1 1 1");
            Texto(42,252,"PAGO REGISTRADO",9,true,"0.05 0.42 0.37");Texto(42,232,f.MedioPago,11);
            Parrafo(42,212,"Titular: "+(f.Titular??"No registrado en el pago original"),78,10);Texto(42,170,"Estado: Aprobado · Factura vinculada al pago N° "+f.IdPago,10);
            Linea(145);Texto(42,122,"Gracias por confiar en VitaPet.",12,true);
            Texto(42,94,"Conservá este documento como detalle de tu suscripción.",9);
            byte[] stream=Codificacion.GetBytes(contenido.ToString());
            var objetos=new List<byte[]> {
                Codificacion.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
                Codificacion.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
                Codificacion.GetBytes("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>"),
                Codificacion.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
                Codificacion.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"),
                Codificacion.GetBytes("<< /Length "+stream.Length+" >>\nstream\n").Concat(stream).Concat(Codificacion.GetBytes("endstream")).ToArray()
            };
            using(var salida=new MemoryStream())
            {
                Action<string> escribir=s=>{byte[] bytes=Codificacion.GetBytes(s);salida.Write(bytes,0,bytes.Length);};
                escribir("%PDF-1.4\n%âãÏÓ\n");var offsets=new List<long>{0};
                for(int i=0;i<objetos.Count;i++){offsets.Add(salida.Position);escribir((i+1)+" 0 obj\n");salida.Write(objetos[i],0,objetos[i].Length);escribir("\nendobj\n");}
                long xref=salida.Position;escribir("xref\n0 "+offsets.Count+"\n0000000000 65535 f \n");
                foreach(long offset in offsets.Skip(1))escribir(offset.ToString("0000000000",CultureInfo.InvariantCulture)+" 00000 n \n");
                escribir("trailer\n<< /Size "+offsets.Count+" /Root 1 0 R >>\nstartxref\n"+xref.ToString(CultureInfo.InvariantCulture)+"\n%%EOF\n");return salida.ToArray();
            }
        }
    }
}
