using MAM.Data;
using MAM.Utilities;
using MAM.Views.MediaBinViews.AssetMetadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using Windows.System;

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
        //public ObservableCollection<MetadataCategory> MetadataCategoryList
        //{
        //    get { return metadataCategoryList; }
        //    set { metadataCategoryList = value;OnPropertyChanged("MetadataCategoryList"); }
        //}
        // public ObservableCollection<MetadataCategory> MetadataCategoryList { get; set; } = [];
        public MetadataCategoryViewModel ViewModel { get; set; }
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Categories()
        {
            this.InitializeComponent();
            ViewModel = new MetadataCategoryViewModel();
            LoadCategories();
            DataContext = this;
        }
        private async void GetMetadataCategories()
        {
            DataTable dt = new DataTable();
            dt =await dataAccess.GetDataAsync("select category_id,category_name,parent_id from metadata_category");
            if (MetadataCategoryList != null)
            {
                MetadataCategoryList.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    MetadataCategoryList.Add(new MetadataCategory
                    {
                        CategoryId = Convert.ToInt32(row[0]),
                        CategoryName = row[1].ToString(),
                        ParentId = (row["parent_id"] == DBNull.Value) ? (int?)null : Convert.ToInt32(row["parent_id"])
                    });
                }
            }
        }
        public void LoadCategories()
        {
            GetMetadataCategories();
            var categories = new ObservableCollection<MetadataCategory>();

            // Fetch all categories from the database
            // var allCategories = await GetCategoriesFromDatabaseAsync(); // Implement this method

            // Dictionary for quick lookup
            var categoryMap = MetadataCategoryList.ToDictionary(c => c.CategoryId);

            // Build hierarchical structure
            foreach (var category in MetadataCategoryList)
            {
                if (category.ParentId == null)
                {
                    //category.IsEditing = true;
                    categories.Add(category); // Root categories

                }
                else if (categoryMap.ContainsKey(category.ParentId.Value))
                {
                    categoryMap[category.ParentId.Value].SubCategories.Add(category); // Add as child
                }
            }

            MetadataCategoryList = categories;
        }
        private async Task<int> InsertMetadataCategoryAsync(MetadataCategory metadataCategory, bool IssubCategory)
        {
            List<MySqlParameter> parameters = new ();
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@MetadataCategory", metadataCategory.CategoryName));
            parameters.Add(new MySqlParameter("@ParentId", metadataCategory.ParentId));
            if (IssubCategory)
            {
                query = "INSERT INTO metadata_Category (category_name, parent_id)" +
                    "SELECT * FROM (SELECT @MetadataCategory,@ParentId) AS tmp" +
                    " WHERE NOT EXISTS (" +
                    "SELECT 1 FROM metadata_Category WHERE parent_id = @ParentId AND category_name = @MetadataCategory);";
            }
            else
            {
                query = $"insert into metadata_category(category_name) values(@MetadataCategory) ";
            }

            var (affectedRows, newMetadataId, errorMessage) = await Task.Run(() => dataAccess.ExecuteNonQuery(query, parameters));
            if (affectedRows > 0)
            {
               
                return newMetadataId;
            }
            else
            {
                DeleteFromList(metadataCategory);
                await GlobalClass.Instance.ShowDialogAsync($"Unable to save category. \r\n {errorMessage}", this.XamlRoot);
                return -1;
            }

        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Visible;
            AddOrEditTextBlock.Text = "Add Metadata Category";
            ViewModel.MetadataCategory = new MetadataCategory();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCategories();
            RefreshTreeView();
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            int result = await InsertMetadataCategoryAsync(ViewModel.MetadataCategory, false);

            if (result > 0)
            {
                LoadCategories();
                RefreshTreeView();
            }
        }


        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.MetadataCategory = new MetadataCategory();
        }
        private async void DeleteMetadataCategory(MetadataCategory category)
        {
            ContentDialogResult result = await GlobalClass.Instance.ShowDialogAsync($"Are you sure you want to delete {category.CategoryName}?", XamlRoot, "Delete", "Cancel", "Delete Confirmation");
            if (result == ContentDialogResult.Primary)
            {
                if (category != null)
                {

                    (int rowsAffected, string errorMessage, int errorCode) = await dataAccess.Delete("metadata_category", "category_id", category.CategoryId);
                    if (rowsAffected>0) 
                    {
                        MetadataCategoryList.Remove(category);
                        LoadCategories();
                        RefreshTreeView();
                    }
                    else
                    {
                        if (errorCode == 1451)
                            errorMessage = $"Cannot delete {category.CategoryName}. Assets are assigned to {category.CategoryName} ";
                        await GlobalClass.Instance.ShowDialogAsync(string.IsNullOrWhiteSpace(errorMessage)? "An unknown error occurred while trying to delete metadata.": errorMessage,this.XamlRoot, "Delete Failed");
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
            // ViewModel.EditMode = true;
            ViewModel.MetadataCategory = metadataCategory;
        }

        private async void UpdateMetadataCategory(MetadataCategory category)
        {
            Dictionary<string, object> propsList = new Dictionary<string, object>();
            propsList.Add("category_name", category.CategoryName);
            await dataAccess.UpdateRecord("metadata_category", "category_id", category.CategoryId, propsList);
        }

        private void Authorize_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {

        }

        //private void StartEditing(object sender, RoutedEventArgs e)
        //{
        //    if (sender is Button button && button.Tag is MetadataCategory category)
        //    {
        //        DispatcherQueue.TryEnqueue(() => category.IsEditing = true);
        //    }
        //}

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                _ = FinishEditingAsync(sender, e);
            }
        }

        private async Task FinishEditingAsync(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is MetadataCategory category)
            {
                if (category.CategoryId > 0)
                {
                    category.CategoryName = textBox.Text;
                    UpdateMetadataCategory(category);
                }
                else
                {
                    if (category.ParentId == null)
                    {
                        category.CategoryName = textBox.Text;
                        SaveCategory(category);
                    }
                    else
                    {
                        MetadataCategory parentCategory = FindParentCategory(MetadataCategoryList, category);
                        if (parentCategory.SubCategories.Where(s => s.CategoryName == textBox.Text).Count() > 0)
                        {

                            await GlobalClass.Instance.ShowDialogAsync("Subcategory already exists", this.XamlRoot);
                            parentCategory.SubCategories.Remove(category);
                            // textBox.Text = "New Subcategory";
                            return;
                        }
                        else
                        {
                            category.CategoryName = textBox.Text;
                            SaveCategory(category);
                        }
                    }
                }
            }
        }
        private async void SaveCategory(MetadataCategory category)
        {
            int result =await InsertMetadataCategoryAsync(category, true);
            if (result> 0)
            {
                category.IsEditing = false; // Exit edit mode
                CategoryTreeView.SelectedItem = category;
            }
        }



        private void RefreshTreeView()
        {
            //var itemsSource = CategoryTreeView.ItemsSource;
            //CategoryTreeView.ItemsSource = null;
            CategoryTreeView.ItemsSource = MetadataCategoryList;
        }

        private void StartEditing(object sender, TappedRoutedEventArgs e)
        {

        }

        private void AddSubcategory_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RenameCategory_Click(object sender, RoutedEventArgs e)
        {

        }

      

        private void TreeViewItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MetadataCategory category)
            {
                // Find the TreeViewItem that contains the category
                var treeViewItem = GetTreeViewItem(element);
                if (treeViewItem == null) return;
                treeViewItem.IsExpanded = true;
                // Create the context menu
                MenuFlyout menuFlyout = new MenuFlyout();
                // Add Subcategory option
                var addItem = new MenuFlyoutItem { Text = "Add Subcategory", Icon = new SymbolIcon(Symbol.Add) };
                addItem.Click += (s, args) => AddSubcategory(category);
                menuFlyout.Items.Add(addItem);
                // Rename option
                var renameItem = new MenuFlyoutItem { Text = "Rename", Icon = new SymbolIcon(Symbol.Edit) };
                renameItem.Click += (s, args) => RenameCategory(category);
                menuFlyout.Items.Add(renameItem);
                // Delete option
                var deleteItem = new MenuFlyoutItem { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) };
                deleteItem.Click += (s, args) => DeleteCategory(category);
                menuFlyout.Items.Add(deleteItem);
                // Show the context menu
                menuFlyout.ShowAt(treeViewItem, new FlyoutShowOptions { Position = e.GetPosition(treeViewItem) });
            }
        }

        // Helper method to find TreeViewItem
        private TreeViewItem GetTreeViewItem(DependencyObject element)
        {
            while (element != null)
            {
                if (element is TreeViewItem item) return item;
                element = VisualTreeHelper.GetParent(element);
            }
            return null;
        }




        private  void AddSubcategory(MetadataCategory category)
        {
            if (category == null) return;
            var newSubCategory = new MetadataCategory
            {
                CategoryName = "New Subcategory",
                ParentId = category.CategoryId,
                IsEditing = true
            };
            category.SubCategories.Add(newSubCategory);
            CategoryTreeView.SelectedItem = newSubCategory;


        }



        private void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Focus(FocusState.Programmatic);
                textBox.SelectAll();
            }
        }

        private void StartEditing(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is MetadataCategory category)
            {
                category.IsEditing = true;
            }
        }
        private void RenameCategory(MetadataCategory category)
        {
            if (category == null) return;
            category.IsEditing = true;
        }

        private void DeleteCategory(MetadataCategory category)
        {
            if (category == null) return;
            //   DeleteFromList(category);
            DeleteMetadataCategory(category);
            
        }
        private void DeleteFromList(MetadataCategory category)
        {
            MetadataCategory parentCategory = FindParentCategory(MetadataCategoryList, category);
            if (parentCategory != null)
            {
                parentCategory.SubCategories.Remove(category); //Remove from parent's list
            }
            else
            {
                MetadataCategoryList.Remove(category); // Remove from the root list
            }
            MetadataCategoryList.Remove(category);
        }
        private MetadataCategory? FindParentCategory(IEnumerable<MetadataCategory> categories, MetadataCategory childCategory)
        {
            foreach (var category in categories)
            {
                if (category.SubCategories.Contains(childCategory))
                {
                    return category; // Found the parent
                }

                var parent = FindParentCategory(category.SubCategories, childCategory);
                if (parent != null)
                {
                    return parent; // Recursively find parent
                }
            }
            return null;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            RightPanel.Visibility = Visibility.Collapsed;
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