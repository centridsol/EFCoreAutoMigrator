using System;
using System.Threading.Tasks;
using CentridNet.EFCoreAutoMigrator;
using Microsoft.EntityFrameworkCore;

namespace EFCoreMigratorExample
{
    class Program
    {
        static void Main()
        {
            using (var db = new BloggingContext())
            {
               ManageDbMigrations(db).Wait();  
            }
        }

        public static async Task ManageDbMigrations(DbContext db){
            DBMigrator dbMigrator = new DBMigrator(db, new Logger());
            MigrationScriptExecutor migrationScriptExcutor = await dbMigrator.PrepareMigration();
            Console.WriteLine(migrationScriptExcutor.GetMigrationScript());
        }

    }
}
