using Google.Protobuf;
using MAM.Data;
using MAM.UserControls;
using MAM.Utilities;
using MAM.ViewModels;
using MAM.Windows;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DownloadHistoryPage : Page
    {
       
      
        public static DownloadHistoryPage DownloadHistoryStatic { get; private set; }
        //public DownloadHistory ViewModel { get; set; } = new();
        private ObservableCollection<DownloadHistory> downloadHistories=new();

        public ObservableCollection<DownloadHistory> DownloadHistories
        {
            get => downloadHistories;
            set
            {
                if (downloadHistories != value)
                {
                    downloadHistories = value;
                    OnPropertyChanged(nameof(downloadHistories));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public DownloadHistoryPage()
        {
            this.InitializeComponent();

            if (DownloadHistoryStatic != null && DownloadHistoryStatic.DownloadHistories != null)
            {
               DownloadHistories = DownloadHistoryStatic.DownloadHistories;

            }
            if (downloadHistories != null)
            {
                DownloadHistories.CollectionChanged += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"AssetList Downdated. New count: {DownloadHistories.Count}");
                };
            }
            DownloadHistoryStatic = this;
            DataContext = this;
        }
      

    }
    public class DownloadHistory : ObservableObject
    {
        private string downloadPath= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        private string extension;
        private int progress;
        private DateTime startTime;
        private DateTime completionTime;
        private string status = "Pending";
        private MediaPlayerItem media;
        private string _selectedFilePath= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";

        public string DownloadPath
        {
            get => downloadPath;
            set => SetProperty(ref downloadPath, value);
        }

        public string Extension
        {
            get => extension;
            set => SetProperty(ref extension, value);
        }
        public MediaPlayerItem Media
        {
            get => media;
            set => SetProperty(ref media, value);
        }

        public int Progress
        {
            get => progress;
            set => SetProperty(ref progress, value);
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
        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set => SetProperty(ref _selectedFilePath, value);
        }

        public void AddRecentFile(string filePath)
        {
            if (!GlobalClass.Instance.RecentFiles.Contains(filePath))
            {
                GlobalClass.Instance.RecentFiles.Insert(0, filePath);
                if (GlobalClass.Instance.RecentFiles.Count > 10) // Keep only last 10 entries
                {
                    GlobalClass.Instance.RecentFiles.RemoveAt(GlobalClass.Instance.RecentFiles.Count - 1);
                }
            }
        }
    }

}
