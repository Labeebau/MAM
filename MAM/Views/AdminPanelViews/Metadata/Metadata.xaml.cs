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
    public sealed partial class Metadata : Page
    {
        DataAccess dataAccess = new DataAccess();
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<MetadataClass> MetadataList { get; set; } = [];
        public ObservableCollection<string> MetadataTypes { get; set; }
        private MetadataClass newMetadata;
        public MetadataViewModel ViewModel { get; set; }
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Metadata()
        {
            this.InitializeComponent();
            ViewModel = new MetadataViewModel();
            GetMetadata();
            GetMetadataTypes();

            DataContext = this;
        }
        public string SelectedItem { get; set; }
        private void GetMetadata(ObservableCollection<MetadataClass> metadataList = null)
        {
            MetadataClass NewMetadata;
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select metadata_id,metadata_name,metadata_type from metadata");
            MetadataList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                NewMetadata = new MetadataClass();
                NewMetadata.MetadataId =Convert.ToInt32(row[0]);
                NewMetadata.Metadata = row[1].ToString();
                NewMetadata.MetadataType = row[2].ToString();
                if (metadataList == null)
                {
                    MetadataList.Add(NewMetadata);
                }
                else
                    metadataList.Add(NewMetadata);
            }
        }
        private void GetMetadataTypes()
        {
            MetadataTypes = new ObservableCollection<string> { "Int", "Double", "Bool", "String", "Text", "Date", "List" };
        }
        private int InsertMetadata(MetadataClass metadata)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string query = string.Empty;
            parameters.Add("@Metadata", metadata.Metadata);
            parameters.Add("@MetadataType", metadata.MetadataType);
            query = $"insert into metadata(metadata_name,metadata_type) values(@Metadata,@MetadataType) ";
            int newMetadataId = 0, errorCode = 0;
            dataAccess.ExecuteNonQuery(query, parameters, out newMetadataId, out errorCode);
            return newMetadataId;
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Visible;
            AddOrEditTextBlock.Text = "Add Metadata";
            ViewModel.Metadata = new MetadataClass();
            GetMetadataTypes();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            GetMetadata();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Metadata.EditMode)
            {
                UpdateMetadata();
                ViewModel.Metadata.EditMode = false;
            }
            else
            {
                if (InsertMetadata(ViewModel.Metadata) > 0)
                    ViewModel.Metadata = new MetadataClass();
            }
            GetMetadata();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private async void DeleteMetadata_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var metadata = button?.Tag as MetadataClass;
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Delete Confirmation",
                Content = $"Are you sure you want to delete {metadata.Metadata}?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot // Required in WinUI 3 for displaying dialogs
            };

            // Show the dialog
            ContentDialogResult result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                
                if (metadata != null)
                {
                    string errorMessage = string.Empty;
                    int errorCode = 0;
                    if (dataAccess.Delete("metadata", "metadata_id", metadata.MetadataId,out errorMessage, out errorCode))
                        MetadataList.Remove(metadata);
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
        private void EditMetadata_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Visible;
            AddOrEditTextBlock.Text = "Edit Metadata";
            var button = sender as Button;
            var metadata = button?.Tag as MetadataClass;
            metadata.EditMode = true;
            ViewModel.Metadata = metadata;
            
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MetadataTypeCombo.Content = ViewModel.Metadata.MetadataType;
            MetadataTypeFlyOut.Hide();
        }
        private void UpdateMetadata()
        {
            Dictionary<string, object> propsList = new Dictionary<string, object>();
            propsList.Add("metadata_name", ViewModel.Metadata.Metadata);
            propsList.Add("metadata_type", ViewModel.Metadata.MetadataType);
            dataAccess.UpdateRecord("metadata", "metadata_id", ViewModel.Metadata.MetadataId, propsList);
        }
    }
    public class MetadataClass : ObservableObject
    {
        private int metadataId;
        private string metadata;
        private string metadataType;
        private bool editMode = false;

        public int MetadataId
        {
            get => metadataId;
            set => SetProperty(ref metadataId, value);
        }
        public string Metadata
        {
            get => metadata;
            set => SetProperty(ref metadata, value);
        }
        public string MetadataType
        {
            get => metadataType;
            set => SetProperty(ref metadataType, value);
        }
        
        public bool EditMode
        {
            get => editMode;
            set => SetProperty(ref editMode, value);
        }
    }

    public class MetadataViewModel : ObservableObject
    {
        private MetadataClass metadata;

        public MetadataViewModel()
        {
            metadata = new MetadataClass();
        }
        public MetadataClass Metadata { get => metadata; set => SetProperty(ref metadata, value); }
    }

            
   
}
