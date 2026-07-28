IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Specialities] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Specialities] PRIMARY KEY ([Id])
);

CREATE TABLE [Doctors] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [LicenseNumber] nvarchar(max) NOT NULL,
    [deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [SpecialityId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Doctors_Specialities_SpecialityId] FOREIGN KEY ([SpecialityId]) REFERENCES [Specialities] ([Id])
);

CREATE INDEX [IX_Doctors_SpecialityId] ON [Doctors] ([SpecialityId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260712205725_InitialDomainModel', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Patients] (
    [Id] uniqueidentifier NOT NULL,
    [Dni] bigint NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [ApplicationUserId] nvarchar(450) NOT NULL,
    [deleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Patients] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Patients_Dni] ON [Patients] ([Dni]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260713014319_AddPatient', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Doctors] DROP CONSTRAINT [FK_Doctors_Specialities_SpecialityId];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Specialities]') AND [c].[name] = N'Name');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Specialities] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Specialities] ALTER COLUMN [Name] nvarchar(100) NOT NULL;

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Specialities]') AND [c].[name] = N'Description');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Specialities] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Specialities] ALTER COLUMN [Description] nvarchar(100) NOT NULL;

ALTER TABLE [Specialities] ADD [Deleted] bit NOT NULL DEFAULT CAST(0 AS bit);

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Doctors]') AND [c].[name] = N'Name');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Doctors] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Doctors] ALTER COLUMN [Name] nvarchar(100) NOT NULL;

CREATE TABLE [AvailabilityRule] (
    [Id] uniqueidentifier NOT NULL,
    [DoctorId] uniqueidentifier NOT NULL,
    [Month] tinyint NOT NULL,
    [Year] smallint NOT NULL,
    [DayOfWeek] tinyint NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [Deleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AvailabilityRule] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AvailabilityRule_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [AvailabilitySlot] (
    [Id] uniqueidentifier NOT NULL,
    [AvailabilityRuleId] uniqueidentifier NOT NULL,
    [Date] date NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Deleted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AvailabilitySlot] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId] FOREIGN KEY ([AvailabilityRuleId]) REFERENCES [AvailabilityRule] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Specialities_Name] ON [Specialities] ([Name]) WHERE [Deleted] = 0;

CREATE UNIQUE INDEX [IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime] ON [AvailabilityRule] ([DoctorId], [Year], [Month], [DayOfWeek], [StartTime], [EndTime]);

CREATE UNIQUE INDEX [IX_AvailabilitySlot_AvailabilityRuleId_Date_StartTime] ON [AvailabilitySlot] ([AvailabilityRuleId], [Date], [StartTime]);

ALTER TABLE [Doctors] ADD CONSTRAINT [FK_Doctors_Specialities_SpecialityId] FOREIGN KEY ([SpecialityId]) REFERENCES [Specialities] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725022841_AddAvailabilityModule', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AvailabilitySlot] DROP CONSTRAINT [FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId];

DROP INDEX [IX_AvailabilitySlot_AvailabilityRuleId_Date_StartTime] ON [AvailabilitySlot];

DROP INDEX [IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime] ON [AvailabilityRule];

EXEC sp_rename N'[AvailabilitySlot].[Date]', N'SlotDate', 'COLUMN';

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AvailabilitySlot]') AND [c].[name] = N'Deleted');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [AvailabilitySlot] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [AvailabilitySlot] ADD DEFAULT CAST(0 AS bit) FOR [Deleted];

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AvailabilityRule]') AND [c].[name] = N'Deleted');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [AvailabilityRule] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [AvailabilityRule] ADD DEFAULT CAST(0 AS bit) FOR [Deleted];

CREATE UNIQUE INDEX [IX_AvailabilitySlot_AvailabilityRuleId_SlotDate_StartTime] ON [AvailabilitySlot] ([AvailabilityRuleId], [SlotDate], [StartTime]) WHERE [Deleted] = 0;

