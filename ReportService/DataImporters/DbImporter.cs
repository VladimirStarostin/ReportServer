﻿using System.Collections.Generic;
using Gerakul.FastSql;
using Newtonsoft.Json;
using ReportService.Interfaces;

namespace ReportService.DataImporters
{
    public class DbImporter : IDataImporter
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string DataSetName { get; set; }
        private readonly string connectionString;
        private readonly string query;
        private readonly int timeOut;

        public DbImporter(string jsonConfig)
        {
            var dbConfig = JsonConvert
                .DeserializeObject<DbImporterConfig>(jsonConfig);

            Number = dbConfig.Number;
            DataSetName = dbConfig.DataSetName;
            connectionString = dbConfig.ConnectionString;
            query = dbConfig.Query;
            timeOut = dbConfig.TimeOut;
        }

        public string Execute()
        {
            var queryResult = new List<Dictionary<string, object>>();
            var queryres2=new Dictionary<string,List<object>>();

            SqlScope.UsingConnection(connectionString, scope =>
            {
                using (var reader = scope
                    .CreateSimple(new QueryOptions(timeOut), $"{query}")
                    .ExecuteReader())
                {
                    if(reader.Read())
                    {
                        var fields = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var name = reader.GetName(i);
                            var val = reader[i];
                            queryres2[name] = new List<object> {val};
                            fields.Add(name, val);
                        }

                        queryResult.Add(fields);
                    }

                    while (reader.Read())
                    {
                        var fields = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var name = reader.GetName(i);
                            var val = reader[i];
                            queryres2[name].Add(val);
                            fields.Add(name, val);
                        }

                        queryResult.Add(fields);
                    }
                }
            });

            string jsString = JsonConvert.SerializeObject(queryResult);
           // string jsString = JsonConvert.SerializeObject(queryres2,Formatting.Indented);
            return jsString;
        }
    }
}