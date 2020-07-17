# EFCoreAutoMigrator
#### Code driven auto-migrations for Entity Framework Core

If you are using Entity Framework Core (EFCore) and want to auto-migrate your database you might know that this is a bit challenging (as noted in this thread https://github.com/dotnet/efcore/issues/6214).
This library builds upon suggested comments from the above thread as to how to implement this. 
**Notes:**

* This library was created as part of a bigger project to meet our particular need. Extensive testing and optimization has not been done on it. Please use this library with caution.  
* This libray does not run manually created migration scripts for you (created via `dotnet ef migrations add ...`). For those you will still need to run `dotnet ef database update`. You can then switch over to migrating through EFCoreAutoMigrator once you run those migrations.


## Getting Started

### 1. Installation

You can either clone this project and add it to your project directly or install it via nuget (See https://www.nuget.org/packages/CentridNet.EFCoreAutoMigrator)


### 2. Integrating 

Once you have installed the package you can intergrate it by passing in your DbContext to the `EFCoreAutoMigrator` class (along with a logger). From there you can call `PrepareMigration()` which returns an instance of `MigrationScriptExecutor`. This instance is what you use to get the migration script to be executed (using `GetMigrationScript()`) and to execute it when ready (using `MigrateDB()`).

Below is a simple example of this based on the EFCore getting started tutorial found at https://docs.microsoft.com/en-us/ef/core/get-started

```c#
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
                    AutoMigrateMyDB(db).Wait();  
                }
            }

            public static async Task AutoMigrateMyDB(DbContext db){
                EFCoreAutoMigrator dbMigrator = new EFCoreAutoMigrator(db, new Logger());
                MigrationScriptExecutor migrationScriptExcutor = await dbMigrator.PrepareMigration();
                await migrationScriptExcutor.MigrateDB();
                Console.WriteLine("Migration Complete");
            }
        }
    }
```

Below is a more complex example that allows for a more user driven/conditional migration.
 
```c#
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
                    AutoMigrateMyDB(db).Wait();  
                }
            }

            public static async Task AutoMigrateMyDB(DbContext db){
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
                            Console.WriteLine("Completed. There was nothing to migrate.");
                        }
                        else if (result == MigrationResult.ErrorMigrating){
                            Console.WriteLine("Error occurred whilst migrating.");
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
```

## EFCoreAutoMigrator Configuration

### ShouldAllowDestructive

This is used to state whether desctructive migrations are allowed or not. If not, and exception will occur when you run the `PrepareMigration()` method. Default is false.

Usage example: 
```c#
    ...
    EFCoreAutoMigrator dbMigrator = new EFCoreAutoMigrator(db, new Logger())
                                        .ShouldAllowDestructive(true);
    ...
```
### SetSnapshotHistoryLimit

For every migration EFCoreAutoMigrator runs it saves a snapshot (and other corresponding metadata...See _SetMigrationTableMetadataClass_ below). Use this method to limt the number of snapshot saved. Default is unlimited 

Usage example: 
```c#
    ...
    EFCoreAutoMigrator dbMigrator = new EFCoreAutoMigrator(db, new Logger())
                                        .SetSnapshotHistoryLimit(1);
    ...
```

### SetMigrationTableMetadataClass

This method takes in a class that implements the `IAutoMigratorTableMetatdata` interface. This class is then used to set the metadata field in the auto-migration database table and add a comment to the top of the generated sql script. This is useful if you want to track additional information, for instance, your application version associated with the migration. A default class is used, if not set. 

Usage example: 
```c#
        class MyMigrationMetadata : IAutoMigratorTableMetatdata {
            public string GetDBMetadata()
            {
                return "MyAppVersion: 1.0.0";
            }

            public string GetGeneratedScriptMetatdata()
            {
                return $"This script was auto-generated by MyApp (Version 1.0.0)";
            }
        }
    ...
    EFCoreAutoMigrator dbMigrator = new EFCoreAutoMigrator(db, new Logger())
                                        .SetMigrationTableMetadataClass(new MyMigrationMetadata());
    ...
```

### SetMigrationProviderFactory

EFCoreAutoMigrator selects the appropiate migration provider (see below) for your connected database using the MigrationProviderFactory. If you want to replace this, use this method to pass in a class that implements the `IMigrationProviderFactory` interface. This is useful if you know exactly what database you will be using and want to remove the overhead associated with figuring out the database when migrating. 

Usage example: 
```c#
    public class MyMigrationProviderFactory : IMigrationProviderFactory{
        public MigrationsProvider Build(DBMigratorProps dBMigratorProps, MigrationScriptExecutor migrationScriptExecutor){

            if (dBMigratorProps.dbContext.Database.IsNpgsql()){
                return new PostgresMigrations(dBMigratorProps, migrationScriptExecutor);
            }
            return null;
        }        
    }
    ...
    EFCoreAutoMigrator dbMigrator = new EFCoreAutoMigrator(db, new Logger())
                                        .SetMigrationProviderFactory(new MyMigrationProviderFactory());
    ...
```

## Migration Providers

In the EFCoreAutoMigrator library, MigrationProviders are responsible for creating and interacting with the auto-migration tables for the various databases. EFCoreAutoMigrator comes with providers for Postgres, MSSQL, MySQL, and SQLLite baked in. 

If you want to create your own provider class (i.e for a particular database) you can do this by creating a class that inherits the `MigrationProvider` class. An example of this is below (For method code examples, look at the MigrationProviders already written. See [src/MigrationProviders/Providers](src/MigrationProviders/Providers)):

```c#
class MSSQLMigrations : MigrationsProvider
    {
        //TODO use CosmoDB
        public MSSQLMigrations(DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor) : base(dbMigratorProps, migrationScriptExecutor){}

        protected override void EnsureMigrateTablesExist(){...};

        protected override void EnsureSnapshotLimitNotReached(){...};

        protected override AutoMigratorTable GetLastMigrationRecord(){...};

        protected override void UpdateMigrationTables(byte[] snapshotData){...};
    }
```

Once you have created a MigrationProvider you will need to let EFCoreAutoMigrator know it exists. You can either do this by creating your own MigrationProviderFactory (as described above) or write a `DBContext` extension method with the format `[ProviderName]DBMigrations` for the method name. Using the example above this will be:

```c#
public static MigrationsProvider CosmoDBDBMigrations(this DbContext dbContext, DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor){   
            return new CosmoDBMigrations(dbMigratorProps, migrationScriptExecutor);
        }
```

## Known Issues

* There is currently a migration issue that occurs when you change the namespace(s) associated with your DBSets (Error messasge `Failed to compile previous snapshot`). Will look into this and should provide a fix soon.

----

## Contributing

EFCoreAutoMigrator is an opensource project and contributions are valued. If there is a bug fix please create a pull request explain what the bug is, how you fixed and tested it.

If it's a new feature, please add it as a issue with the label enhancement, detailing the new feature and why you think it's needed. Will discuss it there and once it's agreed upon you can create a pull request with the details highlighted above. 

## Authors

* **Chido Warambwa** - *Initial Work* - [chidow@centridsol.tech](mailto://chidow@centridsol.tech) 
  
## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## Thanks

Thanks to all those that have been contributing to the dicussion on this (see [(https://github.com/dotnet/efcore/issues/6214)](https://github.com/dotnet/efcore/issues/6214)) and in particular, a huge thanks Jeremy Lakeman's [code snippets](https://gist.github.com/lakeman/1509f790ead00a884961865b5c79b630) that became the starting of point of this libray.

