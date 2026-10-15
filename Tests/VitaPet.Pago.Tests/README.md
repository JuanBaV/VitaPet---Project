# CU-001-003 Gestión de pago

Compilar `VitaPet Rework.slnx` y ejecutar `bin/Debug/net48/VitaPet.Pago.Tests.exe`.
La suite crea una base `VitaPet_Verificacion_Pago_<guid>` y la elimina al terminar;
no registra pagos en la base VitaPet. `Run-Web.ps1` verifica la GUI con IIS Express
y Edge sobre otra base temporal, y deja capturas en `artifacts/pago-web`.

El procesador se habilita mediante `PagoProcesador=PruebaLocal`. No realiza
conexiones de pago ni cobros reales. Estas tarjetas tienen resultados deterministas:

| Número de prueba | Resultado |
| --- | --- |
| `1111111111111111` | Aprobado; es el valor del botón de datos dummy |
| `4111111111111111` | Aprobado |
| `4000000000000002` | Rechazado ER006 |
| `5555555555554444` | No se puede iniciar el procesamiento ER055 |

Completar titular, vencimiento futuro `MM/AAAA` y código de seguridad de tres
o cuatro dígitos. El botón pequeño de la esquina inferior derecha completa todos
los campos y no envía el formulario. Solo aparece al mostrar un pago válido en modo
PruebaLocal. Números diferentes no se aprueban; no usar tarjetas personales.

La carga calcula el precio por las mascotas activas propias. Al pagar, la BLL
valida identidad, integridad y datos, y rechaza un total distinto del resumen
confirmado. Las operaciones se serializan con OperacionIntegridadDAL. Los SP
comprueban titularidad y estado; registro, bitácora, DVH, DVV y referencias se
confirman en una transacción. PAN, titular y código son datos transitorios y
no se guardan en SQL, ViewState, bitácora ni XML.

El pago aprobado muestra su comprobante mediante Post/Redirect/Get. Continuar
llama a ContratacionPlanBLL para activar la cuenta y registrar la contratación.
Ambos pasos admiten reenvíos sin duplicar registros: si falla la activación,
el pago aprobado permanece disponible para continuar desde su comprobante.

La migración 017 agrega cuatro procedimientos y reutiliza las tablas existentes
Pago y Suscripcion. ER054–ER057 se registran desde ErrorSistemaBLL con controles
de integridad; ER056 se conserva en el catálogo documental pero no pertenece a los
flujos vigentes del simulador. CU-001-003 no agrega webservices ni salidas XML;
se mantienen CoberturaService y el archivado de bitácora existentes.
