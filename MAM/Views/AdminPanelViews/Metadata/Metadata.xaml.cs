using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MySql.Data.MySqlClient;
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
        private async Task<int> InsertMetadata(MetadataClass metadata)
        {
            List<MySqlParameter> parameters = new ();
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@Metadata", metadata.Metadata));
            parameters.Add(new MySqlParameter("@MetadataType", metadata.MetadataType));
            query = $"insert into metadata(metadata_name,metadata_type) values(@Metadata,@MetadataType) ";
            var(affectedRows, newMetadataId,errorMessage)=await dataAccess.ExecuteNonQuery(query, parameters);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await GlobalClass.Instance.ShowDialogAsync($"Failed to insert metadata. \n {errorMessage}", this.XamlRoot);
                return -1; // Indicate failure
            }
            else
                return newMetadataId; // Return ID if insert was successful
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
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            int result = 0;
            if (ViewModel.Metadata.EditMode)
            {
                UpdateMetadata();
                ViewModel.Metadata.EditMode = false;
            }
            else
            {
                result = await InsertMetadata(ViewModel.Metadata);
                if (result> 0)
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
            ContentDialogResult result=await GlobalClass.Instance.ShowDialogAsync($"Are you sure you want to delete {metadata.Metadata}?", this.XamlRoot, "Delete", "Cancel", "Delete Confirmation");
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
                        await GlobalClass.Instance.ShowDialogAsync(string.IsNullOrWhiteSpace(errorMessage) ? "An unknown error occurred while trying to delete metadata.": errorMessage, this.XamlRoot, "Delete Failed");

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
        private async void UpdateMetadata()
        {
            Dictionary<string, object> propsList = new Dictionary<string, object>();
            propsList.Add("metadata_name", ViewModel.Metadata.Metadata);
            propsList.Add("metadata_type", ViewModel.Metadata.MetadataType);
            await dataAccess.UpdateRecord("metadata", "metadata_id", ViewModel.Metadata.MetadataId, propsList);
        }
    }
    public class MetadataClass : ObservableObject
    {
        private int metadataId;
        private string metadata;
        private string metadataType;
        private bool editMode = false;
        private bool isSelected=false;

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
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
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
