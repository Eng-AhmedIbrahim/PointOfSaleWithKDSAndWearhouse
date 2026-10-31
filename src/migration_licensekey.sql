-- Migration: AddLicenseKeyToLicensedDevice
-- Run this script once on your database when the SQL Server is running.
-- It is idempotent (safe to run multiple times).

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'LicensedDevices' AND COLUMN_NAME = 'LicenseKey'
)
BEGIN
    ALTER TABLE [LicensedDevices]
    ADD [LicenseKey] NVARCHAR(500) NULL;

    PRINT 'Column LicenseKey added to LicensedDevices successfully.';
END
ELSE
BEGIN
    PRINT 'Column LicenseKey already exists in LicensedDevices — skipped.';
END
