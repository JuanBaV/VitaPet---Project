# Consulta virtual con ASP.NET AJAX

La página **Consulta virtual** (`ConsultaVirtual.aspx`) se abre desde una opción propia del header de clientes. Comparte el permiso existente `Cliente`, aplicado a la página, al enlace del header y al servicio; no requiere migraciones ni nuevos permisos en la base de datos.

## Presentación y eventos de página

- `ConsultaVirtual.aspx` contiene los controles de la pantalla y dos `UpdatePanel`: estado e historial. Reutiliza el `ScriptManager` de `Site.Master`.
- `ConsultaVirtual.aspx.cs` recibe los eventos `EnviarConsulta` y `NuevaConsulta`, comprueba el token de sesión, invoca la BLL y presenta los mensajes mediante un `Repeater` con salida codificada.
- El envío registra una `PageAsyncTask`; la llamada externa termina antes de renderizar la respuesta parcial. La página reutiliza el bloqueo global durante la validación y lo libera antes de esperar al agente. No mantiene una transacción SQL durante esa espera.
- `consulta-virtual.js` se ocupa de sugerencias, contador, vista previa, Enter, indicador de espera y eventos del `PageRequestManager`. No realiza llamadas `fetch`, no recibe JSON del chat y no construye el historial confirmado: lo renderiza ASPX.
- Para enviar una foto sin un postback completo, `FileReader` la codifica en un campo oculto fuera de los paneles actualizados. El servidor limita la longitud codificada y los bytes decodificados antes de comprobar la imagen real. `httpRuntime.maxRequestLength` es de 6144 KB para admitir el transporte de una imagen de hasta 3 MB en Base64 y el formulario. El límite funcional de la foto sigue siendo 3 MB.
- El compositor permanece fuera de los paneles para conservar texto y foto cuando hay errores. Sólo se limpia después de confirmar un envío o una nueva consulta. Las fotos no se guardan en ViewState ni en disco.
- Se retiraron `WebServices/ConsultaVirtual.ashx` y el control `Controls/ConsultaVirtual.ascx`, reemplazados por la propia página ASPX.

## Configuración

Completar **`VitaPet Rework/App_Data/secrets.env`**:

```dotenv
GEMINI_API_KEY=
GEMINI_MODEL=gemini-3.5-flash-lite
```

Pegar la clave después de `GEMINI_API_KEY=`. El archivo no se incluye en Git ni en la publicación. En el servidor de destino hay que crearlo manualmente en `App_Data`, con acceso de lectura para la identidad de IIS. Las variables de entorno `GEMINI_API_KEY` y `GEMINI_MODEL` tienen prioridad. No hace falta reiniciar por cambios en el archivo: se lee al consultar.

El modelo se puede cambiar por otro modelo de texto e imágenes habilitado para la cuenta y compatible con `generateContent`. Se usa REST con `x-goog-api-key`, sin enviar la clave al navegador. Referencias oficiales: [generateContent](https://ai.google.dev/api/generate-content), [imágenes](https://ai.google.dev/gemini-api/docs/image-understanding) y [modelos disponibles](https://ai.google.dev/gemini-api/docs/models).

## Comportamiento

- Requiere sesión y permiso `Cliente`. Valida la suscripción del usuario autenticado en cada solicitud mediante `sp_Suscripcion_ObtenerVigenteCliente`: estado `Activa` y fechas vigentes. Planes pendientes, cancelados, vencidos o futuros no habilitan la consulta.
- Si no hay plan, ofrece un enlace a Planes. Si falla la validación, no llama a Gemini.
- Acepta síntomas (hasta 2000 caracteres), foto, o ambos. Las fotos deben ser JPEG/PNG de hasta 3 MB, 24 megapíxeles y 8000 píxeles por lado. El servidor decodifica el contenido real, aplica orientación y vuelve a codificar una imagen de hasta 1600 píxeles, sin metadatos.
- Al abrir una conversación nueva y al pulsar Nueva consulta se registra Inicio de consulta virtual en bitácora, con usuario, fecha, módulo y criticidad Informativa. Se confirman DVH, DVV y referencias en la misma transacción. Recargar no duplica la auditoría; un fallo muestra ER033 sin reemplazar la conversación anterior.
- Historial de hasta seis intercambios en sesión, incluidas fotos, para preguntas de seguimiento. Nueva consulta lo borra; cerrar sesión también. No se almacena el chat en la base de datos ni se guardan fotos en disco. Borrar el historial en VitaPet no solicita un borrado a Google.
- Los mensajes y fotos del contexto se envían a Google Gemini. La interfaz lo informa antes de enviar.
- El asistente orienta sobre posibilidades, pide datos faltantes, evita diagnósticos definitivos y prescripciones, e indica atención urgente ante señales graves. La advertencia permanece visible.
- Protección CSRF mediante token de sesión, ViewState ligado a la sesión y validación de eventos, respuestas sin caché, contenido presentado como texto, límite de un envío cada diez segundos y espera de hasta 45 segundos al proveedor. No se registran síntomas, fotos ni claves en logs.
- La llamada externa no mantiene abierta una transacción SQL ni el bloqueo global de mantenimiento.
- Los mensajes fallidos no se agregan al historial; el borrador y la foto siguen disponibles para reintentar. Un cambio de plan o una sesión vencida vuelve a bloquear el chat.

## Verificación manual con una clave real

1. Ingresar con un cliente sin plan, con un plan pendiente o vencido: debe mostrar el bloqueo y no llamar a Gemini.
2. Ingresar con un cliente con plan activo: la página verifica el acceso y presenta el chat. Sin clave, intentar enviar informa la indisponibilidad de configuración; con una clave válida responde el asistente.
3. Consultar síntomas, continuar con una pregunta y enviar una foto sola o con texto. Verificar vista previa, respuesta y continuidad.
4. Probar foto inválida, foto de más de 3 MB, falta de sesión/token, agotamiento de cuota y error de red: no deben filtrar secretos ni mostrar errores internos.
5. Usar Nueva consulta y cerrar/reabrir sesión: el historial anterior debe desaparecer. Comprobar que los envíos y Nueva consulta actualicen por AJAX, y que la bitácora no se duplique al recargar.
6. Cambiar el idioma entre español e inglés y revisar la presentación en móvil.

La comprobación con respuestas reales de Gemini requiere una clave y una cuenta con acceso al modelo configurado.
