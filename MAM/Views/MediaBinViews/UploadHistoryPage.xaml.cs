using MAM.Utilities;
using MAM.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UploadHistoryPage : Page,INotifyPropertyChanged
    {
        private ObservableCollection<UploadHistory> uploadHistories=new();
      
        public ObservableCollection<UploadHistory> UploadHistories
        {
            get => uploadHistories;
            set
            {
                if (uploadHistories != value)
                {
                    uploadHistories = value;
                    OnPropertyChanged(nameof(uploadHistories));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public static UploadHistoryPage UploadHistory { get; private set; }

        public UploadHistoryPage()
        {
            this.InitializeComponent();
           
            if(UploadHistory!=null && UploadHistory.UploadHistories!=null)
            {
                this.UploadHistories = UploadHistory.UploadHistories;

            }
            UploadHistories.CollectionChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"AssetList updated. New count: {UploadHistories.Count}");
            };
            UploadHistory = this;
            DataContext=this;
        }
    }

    public class UploadHistory: ObservableObject
    {
        private Asset asset;
        private Process process;
        public Asset AssetObj 
        { 
            get => asset; 
            set => SetProperty(ref asset, value); 
        }
        public Process ProcessObj
        {
            get => process;
            set => SetProperty(ref process, value);
        }
    }
}