CREATE UNIQUE INDEX [IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime] ON [AvailabilityRule] ([DoctorId], [Year], [Month], [DayOfWeek], [StartTime], [EndTime]) WHERE [Deleted] = 0;

ALTER TABLE [AvailabilitySlot] ADD CONSTRAINT [FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId] FOREIGN KEY ([AvailabilityRuleId]) REFERENCES [AvailabilityRule] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725033452_UpdateAvailabilityConstraints', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_AvailabilitySlot_AvailabilityRuleId_SlotDate_StartTime] ON [AvailabilitySlot];

ALTER TABLE [AvailabilitySlot] ADD [DoctorId] uniqueidentifier NULL;

UPDATE slot
SET slot.DoctorId = rule.DoctorId
FROM AvailabilitySlot AS slot
INNER JOIN AvailabilityRule AS rule
    ON slot.AvailabilityRuleId = rule.Id;

DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AvailabilitySlot]') AND [c].[name] = N'DoctorId');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [AvailabilitySlot] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [AvailabilitySlot] ALTER COLUMN [DoctorId] uniqueidentifier NOT NULL;

CREATE INDEX [IX_AvailabilitySlot_AvailabilityRuleId] ON [AvailabilitySlot] ([AvailabilityRuleId]);

CREATE UNIQUE INDEX [IX_AvailabilitySlot_DoctorId_SlotDate_StartTime] ON [AvailabilitySlot] ([DoctorId], [SlotDate], [StartTime]) WHERE [Deleted] = 0;

ALTER TABLE [AvailabilitySlot] ADD CONSTRAINT [FK_AvailabilitySlot_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725143351_AddDoctorIdToAvailabilitySlot', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [AvailabilitySlot] DROP CONSTRAINT [FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId];

ALTER TABLE [AvailabilitySlot] DROP CONSTRAINT [FK_AvailabilitySlot_Doctors_DoctorId];

DROP INDEX [IX_AvailabilitySlot_AvailabilityRuleId] ON [AvailabilitySlot];

EXEC sp_rename N'[AvailabilitySlot].[IX_AvailabilitySlot_DoctorId_SlotDate_StartTime]', N'UX_AvailabilitySlot_Doctor_Date_StartTime', 'INDEX';

EXEC sp_rename N'[AvailabilityRule].[IX_AvailabilityRule_DoctorId_Year_Month_DayOfWeek_StartTime_EndTime]', N'UX_AvailabilityRule_ActiveRule', 'INDEX';

DROP INDEX [UX_AvailabilitySlot_Doctor_Date_StartTime] ON [AvailabilitySlot];
DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AvailabilitySlot]') AND [c].[name] = N'StartTime');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [AvailabilitySlot] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [AvailabilitySlot] ALTER COLUMN [StartTime] time(0) NOT NULL;
CREATE UNIQUE INDEX [UX_AvailabilitySlot_Doctor_Date_StartTime] ON [AvailabilitySlot] ([DoctorId], [SlotDate], [StartTime]) WHERE [Deleted] = 0;

DECLARE @var7 nvarchar(max);
SELECT @var7 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AvailabilitySlot]') AND [c].[name] = N'EndTime');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [AvailabilitySlot] DROP CONSTRAINT ' + @var7 + ';');
ALTER TABLE [AvailabilitySlot] ALTER COLUMN [EndTime] time(0) NOT NULL;

DROP INDEX [UX_AvailabilityRule_ActiveRule] ON [AvailabilityRule];
DECLARE @var8 nvarchar(max);
SELECT @var8 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AvailabilityRule]') AND [c].[name] = N'StartTime');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [AvailabilityRule] DROP CONSTRAINT ' + @var8 + ';');
ALTER TABLE [AvailabilityRule] ALTER COLUMN [StartTime] time(0) NOT NULL;
CREATE UNIQUE INDEX [UX_AvailabilityRule_ActiveRule] ON [AvailabilityRule] ([DoctorId], [Year], [Month], [DayOfWeek], [StartTime], [EndTime]) WHERE [Deleted] = 0;

