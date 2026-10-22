const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path');
(async()=>{
 const env=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Isolated database required');
 const out=path.dirname(process.argv[2]);let checks=0;
 const check=(ok,text)=>{if(!ok)throw Error(text);checks++;console.log('PASS '+text);};
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const context=await browser.newContext({viewport:{width:1440,height:1000},acceptDownloads:true});const page=await context.newPage();
 const c=id=>page.locator('[id$="_'+id+'"], [id*="_'+id+'_"]');
 const click=async id=>{await Promise.all([page.waitForNavigation(),c(id).click()]);};
 const download=async(id,name)=>{const promise=page.waitForEvent('download');await c(id).click();const d=await promise;await d.saveAs(path.join(out,name));const pdf=fs.readFileSync(path.join(out,name));check(pdf.subarray(0,8).toString().startsWith('%PDF-1.4')&&pdf.toString('latin1').trimEnd().endsWith('%%EOF'),'PDF completo: '+name);};
 try {
  await page.goto(env.Url+'Cliente.aspx');check(page.url().includes('Login'),'Mi perfil requiere sesión');await page.goto(env.Url+'Login.aspx');
  await c('txtCorreo').fill('cliente@vitapet.local');await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');
  await click('lnkPerfilCliente');check(await c('lnkConsultarCobertura').isVisible(),'Mi perfil conserva Consultar cobertura');
  check((await page.locator('main').innerText()).includes('Mis facturas')&&(await c('pnlSinFacturas').innerText()).includes('Todavía no tenés'),'Sección Mis facturas con estado vacío');
  await click('lnkPlanesCliente');await Promise.all([page.waitForNavigation(),page.locator('[id*="_lnkSeleccionarPlan"]').first().click()]);
  check((await c('lblTotalMensual').innerText()).replace(',','.').includes('34.20'),'Descuento de la mascota adicional en contratación');
  await click('btnContratar');check((await c('lblImporte').innerText()).includes('34.20'),'Pago mantiene el total con descuento');
  await page.locator('#btnCompletarDummy').click();check(await c('txtNumeroTarjeta').inputValue()==='1111111111111111','Conserva el botón y la tarjeta dummy del simulador existente');
  await click('btnPagar');const receipt=page.url();const invoice=await c('lblNumeroFactura').innerText();
  check(invoice.startsWith('0001-')&&await c('btnDescargarFactura').isVisible(),'Confirmación de pago muestra número y descarga de factura');
  await download('btnDescargarFactura','factura-pago.pdf');await click('btnContinuar');
  check((await page.locator('main').innerText()).includes(invoice)&&await c('lnkDescargarFactura').isVisible(),'Al terminar de contratar, Planes mantiene Descargar factura visible');
  await page.screenshot({path:path.join(out,'contratacion-finalizada.png'),fullPage:true});await download('lnkDescargarFactura','factura-planes.pdf');
  await click('lnkPerfilCliente');check((await page.locator('main').innerText()).includes(invoice)&&await c('lnkDescargarFactura').isVisible(),'Mi perfil permite consultar y descargar la factura registrada');
  check(await c('lnkConsultarCobertura').isVisible(),'La consulta de cobertura sigue accesible después de facturar');
  await download('lnkDescargarFactura','factura-perfil.pdf');await page.screenshot({path:path.join(out,'mi-perfil-facturas.png'),fullPage:true});
  await click('lnkConsultarCobertura');check((await page.locator('main').innerText()).includes('Plan Esencial'),'Consulta cobertura del plan contratado');
  await click('lnkPerfilCliente');await page.setViewportSize({width:390,height:844});check(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),'Facturas de Mi perfil sin desbordamiento en móvil');
  await page.screenshot({path:path.join(out,'mi-perfil-movil.png'),fullPage:true});
  await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEn');check((await page.locator('main').innerText()).includes('My invoices'),'Sección de facturas traducida al inglés');
  await context.clearCookies();await page.goto(env.Url+'Login.aspx');await c('txtCorreo').fill('otro@test.local');await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');await page.goto(env.Url+'Cliente.aspx');
  check(await c('pnlSinFacturas').isVisible()&&!(await page.locator('main').innerText()).includes(invoice),'Otro cliente no ve la factura ajena en su perfil');
  const foreign=await context.request.get(receipt+'&descargar=1');check(!(foreign.headers()['content-type']||'').includes('application/pdf')&&(await foreign.text()).includes('ER081'),'No descarga facturas ajenas cambiando la URL');
  console.log('TOTAL: '+checks+' verificaciones web de VitaPet Rework');
 }catch(e){fs.writeFileSync(path.join(out,'failure.html'),await page.content());console.error((await page.locator('body').innerText()).slice(0,7500));await page.screenshot({path:path.join(out,'failure.png'),fullPage:true});throw e;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
