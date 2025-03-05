using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MAM.Data;
using MAM.ViewModels;
using MAM.Windows;
using System.Collections.ObjectModel;
using System.Data;
using MAM.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews.AssetMetadata
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TagsPage : Page
    {
        //private List<string> suggestions = new List<string>
        //{
        //    "Apple", "Banana", "Cherry", "Date", "Fig", "Grapes", "Mango", "Orange", "Pineapple", "Strawberry"
        //};
        private DataAccess dataAccess = new();
        private DataTable tagTable;

        public MediaPlayerViewModel Asset { get; private set; }
        public ObservableCollection<Tag> AllTags { get; private set; } = new();
        public TagViewModel ViewModel { get; set; }

        public bool HasTags { get; set; }
        public TagsPage()
        {
            this.InitializeComponent();
            ViewModel = new TagViewModel();
            DataContext = ViewModel;
        }
        private void MyAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.UpdateSuggestions(sender.Text);
                sender.ItemsSource = ViewModel.Suggestions; // Update suggestion list
            }
        }

        private void MyAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is string selectedTag)
            {
                ViewModel.NewTag.TagName = selectedTag; // Set selected tag
            }
        }
       
        private void AddKeyword_Click(object sender, RoutedEventArgs e)
        {

        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is MediaPlayerViewModel viewmodel)
            {
                Asset = (MediaPlayerViewModel)viewmodel;
                GetAllTags();
                ViewModel._allTags=AllTags.Select(tag=>tag.TagName).ToList();
                GetAssetTags();
                HasTags =ViewModel.AssetTags.Any();
            }
        }

        private void GetAllTags()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select tag_id,tag_name from tag");
            AllTags.Clear();
            foreach (DataRow row in dt.Rows)
            {
                AllTags.Add(new Tag
                {
                    TagId = Convert.ToInt32(row[0]),
                    TagName = row[1].ToString(),
                });
            }
        }
        private void GetAssetTags()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData($"select t.tag_id,t.tag_name from tag t inner join asset_tag a on t.tag_id=a.tag_id where a.asset_id={Asset.Media.MediaId}");
            ViewModel.AssetTags.Clear();
            foreach (DataRow row in dt.Rows)
            {
                ViewModel.AssetTags.Add( new Tag {
                    TagId = Convert.ToInt32(row[0]),
                    TagName = row[1].ToString(),
                });
            }
        }


        private void AddToCategory_Click(object sender, RoutedEventArgs e)
        {
            //if (SelectedAllCategory != null)
            //{
            //    InsertAssetToCategory(Asset.Media.MediaId, SelectedAllCategory.CategoryName);
            //    GetAssetCategories();
            //}
            //else
            //{
            //    var dialog = new ContentDialog
            //    {

            //        Content = "Select any Category",
            //        CloseButtonText = "OK",
            //        Height = 10,
            //        XamlRoot = this.XamlRoot
            //    };
            //    _ = dialog.ShowAsync();
            //}

        }


        private int InsertTagToAsset(int assetId, string tagName)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string query = string.Empty;
            parameters.Add("@AssetId", assetId);
            parameters.Add("@TagName", tagName);
            query = "INSERT INTO asset_tag(asset_id, tag_id) " +
                    "VALUES(@AssetId, (SELECT tag_id FROM tag WHERE tag_name = @TagName))";


            int newMetadataId = 0, errorCode = 0;
            string errorMessage = string.Empty;
            if (dataAccess.ExecuteNonQuery(query, parameters, out newMetadataId, out errorCode) > 0)
            {
                //var dialog = new ContentDialog
                //{
                //    Title = "Saved",
                //    Content = "Category added successfully",
                //    CloseButtonText = "OK",
                //    Height = 10,
                //    XamlRoot = this.XamlRoot
                //};
                //_ = dialog.ShowAsync();
            }
            else
            {
                if (errorCode == 1062)
                    errorMessage = "Tag already added.";
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = !string.IsNullOrEmpty(errorMessage) ? errorMessage : "Cannot add tag",
                    CloseButtonText = "OK",
                    Height = 10,
                    XamlRoot = this.XamlRoot
                };
                _ = dialog.ShowAsync();
            }

            // MetadataCategoryList = LoadCategories();
            return newMetadataId;
        }
        private int InsertTag(string tagName)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string query = string.Empty;
            parameters.Add("@TagName", tagName);
            query = "INSERT INTO tag(tag_name) VALUES(@TagName)";


            int newTagId = 0, errorCode = 0;
            string errorMessage = string.Empty;
            if (dataAccess.ExecuteNonQuery(query, parameters, out newTagId, out errorCode) > 0)
            {
                //var dialog = new ContentDialog
                //{
                //    Title = "Saved",
                //    Content = "Category added successfully",
                //    CloseButtonText = "OK",
                //    Height = 10,
                //    XamlRoot = this.XamlRoot
                //};
                //_ = dialog.ShowAsync();
            }
            else
            {
                if (errorCode == 1062)
                    errorMessage = "Tag already exists.";
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = !string.IsNullOrEmpty(errorMessage) ? errorMessage : "Cannot add tag",
                    CloseButtonText = "OK",
                    Height = 10,
                    XamlRoot = this.XamlRoot
                };
                _ = dialog.ShowAsync();
            }

            // MetadataCategoryList = LoadCategories();
            return newTagId;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            if(ViewModel.NewTag.TagName!=null)
            InsertTag(ViewModel.NewTag.TagName);
        }

        private void RemoveTagFromAsset_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddTagToAsset_Click(object sender, RoutedEventArgs e)
        { 
            if(ViewModel.NewTag.TagName!=null)
            InsertTagToAsset(Asset.Media.MediaId,ViewModel.NewTag.TagName);
        }
    }
    public class Tag : ObservableObject
    {
        private int tagId;
        private string tagName;
        private bool isEditing = false;

        public int TagId
        {
            get => tagId;
            set => SetProperty(ref tagId, value);
        }
        public string TagName
        {
            get => tagName;
            set => SetProperty(ref tagName, value);
        }

        public Boolean IsEditing
        {
            get => isEditing;
            set => SetProperty(ref isEditing, value);
        }
    }
    //public class TagViewModel : ObservableObject
    //{
    //    private Tag  newTag;
    //    private string tagName;
    //    private bool isEditing = false;

    //    public Tag NewTag
    //    {
    //        get => newTag;
    //        set => SetProperty(ref newTag, value);
    //    }
    //    public TagViewModel()
    //    {
    //        newTag= new Tag();
    //    }
    //}


    public class TagViewModel : ObservableObject
    {
        private Tag newTag;
        private ObservableCollection<string> _suggestions;
        public List<string> _allTags;  // Store all available tags for searching

        public Tag NewTag
        {
            get => newTag;
            set => SetProperty(ref newTag, value);
        }

        public ObservableCollection<string> Suggestions
        {
            get => _suggestions;
            set => SetProperty(ref _suggestions, value);
        }
        public ObservableCollection<Tag> AssetTags { get; private set; }

        public TagViewModel()
        {
            NewTag = new Tag();
            _suggestions = new ObservableCollection<string>();
            AssetTags = new ObservableCollection<Tag>();
            // Example list of tags (You can load this from the database)
            //_allTags = new List<string> { "C#", "WinUI", "XAML", "FFmpeg", "Video", "Audio", "Editing" };
        }

        public void UpdateSuggestions(string query)
        {
            Suggestions.Clear();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var filteredTags = _allTags
                    .Where(t => t.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var tag in filteredTags)
                    Suggestions.Add(tag);
            }
        }
    }


}
