<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Async="true" AsyncTimeout="60" CodeBehind="ConsultaVirtual.aspx.cs" Inherits="ConsultaVirtualPage" %>
<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
<main class="consulta-virtual-page">
<link rel="stylesheet" href="<%: ResolveUrl("~/Content/consulta-virtual.css") %>?v=20261101-aspx-ajax" />
<section class="consulta-virtual esta-bloqueado" data-consulta-virtual
    data-enviando="<%: Texto("Enviando") %>" data-enviar="<%: Texto("Enviar") %>"
    data-tu="<%: Texto("Tu") %>" data-asistente="<%: Texto("Asistente") %>" data-foto="<%: Texto("FotoAdjunta") %>"
    data-error="<%: Texto("Error_NoDisponible") %>" data-grande="<%: Texto("Error_ImagenGrande") %>"
    data-formato="<%: Texto("Error_ImagenInvalida") %>" aria-labelledby="consultaVirtualTitulo">
    <header class="consulta-virtual-cabecera">
        <div class="consulta-virtual-identidad">
            <span class="consulta-virtual-avatar" aria-hidden="true"><svg viewBox="0 0 24 24" fill="currentColor"><ellipse cx="6.2" cy="8" rx="2.4" ry="3.2" transform="rotate(-20 6.2 8)"/><ellipse cx="17.8" cy="8" rx="2.4" ry="3.2" transform="rotate(20 17.8 8)"/><ellipse cx="10.1" cy="5.2" rx="2.1" ry="2.8"/><ellipse cx="14.7" cy="5.2" rx="2.1" ry="2.8"/><path d="M6.1 17.2c0-2.6 2.8-6.1 5.9-6.1s5.9 3.5 5.9 6.1c0 2-1.4 3-3.3 2.4-1.6-.5-3.6-.5-5.2 0-1.9.6-3.3-.4-3.3-2.4Z"/></svg></span>
            <div><h1 id="consultaVirtualTitulo"><%: Texto("Titulo") %></h1><span class="consulta-virtual-subtitulo"><span aria-hidden="true"></span><%: Texto("Asistente") %></span></div>
        </div>
        <button id="btnNuevaConsulta" runat="server" type="button" OnServerClick="NuevaConsulta" data-limpiar class="consulta-virtual-nueva">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-6M16 3l5 5M10 14l1-5 7-7 5 5-7 7-6 1Z"/></svg>
            <span><%: Texto("Nueva") %></span>
        </button>
    </header>
    <asp:HiddenField ID="hfTokenConsulta" runat="server" EnableViewState="false" />
    <asp:UpdatePanel ID="upEstado" runat="server" UpdateMode="Always">
      <ContentTemplate>
        <div id="estadoServidor" runat="server" data-estado-servidor hidden></div>
        <p id="mensajeEstado" runat="server" class="consulta-virtual-estado" data-estado role="status" aria-live="polite"></p>
    <p id="ofertaPlanes" runat="server" class="consulta-virtual-contratar" data-contratar><a href="<%: ResolveUrl("~/Planes.aspx") %>" class="btn btn-default"><%: Texto("Contratar") %></a></p>
      </ContentTemplate>
    </asp:UpdatePanel>
    <div class="consulta-virtual-chat" data-chat hidden>
      <asp:UpdatePanel ID="consultaVirtualRespuesta" ClientIDMode="Static" runat="server" UpdateMode="Always">
       <ContentTemplate>
        <div class="consulta-virtual-conversacion" data-conversacion>
            <div id="bienvenida" runat="server" class="consulta-virtual-bienvenida" data-bienvenida>
                <span class="consulta-virtual-saludo" aria-hidden="true">✦</span>
                <h2><%: Texto("Sintomas") %></h2>
                <p><%: Texto("InicioAyuda") %></p>
                <div class="consulta-virtual-sugerencias" aria-label="<%: Texto("Sugerencias") %>">
                    <button type="button" data-sugerencia="<%: Texto("SugerenciaPielTexto") %>"><span aria-hidden="true">＋</span><%: Texto("SugerenciaPiel") %></button>
                    <button type="button" data-sugerencia="<%: Texto("SugerenciaApetitoTexto") %>"><span aria-hidden="true">＋</span><%: Texto("SugerenciaApetito") %></button>
                    <button type="button" data-sugerencia="<%: Texto("SugerenciaComportamientoTexto") %>"><span aria-hidden="true">＋</span><%: Texto("SugerenciaComportamiento") %></button>
                </div>
            </div>
            <div class="consulta-virtual-historial" data-historial role="log" aria-live="polite" aria-relevant="additions" aria-label="<%: Texto("Historial") %>" tabindex="0"><asp:Repeater ID="rptMensajes" runat="server" EnableViewState="false"><ItemTemplate>
                <article class='<%# (bool)Eval("EsCliente") ? "consulta-virtual-mensaje es-cliente" : "consulta-virtual-mensaje" %>'>
                    <div class="consulta-virtual-autor"><%#: Eval("Autor") %></div>
                    <p><%#: Eval("Texto") %></p>
                    <asp:PlaceHolder runat="server" Visible='<%# Eval("TieneFoto") %>'><small><%: Texto("FotoAdjunta") %></small></asp:PlaceHolder>
                </article>
            </ItemTemplate></asp:Repeater></div>
        </div>
       </ContentTemplate>
      </asp:UpdatePanel>
        <div class="consulta-virtual-pie">
            <div class="consulta-virtual-compositor" data-compositor>
                <div class="consulta-virtual-vista" data-vista hidden>
                    <img data-imagen alt="<%: Texto("VistaPrevia") %>" />
                    <div><strong data-nombre-foto></strong><small><%: Texto("FotoAdjunta") %></small></div>
                    <button type="button" data-quitar class="consulta-virtual-icono" aria-label="<%: Texto("QuitarFoto") %>" title="<%: Texto("QuitarFoto") %>"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><path d="m6 6 12 12M6 18 18 6"/></svg></button>
                </div>
                <label for="consultaVirtualTexto" class="consulta-virtual-solo-lector"><%: Texto("Sintomas") %></label>
                <asp:TextBox ID="consultaVirtualTexto" ClientIDMode="Static" runat="server" TextMode="MultiLine" Rows="1" MaxLength="2000" EnableViewState="false" ValidateRequestMode="Disabled" data-texto="" aria-describedby="consultaVirtualPrivacidad consultaVirtualTeclado" />
                <asp:HiddenField ID="hfFotoBase64" runat="server" EnableViewState="false" ClientIDMode="Static" />
                <div class="consulta-virtual-herramientas">
                    <div class="consulta-virtual-adjuntar">
                        <button type="button" data-adjuntar class="consulta-virtual-icono" aria-label="<%: Texto("Foto") %>" title="<%: Texto("Foto") %>"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m8 12 6-6a3 3 0 0 1 4 4l-8 8a5 5 0 0 1-7-7l9-9a1 1 0 0 1 1 1l-9 9a3 3 0 0 0 4 4l8-8a1 1 0 0 0-1-1l-6 6"/></svg></button>
                        <input type="file" id="consultaVirtualFoto" data-archivo accept="image/jpeg,image/png" hidden />
                        <span><%: Texto("FormatoFoto") %></span>
                    </div>
                    <div class="consulta-virtual-envio"><span class="consulta-virtual-contador" data-contador>0/2000</span>
                        <button id="btnEnviarConsulta" runat="server" type="button" OnServerClick="EnviarConsulta" data-enviar class="consulta-virtual-enviar"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 19V5m-6 6 6-6 6 6"/></svg><span class="consulta-virtual-spinner" aria-hidden="true"></span></button>
                    </div>
                </div>
            </div>
            <div class="consulta-virtual-notas"><span id="consultaVirtualTeclado"><%: Texto("Teclado") %></span><details><summary><%: Texto("Acerca") %></summary><p><%: Texto("Aviso") %></p><p><%: Texto("FotoAyuda") %></p><p><%: Texto("Privacidad") %></p></details></div>
            <p id="consultaVirtualPrivacidad" class="consulta-virtual-privacidad"><%: Texto("PrivacidadBreve") %></p>
        </div>
    </div>
    <noscript><p><%: Texto("Javascript") %></p></noscript>
</section>
<script src="<%: ResolveUrl("~/Scripts/consulta-virtual.js") %>?v=20261101-aspx-ajax" defer></script>

</main>
</asp:Content>
