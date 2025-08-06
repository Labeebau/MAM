using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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
        //public ObservableCollection<Process> ProcessList { get; set; } = new();

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
        public ObservableCollection<MergedProcessViewModel> MergedProcesses { get; set; } = new();
        public void LoadHistory(string openedTab)
        {
            MergedProcesses.Clear();

            var grouped = ProcessManager.AllProcesses
                .Where(p => p.Result == "Waiting" && 
                           (p.ProcessType == ProcessType.FileCopying ||
                            p.ProcessType == ProcessType.ThumbnailGeneration ||
                            p.ProcessType == ProcessType.ProxyGeneration))
                .GroupBy(p => p.FilePath);

            foreach (var group in grouped)
            {
                var copy = group.FirstOrDefault(p => p.ProcessType == ProcessType.FileCopying);
                var thumb = group.FirstOrDefault(p => p.ProcessType == ProcessType.ThumbnailGeneration);
                var proxy = group.FirstOrDefault(p => p.ProcessType == ProcessType.ProxyGeneration);

                // If none exist, skip
                if (copy == null && thumb == null && proxy == null)
                    continue;

                // Provide defaults if any process is missing
                copy ??= new Process { FilePath = group.Key, ProcessType = ProcessType.FileCopying, Result = "Completed", Status = "N/A" };
                thumb ??= new Process { FilePath = group.Key, ProcessType = ProcessType.ThumbnailGeneration, Result = "Completed", Status = "N/A" };
                proxy ??= new Process { FilePath = group.Key, ProcessType = ProcessType.ProxyGeneration, Result = "Completed", Status = "N/A" };

                var merged = new MergedProcessViewModel(copy, thumb, proxy);
                MergedProcesses.Add(merged);
            }
        }

    }
    //private void OnProcessChanged(MergedProcessViewModel merged, PropertyChangedEventArgs e)
    //{
    //    if (e.PropertyName == nameof(Process.Progress))
    //    {
    //        UIThreadHelper.RunOnUIThread(() =>
    //        {
    //            UpdateCombinedProgress(merged);
    //        });
    //    }
    //}
    //    private void UpdateCombinedProgress(MergedProcessViewModel merged)
    //{
    //    int total = 0;

    //    if (merged.CopyProcess != null) { total += merged.CopyProcess.Progress/3;  }
    //    if (merged.ThumbnailProcess != null) { total += merged.ThumbnailProcess.Progress / 3; }
    //    if (merged.ProxyProcess != null) { total += merged.ProxyProcess.Progress / 3; }

    //    merged.CombinedProgress = total;
    //}

    // No need to update other properties; UI binds to individual processes directly
    //}

    //public void LoadHistory(string openedTab)
    //{
    //    MergedProcesses.Clear();

    //    var grouped = ProcessManager.AllProcesses
    //        .Where(p => p.Result == "Waiting" &&
    //                   (p.ProcessType == ProcessType.FileCopying ||
    //                    p.ProcessType == ProcessType.ThumbnailGeneration ||
    //                    p.ProcessType == ProcessType.ProxyGeneration))
    //        .GroupBy(p => p.FilePath)
    //        .ToList(); // Materialize here to avoid deferred execution side effects

    //    foreach (var group in grouped)
    //    {
    //        var copy = group.FirstOrDefault(p => p.ProcessType == ProcessType.FileCopying);
    //        var thumb = group.FirstOrDefault(p => p.ProcessType == ProcessType.ThumbnailGeneration);
    //        var proxy = group.FirstOrDefault(p => p.ProcessType == ProcessType.ProxyGeneration);

    //        if (copy != null) copy.PropertyChanged += (s, e) => OnCombinedProgressChanged(group);
    //        if (thumb != null) thumb.PropertyChanged += (s, e) => OnCombinedProgressChanged(group);
    //        if (proxy != null) proxy.PropertyChanged += (s, e) => OnCombinedProgressChanged(group);

    //        // Pick the first ongoing process
    //        var displayProcess = copy != null && copy.Result == "Waiting" ? copy :
    //                            thumb != null && thumb.Result == "Waiting" ? thumb :
    //                            proxy ?? copy ?? thumb;


    //        if (displayProcess != null)
    //            MergedProcesses.Add(displayProcess);


    //    }
    //}

    //private void OnCombinedProgressChanged(IGrouping<string, Process> group)
    //{
    //    var copy = group.FirstOrDefault(p => p.ProcessType == ProcessType.FileCopying)?.Progress ?? 0;
    //    var thumb = group.FirstOrDefault(p => p.ProcessType == ProcessType.ThumbnailGeneration)?.Progress ?? 0;
    //    var proxy = group.FirstOrDefault(p => p.ProcessType == ProcessType.ProxyGeneration)?.Progress ?? 0;

    //    var average = (copy + thumb + proxy) / 3;

    //    var displayProcess = MergedProcesses.FirstOrDefault(p =>
    //        p.FilePath == group.Key &&
    //        p.Result == "Waiting"); // Ensure it’s still ongoing

    //    if (displayProcess != null)
    //    {
    //        displayProcess.CombinedProgress = average;
    //    }
    //}


    //public void LoadHistory(string openedTab)
    //{
    //    MergedProcesses.Clear();

    //    var grouped = ProcessManager.AllProcesses
    //        .Where(p => p.Result == "Waiting" &&
    //                   (p.ProcessType == ProcessType.FileCopying ||
    //                    p.ProcessType == ProcessType.ThumbnailGeneration ||
    //                    p.ProcessType == ProcessType.ProxyGeneration))
    //        .GroupBy(p => p.FilePath);

    //    foreach (var group in grouped)
    //    {
    //        var copy = group.FirstOrDefault(p => p.ProcessType == ProcessType.FileCopying);
    //        var thumb = group.FirstOrDefault(p => p.ProcessType == ProcessType.ThumbnailGeneration);
    //        var proxy = group.FirstOrDefault(p => p.ProcessType == ProcessType.ProxyGeneration);

    //        if (copy != null) copy.PropertyChanged += (s, e) => OnCombinedProgressChanged(group);
    //        if (thumb != null) thumb.PropertyChanged += (s, e) => OnCombinedProgressChanged(group);
    //        if (proxy != null) proxy.PropertyChanged += (s, e) => OnCombinedProgressChanged(group);

    //        // Add only one representative (e.g., copy or thumb) to MergedProcesses for UI binding
    //        var displayProcess = copy ?? thumb ?? proxy;
    //        //displayProcess.Tag = new[] { copy, thumb, proxy }; // Store grouped references if needed
    //        MergedProcesses.Add(displayProcess);
    //    }
    //}
    //private void OnCombinedProgressChanged(IGrouping<string, Process> group)
    //{
    //    var copy = group.FirstOrDefault(p => p.ProcessType == ProcessType.FileCopying)?.Progress ?? 0;
    //    var thumb = group.FirstOrDefault(p => p.ProcessType == ProcessType.ThumbnailGeneration)?.Progress ?? 0;
    //    var proxy = group.FirstOrDefault(p => p.ProcessType == ProcessType.ProxyGeneration)?.Progress ?? 0;

    //    var average = (copy + thumb + proxy) / 3;

    //    var displayProcess = MergedProcesses.FirstOrDefault(p => p.FilePath == group.Key);
    //    if (displayProcess != null)
    //    {
    //        displayProcess.CombinedProgress = average;
    //       // displayProcess.Status = "Processing"; // Optional
    //    }
    //}






    public class MergedProcessViewModel : ObservableObject
{
        private string filePath;
        private Process? copyProcess;
        private Process? thumbnailProcess;
        private Process? proxyProcess;
        private int combinedProgress;

