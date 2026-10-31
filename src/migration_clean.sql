BEGIN TRANSACTION;
GO

ALTER TABLE [Orders] ADD [ReservationCustomerName] nvarchar(max) NULL;
GO

ALTER TABLE [Orders] ADD [ReservationCustomerPhone] nvarchar(max) NULL;
GO

CREATE TABLE [PosSettings] (
    [Id] int NOT NULL IDENTITY,
    [NameEn] nvarchar(max) NOT NULL,
    [NameAr] nvarchar(max) NOT NULL,
    [Value] bit NOT NULL,
    CONSTRAINT [PK_PosSettings] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260208152727_UpdateReservationFieldsAndAddPosSettings', N'8.0.8');
GO

COMMIT;
GO

