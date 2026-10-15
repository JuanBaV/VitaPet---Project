const { chromium } = require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs = require('fs');
const path = require('path');
const { execFileSync } = require('child_process');

(async () => {
    const env = JSON.parse(fs.readFileSync(process.argv[2], 'utf8').replace(/^\uFEFF/, ''));
    if (!/^VitaPet_Verificacion_Usuarios_[a-f0-9]{32}$/.test(env.Database)) throw Error('Isolated database required');
    const out = path.dirname(process.argv[2]);
    let checks = 0;
    const check = (ok, message) => { if (!ok) throw Error(message); checks++; console.log('PASS ' + message); };
    const browser = await chromium.launch({ headless: true, channel: 'msedge' });
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    const c = id => page.locator('[id$="' + id + '"]');
    const click = async id => { await Promise.all([page.waitForNavigation(), c(id).click()]); };
    const switchTo = async code => {
        await page.locator('#btnSelectorIdioma').click();
        await click(code === 'en' ? 'btnIdiomaEn' : 'btnIdiomaEs');
    };
    try {
        await page.goto(env.Url + 'Login.aspx');
        await c('txtCorreo').fill('webmaster@vitapet.local');
        await c('txtContrasena').fill('PruebaVitaPet2026!');
        await click('btnIngresar');
        check(page.url().includes('Home'), 'Login opens Home');
        check((await page.locator('#btnSelectorIdioma').innerText()).includes('ES'), 'Selector displays current language');
        check(!await page.locator('.language-options').isVisible(), 'Language options are initially closed');
        await page.locator('#btnSelectorIdioma').click();
        check(await page.locator('.language-options').isVisible(), 'Click opens dropdown');
        check(await page.locator('#btnSelectorIdioma').getAttribute('aria-expanded') === 'true', 'Dropdown exposes expanded state');
        await page.keyboard.press('Escape');
        check(!await page.locator('.language-options').isVisible(), 'Escape closes dropdown');
        await switchTo('en');
        check((await page.locator('#btnSelectorIdioma').innerText()).includes('EN'), 'Selector changes to EN');
        check(await page.locator('html').getAttribute('lang') === 'en', 'Document language is English');
        check(await c('lnkInicio').innerText() === 'Home' && await c('lnkBitacora').innerText() === 'Audit log'
            && await c('lnkGestionUsuarios').innerText() === 'Users' && await c('lnkBaseDatos').innerText() === 'Database', 'All header links translate');
        check((await page.locator('.body-content').innerText()).includes('Hello,'), 'Home greeting translates');

        await page.goto(env.Url + 'BaseDatos.aspx');
        check(await page.locator('h1').innerText() === 'Database', 'Database title translates');
        const databaseBody = await page.locator('.body-content').innerText();
        check(databaseBody.includes('Integrity') && databaseBody.includes('Restore a backup') && databaseBody.includes('Webmaster recovery'), 'Database sections translate');
        check(await c('btnCrear').getAttribute('value') === 'Create backup' && await c('btnVerificar').getAttribute('value') === 'Check integrity', 'Database actions translate');
        check(databaseBody.includes('No integrity errors were found.'), 'Integrity empty state translates');
        await click('btnCrear');
        check((await c('pnlConfirmacion').innerText()).includes('Do you confirm'), 'Backup confirmation translates');
        await click('btnCancelar');
        await c('txtCorreoEmergencia').fill('bad');
        await c('txtClaveEmergencia').fill('weak');
        // Native email validation must not block the server validation exercise.
        await page.locator('form').evaluate(form => form.noValidate = true);
        await click('btnConfigurarEmergencia');
        check((await c('pnlMensaje').innerText()).includes('Enter a valid email'), 'Business validation message translates');
        await page.screenshot({ path: path.join(out, 'database-en.png'), fullPage: true });

        await page.goto(env.Url + 'ConsultaBitacora.aspx');
        check(await page.locator('h1').innerText() === 'Audit log', 'Audit title translates');
        const auditBody = await page.locator('.body-content').innerText();
        check(auditBody.includes('Start date') && auditBody.includes('End date') && auditBody.includes('Severity'), 'Audit filters translate');
        check(auditBody.includes('events. Page') && auditBody.includes('events per page'), 'Audit count and paging summary translate');
        check(auditBody.includes('Successful sign-in') && auditBody.includes('Authentication') && auditBody.includes('Information'), 'Stored audit labels translate at display time');
        check(await c('ddlCriticidad').locator('option[value="Informativa"]').innerText() === 'Information', 'Severity label translates while filter value stays unchanged');
        await c('dtpFechaDesde').fill('2026-10-05');
        await c('dtpFechaHasta').fill('2025-01-01');
        await click('btnConsultar');
        check((await c('pnlMensaje').innerText()).includes('The start date is later'), 'Catalog error translates by code');
        await page.screenshot({ path: path.join(out, 'audit-en.png'), fullPage: true });

        await page.goto(env.Url + 'GestionUsuarios.aspx');
        check(await page.locator('h1').innerText() === 'User management', 'User management title translates');
        check((await c('gvUsuarios').innerText()).includes('Customer') && (await c('gvUsuarios').innerText()).includes('Active'), 'Roles and statuses translate');
        check(await c('ddlEstado').locator('option[value="Activo"]').innerText() === 'Active', 'Status filter retains original value');
        // Open each operation through its valid server-side selection ticket.
        await c('txtBuscarUsuario').fill('José Prueba 0');
        await click('btnBuscar');
        await Promise.all([page.waitForNavigation(), c('gvUsuarios').getByRole('link', { name: 'Select', exact: true }).first().click()]);
        await click('btnConsultar');
        check(await page.locator('h1').innerText() === 'User details', 'User details title translates');
        await click('btnVolver');
        await Promise.all([page.waitForNavigation(), c('gvUsuarios').getByRole('link', { name: 'Select', exact: true }).first().click()]);
        await click('btnModificar');
        check(await page.locator('h1').innerText() === 'Edit user', 'Edit user title translates');
        check(await c('ddlRol').locator('option[value="Cliente"]').innerText() === 'Customer', 'Role options retain their business values');
        await c('txtNombre').fill('');
        await click('btnGuardar');
        check((await c('pnlMensaje').innerText()).includes('Incomplete data'), 'User operation validation translates');
        await click('btnCancelar');

        await page.goto(env.Url + 'Logout.aspx');
        check(await page.locator('h1').innerText() === 'Sign out', 'Sign-out title translates');
        check((await page.locator('.login-card').innerText()).includes('Would you like to sign out') && await c('lnkCancelar').innerText() === 'Cancel', 'Sign-out question and cancellation translate');
        await page.screenshot({ path: path.join(out, 'logout-en.png'), fullPage: true });
        await switchTo('es');
        check(await page.locator('h1').innerText() === 'Cerrar sesión' && await c('lnkCancelar').innerText() === 'Cancelar', 'Switching back restores Spanish text');
        await switchTo('en');
        await page.setViewportSize({ width: 390, height: 844 });
        await page.locator('#btnSelectorIdioma').click();
        const box = await page.locator('.language-options').boundingBox();
        check(box && box.x >= 0 && box.x + box.width <= 390, 'Dropdown fits a narrow viewport');
        await page.screenshot({ path: path.join(out, 'dropdown-mobile.png'), fullPage: true });
        await page.keyboard.press('Escape');
        await page.setViewportSize({ width: 1440, height: 1000 });
        await page.goto(env.Url + 'AccesoDenegado.aspx');
        check(await page.locator('h1').innerText() === 'Access denied', 'Public page inherits the same language');
        await page.goto(env.Url + 'Login.aspx');
        check(await page.locator('h1').innerText() === 'Sign in', 'Login respects authenticated language');
        await c('btnIngresar').click();
        check((await c('pnlMensaje').innerText()).includes('Incomplete data'), 'Client-side login validation is localized');
        await page.goto(env.Url + 'CatalogoErrores.aspx');
        check((await page.locator('body').innerText()).includes('Incorrect credentials'), 'Entire error catalog translates');
        // Audit records were never rewritten to English.
        const sqlcmd = 'C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE';
        const raw = execFileSync(sqlcmd, ['-S', 'localhost', '-E', '-b', '-d', env.Database, '-h', '-1', '-W', '-Q', "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.Bitacora WHERE accion='Login exitoso' AND modulo='Autenticación'"], { encoding: 'utf8' });
        check(Number(raw.trim()) > 0, 'Audit data stays in its original language');
        console.log('TOTAL I18N WEB: ' + checks);
    } catch (error) {
        fs.writeFileSync(path.join(out, 'failure.html'), await page.content());
        await page.screenshot({ path: path.join(out, 'failure.png'), fullPage: true });
        throw error;
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exit(1); });