DROP INDEX [UX_AvailabilityRule_ActiveRule] ON [AvailabilityRule];
DECLARE @var9 nvarchar(max);
SELECT @var9 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AvailabilityRule]') AND [c].[name] = N'EndTime');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [AvailabilityRule] DROP CONSTRAINT ' + @var9 + ';');
ALTER TABLE [AvailabilityRule] ALTER COLUMN [EndTime] time(0) NOT NULL;
CREATE UNIQUE INDEX [UX_AvailabilityRule_ActiveRule] ON [AvailabilityRule] ([DoctorId], [Year], [Month], [DayOfWeek], [StartTime], [EndTime]) WHERE [Deleted] = 0;

ALTER TABLE [AvailabilityRule] ADD CONSTRAINT [AK_AvailabilityRule_Id_DoctorId] UNIQUE ([Id], [DoctorId]);

CREATE INDEX [IX_AvailabilitySlot_AvailabilityRuleId_DoctorId] ON [AvailabilitySlot] ([AvailabilityRuleId], [DoctorId]);

ALTER TABLE [AvailabilitySlot] ADD CONSTRAINT [CK_AvailabilitySlot_Status] CHECK ([Status] IN ('AVAILABLE', 'BOOKED', 'BLOCKED'));

ALTER TABLE [AvailabilitySlot] ADD CONSTRAINT [CK_AvailabilitySlot_TimeRange] CHECK ([StartTime] < [EndTime]);

ALTER TABLE [AvailabilityRule] ADD CONSTRAINT [CK_AvailabilityRule_DayOfWeek] CHECK ([DayOfWeek] BETWEEN 0 AND 6);

ALTER TABLE [AvailabilityRule] ADD CONSTRAINT [CK_AvailabilityRule_Month] CHECK ([Month] BETWEEN 1 AND 12);

ALTER TABLE [AvailabilityRule] ADD CONSTRAINT [CK_AvailabilityRule_TimeRange] CHECK ([StartTime] < [EndTime]);

ALTER TABLE [AvailabilitySlot] ADD CONSTRAINT [FK_AvailabilitySlot_AvailabilityRule_AvailabilityRuleId_DoctorId] FOREIGN KEY ([AvailabilityRuleId], [DoctorId]) REFERENCES [AvailabilityRule] ([Id], [DoctorId]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725170130_UpdateAvailabilityConstraintsV2', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var10 nvarchar(max);
SELECT @var10 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Specialities]') AND [c].[name] = N'Deleted');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Specialities] DROP CONSTRAINT ' + @var10 + ';');
ALTER TABLE [Specialities] ADD DEFAULT CAST(0 AS bit) FOR [Deleted];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725190443_RepairSpecialityConfiguration', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
DROP INDEX [IX_Doctors_SpecialityId] ON [Doctors];
DECLARE @var11 nvarchar(max);
SELECT @var11 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Doctors]') AND [c].[name] = N'SpecialityId');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Doctors] DROP CONSTRAINT ' + @var11 + ';');
UPDATE [Doctors] SET [SpecialityId] = '00000000-0000-0000-0000-000000000000' WHERE [SpecialityId] IS NULL;
ALTER TABLE [Doctors] ALTER COLUMN [SpecialityId] uniqueidentifier NOT NULL;
ALTER TABLE [Doctors] ADD DEFAULT '00000000-0000-0000-0000-000000000000' FOR [SpecialityId];
CREATE INDEX [IX_Doctors_SpecialityId] ON [Doctors] ([SpecialityId]);

DECLARE @var12 nvarchar(max);
SELECT @var12 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Doctors]') AND [c].[name] = N'LicenseNumber');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Doctors] DROP CONSTRAINT ' + @var12 + ';');
ALTER TABLE [Doctors] ALTER COLUMN [LicenseNumber] nvarchar(50) NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260725194952_UpdateDoctorConstraints', N'10.0.9');

COMMIT;
GO

