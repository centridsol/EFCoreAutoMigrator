using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentridNet.EFCoreAutoMigrator.MigrationContexts{

    public interface IMigrationProviderFactory {
        MigrationsProvider Build(DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor);
        
    }
}