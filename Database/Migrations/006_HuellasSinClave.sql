CREATE TABLE dbo.__FormatoIntegridad
(
    id TINYINT NOT NULL CONSTRAINT PK_FormatoIntegridad PRIMARY KEY,
    formato VARCHAR(20) NOT NULL,
    CONSTRAINT CK_FormatoIntegridad_Id CHECK (id = 1)
);

INSERT INTO dbo.__FormatoIntegridad (id, formato) VALUES (1, 'pendiente');
