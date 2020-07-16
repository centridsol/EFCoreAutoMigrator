using System;
using System.IO;
using System.Threading.Tasks;
using CentridNet.EFCoreAutoMigrator;
using Microsoft.EntityFrameworkCore;

namespace EFCoreAutoMigratorExample
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

            // Checking if there are migrations
            if (migrationScriptExcutor.HasMigrations()){
                Console.WriteLine("The program `Example` wants to run the following script on your database: ");
                Console.WriteLine("------");

                // Printing out the script to be run if they are
                Console.WriteLine(migrationScriptExcutor.GetMigrationScript());
                Console.WriteLine("------");

                Console.WriteLine("Do you want (R)un it, (S)ave the script or (C)ancel. ?");
                string userInput = Console.ReadLine();
                if (userInput.Length == 0){
                    Console.WriteLine("No value entered. Exiting...");
                    Environment.Exit(0);
                }
                if (userInput[0] == 'R'){
                    // Migrating
                    MigrationResult result = await migrationScriptExcutor.MigrateDB();
                    if (result == MigrationResult.Migrated){
                        Console.WriteLine("Completed succesfully.");
                    }
                    else if (result == MigrationResult.Noop){
                        Console.WriteLine("Completed. These was nothing to migrate.");
                    }
                    else if (result == MigrationResult.ErrorMigrating){
                        Console.WriteLine("Error occured whilst migrating. No changes were made to the database");
                    }
                }
                else if (userInput[0] == 'S'){
                    using (StreamWriter writer = new StreamWriter(Path.Join(Environment.CurrentDirectory,"ERCoreAutoMigratorGenetaedScript.sql"))) 
                    {  
                        writer.WriteLine(migrationScriptExcutor.GetMigrationScript());
                        Console.WriteLine("Migration script saved succefully.");
                    } 
                }
            }
            else{
                Console.WriteLine("Completed. There was nothing to migrate.");
            }
            
        }
    }
}

