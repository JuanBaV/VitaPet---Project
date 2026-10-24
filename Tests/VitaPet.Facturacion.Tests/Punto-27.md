# Punto 27: diagramas de Klein

El alcance incluye el proceso de contratación, pago y factura, y su distribución
por capas. Se toma como referencia el formato de carriles de Artify y los ejemplos
aprobados aportados por el usuario, aplicados al circuito real de VitaPet.

Los diagramas se incorporan al Word en **12.10 Diagrama de Klein** y
**12.10.1 Diagrama de Klein por capas**. Cada uno ocupa una página A4 vertical.
El segundo muestra actividades y capas, sin nombres de clases ni métodos.

## Revisión manual

1. Abrir la versión actualizada del Word y localizar ambas secciones desde el índice.
2. Comprobar que cada diagrama ocupe una sola página A4 vertical, sin recortes,
   y que puedan seguirse las flechas, las decisiones y sus rótulos.
3. Recorrer selección del plan, contratación pendiente, pago aprobado o rechazado,
   factura numerada, PDF, correo, descarga opcional y activación al pulsar Continuar.
4. En el diagrama por capas, comprobar la ubicación de las actividades en GUI,
   Seguridad, BLL, DAL y Servicios. BE transporta datos entre ellas.
5. Identificar la factura PDF: Servicios la genera, Negocio coordina su emisión,
   Datos conserva la información y Presentación entrega el archivo al cliente.

El circuito se contrastó con las páginas y los componentes de contratación,
pago, facturación, PDF y correo. El mapa de esa revisión está en
`Docs/Diagramas/README.md`. Los SVG editables y los PNG están en esa carpeta.

No se modificó el comportamiento de la aplicación ni se ejecutaron cobros o
envíos de correo para este punto. La confirmación del punto 27 queda pendiente
de la revisión del usuario.
