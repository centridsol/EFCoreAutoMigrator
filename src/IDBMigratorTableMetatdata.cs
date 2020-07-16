namespace CentridNet.EFCoreAutoMigrator{
    public interface IDBMigratorTableMetatdata{
        string GetDBMetadata();
        string GetGeneratedScriptMetatdata();
    }
}