        public string FilePath
        {
            get => filePath;
            set => SetProperty(ref filePath, value);
        }
        public Process? CopyProcess
        {
            get => copyProcess;
            set => SetProperty(ref copyProcess, value);
        }
        public Process? ThumbnailProcess
        {
            get => thumbnailProcess;
            set => SetProperty(ref thumbnailProcess, value);
        }
        public Process? ProxyProcess
        {
            get => proxyProcess;
            set => SetProperty(ref proxyProcess, value);
        }
        //public int CombinedProgress
        //{
        //    get => combinedProgress;
        //    set => SetProperty(ref combinedProgress, value);
        //}
        public int CombinedProgress => (CopyProcess.Progress + ThumbnailProcess.Progress + ProxyProcess.Progress) / 3;

        public string CurrentStatus =>
            CopyProcess.Result != "Finished" ? CopyProcess.Status :
            ThumbnailProcess.Result != "Finished" ? ThumbnailProcess.Status :
            ProxyProcess.Status;
        public DateTime CurrentStartTime =>
           CopyProcess.Result != "Finished" ? CopyProcess.StartTime :
           ThumbnailProcess.Result != "Finished" ? ThumbnailProcess.StartTime :
           ProxyProcess.StartTime;
        public DateTime CurrentEndTime =>
           ThumbnailProcess.Result != "Finished" ? ThumbnailProcess.EndTime :
           ProxyProcess.EndTime;
        public MergedProcessViewModel(Process copy, Process thumb, Process proxy)
        {
            CopyProcess = copy;
            ThumbnailProcess = thumb;
            ProxyProcess = proxy;

            CopyProcess.PropertyChanged += ProcessChanged;
            ThumbnailProcess.PropertyChanged += ProcessChanged;
            ProxyProcess.PropertyChanged += ProcessChanged;
        }

        private void ProcessChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Process.Progress))
                OnPropertyChanged(nameof(CombinedProgress));

            if (e.PropertyName == nameof(Process.Status) || e.PropertyName == nameof(Process.Result))
                OnPropertyChanged(nameof(CurrentStatus));

            if (e.PropertyName == nameof(Process.StartTime))
                OnPropertyChanged(nameof(CurrentStartTime));

            if (e.PropertyName == nameof(Process.EndTime)) 
                OnPropertyChanged(nameof(CurrentEndTime));
        }
    }

}
