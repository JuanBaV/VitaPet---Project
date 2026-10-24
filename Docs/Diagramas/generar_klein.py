"""Diagramas del circuito vigente de contratación, pago y facturación."""
from pathlib import Path
from html import escape
from math import atan2, cos, sin
import json
from reportlab.pdfbase.pdfmetrics import stringWidth

ROOT = Path(__file__).parent
W, H = 1050, 1150


class Diagrama:
    def __init__(self, nombre, columnas, titulo):
        self.nombre, self.columnas = nombre, columnas
        self.ancho = (W - 32) / len(columnas)
        self.nodes, self.edges = {}, []
        self.partes = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
                       '<rect width="100%" height="100%" fill="white"/>',
                       f'<title>{escape(titulo)}</title>',
                       '<g font-family="Arial, sans-serif" fill="#171717" stroke-linejoin="round">']
        self.texto(W/2, 23, titulo, 23, True)
        for i, label in enumerate(columnas):
            x = 16 + i * self.ancho
            self.partes.append(f'<path d="M{x},42 V{H-40}" fill="none" stroke="#666" stroke-width="1.2"/>')
            self.texto(x+self.ancho/2, 59, label, 20, True)
        self.partes.append(f'<path d="M{W-16},42 V{H-40} M16,72 H{W-16}" fill="none" stroke="#666" stroke-width="1.2"/>')

    def texto(self, x, y, value, size=18, bold=False):
        self.partes.append(f'<text x="{x}" y="{y}" font-size="{size}" font-weight="{"bold" if bold else "normal"}" text-anchor="middle">{escape(value)}</text>')

    def nodo(self, key, col, row, label, tipo='accion', width=None, size=21):
        assert key not in self.nodes
        x, y = 16 + (col+.5)*self.ancho, 114 + row*54
        width = width or self.ancho-44
        lines = label.split('\n')
        height = 34 if len(lines) == 1 else 40
        if tipo == 'decision': height = 48
        if tipo == 'documento': height = 46
        if tipo == 'datos': height = 44
        self.nodes[key] = dict(x=x, y=y, w=width, h=height, label=label, tipo=tipo)
        if tipo == 'decision':
            self.partes.append(f'<path d="M{x-width/2},{y} L{x},{y-height/2} L{x+width/2},{y} L{x},{y+height/2} Z" fill="#f5f5f5" stroke="#333" stroke-width="1.4"/>')
        elif tipo == 'documento':
            l, r, t, b = x-width/2, x+width/2, y-height/2, y+height/2
            self.partes.append(f'<path d="M{l},{t} H{r} V{b-4} C{x+width/4},{b-12} {x-width/4},{b+4} {l},{b-4} Z" fill="#fafafa" stroke="#333" stroke-width="1.4"/>')
        elif tipo == 'datos':
            l, r, t, b = x-width/2, x+width/2, y-height/2, y+height/2
            self.partes.append(f'<path d="M{l},{t+4} C{l},{t-2} {r},{t-2} {r},{t+4} V{b-4} C{r},{b+2} {l},{b+2} {l},{b-4} Z M{l},{t+4} C{l},{t+10} {r},{t+10} {r},{t+4}" fill="white" stroke="#333" stroke-width="1.4"/>')
        else:
            self.partes.append(f'<rect x="{x-width/2}" y="{y-height/2}" width="{width}" height="{height}" rx="8" fill="#f5f5f5" stroke="#333" stroke-width="1.4"/>')
        font = min(size, (width-16)/max(stringWidth(line, 'Helvetica', 1) for line in lines))
        assert font >= 18, (key, font)
        for i, line in enumerate(lines):
            dy = 10 if tipo=='datos' else 3 if tipo=='documento' else 5
            self.texto(x, y+(i-(len(lines)-1)/2)*17+dy, line, round(font,1))
        return key

    def punto(self, key, col, row, final=False):
        x, y = 16+(col+.5)*self.ancho, 114+row*54
        self.nodes[key] = dict(x=x,y=y,w=14,h=14,label='',tipo='fin' if final else 'inicio')
        if final: self.partes.append(f'<circle cx="{x}" cy="{y}" r="10" fill="white" stroke="#222" stroke-width="1.4"/>')
        self.partes.append(f'<circle cx="{x}" cy="{y}" r="6" fill="#222"/>')

    def conectar(self, a, b, label='', via=None, dashed=False, salida=None, entrada=None):
        n, m = self.nodes[a], self.nodes[b]
        if n['y'] == m['y']:
            side = 1 if m['x'] > n['x'] else -1
            start, end = (n['x']+side*n['w']/2,n['y']), (m['x']-side*m['w']/2,m['y'])
        else:
            start, end = (n['x'],n['y']+n['h']/2), (m['x'],m['y']-m['h']/2)
        if salida: start = (n['x']+salida*n['w']/2,n['y'])
        if entrada: end = (m['x']+entrada*m['w']/2,m['y'])
        if via is None:
            mid = (start[1]+end[1])/2
            points = [start,(start[0],mid),(end[0],mid),end] if start[1]!=end[1] else [start,end]
        else: points = [start]+via+[end]
        points = [p for i,p in enumerate(points) if i==0 or p!=points[i-1]]
        self.edges.append((a,b,points,label,dashed))

    def guardar(self, nota):
        # Las conexiones se dibujan antes de los nodos para conservar los textos legibles.
        arrows = []
        for a,b,points,label,dashed in self.edges:
            path='M'+' L'.join(f'{x},{y}' for x,y in points)
            arrows.append(f'<path d="{path}" fill="none" stroke="#333" stroke-width="1.5"'+(' stroke-dasharray="6 4"' if dashed else '')+'/>')
            x,y=points[-1]; px,py=points[-2]; ang=atan2(y-py,x-px)
            triangle=[(x,y),(x-7*cos(ang-.45),y-7*sin(ang-.45)),(x-7*cos(ang+.45),y-7*sin(ang+.45))]
            arrows.append('<polygon points="'+' '.join(f'{x},{y}' for x,y in triangle)+'" fill="#333"/>')
            if label:
                x,y=points[0]
                dx,dy=(-27,-8) if points[1][0]<x else (12,17)
                if label=='Datos inválidos':dx=-120;dy=-8
                arrows.append(f'<text x="{x+dx}" y="{y+dy}" font-family="Arial" font-size="14" fill="#222">{escape(label)}</text>')
        self.partes[3:3] = arrows
        self.texto(W/2, H-17, nota, 18)
        self.partes.append('</g></svg>')
        (ROOT/(self.nombre+'.svg')).write_text('\n'.join(self.partes),encoding='utf-8')
        return {'nombre':self.nombre,'nodos':self.nodes,'conexiones':[(a,b) for a,b,*_ in self.edges]}


