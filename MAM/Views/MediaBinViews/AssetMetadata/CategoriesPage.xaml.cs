using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews.AssetMetadata
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CategoriesPage : Page
    {
        DataAccess dataAccess=new DataAccess();
        private List<MetadataCategory> AssetCategories = new();
        public ObservableCollection<MetadataCategory> AllCategories { get; set; } = new();

        public CategoriesPage()
        {
            this.InitializeComponent();
        }

        public string mediaPath { get; private set; }
        DataTable categoryTable;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Dictionary<string, object> fileInfo)
            {
                mediaPath = fileInfo["Path"].ToString();
                categoryTable = dataAccess.GetData("select category_id,category_name,parent_id from metadata_category");
                ConvertDataTableToList(categoryTable);
                AllCategories= LoadAllCategories();
            }
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
        private void ConvertDataTableToList(DataTable dataTable)
        {
            foreach (DataRow row in dataTable.Rows)
            {
                AllCategories.Add(new MetadataCategory
                { 
                    CategoryId = Convert.ToInt32(row["category_id"]),
                    CategoryName = Convert.ToString(row["category_name"]),
                    ParentId = (row["parent_id"] == DBNull.Value) ? (int?)null : Convert.ToInt32(row["parent_id"])
                });
            }
        }
       
        private void AddToCategory_Click(object sender, RoutedEventArgs e)
        {
            //dataAccess.GetData("SELECT asset_id FROM asset WHERE asset_name = ")
            //dataAccess.GetData("SELECT category_id FROM metadata_category WHERE category_name IN ('Category A', 'Category B');")
            //    INSERT INTO asset_category(asset_id, category_id) VALUES(1, 2), (1, 3);

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

        

    }
    public class MetadataCategory:ObservableObject
    {
        private int categoryId;
        private string categoryName;
        private int? parentId;
        private bool isEditing=false;

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
