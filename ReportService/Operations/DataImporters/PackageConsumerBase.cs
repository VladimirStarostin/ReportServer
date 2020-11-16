﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using ReportService.Entities;
using ReportService.Interfaces.Protobuf;
using ReportService.Interfaces.ReportTask;
using ReportService.Operations.DataImporters.Configurations;
using ReportService.Operations.Helpers;

namespace ReportService.Operations.DataImporters
{
    public abstract class PackageConsumerBase : BaseDbImporter
    {

        private class RegexpMatchEqualityComparer : IEqualityComparer<Match>
        {
            bool IEqualityComparer<Match>.Equals(Match x, Match y) => x.Value == y.Value;
            int IEqualityComparer<Match>.GetHashCode(Match obj) => 0;
        }
        
        public const string ReportPackageKeyword = "#RepPack";

        private static readonly Regex reportPackageRegex = new Regex($@"\B\{ReportPackageKeyword}\w*\b");
        private static readonly IEqualityComparer<Match> matchComparer = new RegexpMatchEqualityComparer();
        private readonly BasePackageExportScriptCreator complexScriptCreator;
        private readonly SqlCommandInitializer commandInitializer = new SqlCommandInitializer();

        protected PackageConsumerBase(
            IMapper mapper,
            DbImporterConfig config, 
            IPackageBuilder builder, 
            ThreadSafeRandom rnd,
            BasePackageExportScriptCreator exportScriptCreator) : base(mapper, config, builder, rnd)
        {
            this.complexScriptCreator = exportScriptCreator;
        }

        public override async Task ExecuteAsync(IReportTaskRunContext taskContext)
        {
            int parameterGlobalIdx = 0;
            foreach (var packageName in GetRequiredPackageNames(Query))
            {
                if (taskContext.Packages.ContainsKey(packageName) == false)
                    throw new InvalidOperationException($"Required package, name:{packageName} doesn`t exist in the task context.");

                complexScriptCreator.BuildPackageExportQuery(taskContext.Packages[packageName], packageName, commandInitializer, ref parameterGlobalIdx);
            }
            var parametrizedQuery = taskContext.SetStringParameters(Query);
            commandInitializer.AppendQueryString(parametrizedQuery);
            await ExecuteComplexCommand(taskContext, commandInitializer);
        }

        protected abstract Task ExecuteComplexCommand(IReportTaskRunContext taskContext, SqlCommandInitializer commandBuilder);
        
        private string[] GetRequiredPackageNames(string baseQuery)
            =>  reportPackageRegex.Matches(baseQuery).Distinct(matchComparer)
                    .Select(x=>x.Value.Replace(ReportPackageKeyword, string.Empty)).ToArray();
    }
}