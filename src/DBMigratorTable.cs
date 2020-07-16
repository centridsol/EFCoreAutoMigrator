using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CentridNet.EFCoreAutoMigrator{

    public class DBMigratorTable{

        
        public int runId = -1;
        public DateTime? runDate = null;
        public string efcoreVersion = "";
        public string metadata = "";
        public byte [] snapshot;

        public DBMigratorTable(IDBMigratorTableMetatdata migratorTableMetatdata){
             metadata = migratorTableMetatdata.GetDBMetadata();
        }

        public DBMigratorTable(){}
        
    }
}