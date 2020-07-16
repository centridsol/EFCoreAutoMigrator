using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CentridNet.EFCoreAutoMigrator.MigrationContexts;
using CentridNet.EFCoreAutoMigrator.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CentridNet.EFCoreAutoMigrator
{
    //TODO: Check
    public enum MigrationResult
    {
        Noop,
        Migrated,
        ErrorMigrating
    }

    public class MigrationScriptExecutor : IMigrationScriptExecutor{

        private MigrationsProvider contextMigrator;
        DBMigratorProps dBMigratorProps;
        private IList<MigrationSQLCommand> commandsList = new List<MigrationSQLCommand>();
        public MigrationResult executionResult = MigrationResult.Noop;

        public MigrationScriptExecutor(DBMigratorProps _dBMigratorProps){
            dBMigratorProps = _dBMigratorProps;
            contextMigrator = dBMigratorProps.migrationProviderFactory.Build(_dBMigratorProps, this);  
        }

        //TODO: Consider making these methods internal
        public void AddSQLCommand(string _command, params object[] _parameters){
            commandsList.Add(new MigrationSQLCommand(){
                sqlCommand = _command,
                parameters = _parameters,
                commandType = MigrationSQLCommandType.command
            });
        }

        public void AddText(string sqltext){
             commandsList.Add(new MigrationSQLCommand(){
                sqlCommand = sqltext,
                commandType = MigrationSQLCommandType.text
            });
        }

        public bool HasMigrations(){
            return commandsList.Where(x => x.commandType !=  MigrationSQLCommandType.text).Count() > 0;
        }

        public async Task<MigrationResult> MigrateDB(){
            if (HasMigrations()){

                using (IDbContextTransaction transaction = dBMigratorProps.dbContext.Database.BeginTransaction()){
                    try{
                        var builder = new StringBuilder();
                        foreach (var command in commandsList)
                        {
                            if(command.commandType == MigrationSQLCommandType.command){
                                builder.Append(command.sqlCommand)
                                    .AppendLine();
                            }
                        }

                        await dBMigratorProps.dbContext.Database.ExecuteSqlRawAsync(builder.ToString());
                        transaction.Commit();
                        return MigrationResult.Migrated;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        dBMigratorProps.logger.LogError($"Error occured will migrating database. Error message {ex.Message}. StackTrace {ex.StackTrace}");
                        return MigrationResult.ErrorMigrating;
                    }
                }

            }
            else {
                return MigrationResult.Noop;
            }

        }

        public string GetMigrationScript()
        {
            var builder = new StringBuilder();
            builder.Append($"/* {dBMigratorProps.dbMigratorTableMetatdata.GetGeneratedScriptMetatdata()} */").AppendLine();
            foreach (var command in commandsList)
            {
                if (command.commandType == MigrationSQLCommandType.text){
                    builder.Append($"/* {command.sqlCommand} */").AppendLine();
                }
                else if(command.commandType == MigrationSQLCommandType.command){
                    builder.Append(command.sqlCommand)
                                        .AppendLine();
                }

                
            }

            return builder.ToString();
        }

        public void EnsureMigrateTablesExist()
        {
            contextMigrator._EnsureMigrateTablesExist();
        }

        public AutoMigratorTable GetLastMigrationRecord()
        {
            return contextMigrator._GetLastMigrationRecord();
        }

        public void UpdateMigrationTables(byte[] updatedSnapShot)
        {
            contextMigrator._UpdateMigrationTables(updatedSnapShot);
        }

        public enum MigrationSQLCommandType
        {
            command,
            text
        }
        public struct MigrationSQLCommand{
            public string sqlCommand;
            public object[] parameters;
            public MigrationSQLCommandType commandType;
        };
    }
}