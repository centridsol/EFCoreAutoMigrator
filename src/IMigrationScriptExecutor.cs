using System.Threading.Tasks;
using CentridNet.EFCoreAutoMigrator.MigrationContexts;

namespace CentridNet.EFCoreAutoMigrator{

    interface IMigrationScriptExecutor{
        void AddSQLCommand(string command, params object[] parameters);
        void AddText(string sqltext);
        bool HasMigrations();
        Task<MigrationResult> MigrateDB();
        string GetMigrationScript();
        void EnsureMigrateTablesExist();
        DBMigratorTable GetLastMigrationRecord();
        void UpdateMigrationTables(byte[] updatedSnapShot);
        

    }
}