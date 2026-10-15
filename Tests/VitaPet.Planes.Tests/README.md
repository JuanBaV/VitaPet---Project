# Verificación de CU-001-002

Compilar con MSBuild de Visual Studio y ejecutar `bin\Debug\net48\VitaPet.Planes.Tests.exe` para verificar las reglas sobre SQL Server local. Cada ejecución crea una base temporal con prefijo `VitaPet_Verificacion_Planes_` y la elimina al finalizar. No modifica la base VitaPet del proyecto.

`Run-Web.ps1` prepara otra base temporal, inicia IIS Express oculto y verifica las pantallas con Playwright y Microsoft Edge. La Web Application compila WebServices/CoberturaService.asmx.cs en la DLL de la GUI. Usa el runtime Node de Codex indicado en el script. Deja las capturas y el diagnóstico en `artifacts\planes-web`; detiene su propio proceso y elimina la base temporal al finalizar.

Las verificaciones cubren precios, mascotas propias activas, exclusión del Corporativo, autorización, cotización desactualizada, reintentos y concurrencia, transacciones, auditoría, DVH/DVV y referencias, consumo de CoberturaService, mantenimiento, idiomas y visualización móvil. Este recorrido se detiene antes de confirmar el pago: la suscripción queda Pendiente. El cobro de prueba y la activación se verifican en VitaPet.Pago.Tests.
