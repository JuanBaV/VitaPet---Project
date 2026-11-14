(function () {
    'use strict';
    var raiz = document.querySelector('[data-consulta-virtual]');
    if (!raiz) return;
    var buscar = function (selector) { return raiz.querySelector(selector); };
    var texto = buscar('[data-texto]'), archivo = buscar('[data-archivo]');
    var enviar = buscar('button[data-enviar]'), limpiar = buscar('[data-limpiar]');
    var adjuntar = buscar('[data-adjuntar]'), fotoCodificada = buscar('#hfFotoBase64');
    var ocupado = false, leyendoFoto = false, urlFoto = null, habilitado = false;
    var ajax = Sys.WebForms.PageRequestManager.getInstance();
    function avisar(mensaje, error) {
        var estado = buscar('[data-estado]');
        estado.textContent = mensaje || '';
        estado.classList.toggle('es-error', !!error);
    }
    function actualizarEnvio() {
        enviar.disabled = ocupado || leyendoFoto || !habilitado || (!texto.value.trim() && !fotoCodificada.value);
        buscar('[data-contador]').textContent = texto.value.length + '/2000';
    }
    function ajustarTexto() {
        texto.style.height = 'auto';
        texto.style.height = Math.min(160, Math.max(47, texto.scrollHeight)) + 'px';
        actualizarEnvio();
    }
    function bloquear(valor) {
        ocupado = valor;
        [limpiar, texto, archivo, adjuntar, buscar('[data-quitar]')].forEach(function (control) {
            control.disabled = valor || leyendoFoto || !habilitado;
        });
        raiz.querySelectorAll('[data-sugerencia]').forEach(function (control) { control.disabled = valor || !habilitado; });
        enviar.classList.toggle('esta-enviando', valor);
        enviar.setAttribute('aria-label', valor ? raiz.dataset.enviando : raiz.dataset.enviar);
        enviar.title = valor ? raiz.dataset.enviando : raiz.dataset.enviar;
        buscar('[data-compositor]').setAttribute('aria-busy', String(valor));
        actualizarEnvio();
    }
    function quitarFoto() {
        if (urlFoto) URL.revokeObjectURL(urlFoto);
        urlFoto = null; archivo.value = ''; fotoCodificada.value = '';
        buscar('[data-imagen]').removeAttribute('src');
        buscar('[data-nombre-foto]').textContent = '';
        buscar('[data-vista]').hidden = true;
        actualizarEnvio();
    }
    function alFinal() {
        var conversacion = buscar('[data-conversacion]');
        conversacion.scrollTop = conversacion.scrollHeight;
    }
    function mostrarPendiente() {
        var bienvenida = buscar('[data-bienvenida]');
        if (bienvenida) bienvenida.hidden = true;
        var historial = buscar('[data-historial]');
        function agregar(rol, contenido) {
            var articulo = document.createElement('article');
            articulo.className = 'consulta-virtual-mensaje' + (rol === 'user' ? ' es-cliente' : ' consulta-virtual-pendiente');
            articulo.dataset.pendiente = 'true';
            var nombre = document.createElement('div'); nombre.className = 'consulta-virtual-autor';
            nombre.textContent = rol === 'user' ? raiz.dataset.tu : raiz.dataset.asistente;
            var cuerpo = document.createElement('p'); cuerpo.textContent = contenido;
            articulo.append(nombre, cuerpo); historial.appendChild(articulo); return cuerpo;
        }
        agregar('user', texto.value.trim() || raiz.dataset.foto);
        var pendiente = agregar('model', '');
        pendiente.parentNode.setAttribute('aria-label', raiz.dataset.enviando);
        for (var i = 0; i < 3; i++) {
            var punto = document.createElement('i'); punto.setAttribute('aria-hidden', 'true'); pendiente.appendChild(punto);
        }
        alFinal();
    }
    function aplicarEstado() {
        var estado = buscar('[data-estado-servidor]');
        habilitado = estado.dataset.habilitado === 'true';
        buscar('[data-chat]').hidden = !habilitado;
        raiz.classList.toggle('esta-bloqueado', !habilitado);
        if (estado.dataset.accion === 'mensaje' || estado.dataset.accion === 'limpiar') {
            texto.value = ''; quitarFoto();
        }
        if (estado.dataset.codigo === 'SinPlan') { texto.value = ''; quitarFoto(); }
        bloquear(false); ajustarTexto(); alFinal();
    }
    // El archivo viaja en un campo oculto: FileUpload no participa en postbacks parciales.
    // El servidor vuelve a comprobar tamaño, formato real y dimensiones antes de usarlo.
    archivo.addEventListener('change', function () {
        var foto = archivo.files[0];
        if (!foto) { quitarFoto(); return; }
        if (foto.size > 3 * 1024 * 1024) { quitarFoto(); avisar(raiz.dataset.grande, true); return; }
        if (!['image/jpeg', 'image/png'].includes(foto.type)) { quitarFoto(); avisar(raiz.dataset.formato, true); return; }
        if (urlFoto) URL.revokeObjectURL(urlFoto);
        fotoCodificada.value = ''; leyendoFoto = true; bloquear(false);
        var lector = new FileReader();
        lector.onload = function () {
            fotoCodificada.value = String(lector.result).split(',')[1] || '';
            urlFoto = URL.createObjectURL(foto);
            buscar('[data-imagen]').src = urlFoto;
            buscar('[data-nombre-foto]').textContent = foto.name;
            buscar('[data-vista]').hidden = false;
            leyendoFoto = false; bloquear(false); avisar(''); texto.focus();
        };
        lector.onerror = function () { leyendoFoto = false; quitarFoto(); bloquear(false); avisar(raiz.dataset.formato, true); };
        lector.readAsDataURL(foto);
    });
    adjuntar.addEventListener('click', function () { if (!ocupado && !leyendoFoto && habilitado) archivo.click(); });
    buscar('[data-quitar]').addEventListener('click', quitarFoto);
    texto.addEventListener('input', ajustarTexto);
    raiz.addEventListener('click', function (evento) {
        var boton = evento.target.closest('[data-sugerencia]');
        if (!boton || ocupado || !habilitado) return;
        texto.value = boton.dataset.sugerencia; ajustarTexto(); texto.focus();
        texto.setSelectionRange(texto.value.length, texto.value.length);
    });
    enviar.addEventListener('click', function (evento) {
        if (ocupado || leyendoFoto || !habilitado || (!texto.value.trim() && !fotoCodificada.value)) {
            evento.preventDefault(); evento.stopImmediatePropagation();
        }
    }, true);
    limpiar.addEventListener('click', function (evento) {
        if (ocupado || leyendoFoto || !habilitado) { evento.preventDefault(); evento.stopImmediatePropagation(); }
    }, true);
    texto.addEventListener('keydown', function (evento) {
        if (evento.key === 'Enter' && !evento.shiftKey && !evento.isComposing && evento.keyCode !== 229) {
            evento.preventDefault(); if (!enviar.disabled) enviar.click();
        }
    });
    ajax.add_initializeRequest(function (sender, args) { if (ocupado) args.set_cancel(true); });
    ajax.add_beginRequest(function (sender, args) {
        var boton = args.get_postBackElement();
        avisar('');
        if (boton && boton.id === enviar.id) mostrarPendiente();
        bloquear(true);
    });
    ajax.add_endRequest(function (sender, args) {
        var error = args.get_error();
        if (error) {
            args.set_errorHandled(true);
            raiz.querySelectorAll('[data-pendiente]').forEach(function (nodo) { nodo.remove(); });
            var bienvenida = buscar('[data-bienvenida]');
            if (bienvenida && !buscar('[data-historial]').querySelector('article')) bienvenida.hidden = false;
            avisar(raiz.dataset.error, true); bloquear(false);
        } else aplicarEstado();
        if (habilitado) texto.focus();
    });
    aplicarEstado();
}());
