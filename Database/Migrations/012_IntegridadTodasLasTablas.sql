-- Las demás tablas funcionales ya poseen DVH. La BLL inicializa los valores
-- sólo después de validar los controles previos, dentro de una transacción.
ALTER TABLE dbo.ErrorSistema ADD dvh VARCHAR(64) NOT NULL
    CONSTRAINT DF_ErrorSistema_dvh DEFAULT ('');
