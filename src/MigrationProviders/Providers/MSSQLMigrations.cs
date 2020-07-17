using System;
using System.Collections.Generic;
using System.Data;
using CentridNet.EFCoreAutoMigrator.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentridNet.EFCoreAutoMigrator.MigrationContexts{
    public static class SqlServerMigrationsExtensions{
        public static MigrationsProvider SqlServerDBMigrations(this DbContext dbContext, DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor){   
            return new MSSQLMigrations(dbMigratorProps, migrationScriptExecutor);
        }
    }
    public class MSSQLMigrations : MigrationsProvider
    {
        public MSSQLMigrations(DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor) : base(dbMigratorProps, migrationScriptExecutor){}

        protected override void EnsureMigrateTablesExist()
        {
            DataTable resultDataTable = dbContext.ExecuteSqlRawWithoutModel($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_NAME = N'{DalConsts.MIGRATION_TABLE_NAME}';");

            if (resultDataTable.Rows.Count == 0 || !Convert.ToBoolean(resultDataTable.Rows[0][0])){
                migrationScriptExecutor.AddSQLCommand($@"CREATE TABLE {DalConsts.MIGRATION_TABLE_NAME} (
                    runId INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                    runDate DATETIME,
                    efcoreVersion VARCHAR (355) NOT NULL,
                    metadata TEXT,
                    snapshot VARBINARY(MAX) NOT NULL
                );");
            }
        }

        protected override void EnsureSnapshotLimitNotReached()
        {
            if (snapshotHistoryLimit > 0){
                migrationScriptExecutor.AddSQLCommand($"DELETE FROM {DalConsts.MIGRATION_TABLE_NAME} WHERE runId NOT IN (SELECT TOP {snapshotHistoryLimit-1} runId FROM {DalConsts.MIGRATION_TABLE_NAME} ORDER BY runId DESC);");
            }  
        }

        protected override AutoMigratorTable GetLastMigrationRecord()
        {
            IList<AutoMigratorTable> migrationMetadata = dbContext.ExecuteSqlRawWithoutModel<AutoMigratorTable>($"SELECT TOP 1 * FROM {DalConsts.MIGRATION_TABLE_NAME} ORDER BY runId DESC;", (dbDataReader) => {
                return new AutoMigratorTable(){
                    runId = (int)dbDataReader[0],
                    runDate = (DateTime)dbDataReader[1],
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
                                        (getdate(),
                                        '{typeof(DbContext).Assembly.GetName().Version.ToString()}',
                                        '{migrationMetadata.metadata}',
                                        {"0x"+BitConverter.ToString(snapshotData).Replace("-", "")});");
        }
    }
}