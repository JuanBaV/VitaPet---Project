(function () {
    "use strict";
    var form = document.querySelector(".corporativo-form");
    if (!form) return;
    var email = form.querySelector("input[type=email]");
    if (!email) return;
    email.addEventListener("input", function () {
        var allowed = form.dataset.habilitado === "1" && email.value.trim() === form.dataset.correo;
        form.querySelectorAll(".empresa-campos input, input[id$='_btnRegistrarCliente']").forEach(function (input) {
            input.disabled = !allowed;
        });
    });
}());
