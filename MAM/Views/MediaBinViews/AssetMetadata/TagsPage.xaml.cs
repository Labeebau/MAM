using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.System; // For VirtualKey
using Microsoft.UI.Xaml.Input; // For KeyRoutedEventArgs (in WinUI 3)

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.MediaBinViews.AssetMetadata
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TagsPage : Page,INotifyPropertyChanged
    {
        //private List<string> suggestions = new List<string>
        //{
        //    "Apple", "Banana", "Cherry", "Date", "Fig", "Grapes", "Mango", "Orange", "Pineapple", "Strawberry"
        //};
        private DataAccess dataAccess = new();

        public event PropertyChangedEventHandler PropertyChanged;

        public Windows.MediaPlayerViewModel Asset { get; private set; }
        private ObservableCollection<Tag> allTags { get;  set; } = new();
        public TagViewModel ViewModel { get; set; }
        public Tag SelectedAssetTag { get; set; }
        
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public TagsPage()
        {
            this.InitializeComponent();
            ViewModel = new TagViewModel();
            DataContext = ViewModel;
        }
        private void TagsAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.UpdateSuggestions(sender.Text);
                if(ViewModel.Suggestions.Count>0)
                        sender.ItemsSource = ViewModel.Suggestions; // Update suggestion list

            }
        }

        private void TagsAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is Tag selectedTag && selectedTag!=null)
            {
                ViewModel.NewTag.TagName = selectedTag.TagName; // Set selected tag
                ViewModel.NewTag.TagId =selectedTag.TagId;
                //sender.Text = selectedTag.TagName;
                // Refocus the AutoSuggestBox after a short delay to avoid timing issues
                Task.Delay(50);
                sender.Focus(FocusState.Programmatic);
            }

        }
        private void TagsAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion == null)
                AddTag();
        }
        private void AddKeyword_Click(object sender, RoutedEventArgs e)
        {

        }


        protected override  void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Windows.MediaPlayerViewModel viewmodel)
            {
                Asset = (Windows.MediaPlayerViewModel)viewmodel;
                GetAllTags();
                GetAssetTags();
            }
        }

        private void GetAllTags()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData("select tag_id,tag_name from tag");
            allTags.Clear();
            foreach (DataRow row in dt.Rows)
            {
                allTags.Add(new Tag
                {
                    TagId = Convert.ToInt32(row[0]),
                    TagName = row[1].ToString(),
                });
            }
            ViewModel.AllTags = allTags.ToList();
        }
        private void GetAssetTags()
        {
            DataTable dt = new DataTable();
            dt = dataAccess.GetData($"select t.tag_id,t.tag_name from tag t inner join asset_tag a on t.tag_id=a.tag_id where a.asset_id={Asset.Media.MediaId}");
            ViewModel.AssetTags.Clear();
            foreach (DataRow row in dt.Rows)
            {
                ViewModel.AssetTags.Add(new Tag
                {
                    TagId = Convert.ToInt32(row[0]),
                    TagName = row[1].ToString(),
                });
                ViewModel.HasTags = ViewModel.AssetTags.Any();
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


        private async Task<int> InsertTagToAsset(int assetId, string tagName)
        {
            List<MySqlParameter> parameters = new ();
            parameters.Add(new MySqlParameter("@AssetId", assetId));
            parameters.Add(new MySqlParameter("@TagName", tagName));
            string query = "INSERT INTO asset_tag(asset_id, tag_id) " +
                    "VALUES(@AssetId, (SELECT tag_id FROM tag WHERE tag_name = @TagName))";
            var (affectedRows, newMetadataId, errorMessage) = await dataAccess.ExecuteNonQuery(query, parameters);
            if (affectedRows > 0)
            {
                await GlobalClass.Instance.AddtoHistoryAsync("Add tag to asset", $"Added tag '{tagName}' to asset '{Asset.Media.MediaSource.LocalPath}' ");
                return affectedRows;
            }
            else
            {
                await GlobalClass.Instance.ShowDialogAsync($"Unable to add tag {tagName} \n {errorMessage}", this.XamlRoot);
                return -1;
            }
        }
        private async Task<int> InsertTag(string tagName)
        {
            List<MySqlParameter> parameters = new ();
            string query = string.Empty;
            parameters.Add(new MySqlParameter("@TagName", tagName));
            query = "INSERT INTO tag(tag_name) VALUES(@TagName)";

            string errorMessage = string.Empty;

            var (affectedRows, newTagId, errorCode) = await dataAccess.ExecuteNonQuery(query, parameters);
           if(affectedRows>0)
            {
                await GlobalClass.Instance.AddtoHistoryAsync("Add tag", $"Added tag '{tagName}' ");
                return newTagId;
            }
            else
            {
               await GlobalClass.Instance.ShowDialogAsync ($"Unable to add tag {errorMessage}", XamlRoot);
                return -1;
            }
        }

        private  void AddTag_Click(object sender, RoutedEventArgs e)
        {
           AddTag();
        }
        private async void AddTag()
        { 
            if (ViewModel.NewTag.TagName != null && ViewModel.NewTag.TagName!="No results found")
            {
                if (!ViewModel.AllTags.Any(t => t.TagName.Equals(ViewModel.NewTag.TagName, StringComparison.OrdinalIgnoreCase)))
                {
                    int result = await InsertTag(ViewModel.NewTag.TagName);
                    if (result > 0)
                    {
                        ViewModel.UpdateSuggestions(ViewModel.NewTag.TagName);
                        GetAllTags();

                    }
                }
                    if (!ViewModel.AssetTags.Any(t => t.TagName.Equals(ViewModel.NewTag.TagName, StringComparison.OrdinalIgnoreCase)))
                        AddTagToAsset(ViewModel.NewTag.TagName);
                    else
                        await GlobalClass.Instance.ShowDialogAsync("Tag already exists", this.XamlRoot);
                ViewModel.NewTag = new Tag();

            }
        }

        private async void RemoveTagFromAsset_Click(object sender, RoutedEventArgs e)
        {


            if (ViewModel.HasTags)
            {
                var button = sender as Button;
                var tag = button?.Tag as Tag;
               
                await DeleteTagFromAssetAsync(tag);
            }
        }
        private async Task<int> DeleteTagFromAssetAsync(Tag tag)
        {
            if (tag != null)
            {
                //ContentDialogResult result= await GlobalClass.Instance.ShowErrorDialogAsync($"Are you sure you want to delete {SelectedAssetTag.TagName}?", this.XamlRoot,"Delete","Cancel", "Delete Confirmation");

                //if (result == ContentDialogResult.Primary)
                //{
                    List<MySqlParameter> parameters = new ();
                    string query = string.Empty;
                    parameters.Add(new MySqlParameter("@AssetId", Asset.Media.MediaId));
                    parameters.Add(new MySqlParameter("@TagName", tag.TagName));
                    string errorMessage = string.Empty;
                    query = "delete from asset_tag where asset_id = @AssetId and tag_id = (SELECT tag_id FROM tag WHERE tag_name = @TagName LIMIT 1)";
                    var (affectedRows, newId, errorCode) = await dataAccess.ExecuteNonQuery(query, parameters);
                    if(affectedRows>0)
                    {
                        ViewModel.AssetTags.Remove(tag);
                        await GlobalClass.Instance.AddtoHistoryAsync("Delete tag from asset", $"Delete tag '{tag.TagName}' from asset '{Asset.Media.MediaSource.LocalPath}' ");
                        GetAssetTags();
                        return affectedRows;
                    }
                    else
                    {
                        await GlobalClass.Instance.ShowDialogAsync(string.IsNullOrWhiteSpace(errorMessage) ? "An unknown error occurred while trying to delete tag." : errorMessage, this.XamlRoot,"Error!!!");
                        return -1;
                    }
                }
                else
                    return -1;
            //}
            //else
            //{
            //    await GlobalClass.Instance.ShowErrorDialogAsync("Select any Tag", this.XamlRoot);
            //    return -1;
            //}
        }
        private async Task DeleteTagAsync()
        {
            if (ViewModel.NewTag != null)
            {
                if(ViewModel.NewTag.TagId<=0)
                {
                    await GlobalClass.Instance.ShowDialogAsync("No such tag exists.Try selecting from the suggestions.", this.XamlRoot,"Error!!!");
                    return;
                }
                ContentDialogResult result=await GlobalClass.Instance.ShowDialogAsync($"Are you sure you want to delete {ViewModel.NewTag.TagName}?", this.XamlRoot,"Delete","Cancel", "Delete Confirmation");
                if (result == ContentDialogResult.Primary)
                {

                    string errorMessage = string.Empty;
                    int errorCode = 0;
                    if (dataAccess.Delete("tag", "tag_id", ViewModel.NewTag.TagId, out errorMessage, out errorCode))
                    {
                        await GlobalClass.Instance.AddtoHistoryAsync("Delete tag", $"Deleted tag '{ViewModel.NewTag.TagName}' ");
                        allTags.Remove(ViewModel.NewTag);
                        GetAllTags();
                    }
                    else
                    {
                        if (errorCode == 1451)
                            errorMessage = $"Cannot delete {ViewModel.NewTag.TagName}.  {ViewModel.NewTag.TagName} is assigned to assets ";
                        await GlobalClass.Instance.ShowDialogAsync(string.IsNullOrWhiteSpace(errorMessage)? "An unknown error occurred while trying to delete tag.": errorMessage,this.XamlRoot);
                    }
                }
                else
                {
                    await GlobalClass.Instance.ShowDialogAsync("Select any Tag",this.XamlRoot);
                }
            }
        }
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if(e.AddedItems.Count>0)
            //SelectedAssetTag = (Tag)((ListView)sender).SelectedItem;
        }

        private async void AddTagToAsset(string tag)
        {
            if (Asset.Media.MediaId != 0 && !string.IsNullOrEmpty(tag))
            {
                if (await InsertTagToAsset(Asset.Media.MediaId, ViewModel.NewTag.TagName) > 0)
                {
                    Asset.MetadataUpdatedCallback?.Invoke(); // Call back to MediaLibraryPage
                    GetAssetTags();
                }
            }
        }

        private async void RemoveTag_Click(object sender, RoutedEventArgs e)
        {
            await DeleteTagAsync();
            //ViewModel.UpdateSuggestions(ViewModel.NewTag.TagName);
            ViewModel.NewTag = new Tag();
            GetAllTags();
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
        private ObservableCollection<Tag> _suggestions;
        private bool hasTags = false;
        private ObservableCollection<Tag> assetTags;
        private List<Tag> allTags;  // Store all available tags for searching

        public List<Tag> AllTags  // Store all available tags for searching
         {
            get => allTags;
            set => SetProperty(ref allTags, value);
        }
        public Tag NewTag
        {
            get => newTag;
            set => SetProperty(ref newTag, value);
        }

        public ObservableCollection<Tag> Suggestions
        {
            get => _suggestions;
            set => SetProperty(ref _suggestions, value);
        }
        public ObservableCollection<Tag> AssetTags 
        {
            get => assetTags;
            set { SetProperty(ref assetTags, value); }
        }
        public bool HasTags
        {
            get => hasTags;
            set => SetProperty(ref hasTags, value);
        }
        public TagViewModel()
        {
            NewTag = new Tag();
            _suggestions = new ObservableCollection<Tag>();
            AssetTags = new ObservableCollection<Tag>();
            // Example list of tags (You can load this from the database)
            //_allTags = new List<string> { "C#", "WinUI", "XAML", "FFmpeg", "Video", "Audio", "Editing" };
        }
        public void UpdateSuggestions(string query)
        {
            Suggestions.Clear();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var filteredTags = AllTags
                    .Where(t => t.TagName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var tag in filteredTags)
                    Suggestions.Add(tag);
                if (Suggestions.Count() == 0)
                    Suggestions.Add(new Tag{ TagId = 0, TagName = "No results found" });
            }
            else
            {
                foreach (var tag in AllTags)
                    Suggestions.Add(tag);
            }
        }
       
    }
  

}
