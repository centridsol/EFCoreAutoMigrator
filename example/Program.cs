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
            EFCoreAutoMigrator dbMigrator = new EFCoreAutoMigrator(db, new Logger());
            MigrationScriptExecutor migrationScriptExcutor = await dbMigrator.PrepareMigration();

            if (migrationScriptExcutor.HasMigrations()){
                Console.WriteLine("The program `Example` wants to run the following script on your database: ");
                Console.WriteLine("------");
                Console.WriteLine(migrationScriptExcutor.GetMigrationScript());
                Console.WriteLine("------");
                Console.WriteLine("Do you want (R)un it, (S)ave the script or (C)ancel. ?");

            }
            MigrationResult result = await migrationScriptExcutor.MigrateDB();
            if (result == MigrationResult.Migrated){
                Console.WriteLine("Completed succesfully.");
            }
            else if (result == MigrationResult.Noop){
                Console.WriteLine("Completed. These was nothing to migrate.");
            }
            else if (result == MigrationResult.ErrorMigrating){
                Console.WriteLine("Error occured whilst migrating.");
            }
        }

    }
}

