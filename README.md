# EFCoreAutoMigrator
#### Code driven auto-migrations for Entity Framework Core

If you are using Entty Framework Core (EFCore) and want to auto-migrate your database you might know that this is a bit challenging (as can be noted in this thread ___).
This library builds upon work done by [lukema]() to provide for code driven auto-migrations. 

**Notes:**

* This library was create for a specific purpose to meet our need and has not been extensively tested/optimized, hence use in a production enviroment with caution.  
* This libray does not run manually created migration scripts for you (created via ). For those you will still need to run ___. 


## Getting Started

### 1. Installation

You can either clone this project and add it to you project directly or install it via nuget

`nuget `

### 2. Integrating 

Once you have installed the package you can intergrate it by passing you DbContext to the `EFCoreAutoMigrator(db)` class (along with a logger). From there you can call `PrepareMigration()` which return and instance of `MigrationScriptExecutor`. This instance is what you use to execute the migration when ready. This is done by calling `MigrateDB()`.

Below is an example of this based on the EFCore getting started tutorial found at ___

```c#
        using System;
        using System.Threading.Tasks;
        using Microsoft.EntityFrameworkCore;
        using CentridNet.EFCoreAutoMigrator;

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
            MigrationResult result = await migrationScriptExcutor.MigrateDB();
            if (result == MigrationResult.Migrated){
                Console.WriteLine("Completed succesfully.")
            }
            else if (result == MigrationResult.Noop){
                Console.WriteLine("Completed. These was nothing to migrate.")
            }
            else if (result == MigrationResult.ErrorMigrating){
                Console.WriteLine("Error occured whilst migrating.")
            }
        }
```

**A note of MigrateDB():**

EFCoreAutoMigrator migrated your database by first generating a complete script that it will run when migrating you database (this is done when you call `PrepareMigration()`). This means we can run `MigrateDB()` as a transactional process, which we do. If this fails, no changes will be made to you database, and you can still get the script it was trying to run. 

Below is a more user driven migration workflow example:

```c#
        using System;
        using System.Threading.Tasks;
        using Microsoft.EntityFrameworkCore;
        using CentridNet.EFCoreAutoMigrator;

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
                Console.WriteLine("The program `Example` want to run the following script on your database: ")
                Console.WriteLine("------")
                Console.WriteLine(migrationScriptExcutor.GetMigrationScript());
                Console.WriteLine("------")
                Console.WriteLine("Do you want us to run it?")

            }
            MigrationResult result = await dbMigrator.MigrateDB();
            if (result == MigrationResult.Migrated){
                Console.WriteLine("Completed succesfully.")
            }
            else if (result == MigrationResult.Noop){
                Console.WriteLine("Completed. These was nothing to migrate.")
            }
            else if (result == MigrationResult.ErrorMigrating){
                Console.WriteLine("Error occured whilst migrating.")
            }
        }
```
## EFCoreAutoMigrator Configuration

### ShouldAllowDestructive

This is used to state whether desctructive migrations are allowed or not. If not, and exception will occur when you run the `PrepareMigration()` command. Default is false.

Usage example: 
```c#
    ...
    EFCoreAutoMigrator dbMigrator = new EFCoreAutoMigrator(db, new Logger())
                                        .ShouldAllowDestructive(true);
    ...
```
### SetSnapshotHistoryLimit

For every migration EFCoreAutoMigrator it saves a snapshot (and other corresponding metadata...See _SetMigrationTableMetadataClass_ below). Use this method to limt the number of snapshot saved. Default is unlimited 

Usage example: 
```c#
    ...
    EFCoreAutoMigrator dbMigrator = new EFCoreAutoMigrator(db, new Logger())
                                        .SetSnapshotHistoryLimit(1);
    ...
```

### SetMigrationTableMetadataClass

This method takes in a class the implements the `IDBMigratorTableMetatdata` interface. This class is then used to set the metadata field in the auto-migration database table and top line comments on the generated sql script. This is useful if you want to track additional information, for instance, your application version associated with the migration. A default class is used if not set. 

Usage example: 
```c#
    private class MyMigrationMetadata : IDBMigratorTableMetatdata {
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

EFCoreAutoMigrator selects the appropiate migration provider (see below) for your connect database using the MigrationProviderFactory. Although we already handle, and other providers can be added (see below) there might be times you replace the factory. For instance if you know your are only going to use one database, and want to remove the overhead of searching for the appropiate provider. Use this method to pass in a class that implements the `IMigrationProviderFactory` interface.

Usage example: 
```c#
   public class MyMigrationProviderFactory : IMigrationProviderFactory{
        public MigrationsProvider Build(DBMigratorProps dBMigratorProps, MigrationScriptExecutor migrationScriptExecutor){

            if (dBMigratorProps.dbContext.IsNpgsql()){
                return PostgresMigrations(dBMigratorProps, migrationScriptExecutor);
            }
        }        
    }
    ...
    EFCoreAutoMigrator dbMigrator = new EFCoreAutoMigrator(db, new Logger())
                                        .SetMigrationProviderFactory(new MyMigrationProviderFactory());
    ...
```

## Migration Providers

In the EFCoreAutoMigrator library, MigrationProviders are what are reasonable for creating appropiate auto-migration tables for the various databases. EFCoreAutoMigrator comes with providers for Postgres, MSSQL, MySQL, and SQLLite baked in. 

If you want to create your own provider class (i.e for a particular database) you can by implememting a class the inherits the `MigrationProvider` class. An example of this is below (For code examples, look atthe MigrationProviders already written. See src/MigrationProviders):

```c#
class MSSQLMigrations : MigrationsProvider
    {
        //TODO use CosmoDB
        public MSSQLMigrations(DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor) : base(dbMigratorProps, migrationScriptExecutor){}

        protected override void EnsureMigrateTablesExist(){...};

        protected override void EnsureSnapshotLimitNotReached(){...};

        protected override DBMigratorTable GetLastMigrationRecord(){...};

        protected override void UpdateMigrationTables(byte[] snapshotData){...};
    }
```

Once you have created a MigrationProvider you will need to let EFCoreAutoMigrator know it exists. You can either do this by creating your own MigrationProviderFactory which implements `IMigrationProviderFactory` or write a `DBContext` extension method with the format `[ProviderName]DBMigrations` for the method name. Using the example above this will be 

```c#
public static MigrationsProvider CosmoDBDBMigrations(this DbContext dbContext, DBMigratorProps dbMigratorProps, MigrationScriptExecutor migrationScriptExecutor){   
            return new CosmoDBMigrations(dbMigratorProps, migrationScriptExecutor);
        }
```



----

## Contributing

EFCoreAutoMigrator is an opensource project and contributions are valued. If there is a bug fix please create a pull request explain what the bug is, and how you fixed and tested it.

If it's a new feature, please add it as a issue with the label enhancement, detailing the new feature and why you think it's needed. Will discuss it there and once it's agreed upon you can create a pull request with the details highlighted above. 

## Authors

* **Chido Warambwa** - *Initial Work* - [chidow@centridsol.tech](mailto://chidow@centridsol.tech) 
  
## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## Thanks

Thanks to all those that have been contributing to the dicussion on this (see ___) and in particular [lakem] contirbution of which most of the main code is based.





