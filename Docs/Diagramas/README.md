# Diagramas de Klein de VitaPet

`Klein-VitaPet.svg` representa el circuito de contratación de un plan, pago y
facturación. `Klein-VitaPet-Capas.svg` ubica las actividades del mismo circuito
en las capas que las ejecutan. Los PNG se incorporan a la documentación Word;
los SVG conservan texto y trazos vectoriales editables.

Las dos láminas del Word usan A4 vertical. El diagrama de negocio conserva sus
carriles de Cliente, VitaPet, Servicio de pago y Correo. El servicio de pago
representa el simulador local existente.

El diagrama por capas sigue el formato de las fotografías aprobadas: marco y
columnas cerradas, encabezado oscuro, actividades rectangulares, cilindros de
base de datos y símbolo de documento para la factura. Las columnas son
Presentación, Negocio, Acceso a Datos, Servicios y Base de datos. Seguridad y BE
se indican como apoyo transversal. Las actividades no incluyen clases ni métodos.

## Correspondencia con el código

| Operación | Componentes revisados |
| --- | --- |
| Cotización y contratación pendiente | `ContratarPlan.aspx.cs`, `ContratacionPlanBLL.PrepararContratacion`, `PlanDAL.Preparar` |
| Permisos e integridad | `SecurityManager`, `OperacionIntegridadDAL` |
| Validación y autorización simulada | `GestionPago.aspx.cs`, `GestionPagoBLL.Pagar`, `ProcesarPrueba` |
| Registro del pago | `PagoDAL.Registrar`, `sp_Pago_Registrar`, tablas Pago y DetallePago |
| Emisión del documento | `FacturaBLL.Emitir`, `FacturaDAL.Emitir/Obtener`, tabla Factura |
| Entidades intercambiadas | `CotizacionPlanBE`, `DatosPagoBE`, `PagoBE`, `FacturaBE` |
| PDF y correo | `FacturaPdfService.Generar`, `CorreoFacturaService.Enviar`, SMTP |
| Certificado de autorización local | `CertificadoPagoService`, archivos CER y P7M |
| Descarga propia | `DescargarFactura_Click`, `FacturaBLL.Descargar`, `FacturaDAL.Obtener` |
| Activación después de Continuar | `Continuar_Click`, `ContratacionPlanBLL.ConfirmarPago`, `PagoDAL.Activar` |

La factura PDF es el documento comercial de este circuito. La contratación
corresponde a un servicio. El PDF se genera en SERVICES a partir
de FacturaBE; DAL persiste los datos, BLL coordina la emisión y GUI entrega el
archivo al cliente. El correo adjunta ese mismo documento.

Las flechas representan el avance del proceso, incluidas las respuestas entre
capas; no son un inventario de dependencias directas entre clases. BE transporta
datos y no coordina llamadas. La descarga es opcional. Un fallo de correo permite
reintentar sin registrar otro pago. La activación se realiza al pulsar Continuar.

## Regeneración

Ejecutar `generar_klein.py` con Python y ReportLab para reconstruir los SVG y su
manifiesto. Los PNG se rasterizan desde los SVG con Sharp a densidad 220. Estas
herramientas se usan para la documentación y no son dependencias de VitaPet.

La generación comprueba que cada identificador sea único y que los textos
mantengan el tamaño mínimo establecido. La revisión final debe incluir las
flechas, los rótulos y su presentación dentro de Word.
