using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using VitaPet.BLL.Seguridad;
using VitaPet.BE;
using VitaPet.DAL;

namespace VitaPet.BLL
{
    public class ErrorSistemaBLL
    {
        private readonly ErrorSistemaDAL _errorSistemaDAL;

        public ErrorSistemaBLL()
        {
            _errorSistemaDAL = new ErrorSistemaDAL();
        }

        // Alta idempotente: nunca reemplaza mensajes existentes ni repara datos corruptos.
        public void AsegurarErroresRestauracion()
        {
            var errores = new[]
            {
                new ErrorSistemaBE { Codigo = "ER046", Descripcion = "No se seleccionó un respaldo o el seleccionado ya no está disponible", Criticidad = "Advertencia" },
                new ErrorSistemaBE { Codigo = "ER047", Descripcion = "No se pudo completar la restauración de la base de datos", Criticidad = "Crítico" }
            };
            AsegurarErrores(errores);
        }

        public void AsegurarErroresIdioma()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE { Codigo = "ER048", Descripcion = "No se pudo cambiar el idioma. Intentá nuevamente.", Criticidad = "Grave" }
            });
        }

        public void AsegurarErroresRegistroCliente()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE { Codigo = "ER049", Descripcion = "No se pudo registrar el cliente", Criticidad = "Grave" }
            });
        }

        public void AsegurarErroresPlanes()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE { Codigo="ER050",Descripcion="El plan seleccionado ya no está disponible",Criticidad="Advertencia" },
                new ErrorSistemaBE { Codigo="ER051",Descripcion="No existen planes disponibles para contratar",Criticidad="Advertencia" },
                new ErrorSistemaBE { Codigo="ER052",Descripcion="No existen mascotas activas asociadas al cliente",Criticidad="Advertencia" },
                new ErrorSistemaBE { Codigo="ER053",Descripcion="Los datos de la contratación cambiaron. Volvé a consultar el plan.",Criticidad="Advertencia" }
            });
        }

        public void AsegurarErroresPago()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE{Codigo="ER054",Descripcion="La suscripción no admite un nuevo pago",Criticidad="Advertencia"},
                new ErrorSistemaBE{Codigo="ER055",Descripcion="No se pudo iniciar el procesamiento del pago",Criticidad="Grave"},
                new ErrorSistemaBE{Codigo="ER056",Descripcion="El pago está pendiente de confirmación. No vuelvas a pagar.",Criticidad="Advertencia"},
                new ErrorSistemaBE{Codigo="ER057",Descripcion="No se pudo registrar el pago. No vuelvas a pagar.",Criticidad="Grave"}
            });
        }
        public void AsegurarErroresCobertura()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE{Codigo="ER058",Descripcion="No se pudo consultar el servicio de cobertura",Criticidad="Grave"}
            });
        }
        public void AsegurarErroresReintegros()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE{Codigo="ER059",Descripcion="No se pudo consultar la información de reintegros",Criticidad="Grave"}
            });
        }
        public void AsegurarErroresRegistroMascota()
        {
            AsegurarErrores(new[] {new ErrorSistemaBE {Codigo="ER060",Descripcion="No se pudo registrar la mascota",Criticidad="Grave"}});
        }
        public void AsegurarErroresGestionMascotas()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE {Codigo="ER061",Descripcion="No se pudo consultar la información de mascotas",Criticidad="Grave"},
                new ErrorSistemaBE {Codigo="ER062",Descripcion="No se pudieron actualizar los datos de la mascota",Criticidad="Grave"},
                new ErrorSistemaBE {Codigo="ER063",Descripcion="No se pudo consultar el servicio de historial clínico",Criticidad="Grave"},
                new ErrorSistemaBE {Codigo="ER064",Descripcion="Los datos de la mascota cambiaron desde su consulta",Criticidad="Advertencia"}
            });
        }
        public void AsegurarErroresTurnos()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE {Codigo="ER065",Descripcion="No se pudo completar la operación del servicio de turnos",Criticidad="Grave"},
                new ErrorSistemaBE {Codigo="ER066",Descripcion="No se pudo registrar el turno veterinario",Criticidad="Grave"}
            });
        }
        public void AsegurarErroresEmpresas()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE {Codigo="ER067",Descripcion="No se pudo consultar la información de empresas",Criticidad="Grave"},
                new ErrorSistemaBE {Codigo="ER068",Descripcion="El CUIT ya se encuentra registrado para otra empresa",Criticidad="Advertencia"},
                new ErrorSistemaBE {Codigo="ER069",Descripcion="No se pudo registrar la empresa",Criticidad="Grave"},
                new ErrorSistemaBE {Codigo="ER070",Descripcion="No se pudieron actualizar los datos de la empresa",Criticidad="Grave"}
            });
        }
        public void AsegurarErroresAdministracionPlanes()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE {Codigo="ER075",Descripcion="No se pudo consultar la información del plan",Criticidad="Grave"},
                new ErrorSistemaBE {Codigo="ER076",Descripcion="No se pudo registrar el plan",Criticidad="Grave"},
                new ErrorSistemaBE {Codigo="ER077",Descripcion="No se pudieron actualizar los datos del plan",Criticidad="Grave"},
                new ErrorSistemaBE {Codigo="ER078",Descripcion="El nombre del plan ya se encuentra registrado",Criticidad="Advertencia"}
            });
        }
        public void AsegurarErroresClienteCorporativo()
        {
            AsegurarErrores(new[] {
                new ErrorSistemaBE {Codigo="ER072",Descripcion="La empresa seleccionada se encuentra inactiva",Criticidad="Advertencia"},
                new ErrorSistemaBE {Codigo="ER073",Descripcion="No se encuentra disponible el plan Corporativo",Criticidad="Advertencia"},
                new ErrorSistemaBE {Codigo="ER074",Descripcion="No se pudo consultar el correo o el plan Corporativo",Criticidad="Grave"}
            });
        }
        private void AsegurarErrores(IEnumerable<ErrorSistemaBE> errores)
        {
            using (var operacion = new OperacionIntegridadDAL())
            {
                var seguridad = new SecurityManager();
                seguridad.ExigirIntegridad();
                var tablas = new IntegridadTablasDAL();
                var integridad = new IntegridadTablasBLL();
                bool cambios = false;
                foreach (var error in errores)
                {
                    var existente = _errorSistemaDAL.ObtenerPorCodigo(error.Codigo);
                    if (existente != null)
                    {
                        if (existente.Descripcion != error.Descripcion || existente.Criticidad != error.Criticidad)
                            throw new InvalidOperationException("Código de error ocupado con otro mensaje: " + error.Codigo);
                        continue;
                    }
                    _errorSistemaDAL.Agregar(error);
                    var fila = tablas.Leer("ErrorSistema").Rows.Cast<DataRow>()
                        .Single(r => (string)r["codigo"] == error.Codigo);
                    tablas.GuardarDvh("ErrorSistema", error.Codigo, integridad.CalcularDVH(fila));
                    cambios = true;
                }
                if (cambios)
                {
                    new DVVDAL().Guardar(new DVVBE {
                        NombreTabla = "ErrorSistema",
                        ValorDVV = new IntegridadBLL().CalcularDVV(tablas.Leer("ErrorSistema")
                            .Rows.Cast<DataRow>().Select(r => (string)r["dvh"]))
                    });
                    new ReferenciaIntegridadBLL().GuardarEstadoVerificado();
                    seguridad.ExigirIntegridad();
                }
                operacion.Complete();
            }
        }

        public List<ErrorSistemaBE> ObtenerTodos()
        {
            return _errorSistemaDAL.ObtenerTodos();
        }

        public ErrorSistemaBE ObtenerPorCodigo(string codigo)
        {
            return _errorSistemaDAL.ObtenerPorCodigo(codigo);
        }
    }
}
