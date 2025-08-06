using CommunityToolkit.WinUI.UI.Controls;
using Google.Protobuf.WellKnownTypes;
using MAM.Data;
using MAM.Utilities;
using MAM.Windows;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using Enum = System.Enum;

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
        public ObservableCollection<Process> MergedProcesses { get; set; } = new();
        public void LoadHistory(string openedTab)
        {
            MergedProcesses.Clear();

            var grouped = ProcessManager.AllProcesses
                .Where(p => p.Result == "Waiting" &&
                           (p.ProcessType == ProcessType.FileCopying ||
                            p.ProcessType == ProcessType.ThumbnailGeneration ||
                            p.ProcessType == ProcessType.ProxyGeneration))
                .GroupBy(p => p.FilePath)
                .ToList(); // Materialize here to avoid deferred execution side effects

            foreach (var group in grouped)
            {
                var copy = group.FirstOrDefault(p => p.ProcessType == ProcessType.FileCopying);
                var thumb = group.FirstOrDefault(p => p.ProcessType == ProcessType.ThumbnailGeneration);
                var proxy = group.FirstOrDefault(p => p.ProcessType == ProcessType.ProxyGeneration);

                if (copy != null) copy.PropertyChanged += (s, e) => OnCombinedProgressChanged(group);
                if (thumb != null) thumb.PropertyChanged += (s, e) => OnCombinedProgressChanged(group);
                if (proxy != null) proxy.PropertyChanged += (s, e) => OnCombinedProgressChanged(group);

                // Pick the first ongoing process
                var displayProcess = proxy?.Result == "Waiting" ? proxy :
                                     thumb?.Result == "Waiting" ? thumb :
                                     copy ?? thumb ?? proxy;

                if (displayProcess != null)
                    MergedProcesses.Add(displayProcess);
            }
        }

        private void OnCombinedProgressChanged(IGrouping<string, Process> group)
        {
            var copy = group.FirstOrDefault(p => p.ProcessType == ProcessType.FileCopying)?.Progress ?? 0;
            var thumb = group.FirstOrDefault(p => p.ProcessType == ProcessType.ThumbnailGeneration)?.Progress ?? 0;
            var proxy = group.FirstOrDefault(p => p.ProcessType == ProcessType.ProxyGeneration)?.Progress ?? 0;

            var average = (copy + thumb + proxy) / 3;

            var displayProcess = MergedProcesses.FirstOrDefault(p =>
                p.FilePath == group.Key &&
                p.Result == "Waiting"); // Ensure it’s still ongoing

            if (displayProcess != null)
            {
                displayProcess.CombinedProgress = average;
            }
        }


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





    }

    public class ProgressToPercentageConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            if (value is double progress)
            {
                return $"{progress:F0}%";
            }
            return "0%";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
