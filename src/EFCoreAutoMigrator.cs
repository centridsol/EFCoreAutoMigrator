using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using CentridNet.EFCoreAutoMigrator.MigrationContexts;
using CentridNet.EFCoreAutoMigrator.Utilities;
using Microsoft.EntityFrameworkCore.Migrations.Design;

//Base on code from lakeman. See https://gist.github.com/lakeman/1509f790ead00a884961865b5c79b630/ for reference.
namespace CentridNet.EFCoreAutoMigrator
{
    public class EFCoreAutoMigrator : IOperationReporter
    {
        private DBMigratorProps dbMigratorProps;
        public MigrationScriptExecutor migrationScriptExecutor;
         
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Allowing for code driven migrations")]
        public EFCoreAutoMigrator(DbContext _dbContext, ILogger _logger)
        {
            dbMigratorProps = new DBMigratorProps(){
                dbContext = _dbContext,
                logger = _logger,
                dbMigratorTableMetatdata = new DefaultMigrationMetadata(),
                migrationProviderFactory = new MigrationProviderFactory(),
                allowDestructive = false,
                snapshotHistoryLimit = -1
            };
            var migrationAssembly = _dbContext.GetService<IMigrationsAssembly>();
            DesignTimeServicesBuilder builder = new DesignTimeServicesBuilder(migrationAssembly.Assembly, Assembly.GetEntryAssembly(), this, null);
            var dbServices = builder.Build(_dbContext);

            var dependencies = dbServices.GetRequiredService<MigrationsScaffolderDependencies>(); 
            var migrationName = dependencies.MigrationsIdGenerator.GenerateId(Utilities.DalConsts.MIGRATION_NAME_PREFIX);

            dbMigratorProps.dbServices = dbServices;
            dbMigratorProps.migrationName = migrationName;
            
        }

        public EFCoreAutoMigrator ShouldAllowDestructive(bool shouldAllowDestruction){
            dbMigratorProps.allowDestructive = shouldAllowDestruction;
            return this;
        }

        public EFCoreAutoMigrator SetSnapshotHistoryLimit(int limit){
            dbMigratorProps.snapshotHistoryLimit = limit;
            return this;
        }
        public EFCoreAutoMigrator SetMigrationTableMetadataClass(IAutoMigratorTableMetatdata updateddbMigratorTableMetatdata){
            dbMigratorProps.dbMigratorTableMetatdata = updateddbMigratorTableMetatdata;
            return this;
        }
        public EFCoreAutoMigrator SetMigrationProviderFactory(IMigrationProviderFactory updateddbMigratorProviderFactory){
            dbMigratorProps.migrationProviderFactory = updateddbMigratorProviderFactory;
            return this;
        }
    
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Allowing for code driven migrations")]
        public async Task<MigrationScriptExecutor> PrepareMigration() 
        {           
            migrationScriptExecutor = new MigrationScriptExecutor(dbMigratorProps);
            
            var migrationAssembly = dbMigratorProps.dbContext.GetService<IMigrationsAssembly>();
            var designTimeModel = dbMigratorProps.dbContext.GetService<IDesignTimeModel>();
            var declaredMigrations = dbMigratorProps.dbContext.Database.GetMigrations().ToList();
            var appliedMigrations = (await dbMigratorProps.dbContext.Database.GetAppliedMigrationsAsync()).ToList();

            if (declaredMigrations.Except(appliedMigrations).Any()){
                    throw new InvalidOperationException("Pending migration scripts have been found. Please run those migrations first () before trying to use the DBMigrator.");
            }

            string dbMigratorRunMigrations = null;
            var lastRunMigration = appliedMigrations.LastOrDefault();
            if (lastRunMigration != null && declaredMigrations.Find(s=> string.Compare(s, lastRunMigration) == 0) == null){
                dbMigratorRunMigrations = lastRunMigration;
            }

            migrationScriptExecutor.EnsureMigrateTablesExist(); 

            ModelSnapshot modelSnapshot = null;

            if (dbMigratorRunMigrations != null)
            {
                AutoMigratorTable migrationRecord = migrationScriptExecutor.GetLastMigrationRecord();
                var source = await Utilities.Utilities.DecompressSource(migrationRecord.snapshot);

                if (source == null || !source.Contains(dbMigratorRunMigrations))
                    throw new InvalidOperationException($"Expected to find the source code of the {dbMigratorRunMigrations} ModelSnapshot stored in the database");

                try{
                    modelSnapshot = Utilities.Utilities.CompileSnapshot(migrationAssembly.Assembly, dbMigratorProps.dbContext, source);
                }
                catch(Exception ex){
                    throw new InvalidOperationException("Failed to compile previous snapshot. This usually occurs when you have changed the namespace(s) associates with your DBSets. To fix you will have to delete the table causing the problem in your database (see below).", ex);
                }
                
            }
            else
            {
                modelSnapshot = migrationAssembly.ModelSnapshot;
            }

            var snapshotModel = modelSnapshot?.Model;
            if (snapshotModel is IMutableModel mutableModel)
            {
                snapshotModel = mutableModel.FinalizeModel();
            }

            if (snapshotModel != null)
            {
                snapshotModel = dbMigratorProps.dbContext.GetService<IModelRuntimeInitializer>().Initialize(snapshotModel);
            }

            if (SetMigrationCommands(migrationAssembly.Assembly, snapshotModel?.GetRelationalModel(), designTimeModel.Model.GetRelationalModel())){
                await UpdateMigrationTables();
            }   
                
            return migrationScriptExecutor;
        }
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Allowing for code driven migrations")]
        private bool SetMigrationCommands(Assembly migrationAssembly, IRelationalModel oldModel, IRelationalModel newModel)
        {
            bool hasMigrations = false;
            var dependencies = dbMigratorProps.dbServices.GetRequiredService<MigrationsScaffolderDependencies>();
            
            if (oldModel == null)
            {
                 migrationScriptExecutor.AddSQLCommand(dbMigratorProps.dbContext.Database.GenerateCreateScript());
                 hasMigrations = true;
            }
            else
            {
                // apply fixes for upgrading between major / minor versions
                //oldModel = dependencies.SnapshotModelProcessor.Process(oldModel);

                var operations = dependencies.MigrationsModelDiffer
                    .GetDifferences(oldModel, newModel)
                    // Ignore all seed updates. Workaround for (https://github.com/aspnet/EntityFrameworkCore/issues/18943)
                    .Where(o => !(o is UpdateDataOperation))
                    .ToList();

                if (operations.Any())
                {
                    if (!dbMigratorProps.allowDestructive && operations.Any(o => o.IsDestructiveChange))
                        throw new InvalidOperationException(
                            "Automatic migration was not applied because it could result in data loss.");

                    var sqlGenerator = dbMigratorProps.dbContext.GetService<IMigrationsSqlGenerator>();
                    var commands = sqlGenerator.Generate(operations, dbMigratorProps.dbContext.Model);

                    foreach (MigrationCommand migrateCommand in commands)
                    {
                        migrationScriptExecutor.AddSQLCommand(migrateCommand.CommandText);
                    }

                    hasMigrations = true;
                }
            } 
            return hasMigrations;
        }

