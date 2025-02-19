using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
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
    public sealed partial class DownloadHistoryPage : Page
    {
        private static ArchivePage archivePage;

        public DownloadHistoryPage()
        {
            this.InitializeComponent();
        }
    }
    public class DownloadHistoryViewModel : ObservableObject
    {
        private MediaPlayerItem media;
        private Archive archive;
        private string path;

        public DownloadHistoryViewModel()
        {
            media = new MediaPlayerItem();
            archive = new Archive();
        }

        public MediaPlayerItem MediaObj { get => media; set => SetProperty(ref media, value); }
        public Archive ArchiveObj { get => archive; set => SetProperty(ref archive, value); }

    }
}
