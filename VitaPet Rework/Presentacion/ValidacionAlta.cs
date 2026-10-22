using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using VitaPet.BLL;

namespace VitaPet.GUI.Presentacion
{
    // Adaptador de presentacion: las reglas de datos siguen perteneciendo a la BLL.
    public static class ValidacionAlta
    {
        public static bool Validar(Page pagina, params string[] grupos)
        {
            foreach (string grupo in grupos) pagina.Validate(grupo);
            return pagina.IsValid;
        }

        public static void Comprobar(CustomValidator control, ServerValidateEventArgs args, Action validar)
        {
            args.IsValid = true;
            try { validar(); }
            catch (RegistroClienteException ex) { Fallar(control, args, ex.Codigo); }
            catch (ClienteCorporativoException ex) { Fallar(control, args, ex.Codigo); }
            catch (RegistroMascotaException ex) { Fallar(control, args, ex.Codigo); }
            catch (EmpresaException ex) { Fallar(control, args, ex.Codigo); }
            catch (PlanException ex) { Fallar(control, args, ex.Codigo); }
        }

        private static void Fallar(CustomValidator control, ServerValidateEventArgs args, string codigo)
        {
            args.IsValid = false;
            control.ErrorMessage = codigo + " - " + TextosUI.Error(codigo,
                codigo == "ER002" ? "Datos incompletos" : "Datos inválidos");
        }
    }
}