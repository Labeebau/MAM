using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TransactionHistoryPage : Page
    {

        private DataAccess dataAccess = new();
        public static TransactionHistoryPage TransactionHistoryStatic { get; private set; }
        public ObservableCollection<Process> ProcessList { get; set; } = new();
        public string OpenedTab { get; set; }
        public TransactionHistoryPage()
        {
            this.InitializeComponent();
            //GetProcesses("Proxy Generation");
            //SyncProcessesFromDatabase(ProcessList);
            LoadHistory("UploadHistory");
            TransactionHistoryStatic = this;
            DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string openedTab)
            {
                InitializeWithParameter(openedTab);
            }
        }
        public void InitializeWithParameter(string openedTab)
        {
            OpenedTab = openedTab;
            LoadHistory(OpenedTab);
            TransactionHistoryStatic = this;
            DataContext = this;
        }

        public void LoadHistory(string openedTab)
        {
            ProcessList.Clear();
            IEnumerable<Process> filtered = Enumerable.Empty<Process>();
            if (openedTab == "UploadHistory")
            {
               List<Process> nullProcesses=ProcessManager.AllProcesses.Where(p =>p==null).ToList();
                foreach (Process process in nullProcesses)
                {
                    ProcessManager.AllProcesses.Remove(process);
                }
               filtered = ProcessManager.AllProcesses.Where(p => 
                (p.ProcessType == ProcessType.ProxyGeneration||
                p.ProcessType==ProcessType.FileCopying||
                p.ProcessType==ProcessType.ThumbnailGeneration)&& 
                p.Result == "Waiting").ToList();

            }
            else if (openedTab == "DownloadHistory")
            {
                filtered = ProcessManager.AllProcesses.Where(p => 
                p.ProcessType.ToString().Contains("Download") 
                && p.Result == "Waiting").ToList();
            }
            foreach (var p in filtered)
            {
                p.PropertyChanged += Process_PropertyChanged;
                ProcessList.Add(p);
            }
        }
        private void Process_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Process process)
            {
                if ((e.PropertyName == nameof(Process.Progress) && process.Progress >= 100)
                    || (e.PropertyName == nameof(Process.Result) && process.Result == "Finished"))
                {
                    // Remove from UI-bound list
                    UIThreadHelper.RunOnUIThread(() =>
                    {
                        process.PropertyChanged -= Process_PropertyChanged;
                        ProcessList.Remove(process);
                    });
                }
            }
        }

        public void GetProcesses()
        {
            //var processList = new ObservableCollection<Process>();
            string query = "SELECT * FROM process ORDER BY start_time DESC";
            DataTable dt = dataAccess.GetData(query);
            foreach (DataRow row in dt.Rows)
            {
                ProcessList.Add(new Process
                {
                    ProcessId = Convert.ToInt32(row["process_id"]),
                    Server = row["server"].ToString(),
                    ProcessType = Enum.Parse<ProcessType>(row["type"].ToString()),
                    FilePath = row["file_name"].ToString(),
                    ScheduleStart = Convert.ToDateTime(row["schedule_start"]),
                    ScheduleEnd = Convert.ToDateTime(row["schedule_end"]),
                    StartTime = Convert.ToDateTime(row["start_time"]),
                    CompletionTime = Convert.ToDateTime(row["end_time"]),
                    Status = row["status"].ToString(),
                    Result = row["result"].ToString(),

                });
            }
            // return processList;
        }
        public void GetProcesses(string processType)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@ProcessType", processType);
            parameters.Add("@Result", "Waiting");
            string query = "SELECT * FROM process WHERE  type=@ProcessType and result=@Result ORDER BY start_time DESC";
            DataTable dt = dataAccess.GetData(query, parameters);
            foreach (DataRow row in dt.Rows)
            {
                ProcessList.Add(new Process
                {
                    ProcessId = Convert.ToInt32(row["process_id"]),
                    Server = row["server"].ToString(),
                    ProcessType = Enum.Parse<ProcessType>(row["type"].ToString()),
                    FilePath = row["file_name"].ToString(),
                    ScheduleStart = Convert.ToDateTime(row["schedule_start"]),
                    ScheduleEnd = Convert.ToDateTime(row["schedule_end"]),
                    StartTime = Convert.ToDateTime(row["start_time"]),
                    CompletionTime = Convert.ToDateTime(row["end_time"]),
                    Status = row["status"].ToString(),
                    Result = row["result"].ToString(),

                });
            }
        }
        public void SyncProcessesFromDatabase(ObservableCollection<Process> dbProcesses)
        {
            foreach (var dbProcess in dbProcesses)
            {
                var existing = ProcessManager.AllProcesses.FirstOrDefault(p => p.ProcessId == dbProcess.ProcessId);
                if (existing != null)
                {
                    // Update properties individually to trigger INotifyPropertyChanged
                    existing.Server = dbProcess.Server;
                    existing.ProcessType = dbProcess.ProcessType;
                    existing.FilePath = dbProcess.FilePath;
                    existing.ScheduleStart = dbProcess.ScheduleStart;
                    existing.ScheduleEnd = dbProcess.ScheduleEnd;
                    existing.StartTime = dbProcess.StartTime;
                    existing.CompletionTime = dbProcess.CompletionTime;
                    existing.Result = dbProcess.Result;
                    existing.Progress = dbProcess.Progress;
                }
                else
                {
                    ProcessList.Add(dbProcess); // New item
                }
            }

            // Remove any items that are no longer in the DB
            //var toRemove = ProcessList
            //    .Where(p => !dbProcesses.Any(dp => dp.ProcessId == p.ProcessId))
            //    .ToList();
            //foreach (var item in toRemove)
            //    ProcessList.Remove(item);
        }
    }
}