def negocio():
    d=Diagrama('Klein-VitaPet',['Cliente','VitaPet','Pago simulado','Correo'],
               'Diagrama de Klein - Contratación, pago y factura')
    d.punto('inicio',0,-.6)
    items=[('plan',0,0,'Seleccionar plan'),('cotizar',1,1,'Mostrar cobertura\ny precio por mascota'),
           ('confirmar',0,2,'Confirmar plan'),('pendiente',1,3,'Registrar suscripción\npendiente de pago'),
           ('datos',0,4,'Elegir medio de pago\nConfirmar los datos'),('validar',1,5,'Validar datos\ne importe vigente'),
           ('autorizar',2,6,'Autorizar pago\n(simulador local)'),('pago',1,8,'Registrar pago\ny detalle'),
           ('factura',1,9,'Emitir factura\nnumerada'),('pdf',1,10,'Generar PDF\nde la factura'),
           ('correo',3,11,'Enviar factura\nPDF adjunta'),('resultado',1,12,'Informar estado\ny permitir reintento'),
           ('certificar',2,13,'Firmar autorización\nCER / P7M'),('comprobante',1,14,'Mostrar comprobante\ny estado del correo'),
           ('documento',0,15,'Factura PDF\nGuardar / imprimir'),('continuar',0,16,'Pulsar Continuar'),
           ('activar',1,17,'Activar suscripción')]
    for key,c,r,t in items:d.nodo(key,c,r,t,'documento' if key=='documento' else 'accion',size=21)
    d.nodo('aprobado',2,7,'¿Aprobado?','decision',size=20)
    d.nodo('rechazo',0,7,'Informar error\nCorregir y reintentar',size=21)
    d.punto('fin',1,18,True)
    seq=['inicio','plan','cotizar','confirmar','pendiente','datos','validar','autorizar','aprobado','pago','factura','pdf','correo','resultado','certificar','comprobante']
    for a,b in zip(seq,seq[1:]):d.conectar(a,b,'Sí' if a=='aprobado' else '')
    d.conectar('aprobado','rechazo','No')
    d.conectar('rechazo','datos',via=[(34,d.nodes['rechazo']['y']),(34,d.nodes['datos']['y'])],salida=-1,entrada=-1,dashed=True)
    d.conectar('validar','rechazo','Datos inválidos',via=[(d.nodes['validar']['x']-120,d.nodes['validar']['y']),(d.nodes['rechazo']['x'],d.nodes['validar']['y'])],salida=-1)
    d.conectar('comprobante','documento','Opcional',dashed=True)
    track=d.nodes['comprobante']['x']+d.nodes['comprobante']['w']/2+16
    d.conectar('comprobante','continuar',via=[(track,d.nodes['comprobante']['y']),(track,d.nodes['continuar']['y']-26),(d.nodes['continuar']['x'],d.nodes['continuar']['y']-26)],salida=1)
    d.conectar('continuar','activar');d.conectar('activar','fin')
    return d.guardar('La descarga es opcional. El correo se reintenta sin volver a cobrar.')


