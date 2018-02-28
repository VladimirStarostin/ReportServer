﻿using ReportService.Interfaces;
using System;
using System.Diagnostics;

namespace ReportService.Implementations
{
    public class RTask : IRTask
    {
        public int ID { get; }
        public string[] SendAddresses { get; } 
        public string ViewTemplate { get; }
        public string Schedule { get; }
        public string Query { get; }
        public int TryCount { get; }
        public int TimeOut { get; }

        private IDataExecutor dataEx_;
        private IViewExecutor viewEx_;
        private IPostMaster postMaster_;
        private IConfig config_;

        public RTask(IDataExecutor aDataEx, IViewExecutor aViewEx, IPostMaster aPostMaster, IConfig aConfig,
            int ID, string aTemplate, string aSchedule, string aQuery, string aSendAddress, int aTryCount, int aTimeOut)
        {
            dataEx_ = aDataEx;
            viewEx_ = aViewEx;
            postMaster_ = aPostMaster;
            this.ID = ID;
            Query = aQuery;
            ViewTemplate = aTemplate;
            SendAddresses = aSendAddress.Split(';');
            Schedule = aSchedule;
            config_ = aConfig;
            TryCount = aTryCount;
            TimeOut = aTimeOut;
        }

        public void Execute()
        {
            Stopwatch duration = new Stopwatch();
            int i = 1;
            bool dataObtained = false;
            string jsonReport = "";
            string htmlReport = "";
            while (!dataObtained && i< TryCount)
            {
                try
                {
                    jsonReport = dataEx_.Execute(Query, TimeOut);
                    htmlReport = viewEx_.Execute(ViewTemplate, jsonReport);
                    dataObtained = true;
                }
                catch(Exception s)
                {
                    jsonReport = s.Message;
                    htmlReport = s.Message;
                }
                i++;
            }

            if(dataObtained)
            foreach (string addr in SendAddresses)
                        postMaster_.Send(htmlReport, addr);

           // config_.CreateInstance(ID, jsonReport, htmlReport, duration.ElapsedMilliseconds, dataObtained, i);
        }
    }
}