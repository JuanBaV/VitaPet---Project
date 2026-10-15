const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path');
(async()=>{
 const env=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Pago_[a-f0-9]{32}$/.test(env.Database))throw Error('Only isolated payment databases');
 const out=path.dirname(process.argv[2]);let checks=0;
 const check=(ok,msg)=>{if(!ok)throw Error(msg);checks++;console.log('PASS '+msg);};
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const context=await browser.newContext({viewport:{width:1440,height:1100}}),page=await context.newPage();
 const c=id=>page.locator('[id$="_'+id+'"]');
 const click=async id=>Promise.all([page.waitForNavigation(),c(id).click()]);
 const dummy=async()=>page.locator('#btnCompletarDummy').click();
 try{
  await page.goto(env.Url+'GestionPago.aspx?contratacion=1');check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Pago requiere sesion');
  await c('txtCorreo').fill('cliente@vitapet.local');await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');
  await page.goto(env.Url+'ContratarPlan.aspx?plan=1');await click('btnContratar');
  const pending=page.url();check((await c('lblImporte').innerText())==='USD 38.00','Cotizacion de dos mascotas en servidor');
  check((await c('lblPrecioMascota').innerText())==='USD 19.00'&&(await c('lblCantidadMascotas').innerText())==='2','Precio unitario y cantidad separados');
  check((await page.locator('main').innerText()).includes('No se realizan cobros reales'),'Modo de prueba identificado');
  await dummy();
  check(await c('txtNumeroTarjeta').inputValue()==='1111111111111111'&&await c('txtTitular').inputValue()==='Prueba Prueba'&&await c('txtCodigoSeguridad').inputValue()==='111','Boton dummy completa todos los campos');
  check((await c('ddlMedioPago').inputValue())==='Tarjeta de credito'&&/^12\/\d{4}$/.test(await c('txtVencimiento').inputValue()),'Dummy selecciona medio y vencimiento futuro');
  check(await c('btnPagar').count()===1&&page.url()===pending,'Completar dummy no ejecuta pago ni envia formulario');
  const bounds=await page.locator('#btnCompletarDummy').boundingBox();check(bounds.x+bounds.width<=1440&&bounds.x+bounds.width>=1420&&bounds.y+bounds.height>=1080,'Boton pequeno en esquina inferior derecha');
  await page.screenshot({path:path.join(out,'pago-formulario.png'),fullPage:true});
  await c('txtTitular').fill('');await click('btnPagar');check((await c('pnlMensaje').innerText()).includes('ER002'),'Datos obligatorios validados por servidor');
  check(await page.locator('#btnCompletarDummy').count()===1,'Dummy disponible tambien al corregir errores');
  await dummy();await c('txtVencimiento').fill('01/2020');await click('btnPagar');check((await c('pnlMensaje').innerText()).includes('ER025'),'Vencimiento invalido rechazado');
  await dummy();await c('txtNumeroTarjeta').fill('4000000000000002');await click('btnPagar');check((await c('pnlMensaje').innerText()).includes('ER006'),'Pago rechazado de prueba');
  check(!await c('txtNumeroTarjeta').inputValue()&&!await c('txtCodigoSeguridad').inputValue(),'No devuelve tarjeta ni codigo al cliente tras postback');
  await page.screenshot({path:path.join(out,'pago-rechazado.png'),fullPage:true});
  await dummy();await c('txtNumeroTarjeta').fill('5555555555554444');await click('btnPagar');check((await c('pnlMensaje').innerText()).includes('ER055'),'Procesador no disponible de prueba');
  const nojs=await browser.newContext({javaScriptEnabled:false,storageState:await context.storageState()});
  try{
   const plain=await nojs.newPage();await plain.goto(pending);
   await Promise.all([plain.waitForNavigation(),plain.locator('[id$="_btnPagar"]').click()]);
   check((await plain.locator('[id$="_pnlMensaje"]').innerText()).includes('ER002'),'Validacion funciona con JavaScript deshabilitado');
  }finally{await nojs.close();}
  await page.goto(env.Url+'GestionPago.aspx?contratacion=999999');check((await c('pnlMensaje').innerText()).includes('ER054'),'Contrato inexistente no muestra controles de pago');
  check(await c('btnPagar').count()===0&&await page.locator('#btnCompletarDummy').count()===0,'Dummy oculto cuando no hay pago valido');
  await page.goto(pending);await page.setViewportSize({width:390,height:844});
  check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Formulario sin desbordamiento movil');
  await page.screenshot({path:path.join(out,'pago-movil.png'),fullPage:true});
  await page.setViewportSize({width:1440,height:1100});await dummy();await click('btnPagar');
  check(new URL(page.url()).searchParams.get('comprobante')==='1','Post/Redirect/Get despues de aprobar pago');
  check((await page.locator('main').innerText()).includes('Pago aprobado')&&(await c('lblImportePagado').innerText())==='USD 38.00','Comprobante propio con importe aprobado');
  check(await page.locator('#btnCompletarDummy').count()===0&&await c('txtNumeroTarjeta').count()===0,'Comprobante sin datos de tarjeta ni boton dummy');
  const receipt=page.url(),number=await c('lblNumeroPago').innerText();await page.reload();check((await c('lblNumeroPago').innerText())===number,'Recarga no registra otro pago');
  await page.screenshot({path:path.join(out,'pago-comprobante.png'),fullPage:true});
  await page.goto(pending);check((await c('pnlMensaje').innerText()).includes('ER054'),'No permite pagar de nuevo una suscripcion ya abonada');
  check(await c('btnContinuar').count()===1&&await c('btnPagar').count()===0,'Reingresar a un pago aprobado permite retomar desde el comprobante');
  await page.goto(receipt);await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEn');
  check((await page.locator('main').innerText()).includes('Payment approved')&&(await c('lblMedioPagado').innerText())==='Credit card','Comprobante y medio traducidos a ingles');
  await page.screenshot({path:path.join(out,'pago-en.png'),fullPage:true});
  await click('btnContinuar');check(/\/Planes(?:\.aspx)?$/.test(new URL(page.url()).pathname)&&(await c('pnlMensaje').innerText()).includes('now active'),'Continuar vuelve a contratacion y activa el plan');
  await page.goto(receipt);await click('btnContinuar');check((await c('pnlMensaje').innerText()).includes('now active'),'Continuar repetido es idempotente');
  await context.clearCookies();await page.goto(receipt);check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Comprobante requiere sesion');
  fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify({checks,status:'passed',paymentNumber:number},null,2));
  console.log('TOTAL: '+checks+' browser checks passed');
 }catch(e){fs.writeFileSync(path.join(out,'failure.html'),await page.content());console.error((await page.locator('body').innerText()).slice(0,6000));await page.screenshot({path:path.join(out,'failure.png'),fullPage:true});throw e;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