def capas():
    d=Diagrama('Klein-VitaPet-Capas',['GUI','SEGURIDAD','BLL','DAL','SERVICIOS'],
               'Diagrama de Klein por capas - Contratación y factura')
    nodes=[('gui',0,0,'Confirmar\ncontratación'),('seg',1,1,'Validar acceso\ne integridad'),
           ('preparar',2,2,'Validar plan\ny cotización'),('pendiente',3,3,'Suscripción\npendiente'),
           ('form',0,4,'Confirmar pago\nDébito o crédito'),('pagar',2,5,'Validar datos\ny autorizar pago'),
           ('rechazo',0,6,'Informar error\ny reintentar'),('pago',3,7,'Guardar pago\ny detalle'),
           ('emitir',2,8,'Emitir factura\nVerificar datos'),('factura',3,9,'Guardar y leer\ndatos de factura'),
           ('mail',4,10,'Generar PDF\nEnviar correo'),('cert',4,11,'Firmar pago\nCER / P7M'),
           ('res',0,12,'Ver comprobante\nEstado del correo'),('download',2,13,'Consultar factura\npropia'),
           ('obtener',3,13,'Leer datos\nde la factura'),('pdf',4,14,'Generar PDF\nde la factura'),
           ('doc',0,15,'Factura PDF\nDescarga'),('continuar',0,16,'Pulsar Continuar'),
           ('activar',2,16,'Confirmar pago\nde suscripción'),('dalactivar',3,17,'Activar\nsuscripción'),
           ('fin',0,18,'Mostrar plan\nactivo')]
    for key,c,r,label in nodes:d.nodo(key,c,r,label,'documento' if key=='doc' else 'accion',size=21)
    d.nodo('ok',2,6,'¿Aprobado?','decision',size=18)
    seq=['gui','seg','preparar','pendiente','form','pagar','ok','pago','emitir','factura','mail','cert','res']
    for a,b in zip(seq,seq[1:]):d.conectar(a,b,'Sí' if a=='ok' else '')
    d.conectar('ok','rechazo','No')
    d.conectar('rechazo','form',via=[(28,d.nodes['rechazo']['y']),(28,d.nodes['form']['y'])],salida=-1,entrada=-1,dashed=True)
    for a,b in [('res','download'),('download','obtener'),('obtener','pdf'),('pdf','doc')]:d.conectar(a,b,'Opcional' if a=='res' else '',dashed=True)
    track=d.nodes['res']['x']+d.nodes['res']['w']/2+12
    d.conectar('res','continuar',via=[(track,d.nodes['res']['y']),(track,d.nodes['continuar']['y']-26),(d.nodes['continuar']['x'],d.nodes['continuar']['y']-26)],salida=1)
    for a,b in [('continuar','activar'),('activar','dalactivar'),('dalactivar','fin')]:d.conectar(a,b)
    return d.guardar('BE transporta los datos entre capas. Persistencia: SQL Server. Correo: SMTP.')


if __name__=='__main__':
    result=[negocio(),capas()]
    (ROOT/'Klein-manifest.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Generados los dos diagramas SVG.')
