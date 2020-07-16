using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentridNet.EFCoreAutoMigrator.MigrationContexts{

    public class MSSQLMigrations : MigrationsProvider
    {
        public MSSQLMigrations(DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor) : base(dbMigratorProps, migrationScriptExecutor){}

        protected override void EnsureMigrateTablesExist()
        {
            throw new System.NotImplementedException();
        }

        protected override void EnsureSnapshotLimitNotReached()
        {
            throw new NotImplementedException();
        }

        protected override AutoMigratorTable GetLastMigrationRecord()
        {
            throw new System.NotImplementedException();
        }

        protected override void UpdateMigrationTables(byte[] snapshotData)
        {
            throw new System.NotImplementedException();
        }
    }
}