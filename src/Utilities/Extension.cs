using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Utilities;

//TODO: Conisder removing these as we are not using them
namespace CentridNet.EFCoreAutoMigrator.Utilities{

    public static class MigrationExtensions{ 
        public static DataTable ExecuteSqlRawWithoutModel(this DbContext dbContext, string query){

            DataTable dataTable = new DataTable();
            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                dbContext.Database.OpenConnection();

                using (var result = command.ExecuteReader())
                {
                    dataTable.Load(result);
                }

                dbContext.Database.CloseConnection();
            }
            return dataTable;
            
        }
        public static IList<T> ExecuteSqlRawWithoutModel<T>(this DbContext dbContext, string query, Func<DbDataReader, T> map){
            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                dbContext.Database.OpenConnection();

                using (var result = command.ExecuteReader())
                {
                    DataTable schemaTable = result.GetSchemaTable();

                    var entities = new List<T>();

                    while (result.Read())
                    {
                        entities.Add(map(result));
                    }
                    return entities;
                }
            }
        }
    }
} 