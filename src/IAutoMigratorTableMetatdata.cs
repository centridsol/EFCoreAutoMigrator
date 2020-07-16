namespace CentridNet.EFCoreAutoMigrator{
    public interface IAutoMigratorTableMetatdata{
        string GetDBMetadata();
        string GetGeneratedScriptMetatdata();
    }
}