using System;
using System.IO;
using System.Reflection;

namespace CentridNet.EFCoreAutoMigrator.Utilities{

    //TODO: Change for public
    struct DalConsts{
        public static string MIGRATION_TABLE_NAME  = "__cnf_db_migrations";
        public static string MIGRATION_NAME_PREFIX  = "CNF_DAL_Migrator";
    }
   
}