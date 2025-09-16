using MAM.Data;
using MAM.Utilities;
using MAM.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews.AssetMetadata
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AssetCategoriesPage : Page
    {
        DataAccess dataAccess = new DataAccess();
        public ObservableCollection<MetadataCategory> AssetCategories { get; set; } = new();
        public ObservableCollection<MetadataCategory> AllCategories { get; set; } = new();
        public MediaPlayerViewModel Viewmodel { get; set; }
        public MetadataCategory SelectedAllCategory { get; set; }
        public MetadataCategory SelectedAssetCategory { get; set; }
        public bool HasCategories;

        public AssetCategoriesPage()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        // public string mediaPath { get; private set; }
        DataTable categoryTable;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is MediaPlayerViewModel viewmodel)
            {
                Viewmodel = (MediaPlayerViewModel)viewmodel;
                //mediaPath =viewmodel.Media.MediaPath.ToString();
                categoryTable =await dataAccess.GetDataAsync("select category_id,category_name,parent_id from metadata_category");
                ConvertDataTableToListAsync(categoryTable, AllCategories);
                AllCategories = LoadAllCategories();
                GetAssetCategories();
                HasCategories = AssetCategories.Any();
            }
        }
        private async void GetAssetCategories()
        {
            categoryTable.Clear();
            categoryTable =await dataAccess.GetDataAsync($"select c.category_id,c.category_name,c.parent_id from metadata_category c inner join asset_category a on c.category_id=a.category_id where a.asset_id={Viewmodel.Media.MediaId}");
            AssetCategories.Clear();
            ConvertDataTableToListAsync(categoryTable, AssetCategories);
        }
        //private void GetMetadataCategories()
        //{
        //    DataTable dt = new DataTable();
        //    dt = dataAccess.GetData("select category_id,category_name,parent_id from metadata_category");
        //    MetadataCategoryList.Clear();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        MetadataCategoryList.Add(new MetadataCategory
        //        {
        //            CategoryId = Convert.ToInt32(row[0]),
        //            CategoryName = row[1].ToString(),
        //            ParentId = (row["parent_id"] == DBNull.Value) ? (int?)null : Convert.ToInt32(row["parent_id"])
        //        });
        //    }
        //}
        private void LoadAssetCategory()
        {

        }


        private void ConvertDataTableToListAsync(DataTable dataTable, ObservableCollection<MetadataCategory> categoryList)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                categoryList.Add(new MetadataCategory
                {
                    CategoryId = Convert.ToInt32(row["category_id"]),
                    CategoryName = Convert.ToString(row["category_name"]),
                    ParentId = (row["parent_id"] == DBNull.Value) ? (int?)null : Convert.ToInt32(row["parent_id"])
                });
            }
        }

        private async void AddToCategory_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedAllCategory != null)
            {
                bool addedRows = await InsertAssetToCategory(Viewmodel.Media.MediaId, SelectedAllCategory.CategoryName);
                if (addedRows)
                {
                    await GlobalClass.Instance.AddtoHistoryAsync("Add asset to category", $"Added asset '{Viewmodel.Media.MediaSource.LocalPath}' to category '{SelectedAllCategory.CategoryName}' ");
                    GetAssetCategories();
                }
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync("Select any Category", this.XamlRoot);
            }

        }
        public ObservableCollection<MetadataCategory> LoadAllCategories()
        {
            var categories = new ObservableCollection<MetadataCategory>();

            // Fetch all categories from the database
            // var allCategories = await GetCategoriesFromDatabaseAsync(); // Implement this method

            // Dictionary for quick lookup
            var categoryMap = AllCategories.ToDictionary(c => c.CategoryId);

            // Build hierarchical structure
            foreach (var category in AllCategories)
            {
                if (category.ParentId == null)
                {
                    categories.Add(category); // Root categories
                }
                else if (categoryMap.ContainsKey(category.ParentId.Value))
                {
                    categoryMap[category.ParentId.Value].SubCategories.Add(category); // Add as child
                }
            }

            return categories;
        }

        private async Task<bool> InsertAssetToCategory(int assetId, string categoryName)
        {
            List<MySqlParameter> parameters = new();
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@AssetId", assetId));
            parameters.Add(new MySqlParameter("@CategoryName", categoryName));
            query = "INSERT INTO asset_category(asset_id, category_id) " +
                    "VALUES(@AssetId, (SELECT category_id FROM metadata_category WHERE category_name = @CategoryName))";
            var (affectedRows, lastInsertedId, errorMessage) = await dataAccess.ExecuteNonQuery(query, parameters);

            if (affectedRows <= 0)
            {
                await GlobalClass.Instance.ShowDialogAsync($"Unable to add Category {categoryName} \n {errorMessage}", this.XamlRoot);
                return false;
            }
            return affectedRows > 0;
        }



        private void CategoryTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            SelectedAllCategory = ((MetadataCategory)sender.SelectedNode.Content);
        }

        private async void RemoveCategoryFromAsset_Click(object sender, RoutedEventArgs e)
        {
            await DeleteCategoryFromAsset();
        }
        private async Task DeleteCategoryFromAsset()
        {
            if (SelectedAssetCategory != null)
            {
                ContentDialogResult result= await GlobalClass.Instance.ShowDialogAsync($"Are you sure you want to delete {SelectedAssetCategory.CategoryName}?", XamlRoot, "Delete", "Cancel", "Delete Confirmation");
               
                if (result == ContentDialogResult.Primary)
                {
                    List<MySqlParameter> parameters = new();
                    string query = string.Empty;
                    parameters.Add(new MySqlParameter("@AssetId", Viewmodel.Media.MediaId));
                    parameters.Add(new MySqlParameter("@CategoryName", SelectedAssetCategory.CategoryName));
                    parameters.Add(new MySqlParameter("@ParntId", SelectedAssetCategory.ParentId));
                    string errorMessage = string.Empty;
                    query = SelectedAssetCategory.ParentId != null
                        ? "delete from asset_category where asset_id = @AssetId and category_id = (SELECT category_id FROM metadata_category WHERE category_name = @CategoryName and parent_id = @ParntId LIMIT 1)"
                        : "delete from asset_category where asset_id = @AssetId and category_id = (SELECT category_id FROM metadata_category WHERE category_name = @CategoryName and parent_id IS NULL LIMIT 1)";

                    var (affectedRows, newId, errorCode) = await dataAccess.ExecuteNonQuery(query, parameters);
                    if (affectedRows > 0)
                    {
                        await GlobalClass.Instance.AddtoHistoryAsync("Delete asset from category", $"Deleted asset '{Viewmodel.Media.MediaSource.LocalPath}' from category '{SelectedAssetCategory.CategoryName}' ");
                        AssetCategories.Remove(SelectedAssetCategory);
                    }
                    else
                    {
                        await GlobalClass.Instance.ShowDialogAsync(string.IsNullOrWhiteSpace(errorMessage)
                         ? "An unknown error occurred while trying to delete category."
                         : errorMessage, this.XamlRoot);
                    }
                }
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync("Select any Category", this.XamlRoot);

            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedAssetCategory = (MetadataCategory)((ListView)sender).SelectedItem;
        }
    }
    public class MetadataCategory : ObservableObject
    {
        private int categoryId;
        private string categoryName;
        private int? parentId;
        private bool isEditing = false;

        public int CategoryId
        {
            get => categoryId;
            set => SetProperty(ref categoryId, value);
        }
        public string CategoryName
        {
            get => categoryName;
            set => SetProperty(ref categoryName, value);
        }
        public int? ParentId
        {
            get => parentId;
            set => SetProperty(ref parentId, value);
        }
        public Boolean IsEditing
        {
            get => isEditing;
            set => SetProperty(ref isEditing, value);
        }
        public ObservableCollection<MetadataCategory> SubCategories = new();
    }
}
