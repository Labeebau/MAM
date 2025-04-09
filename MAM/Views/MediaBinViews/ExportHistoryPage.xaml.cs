using MAM.Utilities;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExportHistoryPage : Page
    {

        public ExportHistoryPage()
        {
            this.InitializeComponent();
        }
    }
    public class ExportHistoryViewModel : ObservableObject
    {
        private MediaPlayerItem media;
        private MediaLibrary mediaLibrary;

        public ExportHistoryViewModel()
        {
            media = new MediaPlayerItem();
            mediaLibrary = new MediaLibrary();
        }

        public MediaPlayerItem MediaObj { get => media; set => SetProperty(ref media, value); }
        public MediaLibrary mediaLibraryObj { get => mediaLibrary; set => SetProperty(ref mediaLibrary, value); }

    }
}
