using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CentridNet.EFCoreAutoMigrator.MigrationContexts{

    public abstract class MigrationsProvider {
        protected DbContext dbContext;
        protected DBMigratorTable migrationMetadata;
        protected IServiceProvider dbServices;
        protected string migrationName;
        protected dynamic migrationScriptExecutor;
        protected dynamic dbMigrateDependencies;
        protected int snapshotHistoryLimit;
        
    
        public MigrationsProvider(DBMigratorProps dbMigratorProps, MigrationScriptExecutor _migrationScriptExecutor){
            dbContext = dbMigratorProps.dbContext;
            dbServices = dbMigratorProps.dbServices;
            migrationName = dbMigratorProps.migrationName;
            snapshotHistoryLimit = dbMigratorProps.snapshotHistoryLimit;
            migrationScriptExecutor = _migrationScriptExecutor;
            dbMigrateDependencies = dbServices.GetRequiredService<MigrationsScaffolderDependencies>(); 
            migrationMetadata = new DBMigratorTable(dbMigratorProps.dbMigratorTableMetatdata);
        }
        protected void CreateEFHistoryTable(){
            if (!dbMigrateDependencies.HistoryRepository.Exists()){
                migrationScriptExecutor.AddText("Creating migration history tables");
                migrationScriptExecutor.AddSQLCommand(dbMigrateDependencies.HistoryRepository.GetCreateScript());
            }
            
        }
        protected void AddEFHistoryRecord(){
            var insert = dbMigrateDependencies.HistoryRepository.GetInsertScript(
                    new HistoryRow(
                        migrationName,
                        typeof(DbContext).Assembly.GetName().Version.ToString()
                    ));
            migrationScriptExecutor.AddSQLCommand(insert);
        }
        protected abstract void EnsureMigrateTablesExist();
        protected abstract void EnsureSnapshotLimitNotReached();
        protected abstract void  UpdateMigrationTables(byte [] snapshotData);
        protected abstract DBMigratorTable GetLastMigrationRecord();

        public  void _EnsureMigrateTablesExist(){
            CreateEFHistoryTable();
            EnsureMigrateTablesExist();
            migrationScriptExecutor.AddText("DB Context Migrations");
        }
        public void  _UpdateMigrationTables(byte [] snapshotData){
            migrationScriptExecutor.AddText("Updating migration history tables");
            EnsureSnapshotLimitNotReached();
            UpdateMigrationTables(snapshotData);
            AddEFHistoryRecord(); 
        }
        public  DBMigratorTable _GetLastMigrationRecord(){
            return GetLastMigrationRecord();
        }
        
    }
}