        private async Task UpdateMigrationTables(){
            var codeGen = dbMigratorProps.dbServices.GetRequiredService<MigrationsScaffolderDependencies>().MigrationsCodeGeneratorSelector.Select(null);
            var designTimeModel = dbMigratorProps.dbContext.GetService<IDesignTimeModel>();
            string modelSource =  codeGen.GenerateSnapshot("AutoMigrations", dbMigratorProps.dbContext.GetType(), $"Migration_{dbMigratorProps.migrationName}",
                                designTimeModel.Model);
            byte[] newSnapshotBinary = await Utilities.Utilities.CompressSource(modelSource);
            migrationScriptExecutor.UpdateMigrationTables(newSnapshotBinary);   
        }

        void IOperationReporter.WriteError(string message) => dbMigratorProps.logger.LogError(message);
        void IOperationReporter.WriteInformation(string message) => dbMigratorProps.logger.LogInformation(message);
        void IOperationReporter.WriteVerbose(string message) => dbMigratorProps.logger.LogTrace(message);
        void IOperationReporter.WriteWarning(string message) => dbMigratorProps.logger.LogWarning(message);

        private class DefaultMigrationMetadata : IAutoMigratorTableMetatdata {

            private string name;
            private string version;

            public DefaultMigrationMetadata(){
                AssemblyName assebmlyDetail = typeof(DefaultMigrationMetadata).Assembly.GetName();
                name = assebmlyDetail.Name;
                version = assebmlyDetail.Version.ToString();
            }
            public string GetDBMetadata()
            {
                return $"{name} (Version {version})";
            }

            public string GetGeneratedScriptMetatdata()
            {
                return $"This script was auto-generated by {name} (Version {version})";
            }
        }
    }

     public struct DBMigratorProps{
            public bool allowDestructive;
            public int snapshotHistoryLimit;
            public IAutoMigratorTableMetatdata dbMigratorTableMetatdata;
            public IMigrationProviderFactory migrationProviderFactory;
            public DbContext dbContext; 
            public ILogger logger;
            public IServiceProvider dbServices;
            public string  migrationName;
        }
}
