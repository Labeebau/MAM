using MAM.Data;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.ProcessesViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProcessStatusPage : Page
    {
        public static ProcessStatusPage Instance { get; private set; }
        // Collection that will be bound to the GridView and filtered
        public ObservableCollection<Process> FilteredProcesses { get; set; } = new ObservableCollection<Process>();
      //  public ObservableCollection<Process> AllProcesses { get; set; } = new ObservableCollection<Process>();
        private static DataAccess dataAccess = new DataAccess();
        private string OpenedTab { get; set; } = string.Empty;
        // public ObservableCollection<Process> ProcessList { get; set; }
        public ProcessStatusPage()
        {
            this.InitializeComponent();
            Instance = this;
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string openedTab)
            {
                OpenedTab = openedTab;
                FilteredProcesses.Clear();
                ProcessManager.AllProcesses =await GetProcesses();
                //AllProcesses = GetProcesses();
                SyncProcessesFromDatabase(ProcessManager.AllProcesses);

                //SyncProcessesFromDatabase(AllProcesses);
                foreach (var process in ProcessManager.AllProcesses)
                    FilteredProcesses.Add(process);
                //foreach (var process in AllProcesses)
                //    FilteredProcesses.Add(process);
            }
            FromDateSearchBox.Date = DateTime.Now;// new DateTimeOffset(new DateTime(2000, 1, 1));
            ToDateSearchBox.Date = DateTime.Now;
            DataContext = this;
            // FilterData();
        }
        public static async Task<ObservableCollection<Process>> GetProcesses()
        {
            var processList = new ObservableCollection<Process>();
            string query = "SELECT * FROM process ORDER BY start_time DESC";
            DataTable dt =await dataAccess.GetDataAsync(query);
            foreach (DataRow row in dt.Rows)
            {
                processList.Add(new Process
                {
                    ProcessId = Convert.ToInt32(row["process_id"]),
                    Server = row["server"].ToString(),
                    ProcessType = Enum.Parse<ProcessType>(row["type"].ToString()),
                    FilePath = row["file_name"].ToString(),
                    ScheduleStart = Convert.ToDateTime(row["schedule_start"]),
                    ScheduleEnd = Convert.ToDateTime(row["schedule_end"]),
                    StartTime = Convert.ToDateTime(row["start_time"]),
                    EndTime = Convert.ToDateTime(row["end_time"]),
                    Status = row["status"].ToString(),
                    Result = row["result"].ToString(),

                });
            }
            return processList;
        }
        public static async Task<ObservableCollection<Process>> GetProcesses(string result)
        {
            var processList = new ObservableCollection<Process>();
            string query = "SELECT * FROM process WHERE result = @result ORDER BY start_time DESC";
            List<MySqlParameter> parameters = new () {new MySqlParameter("@result", result)};
            DataTable dt =await dataAccess.GetDataAsync(query, parameters);
            foreach (DataRow row in dt.Rows)
            {
                processList.Add(new Process
                {
                    ProcessId = Convert.ToInt32(row["process_id"]),
                    Server = row["server"].ToString(),
                    ProcessType = Enum.Parse<ProcessType>(row["type"].ToString()),
                    FilePath = row["file_name"].ToString(),
                    ScheduleStart = Convert.ToDateTime(row["schedule_start"]),
                    ScheduleEnd = Convert.ToDateTime(row["schedule_end"]),
                    StartTime = Convert.ToDateTime(row["start_time"]),
                    EndTime = Convert.ToDateTime(row["end_time"]),
                    Status = row["status"].ToString(),
                    Result = row["result"].ToString(),

                });
            }
            return processList;
        }

        public void SyncProcessesFromDatabase(ObservableCollection<Process> dbProcesses)
        {
            foreach (var dbProcess in dbProcesses)
            {
                var existing = FilteredProcesses.FirstOrDefault(p => p.ProcessId == dbProcess.ProcessId);
                if (existing != null)
                {
                    // Update properties individually to trigger INotifyPropertyChanged
                    existing.Server = dbProcess.Server;
                    existing.ProcessType = dbProcess.ProcessType;
                    existing.FilePath = dbProcess.FilePath;
                    existing.ScheduleStart = dbProcess.ScheduleStart;
                    existing.ScheduleEnd = dbProcess.ScheduleEnd;
                    existing.StartTime = dbProcess.StartTime;
                    existing.EndTime = dbProcess.EndTime;
                    existing.Result = dbProcess.Result;
                    existing.Progress = dbProcess.Progress;

                    // ... any other properties
                }
                else
                {
                    FilteredProcesses.Add(dbProcess); // New item
                }
            }

            // Remove any items that are no longer in the DB
            var toRemove = FilteredProcesses
                .Where(p => !dbProcesses.Any(dp => dp.ProcessId == p.ProcessId))
                .ToList();
            foreach (var item in toRemove)
                FilteredProcesses.Remove(item);
        }

        // Event handler for text changes in search boxes
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterData();
        }

        public void FilterData()
        {
            var baseList = OpenedTab == "WaitingProcesses" ? 
                                        ProcessManager.AllProcesses.Where(p => p.Result == "Waiting") :
                                        ProcessManager.AllProcesses.Where(p => p.Result == "Finished");
            string ServerFilter = ServerSearchBox.Text?.ToLower();
            string TypeFilter = TypeSearchBox.Text?.ToLower();
            DateTime? FromDateFilter = FromDateSearchBox.Date != null ? FromDateSearchBox.Date.Value.Date : (DateTime?)null;
            DateTime? ToDateFilter = ToDateSearchBox.Date != null ? ToDateSearchBox.Date.Value.Date.AddDays(1).AddTicks(-1) : (DateTime?)null;

            //DateTime? FromDateFilter = Convert.ToDateTime(FromDateSearchBox.Date?.DateTime);
            //DateTime? ToDateFilter = ToDateSearchBox.Date?.DateTime?.Date.AddDays(1).AddTicks(-1); // include entire day
            //Filter the master list and add matching items to FilteredProcesss
            var filtered = baseList.Where(p =>
                            (string.IsNullOrEmpty(ServerFilter) || p.Server.Contains(ServerFilter, StringComparison.CurrentCultureIgnoreCase)) &&
                (string.IsNullOrEmpty(TypeFilter) || p.ProcessType.ToString().Contains(TypeFilter, StringComparison.CurrentCultureIgnoreCase)) &&
                (!FromDateFilter.HasValue || p.StartTime >= FromDateFilter.Value) &&
                (!ToDateFilter.HasValue || p.StartTime <= ToDateFilter.Value)); // include whole day
            FilteredProcesses.Clear();// Clear previous filtered results
            foreach (var Process in filtered)
            {
                FilteredProcesses.Add(Process);
            }
        }
        private void DateSearchBox_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            FilterData();
        }
    }
}
