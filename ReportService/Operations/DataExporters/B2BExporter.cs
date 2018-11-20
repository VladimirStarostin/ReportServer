﻿using System;
using System.IO;
using System.Linq;
using AutoMapper;
using Gerakul.FastSql.Common;
using Gerakul.FastSql.SqlServer;
using Google.Protobuf;
using ReportService.Interfaces.Core;
using ReportService.Interfaces.ReportTask;

namespace ReportService.Operations.DataExporters
{
    public class B2BExporter : CommonDataExporter
    {
        public string ConnectionString;
        public string ExportTableName;
        public string ExportInstanceTableName;
        public int DbTimeOut;
        private readonly IArchiver archiver;
        public string ReportName;

        public B2BExporter(IMapper mapper, IArchiver archiver,
            B2BExporterConfig config)
        {
            this.archiver = archiver;
            mapper.Map(config, this);
        }

        public override void Send(IRTaskRunContext taskContext)
        {
            var package = taskContext.Packages[PackageName];

            if (!RunIfVoidPackage && package.DataSets.Count == 0)
                return;

            var context = SqlContextProvider.DefaultInstance
                .CreateContext(ConnectionString);

            if (context.CreateSimple($@"
                IF OBJECT_ID('{ExportTableName}') IS NOT NULL
                IF EXISTS(SELECT * FROM {ExportTableName} WHERE id = {Id})
                AND OBJECT_ID('{ExportInstanceTableName}') IS NOT NULL
                SELECT 1
                ELSE SELECT 0").ExecuteQueryFirstColumn<int>().First() != 1)
                return;

            byte[] archivedPackage;

            using (var stream = new MemoryStream())
            {
                package.WriteTo(stream);
                archivedPackage = archiver.CompressStream(stream);
            }

            var newInstance = new
            {
                ExportId = Id,
                ExecuteTime = DateTime.Now,
                OperationPackage = archivedPackage
            };

            context.Insert(ExportInstanceTableName, newInstance, new QueryOptions(DbTimeOut), "Id");
        }
    }
}