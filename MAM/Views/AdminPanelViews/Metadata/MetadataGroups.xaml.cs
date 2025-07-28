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
    public sealed partial class MetadataGroups : Page
    {
        DataAccess dataAccess = new DataAccess();
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<MetadataGroup> MetadataGroupList { get; set; } = [];
        public MetadataGroupViewModel ViewModel { get; set; }
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public MetadataGroups()
        {
            this.InitializeComponent();
            ViewModel = new MetadataGroupViewModel();
            GetMetadataGroups();
            DataContext = this;
        }
        private void GetMetadataGroups(ObservableCollection<MetadataGroup> metadataList = null)
        {
            MetadataGroup NewMetadataGroup;
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select group_id,group_name from metadata_groups");
            MetadataGroupList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                NewMetadataGroup = new MetadataGroup();
                NewMetadataGroup.MetadataGroupId = Convert.ToInt32(row[0]);
                NewMetadataGroup.MetadataGroupName = row[1].ToString();
                if (metadataList == null)
                {
                    MetadataGroupList.Add(NewMetadataGroup);
                }
                else
                    metadataList.Add(NewMetadataGroup);
            }
        }
        private async Task<int> InsertMetadataGroup(MetadataGroup metadataGroup)
        {
            List<MySqlParameter> parameters = new ();
            parameters.Add(new MySqlParameter( "@MetadataGroup", metadataGroup.MetadataGroupName));
            string query = $"insert into metadata_groups(group_name) values(@MetadataGroup) ";
            var(affectedRows, newMetadataId, errorMessage) =await dataAccess.ExecuteNonQuery(query, parameters);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                await GlobalClass.Instance.ShowErrorDialogAsync(errorMessage, this.XamlRoot);
                return -1;
            }
            else
                return newMetadataId;
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Visible;
            AddOrEditTextBlock.Text = "Add Metadata Group";
            ViewModel.MetadataGroup = new MetadataGroup();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            GetMetadataGroups();
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.MetadataGroup.EditMode)
            {
                UpdateMetadataGroup();
                ViewModel.MetadataGroup.EditMode = false;
            }
            else
            {
                int result = await InsertMetadataGroup(ViewModel.MetadataGroup);
                if (result > 0)
                    ViewModel.MetadataGroup = new MetadataGroup();
            }
            GetMetadataGroups();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MetadataGroup=new MetadataGroup ();   
        }
        private async void DeleteMetadataGroup_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var metadataGroup = button?.Tag as MetadataGroup;
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Delete Confirmation",
                Content = $"Are you sure you want to delete {metadataGroup.MetadataGroupName}?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot // Required in WinUI 3 for displaying dialogs
            };

            // Show the dialog
            ContentDialogResult result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {

                if (metadataGroup != null)
                {
                    string errorMessage = string.Empty;
                    int errorCode = 0;
                    if (dataAccess.Delete("metadata_groups", "group_id", metadataGroup.MetadataGroupId, out errorMessage, out errorCode))
                        MetadataGroupList.Remove(metadataGroup);
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
        private void EditMetadataGroup_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Visible;
            AddOrEditTextBlock.Text = "Edit Metadata Group";
            var button = sender as Button;
            var metadataGroup = button?.Tag as MetadataGroup;
            metadataGroup.EditMode = true;
            ViewModel.MetadataGroup = metadataGroup;
        }

        private void UpdateMetadataGroup()
        {
            Dictionary<string, object> propsList = new Dictionary<string, object>();
            propsList.Add("group_name", ViewModel.MetadataGroup.MetadataGroupName);
            dataAccess.UpdateRecord("metadata_groups", "group_id", ViewModel.MetadataGroup.MetadataGroupId, propsList);
        }

        private void Authorize_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public class MetadataGroup : ObservableObject
    {
        private int metadataGroupId;
        private string metadataGroupName;
        private bool editMode = false;

        public int MetadataGroupId
        {
            get => metadataGroupId;
            set => SetProperty(ref metadataGroupId, value);
        }
        public string MetadataGroupName
        {
            get => metadataGroupName;
            set => SetProperty(ref metadataGroupName, value);
        }
        public bool EditMode
        {
            get => editMode;
            set => SetProperty(ref editMode, value);
        }
    }

    public class MetadataGroupViewModel : ObservableObject
    {
        private MetadataGroup metadataGroup;

        public MetadataGroupViewModel()
        {
            metadataGroup = new MetadataGroup();
        }
        public MetadataGroup MetadataGroup { get => metadataGroup; set => SetProperty(ref metadataGroup, value); }
    }
}
