using System;
using System.Collections.Generic;
using System.Data;
using CentridNet.EFCoreAutoMigrator.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentridNet.EFCoreAutoMigrator.MigrationContexts{

    public static class SqliteMigrationsExtensions{
        public static MigrationsProvider SqliteDBMigrations(this DbContext dbContext, DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor){   
            return new SQLiteMigrations(dbMigratorProps, migrationScriptExecutor);
        }
    }
    public class SQLiteMigrations : MigrationsProvider
    {
        public SQLiteMigrations(DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor) : base(dbMigratorProps, migrationScriptExecutor){}

        protected override void EnsureMigrateTablesExist()
        {
            DataTable resultDataTable = dbContext.ExecuteSqlRawWithoutModel($"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name= '{DalConsts.MIGRATION_TABLE_NAME}';");

            if (resultDataTable.Rows.Count == 0 || !Convert.ToBoolean(resultDataTable.Rows[0][0])){
                migrationScriptExecutor.AddSQLCommand($@"CREATE TABLE {DalConsts.MIGRATION_TABLE_NAME} (
                    runId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    runDate TEXT NOT NULL,
                    efcoreVersion TEXT NOT NULL,
                    metadata TEXT,
                    snapshot BLOB NOT NULL
                );");
            }
        }

        protected override void EnsureSnapshotLimitNotReached()
        {
            if (snapshotHistoryLimit > 0){
                migrationScriptExecutor.AddSQLCommand($"DELETE FROM {DalConsts.MIGRATION_TABLE_NAME} WHERE runId NOT IN (SELECT runId FROM {DalConsts.MIGRATION_TABLE_NAME} ORDER BY runId DESC LIMIT {snapshotHistoryLimit-1});");
            }  
        }

        protected override AutoMigratorTable GetLastMigrationRecord()
        {
            IList<AutoMigratorTable> migrationMetadata = dbContext.ExecuteSqlRawWithoutModel<AutoMigratorTable>($"SELECT * FROM {DalConsts.MIGRATION_TABLE_NAME} ORDER BY runId DESC LIMIT 1;", (dbDataReader) => {
                return new AutoMigratorTable(){
                    runId = Convert.ToInt32(dbDataReader[0]),
                    runDate = DateTime.Parse((string)dbDataReader[1]),
                    efcoreVersion = (string)dbDataReader[2],
                    metadata = (string)dbDataReader[3],
                    snapshot = (byte[])dbDataReader[4]
                };
            });

            if (migrationMetadata.Count >0){
                return migrationMetadata[0];
            }
            return null;
        }

        protected override void UpdateMigrationTables(byte[] snapshotData)
        {
             migrationScriptExecutor.AddSQLCommand($@"INSERT INTO {DalConsts.MIGRATION_TABLE_NAME}  (
                                                    runDate,
                                                    efcoreVersion,
                                                    metadata,
                                                    snapshot
                                                    ) 
                                                    VALUES
                                                    (strftime('%Y-%m-%d %H:%M','now'),
                                                    '{typeof(DbContext).Assembly.GetName().Version.ToString()}',
                                                    '{migrationMetadata.metadata}',
                                                    x'{BitConverter.ToString(snapshotData).Replace("-", "")}');");
        }
    }
}