using System;
using System.IO;
using System.IO.Compression;

namespace BillingSoftware.Backup;

public class BackupService
{
    public void CreateBackup(string dbFilePath, string targetZipPath)
    {
        if (!File.Exists(dbFilePath))
        {
            throw new FileNotFoundException("Database file not found.", dbFilePath);
        }

        string? targetDir = Path.GetDirectoryName(targetZipPath);
        if (targetDir != null && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Create temporary copy to avoid file-locks while database is active
        string tempCopy = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".db");
        File.Copy(dbFilePath, tempCopy, true);

        try
        {
            if (File.Exists(targetZipPath))
            {
                File.Delete(targetZipPath);
            }

            using (var zipArchive = ZipFile.Open(targetZipPath, ZipArchiveMode.Create))
            {
                // Add the database file
                zipArchive.CreateEntryFromFile(tempCopy, "billing.db", CompressionLevel.Optimal);
            }
        }
        finally
        {
            // Clean up temporary copy
            if (File.Exists(tempCopy))
            {
                File.Delete(tempCopy);
            }
        }
    }

    public void RestoreBackup(string zipFilePath, string targetDbFilePath)
    {
        if (!File.Exists(zipFilePath))
        {
            throw new FileNotFoundException("Backup ZIP file not found.", zipFilePath);
        }

        string? dbDir = Path.GetDirectoryName(targetDbFilePath);
        if (dbDir != null && !Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        // Safety backup of current database
        string safetyBackup = targetDbFilePath + ".bak";
        bool safetyCreated = false;

        if (File.Exists(targetDbFilePath))
        {
            if (File.Exists(safetyBackup))
            {
                File.Delete(safetyBackup);
            }
            File.Move(targetDbFilePath, safetyBackup);
            safetyCreated = true;
        }

        try
        {
            using (var zip = ZipFile.OpenRead(zipFilePath))
            {
                var dbEntry = zip.GetEntry("billing.db");
                if (dbEntry == null)
                {
                    throw new InvalidDataException("Invalid backup package. Database file is missing.");
                }

                dbEntry.ExtractToFile(targetDbFilePath, true);
            }

            // Success, remove safety backup
            if (safetyCreated && File.Exists(safetyBackup))
            {
                File.Delete(safetyBackup);
            }
        }
        catch
        {
            // Recovery: Roll back to safety backup if failed
            if (safetyCreated && File.Exists(safetyBackup))
            {
                if (File.Exists(targetDbFilePath))
                {
                    File.Delete(targetDbFilePath);
                }
                File.Move(safetyBackup, targetDbFilePath);
            }
            throw;
        }
    }
}
