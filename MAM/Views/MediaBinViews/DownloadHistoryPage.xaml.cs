using MAM.Utilities;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DownloadHistoryPage : Page
    {
        private static MediaLibraryPage mediaLibraryPage;

        public DownloadHistoryPage()
        {
            this.InitializeComponent();
        }
    }
    public class DownloadHistoryViewModel : ObservableObject
    {
        private MediaPlayerItem media;
        private MediaLibrary mediaLibrary;
        private string path;

        public DownloadHistoryViewModel()
        {
            media = new MediaPlayerItem();
            mediaLibrary = new MediaLibrary();
        }

        public MediaPlayerItem MediaObj { get => media; set => SetProperty(ref media, value); }
        public MediaLibrary MediaLibraryObj { get => mediaLibrary; set => SetProperty(ref mediaLibrary, value); }

    }
}
