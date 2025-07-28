using MAM.Data;
using MAM.Views.AdminPanelViews.Metadata;
using MAM.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews.AssetMetadata
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AssetMetadata : Page
    {
        public MediaPlayerViewModel ViewModel { get; set; }
        public ObservableCollection<MetadataClass> FilteredMetadata { get; set; } = new();
        private DataAccess dataAccess = new();
        public AssetMetadata()
        {
            this.InitializeComponent();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is MediaPlayerViewModel viewmodel)
            {
                ViewModel = (MediaPlayerViewModel)viewmodel;
                this.DataContext = ViewModel;
                GetAllMetadata();
                //mediaPath =viewmodel.Media.MediaPath.ToString();
                //categoryTable = dataAccess.GetData("select category_id,category_name,parent_id from metadata_category");
                //ConvertDataTableToListAsync(categoryTable, AllCategories);
                //AllCategories = LoadAllCategories();
                //GetAssetCategories();
                //HasCategories = AssetCategories.Any();
            }
        }
        private void GetAllMetadata()
        {
            MetadataClass NewMetadata;
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select metadata_id,metadata_name,metadata_type from metadata");
            ViewModel.AllMetadata.Clear();
            foreach (DataRow row in dt.Rows)
            {
                NewMetadata = new MetadataClass();
                NewMetadata.MetadataId = Convert.ToInt32(row[0]);
                NewMetadata.Metadata = row[1].ToString();
                NewMetadata.MetadataType = row[2].ToString();
                ViewModel.AllMetadata.Add(NewMetadata);
            }
        }
        public void UpdateSuggestions(string query)
        {
            FilteredMetadata.Clear();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var filtered = ViewModel.AllMetadata.Where(m => m.Metadata.Contains(query, StringComparison.OrdinalIgnoreCase));
                foreach (var meta in filtered)
                    FilteredMetadata.Add(meta);
                if (filtered.Count() == 0)
                    FilteredMetadata.Add(new MetadataClass { Metadata = "No results found" });
            }
            else
            {
                // If the query is empty (like after pressing Backspace), show all metadata
                foreach (var meta in ViewModel.AllMetadata)
                    FilteredMetadata.Add(meta);
            }
        }
        private void MetadataAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                //if (sender.Text == "")
                //    FilteredMetadata = ViewModel.AllMetadata;
                UpdateSuggestions(sender.Text);
                
                sender.ItemsSource = FilteredMetadata.Any() ? FilteredMetadata : null;
            }
        }

        private void MetadataAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is MetadataClass selected)
            {
                ViewModel.SelectedMetadata = selected; // ✅ Update text with chosen tag
                sender.Text = selected.Metadata;
            }
        }
        private async void MetadataButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Get the DataContext of the button (which should be AssetsMetadata)
                if (button.DataContext is AssetsMetadata metadataItem)
                {
                   if(await UpdateMetadata(metadataItem.MetadataValue, metadataItem.MetadataId)>0)
                    await GlobalClass.Instance.AddtoHistoryAsync("Update Metadata", $"Added metadata '{metadataItem.Metadata}'={metadataItem.MetadataValue} ");
                }
            }
        }

        private async Task<int> UpdateMetadata(string metadataValue, int id)
        {
            List<MySqlParameter> propsList = new();
            propsList.Add(new MySqlParameter("@asset_id", ViewModel.Media.MediaId));
            propsList.Add(new MySqlParameter("@metadata_id", id));
            propsList.Add(new MySqlParameter("@metadata_value", metadataValue));
            var(affectedRows,lastUpdatedId, errorMessage)=await dataAccess.ExecuteNonQuery("INSERT INTO asset_metadata (asset_id, metadata_id, metadata_value)" +
                " VALUES (@asset_id,@metadata_id, @metadata_value)" +
                " ON DUPLICATE KEY UPDATE" +
                " metadata_value = VALUES(@metadata_value);", propsList);
            if(!string.IsNullOrEmpty(errorMessage))
            {
                await GlobalClass.Instance.ShowDialogAsync($"Unable to update metadata '{metadataValue} \n {errorMessage}", this.Content.XamlRoot);
                return -1;
            }
            else
                return affectedRows;
        }
        private async void AddMetadata_Click(object sender, RoutedEventArgs e)
        {
            AssetsMetadata assetsMetadata = new AssetsMetadata { AssetId = ViewModel.Media.MediaId, Metadata = ViewModel.SelectedMetadata.Metadata };
            if (ViewModel.Media.AssetMetadataList.Where(m => m.MetadataId == ViewModel.SelectedMetadata.MetadataId).Count() == 0)
                ViewModel.Media.AssetMetadataList.Add(assetsMetadata);
            else
               await GlobalClass.Instance.ShowDialogAsync($"'{ViewModel.SelectedMetadata.Metadata}' already exists !", this.Content.XamlRoot);

        }
       
        private void RemoveMetadata_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
