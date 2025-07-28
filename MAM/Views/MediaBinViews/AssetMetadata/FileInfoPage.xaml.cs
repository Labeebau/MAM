using MAM.Data;
using MAM.Utilities;
using MAM.Views.AdminPanelViews.Metadata;
using MAM.Windows;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews.AssetMetadata
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileInfoPage : Page, INotifyPropertyChanged
    {
        private string mediaPath;
        private string metadata;
        private ObservableCollection<KeyValuePair<string, object>> _fileInfo;
        public ObservableCollection<KeyValuePair<string, object>> FileInfo
        {
            get => _fileInfo;
            set
            {
                _fileInfo = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<TreeNode> CodecInfo { get; } = new();



        
        public FileInfoPage()
        {
            this.InitializeComponent();
            DataContext = this;

        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is MediaPlayerViewModel viewModel)
            {
                mediaPath = viewModel.Media.MediaSource.LocalPath;
                if (viewModel.Metadata != null)
                    FileInfo = new ObservableCollection<KeyValuePair<string, object>>(viewModel.Metadata);
                else
                    FileInfo = new ObservableCollection<KeyValuePair<string, object>>();
            }
            UIThreadHelper.RunOnUIThread(() =>{ App.MainAppWindow.StatusBar.ShowStatus("Fetching file info...", true); });
            await LoadMetadataAsync(mediaPath);
            UIThreadHelper.RunOnUIThread(() => { App.MainAppWindow.StatusBar.HideStatus(); });

        }

        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    base.OnNavigatedTo(e);

        //    if (e.Parameter is MediaPlayerViewModel viewModel)
        //    {
        //        mediaPath = viewModel.Media.MediaSource.LocalPath.ToString();
        //        if (viewModel.Metadata != null)
        //            FileInfo = new ObservableCollection<KeyValuePair<string, object>>(viewModel.Metadata);
        //    }
        //    else
        //    {
        //        FileInfo = new ObservableCollection<KeyValuePair<string, object>>();
        //    }
        //    metadata = GetVideoMetadata(mediaPath);
        //    LoadCodecInfo(mediaPath);
        //}
        public static async Task<string> GetVideoMetadataAsync(string videoPath)
        {
            string ffprobePath = GlobalClass.Instance.ffprobePath;
            string arguments = $"-v error -show_entries stream=index,codec_name,codec_type,sample_rate,channels,width,height,r_frame_rate -of csv=p=0 \"{videoPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = psi };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            string output = outputTask.Result;
            string error = errorTask.Result;

            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.WriteLine("FFprobe Error: " + error);
            }

            return output;
        }

        ////public static string GetVideoMetadata(string videoPath)
        ////{
        ////    string ffprobePath = GlobalClass.Instance.ffprobePath;
        ////    string arguments = $"-v error -show_entries stream=index,codec_name,codec_type,sample_rate,channels,width,height,r_frame_rate -of csv=p=0 \"{videoPath}\"";

        ////    //  string arguments = $"-v error -select_streams v:0 -show_entries stream=codec_name,width,height,r_frame_rate -of csv=p=0 \"{videoPath}\"";

        ////    ProcessStartInfo psi = new()
        ////    {
        ////        FileName = ffprobePath,
        ////        Arguments = arguments,
        ////        RedirectStandardOutput = true,
        ////        RedirectStandardError = true, // Capture errors
        ////        UseShellExecute = false,
        ////        CreateNoWindow = true
        ////    };

        ////    using System.Diagnostics.Process process = new()
        ////    { StartInfo = psi };
        ////    process.Start();
        ////    string output = process.StandardOutput.ReadToEnd();
        ////    string error = process.StandardError.ReadToEnd();  // Capture error output
        ////    process.WaitForExit();

        ////    if (!string.IsNullOrWhiteSpace(error))
        ////    {
        ////        Console.WriteLine("FFprobe Error: " + error);
        ////    }

        ////    Console.WriteLine("FFprobe Output: " + output);
        ////    return output;
        ////}

        public async Task LoadMetadataAsync(string videoPath)
        {
            CodecInfo.Clear();
            TreeNode generalNode = new("General");
            if (FileInfo != null)
            {
                foreach (var info in FileInfo)
                {
                    if (info.Value is IList<string> list)
                    {
                        if (list.Count > 0)
                        {
                            var listNode = new TreeNode(info.Key);
                            foreach (var item in list)
                            {
                                listNode.Children.Add(new TreeNode(item));
                            }
                            generalNode.Children.Add(listNode);
                        }
                    }
                    else if (info.Value != null)
                    {
                        generalNode.Children.Add(new TreeNode(info.Key, info.Value.ToString()));
                    }
                }

                //foreach (var info in FileInfo)
                //{
                //    //if (info.Value != null && info.Key != "Keywords")
                //        generalNode.Children.Add(new TreeNode(info.Key, info.Value.ToString()));
                //}
                CodecInfo.Add(generalNode);
            }
            string ffmpegData = await GetVideoMetadataAsync(videoPath);
            var lines = ffmpegData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            TreeNode videoStream = null;
            TreeNode audioStream = null;

            foreach (var line in lines)
            {
                string[] parts = line.Split(',');
                if (parts.Length < 3) continue;

                string streamIndex = parts[0];
                string codec = parts[1];
                string type = parts[2];

                if (type == "video" && parts.Length >= 6)
                {
                    videoStream = new TreeNode("Video");
                    videoStream.Children.Add(new TreeNode("Codec", codec));
                    videoStream.Children.Add(new TreeNode("Resolution", $"{parts[3]}x{parts[4]}"));
                    videoStream.Children.Add(new TreeNode("Frame Rate", parts[5].Replace("/1", " fps")));
                }
                else if (type == "audio" && parts.Length >= 5)
                {
                    audioStream = new TreeNode("Audio");
                    audioStream.Children.Add(new TreeNode("Codec", codec));
                    audioStream.Children.Add(new TreeNode("Sample Rate", parts[3] + " Hz"));
                    audioStream.Children.Add(new TreeNode("Channels", parts[4]));
                }
            }

            if (videoStream != null) CodecInfo.Add(videoStream);
            if (audioStream != null) CodecInfo.Add(audioStream);
        }

        //public void LoadCodecInfo(string videoPath)
        //{
        //    CodecInfo.Clear();
        //    TreeNode GeneralNode = new("General");
        //    if (FileInfo != null)
        //    {
        //        foreach (var info in FileInfo)
        //        {
        //            if (info.Value != null && info.Key != "Keywords")
        //                GeneralNode.Children.Add(new TreeNode(info.Key, info.Value.ToString()));
        //        }
        //        CodecInfo.Add(GeneralNode);

        //        string ffmpegData = GetVideoMetadata(videoPath);
        //        var lines = ffmpegData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        //        TreeNode videoStream = null;
        //        TreeNode audioStream = null;

        //        foreach (var line in lines) //"0,h264,video,1920,1080,25/1"
        //        {                           //"1,aac,audio,44100,2,0/0"
        //            string[] parts = line.Split(',');
        //            if (parts.Length < 3) continue; // Ensure at least stream index, codec, and type exist

        //            string streamIndex = parts[0];   // "0" or "1"
        //            string codec = parts[1];         // "h264" or "aac"
        //            string type = parts[2];          // "video" or "audio"

        //            if (type == "video" && parts.Length >= 6)
        //            {
        //                //videoStream = new TreeNode($"Stream {streamIndex}");
        //                videoStream = new TreeNode($"Video");
        //                videoStream.Children.Add(new TreeNode("Codec ", codec));
        //                videoStream.Children.Add(new TreeNode("Resolution ", $"{parts[3]}x{parts[4]}"));
        //                videoStream.Children.Add(new TreeNode("Frame Rate ", parts[5].Replace("/1", " fps")));
        //            }
        //            else if (type == "audio" && parts.Length >= 5)
        //            {
        //                //audioStream = new TreeNode($"Stream {streamIndex}");
        //                audioStream = new TreeNode($"Audio");
        //                audioStream.Children.Add(new TreeNode("Codec ", codec));
        //                audioStream.Children.Add(new TreeNode("Sample Rate ", parts[3] + " Hz"));
        //                audioStream.Children.Add(new TreeNode("Channels ", parts[4]));
        //            }
        //        }
        //        if (videoStream != null) CodecInfo.Add(videoStream);
        //        if (audioStream != null) CodecInfo.Add(audioStream);
        //    }
        //}
       

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnPropertyChanged: {ex.Message}");
            }
        }

    }
    public class TreeNode : ObservableObject
    {
        private string _name;
        private string _value;
        private ObservableCollection<TreeNode> _children;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public ObservableCollection<TreeNode> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }

        public TreeNode(string name, string value = "")
        {
            Name = name;
            Value = value;
            Children = new ObservableCollection<TreeNode>();
        }
    }
}
