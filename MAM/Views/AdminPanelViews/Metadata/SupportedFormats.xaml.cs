using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews.Metadata
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SupportedFormats : Page
    {
        DataAccess dataAccess = new DataAccess();
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<Format> FormatList { get; set; } = [];
        public ObservableCollection<string> FileTypeList { get; set; } = [];

        public FormatViewModel ViewModel { get; set; }
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public SupportedFormats()
        {
            this.InitializeComponent();
            ViewModel = new FormatViewModel();
            GetFormats();
            DataContext = this;
        }
        private void GetFormats(ObservableCollection<Format> formatList = null)
        {
            Format NewFormat;
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select ft.file_type,f.format_id,f.extension,f.description from format f inner join file_type ft on ft.file_id=f.file_id");
            FormatList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                NewFormat = new Format();
                NewFormat.Type = row["file_type"].ToString();
                NewFormat.FormatId =Convert.ToInt32(row["format_id"].ToString());
                NewFormat.Extension = row["extension"].ToString();
                NewFormat.Description = row["description"].ToString();

                if (formatList == null)
                {
                    FormatList.Add(NewFormat);
                }
                else
                    formatList.Add(NewFormat);
            }
        }
        private void GetFileTypes()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select file_id, file_type from  file_type");
            FileTypeList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                FileTypeList.Add(row["file_type"].ToString());
            }
        }
        private int InsertFormat(Format Format)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string query = string.Empty;
            parameters.Add("@file_type", Format.Type);
            parameters.Add("@extension", Format.Extension);
            parameters.Add("@description", Format.Description);
            query = $"INSERT INTO format (file_id,extension,description) "+
                $"SELECT file_id,'{Format.Extension}','{Format.Description}' FROM file_type WHERE file_type='{Format.Type}'; ";
            int newMetadataId = 0;
            dataAccess.ExecuteNonQuery(query, parameters, out newMetadataId);
            return newMetadataId;
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Visible;
            AddOrEditTextBlock.Text = "Add Format";
            ViewModel.Format = new Format();
            GetFileTypes();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            GetFormats();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            //if (ViewModel.Format.EditMode)
            //{
            //    UpdateFormat();
            //    ViewModel.Format.EditMode = false;
            //}
            //else
            //{

                if (InsertFormat(ViewModel.Format) > 0)
                    ViewModel.Format = new Format();
            //}
            GetFormats();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Format = new Format();
        }
        private async void DeleteFormat_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var format = button?.Tag as Format;
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Delete Confirmation",
                Content = $"Are you sure you want to delete {format.Extension}?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot // Required in WinUI 3 for displaying dialogs
            };

            // Show the dialog
            ContentDialogResult result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {

                if (format != null)
                {
                    string errorMessage = string.Empty;
                    if (dataAccess.Delete("format", "format_id", format.FormatId, out errorMessage))
                        FormatList.Remove(format);
                    else
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "Delete Failed",
                            Content = string.IsNullOrWhiteSpace(errorMessage)
                        ? "An unknown error occurred while trying to delete metadata."
                        : errorMessage,
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot // Assuming this code is in a page or window with access to XamlRoot
                        };

                        await dialog.ShowAsync();
                    }
                }
            }
        }
        private void EditFormat_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Visible;
            AddOrEditTextBlock.Text = "Edit Metadata Group";
            var button = sender as Button;
            var Format = button?.Tag as Format;
            Format.EditMode = true;
            ViewModel.Format = Format;
        }

        //private void UpdateFormat()
        //{
        //    Dictionary<string, object> propsList = new Dictionary<string, object>();
        //    propsList.Add("group_name", ViewModel.Format.Type);
        //    dataAccess.UpdateRecord("metadata_groups", "group_id", ViewModel.Format.FormatId, propsList);
        //}

        private void Authorize_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FileTypeCombo.Content = ViewModel.Format.Type;
            FileTypeFlyOut.Hide();  
        }
    }
    public class Format : ObservableObject
    {
        private int formatId;
        private string type;
        private string extension;
        private string description=string.Empty;

        private bool editMode = false;

        public int FormatId
        {
            get => formatId;
            set => SetProperty(ref formatId, value);
        }
        public string Type
        {
            get => type;
            set => SetProperty(ref type, value);
        }
        public string Extension
        {
            get => extension;
            set => SetProperty(ref extension, value);
        }
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }
        public bool EditMode
        {
            get => editMode;
            set => SetProperty(ref editMode, value);
        }
    }

    public class FormatViewModel : ObservableObject
    {
        private Format format;

        public FormatViewModel()
        {
            Format = new Format();
        }
        public Format Format { get => format; set => SetProperty(ref format, value); }
    }
}
