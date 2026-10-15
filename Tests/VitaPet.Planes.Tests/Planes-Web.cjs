const { chromium } = require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs = require('fs');
const path = require('path');
(async () => {
    const env = JSON.parse(fs.readFileSync(process.argv[2], 'utf8').replace(/^\uFEFF/, ''));
    if (!/^VitaPet_Verificacion_Planes_[a-f0-9]{32}$/.test(env.Database)) throw Error('Isolated database required');
    const out = path.dirname(process.argv[2]);
    let checks = 0;
    const check = (ok, text) => { if (!ok) throw Error(text); checks++; console.log('PASS ' + text); };
    const browser = await chromium.launch({headless:true, channel:'msedge'});
    const context = await browser.newContext({viewport:{width:1440,height:1000}});
    const page = await context.newPage();
    const c = id => page.locator(id==='lnkSeleccionarPlan'?'[id*="_lnkSeleccionarPlan"]':'[id$="_' + id + '"]');
    const click = async id => { await Promise.all([page.waitForNavigation(), c(id).click()]); };
    const login = async () => {
        await page.goto(env.Url+'Login.aspx');
        await c('txtCorreo').fill('cliente@vitapet.local');
        await c('txtContrasena').fill('Prueba2026!'); await click('btnIngresar');
    };
    const coverageSoap = id => context.request.post(env.Url+'WebServices/CoberturaService.asmx',{
        headers:{'Content-Type':'text/xml; charset=utf-8',SOAPAction:'"https://vitapet.local/services/ObtenerCobertura"'},
        data:'<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><ObtenerCobertura xmlns="https://vitapet.local/services/"><idPlan>'+id+'</idPlan></ObtenerCobertura></soap:Body></soap:Envelope>'
    });
    try {
        await page.goto(env.Url+'Planes.aspx');
        check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Listado requiere sesión');
        let service=await coverageSoap(1);
        check(!service.ok(),'Servicio rechaza consulta anónima');
        await login(); await click('lnkPlanesCliente');
        check(/\/Planes(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Menú del cliente abre planes');
        const grid=await c('gvPlanes').innerText();
        check((await c('lnkSeleccionarPlan').count())===3 && !grid.includes('Corporativo'),'Solo tres planes contratables');
        check(['19','28','39'].every(n=>grid.includes('USD '+n)), 'Precios del catálogo: USD 19, 28 y 39');
        await page.screenshot({path:path.join(out,'planes-listado.png'),fullPage:true});
        let browserServiceCalls=0;
        page.on('request',r=>{if(r.url().includes('CoberturaService.asmx'))browserServiceCalls++;});
        await Promise.all([page.waitForNavigation(),c('lnkSeleccionarPlan').first().click()]);
        await c('gvCoberturas').getByText('Alcance del plan',{exact:true}).waitFor();
        check(browserServiceCalls===0,'Coberturas renderizadas por la llamada C# del servidor');
        service=await coverageSoap(1);
        check(service.ok() && (await service.text()).includes('<Prestacion>Alcance del plan</Prestacion>'),'ASMX publicado con su codebehind y respuesta SOAP');
        check(await c('lblCantidadMascotas').innerText()==='2' && (await c('lblTotalMensual').innerText()).includes('38'), 'Total de la cuenta: USD 19 × 2 mascotas = USD 38');
        check(await page.locator('select').count()===0,'No solicita seleccionar una mascota');
        await page.screenshot({path:path.join(out,'planes-detalle.png'),fullPage:true});
        const detail=page.url();
        await click('btnContratar');
        check(/\/GestionPago(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Contratar abre el acceso preparado a Gestión de pago');
        const pending=page.url();
        check(await c('btnPagar').count()===1 && (await c('lblImporte').innerText()).includes('38'),'Muestra el formulario y total pendiente sin procesar un pago');
        await page.screenshot({path:path.join(out,'planes-pendiente.png'),fullPage:true});
        await page.goto(detail); await click('btnContratar');
        check(page.url()===pending,'Reintento conserva la misma suscripción pendiente');
        await page.goto(env.Url+'ContratarPlan.aspx?plan=4');
        check((await c('pnlMensaje').innerText()).includes('ER050') && await c('btnContratar').count()===0,'Corporativo rechazado también por URL directa');
        await page.goto(env.Url+'GestionPago.aspx?contratacion=99999');
        check((await c('pnlMensaje').innerText()).includes('ER054'),'No permite acceder a otra contratación');
        service=await coverageSoap(4);
        check((await service.text()).includes('<CodigoError>ER050</CodigoError>'),'El servicio también excluye el plan corporativo');
        const maintenance=path.join(env.Runtime,'recuperacion','mantenimiento.txt');
        fs.writeFileSync(maintenance,'Prueba aislada');
        try {
            service=await coverageSoap(1);
            check(!service.ok(),'El servicio respeta el bloqueo por mantenimiento');
        } finally {fs.unlinkSync(maintenance);}
        await page.locator('#btnSelectorIdioma').click(); await click('btnIdiomaEn');
        await page.goto(env.Url+'ContratarPlan.aspx?plan=4');
        check((await c('pnlMensaje').innerText()).includes('no longer available'),'Nuevo error traducido al inglés');
        await page.goto(detail);
        check((await page.locator('main').innerText()).includes('Monthly total'),'Resumen de la cuenta traducido');
        await page.screenshot({path:path.join(out,'planes-en.png'),fullPage:true});
        await page.setViewportSize({width:390,height:844});await page.goto(env.Url+'Planes.aspx');
        check(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),'Listado sin desbordamiento en móvil');
        await page.screenshot({path:path.join(out,'planes-movil.png'),fullPage:true});
        await page.goto(detail);
        check(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth),'Detalle sin desbordamiento en móvil');
        const nojs=await browser.newContext({javaScriptEnabled:false,storageState:await context.storageState()});
        try {
            const plain=await nojs.newPage();await plain.goto(detail);
            check((await plain.locator('[id$="_gvCoberturas"]').innerText()).includes('Alcance del plan'),'Coberturas disponibles sin JavaScript');
        } finally {await nojs.close();}
        await context.clearCookies();await page.goto(pending);
        check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Acceso a pendiente requiere sesión');
        console.log('TOTAL: '+checks+' browser checks passed');
    } catch(error) {
        fs.writeFileSync(path.join(out,'failure.html'),await page.content());
        console.error((await page.locator('body').innerText()).slice(0,9000));
        await page.screenshot({path:path.join(out,'failure.png'),fullPage:true});throw error;
    } finally {await browser.close();}
})().catch(error=>{console.error(error);process.exit(1);});
