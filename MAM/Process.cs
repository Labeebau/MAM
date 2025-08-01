﻿using MAM.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAM
{
   
    public class Process : ObservableObject
    {
        private int processId;
        private string server = string.Empty;
        private ProcessType processType ;
        private string filePath = string.Empty;
        private DateTime scheduleStart;
        private DateTime scheduleEnd;
        private DateTime startTime=DateTime.ParseExact("01:01:2000", "dd:MM:yyyy", CultureInfo.InvariantCulture);
        private DateTime completionTime;
        private string result = string.Empty;
        private string status = string.Empty;
        private int progress;
        private bool isVisible = true;

        public int ProcessId
        {
            get => processId;
            set => SetProperty(ref processId, value);
        }
        public string Server
        {
            get => server;
            set => SetProperty(ref server, value);
        }
        public ProcessType ProcessType
        {
            get => processType;
            set => SetProperty(ref processType, value);
        }
        public string FilePath
        {
            get => filePath;
            set => SetProperty(ref filePath, value);
        }
        public DateTime ScheduleStart
        {
            get => scheduleStart;
            set => SetProperty(ref scheduleStart, value);
        }
        public DateTime ScheduleEnd
        {
            get => scheduleEnd;
            set => SetProperty(ref scheduleEnd, value);
        }
        public DateTime StartTime
        {
            get => startTime;
            set => SetProperty(ref startTime, value);
        }
        public DateTime CompletionTime
        {
            get => completionTime;
            set => SetProperty(ref completionTime, value);
        }
        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }
        public int Progress
        {
            get => progress;
            set => SetProperty(ref progress, value);
        }
        public string Result
        {
            get => result;
            set
            {
                SetProperty(ref result, value);
                if(value=="Finished")
                {
                    Progress = 100;
                }
            }
        }
        public Process(string filePath)
        {
            FilePath = filePath;
        }
        public Process()
        {
        }
    }
}
