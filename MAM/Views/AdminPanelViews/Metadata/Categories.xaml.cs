using MAM.Data;
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.AdminPanelViews.Metadata
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Categories : Page
    {
        DataAccess dataAccess = new DataAccess();
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<MetadataCategory> MetadataCategoryList { get; set; } = [];
        public MetadataCategoryViewModel ViewModel { get; set; }
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Categories()
        {
            this.InitializeComponent();
            ViewModel = new MetadataCategoryViewModel();
            GetMetadataCategorys();
            DataContext = this;
        }
        private void GetMetadataCategorys(ObservableCollection<MetadataCategory> metadataList = null)
        {
            MetadataCategory NewMetadataCategory;
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select category_id,category_name from metadata_Categories");
            MetadataCategoryList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                NewMetadataCategory = new MetadataCategory();
                NewMetadataCategory.MetadataCategoryId = Convert.ToInt32(row[0]);
                NewMetadataCategory.MetadataCategoryName = row[1].ToString();
                if (metadataList == null)
                {
                    MetadataCategoryList.Add(NewMetadataCategory);
                }
                else
                    metadataList.Add(NewMetadataCategory);
            }
        }
        private int InsertMetadataCategory(MetadataCategory metadataCategory)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string query = string.Empty;
            parameters.Add("@MetadataCategory", metadataCategory.MetadataCategoryName);
            query = $"insert into metadata_Categories(category_name) values(@MetadataCategory) ";
            int newMetadataId = 0;
            dataAccess.ExecuteNonQuery(query, parameters, out newMetadataId);
            return newMetadataId;
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Visible;
            AddOrEditTextBlock.Text = "Add Metadata Category";
            ViewModel.MetadataCategory = new MetadataCategory();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            GetMetadataCategorys();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.MetadataCategory.EditMode)
            {
                UpdateMetadataCategory();
                ViewModel.MetadataCategory.EditMode = false;
            }
            else
            {
                if (InsertMetadataCategory(ViewModel.MetadataCategory) > 0)
                    ViewModel.MetadataCategory = new MetadataCategory();
            }
            GetMetadataCategorys();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MetadataCategory = new MetadataCategory();
        }
        private async void DeleteMetadataCategory_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var metadataCategory = button?.Tag as MetadataCategory;
            ContentDialog deleteDialog = new ContentDialog
            {
                Title = "Delete Confirmation",
                Content = $"Are you sure you want to delete {metadataCategory.MetadataCategoryName}?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot // Required in WinUI 3 for displaying dialogs
            };

            // Show the dialog
            ContentDialogResult result = await deleteDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {

                if (metadataCategory != null)
                {
                    string errorMessage = string.Empty;
                    if (dataAccess.Delete("metadata_Categorys", "Category_id", metadataCategory.MetadataCategoryId, out errorMessage))
                        MetadataCategoryList.Remove(metadataCategory);
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
        private void EditMetadataCategory_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Visible;
            AddOrEditTextBlock.Text = "Edit Metadata Category";
            var button = sender as Button;
            var metadataCategory = button?.Tag as MetadataCategory;
            metadataCategory.EditMode = true;
            ViewModel.MetadataCategory = metadataCategory;
        }

        private void UpdateMetadataCategory()
        {
            Dictionary<string, object> propsList = new Dictionary<string, object>();
            propsList.Add("Category_name", ViewModel.MetadataCategory.MetadataCategoryName);
            dataAccess.UpdateRecord("metadata_Categorys", "Category_id", ViewModel.MetadataCategory.MetadataCategoryId, propsList);
        }

        private void Authorize_Click(object sender, RoutedEventArgs e)
        {

        }
    }
    public class MetadataCategory : ObservableObject
    {
        private int metadataCategoryId;
        private string metadataCategoryName;
        private bool editMode = false;

        public int MetadataCategoryId
        {
            get => metadataCategoryId;
            set => SetProperty(ref metadataCategoryId, value);
        }
        public string MetadataCategoryName
        {
            get => metadataCategoryName;
            set => SetProperty(ref metadataCategoryName, value);
        }
        public bool EditMode
        {
            get => editMode;
            set => SetProperty(ref editMode, value);
        }
    }

    public class MetadataCategoryViewModel : ObservableObject
    {
        private MetadataCategory metadataCategory;

        public MetadataCategoryViewModel()
        {
            metadataCategory = new MetadataCategory();
        }
        public MetadataCategory MetadataCategory { get => metadataCategory; set => SetProperty(ref metadataCategory, value); }
    }
}