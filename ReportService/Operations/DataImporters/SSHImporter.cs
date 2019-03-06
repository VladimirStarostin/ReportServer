﻿using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using Renci.SshNet;
using ReportService.Interfaces.Core;
using ReportService.Interfaces.ReportTask;

namespace ReportService.Operations.DataImporters
{
    public class SshImporter : IOperation
    {
        public CommonOperationProperties Properties { get; set; } = new CommonOperationProperties();
        public string Host; // = @"10.0.10.205";
        public string Login; // = "tester";
        public string Password; // = "password";
        public string FilePath; // = @"412412\newfile2.txt";

        public SshImporter(IMapper mapper, SshImporterConfig config)
        {
            mapper.Map(config, this);
            mapper.Map(config, Properties);
        }

        public void Execute(IRTaskRunContext taskContext)
        {
            using (var client = new SftpClient(Host, Login, Password))
            {
                client.Connect();
                using (FileStream fstr =
                    File.Create(Path.Combine(taskContext.DataFolderPath, Path.GetFileName(FilePath))))
                {
                    client.DownloadFile(FilePath, fstr);
                }
            }
        }

        public Task ExecuteAsync(IRTaskRunContext taskContext)
        {
            Execute(taskContext);
            return Task.CompletedTask;
        }
    }
}