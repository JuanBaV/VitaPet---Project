-- Los procedimientos conservan sus controles de estado y pertenencia; el
-- acceso deja de depender del nombre fijo del rol y usa el mismo Composite.
CREATE OR ALTER FUNCTION dbo.fn_PerfilPermite(@perfil VARCHAR(30), @pantallas VARCHAR(1000))
RETURNS BIT
AS
BEGIN
    DECLARE @permitido BIT=0;
    WITH componentes AS (
        SELECT idComponente,pantalla FROM dbo.PermisoComponente WHERE nombre=@perfil AND tipo='Perfil'
        UNION ALL
        SELECT hijo.idComponente,hijo.pantalla FROM componentes padre
        INNER JOIN dbo.PermisoRelacion r ON r.idPadre=padre.idComponente
        INNER JOIN dbo.PermisoComponente hijo ON hijo.idComponente=r.idHijo
    )
    SELECT TOP(1) @permitido=1 FROM componentes
    WHERE CHARINDEX(','+pantalla+',',','+@pantallas+',')>0;
    RETURN @permitido;
END;
GO
DECLARE @mapa TABLE(procedimiento SYSNAME,pantallas VARCHAR(1000));
INSERT @mapa VALUES
('sp_Plan_ListarAdministracion','GestionPlanes'),
('sp_Plan_ConsultarAdministracion','GestionPlanes,ConsultaPlan,ModificarPlan'),
('sp_Plan_Registrar','AltaPlan'),('sp_Plan_Actualizar','ModificarPlan'),
('sp_Cobertura_GuardarAdministracion','AltaPlan,ModificarPlan'),
('sp_Empresa_Listar','Empresas,RegistroClienteCorporativo'),
('sp_Empresa_Consultar','Empresas,ConsultaEmpresa,ModificarEmpresa,RegistroClienteCorporativo'),
('sp_Empresa_CuitRegistrado','AltaEmpresa,ModificarEmpresa'),
('sp_Empresa_Registrar','AltaEmpresa'),('sp_Empresa_Actualizar','ModificarEmpresa'),
('sp_ClienteCorporativo_ConsultarPlan','RegistroClienteCorporativo'),
('sp_ClienteCorporativo_CorreoRegistrado','RegistroClienteCorporativo'),
('sp_ClienteCorporativo_RegistrarUsuario','RegistroClienteCorporativo'),
('sp_ClienteCorporativo_RegistrarCliente','RegistroClienteCorporativo'),
('sp_ClienteCorporativo_AsignarPlan','RegistroClienteCorporativo'),
('sp_Cliente_CotizarPlan','Planes,ContratarPlan'),
('sp_Suscripcion_PrepararContratacion','ContratarPlan'),
('sp_Pago_ConsultarPendiente','GestionPago'),
('sp_Cliente_ObtenerActivoUsuario','Cliente,Planes,ContratarPlan,GestionPago,RegistroMascota,Mascotas,ActualizarMascota,ConsultaMascota,HistorialClinico,DatosClinicos,Turnos,ConsultaCobertura,Reintegros'),
('sp_Mascota_Registrar','RegistroMascota'),
('sp_Mascota_ListarUsuario','Mascotas,Turnos'),
('sp_Mascota_ConsultarUsuario','Mascotas,ActualizarMascota,ConsultaMascota,HistorialClinico,DatosClinicos,Turnos'),
('sp_Mascota_Actualizar','ActualizarMascota'),
('sp_HistorialClinico_ConsultarMascota','HistorialClinico'),
('sp_DatosClinicos_ConsultarMascota','DatosClinicos'),
('sp_AgendaTurno_ListarZonas','Turnos'),('sp_Turno_ConsultarDisponibilidad','Turnos'),
('sp_Turno_VerificarDisponibilidad','Turnos'),('sp_Turno_Registrar','Turnos'),('sp_Turno_ConsultarMascota','Turnos');
DECLARE @nombre SYSNAME,@pantallas VARCHAR(1000),@definicion NVARCHAR(MAX);
DECLARE procedimientos CURSOR LOCAL FAST_FORWARD FOR SELECT procedimiento,pantallas FROM @mapa;
OPEN procedimientos;
FETCH NEXT FROM procedimientos INTO @nombre,@pantallas;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @definicion=OBJECT_DEFINITION(OBJECT_ID('dbo.'+@nombre));
    IF @definicion IS NULL THROW 50039,'Falta un procedimiento requerido para migrar permisos.',1;
    SET @definicion=REPLACE(@definicion,'CREATE OR ALTER PROCEDURE','ALTER PROCEDURE');
    SET @definicion=REPLACE(@definicion,'CREATE PROCEDURE','ALTER PROCEDURE');
    -- SQL Server persiste CREATE OR ALTER como CREATE seguido de espacios.
    IF CHARINDEX('CREATE',@definicion)>0 SET @definicion=STUFF(@definicion,CHARINDEX('CREATE',@definicion),6,'ALTER');
    SET @definicion=REPLACE(@definicion,'u.rol=''Cliente''','dbo.fn_PerfilPermite(u.rol,'''+@pantallas+''')=1');
    SET @definicion=REPLACE(@definicion,'rol=''Administrador''','dbo.fn_PerfilPermite(rol,'''+@pantallas+''')=1');
    EXEC sys.sp_executesql @definicion;
    FETCH NEXT FROM procedimientos INTO @nombre,@pantallas;
END;
CLOSE procedimientos;
DEALLOCATE procedimientos;
