using System;
using System.Collections.Generic;
using System.Data;
using CentridNet.EFCoreAutoMigrator.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CentridNet.EFCoreAutoMigrator.MigrationContexts{

    public static class MySqlMigrationsExtensions{
        public static MigrationsProvider MySqlDBMigrations(this DbContext dbContext, DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor){   
            return new MySQLMigrations(dbMigratorProps, migrationScriptExecutor);
        }
    }
    public class MySQLMigrations : MigrationsProvider
    {
        public MySQLMigrations(DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor) : base(dbMigratorProps, migrationScriptExecutor){}
        protected override void EnsureMigrateTablesExist()
        {
            DataTable resultDataTable = dbContext.ExecuteSqlRawWithoutModel($"SELECT COUNT(*) FROM information_schema.tables WHERE table_name = '{DalConsts.MIGRATION_TABLE_NAME}';");

            if (resultDataTable.Rows.Count == 0 || !Convert.ToBoolean(resultDataTable.Rows[0][0])){
                migrationScriptExecutor.AddSQLCommand($@"CREATE TABLE {DalConsts.MIGRATION_TABLE_NAME} (
                    runId INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    runDate TIMESTAMP,
                    efcoreVersion VARCHAR (355) NOT NULL,
                    metadata TEXT,
                    snapshot LONGBLOB NOT NULL
                );");
            }

        }
        protected override void EnsureSnapshotLimitNotReached()
        {
            if (snapshotHistoryLimit > 0){
                migrationScriptExecutor.AddSQLCommand($"DELETE FROM {DalConsts.MIGRATION_TABLE_NAME} WHERE runId NOT IN (SELECT * FROM (SELECT runId FROM {DalConsts.MIGRATION_TABLE_NAME} ORDER BY runId DESC LIMIT {snapshotHistoryLimit-1}) as t) ORDER BY runId ASC;");
            }  
        }
        protected override AutoMigratorTable GetLastMigrationRecord()
        {
            IList<AutoMigratorTable> migrationMetadata = dbContext.ExecuteSqlRawWithoutModel<AutoMigratorTable>($"SELECT * FROM {DalConsts.MIGRATION_TABLE_NAME} ORDER BY runId DESC LIMIT 1;", (dbDataReader) => {
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
                                                    (NOW(),
                                                    '{typeof(DbContext).Assembly.GetName().Version.ToString()}',
                                                    '{migrationMetadata.metadata}',
                                                     X'{BitConverter.ToString(snapshotData).Replace("-", "")}');");
        }
    }
}