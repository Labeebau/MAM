using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.TransferJobs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TransferJobStatus : Page
    {
        // Collection that will be bound to the GridView and filtered
        public ObservableCollection<Process> FilteredProcesss { get; set; } = new ObservableCollection<Process>();
        public ObservableCollection<Process> AllProcesss { get; set; } = new ObservableCollection<Process>();

        public TransferJobStatus()
        {
            this.InitializeComponent();
            // Sample data
            AllProcesss.Add(new Process { Server = "", Type = "Quality Check", Date = DateTime.Today.Date });
            AllProcesss.Add(new Process { Server = "", Type = "Quality Check", Date = DateTime.Today.Date });
            AllProcesss.Add(new Process { Server = "", Type = "Quality Check", Date = DateTime.Today.Date });
            AllProcesss.Add(new Process { Server = "", Type = "Proxy Generation", Date = DateTime.Today.Date });
            AllProcesss.Add(new Process { Server = "", Type = "Proxy Generation", Date = DateTime.Today.Date });

            // Initialize with full list
            FilterData();
        }
        // Event handler for text changes in search boxes
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterData();
        }

        // Filtering logic
        private void FilterData()
        {
            string ServerFilter = ServerSearchBox.Text?.ToLower();
            string TypeFilter = TypeSearchBox.Text?.ToLower();
            DateTime DateFilter = Convert.ToDateTime(DateSearchBox.Date);

            // Clear previous filtered results
            FilteredProcesss.Clear();

            // Filter the master list and add matching items to FilteredProcesss
            var filtered = AllProcesss.Where(p =>
                (string.IsNullOrEmpty(ServerFilter) || p.Server.ToLower().Contains(ServerFilter)) &&
                (string.IsNullOrEmpty(TypeFilter) || p.Type.ToLower().Contains(TypeFilter)) &&
                (true));

            foreach (var Process in filtered)
            {
                FilteredProcesss.Add(Process);
            }
        }
    }
    public class Process
    {
        public string Server { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
    }
}
    

