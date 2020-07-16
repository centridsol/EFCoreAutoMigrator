
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

//TODO: Change namesapces
namespace CentridNet.EFCoreAutoMigrator.MigrationContexts{

    public class MigrationProviderFactory : IMigrationProviderFactory{


        public MigrationsProvider Build(DBMigratorProps dBMigratorProps, MigrationScriptExecutor migrationScriptExecutor){

            string extensionMethod =  $"{dBMigratorProps.dbContext.Database.ProviderName.Split('.').Last()}DBMigrations";

            List<MethodInfo> contextMigrationMethods = Utilities.Utilities.GetExtensionMethods(extensionMethod , typeof(DbContext)).ToList();
            
            if (contextMigrationMethods.Count() > 0){
                return (MigrationsProvider)contextMigrationMethods[0].Invoke(null, new object[] {dBMigratorProps.dbContext, dBMigratorProps, migrationScriptExecutor});
            }
            throw new InvalidOperationException($"The extension method '{extensionMethod}' for type {typeof(DbContext)} was not found");
        }

        
    }
}