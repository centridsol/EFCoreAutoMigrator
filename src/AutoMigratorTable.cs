using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CentridNet.EFCoreAutoMigrator{

    public class AutoMigratorTable{

        
        public int runId = -1;
        public DateTime? runDate = null;
        public string efcoreVersion = "";
        public string metadata = "";
        public byte [] snapshot;

        public AutoMigratorTable(IAutoMigratorTableMetatdata migratorTableMetatdata){
             metadata = migratorTableMetatdata.GetDBMetadata();
        }

        public AutoMigratorTable(){}
        
    }
}