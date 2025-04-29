using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Org.BouncyCastle.Utilities.Collections;
using System.Collections.ObjectModel;
using System.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HistoryPage : Page
    {
        private DataAccess dataAccess = new DataAccess();

        // { "Login", "Create Asset","Edit Asset","Delete Asset","Update Metadata" };
        public History ViewModel { get; set; }
        public HistoryPage()
        {
            this.InitializeComponent();


        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel = new History();
            GetHistory();
            GetActions();
            DataContext = ViewModel;
            // ? Set date constraints in code
            FromDateSearchBox.MinDate = new DateTimeOffset(new DateTime(2000, 1, 1));
            FromDateSearchBox.MaxDate = new DateTimeOffset(new DateTime(2099, 12, 31));
            ToDateSearchBox.MinDate = new DateTimeOffset(new DateTime(2000, 1, 1));
            ToDateSearchBox.MaxDate = new DateTimeOffset(new DateTime(2099, 12, 31));
            FromDateSearchBox.Date = DateTime.Now;
            ToDateSearchBox.Date = DateTime.Now;
        }
        private void GetHistory()
        {
            string query = "select u.user_id, u.user_name,h.action,h.description,h.date from history h inner join user u on h.user_id=u.user_id  ORDER BY date DESC";
            DataTable dt = dataAccess.GetData(query);
            foreach (DataRow row in dt.Rows)
            {
                ViewModel.HistoryList.Add(new History
                {
                    UserId = Convert.ToInt32(row["user_id"]),
                    UserName = row["user_name"].ToString(),
                    Action = row["action"].ToString(),
                    Description = row["description"].ToString(),
                    Date = Convert.ToDateTime(row["date"]),
                });
            }
        }
        private void GetActions()
        {
            ViewModel.ActionList.Add("Select Action");
            foreach (History history in ViewModel.HistoryList)
            {
                if (!ViewModel.ActionList.Contains(history.Action))
                {
                    ViewModel.ActionList.Add(history.Action);
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.FilterUSer = UserNameTextBox.Text;
            // ViewModel.FilterAction = testListView.SelectedItem.ToString();
            ViewModel.FilterDescription = DescriptionTextBox.Text;
            ViewModel.FilterFromDate = FromDateSearchBox.Date?.DateTime;
            ViewModel.FilterToDate = ToDateSearchBox.Date?.DateTime;
            ViewModel.ApplyFilter();
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            ActionDropDown.Content = ViewModel.FilterAction;
            if (ViewModel.FilterAction == ("Select Action"))
                ViewModel.FilterAction = string.Empty;
            ActionDropDown.Flyout.Hide();

        }
    }
    public class History : ObservableObject
    {
        private int userId;
        private string userName;
        private string action;
        private string description;
        private DateTime date;
        private string filterUser = string.Empty;
        private string filterAction = string.Empty;
        private string filterDescription = string.Empty;
        private DateTime? filterFromDate = DateTime.Today;
        private DateTime? filterToDate = DateTime.Today;
        public int UserId
        {
            get => userId;
            set => SetProperty(ref userId, value);
        }

        public string UserName
        {
            get => userName; set => SetProperty(ref userName, value);
        }
        public string Action
        {
            get => action;
            set => SetProperty(ref action, value);
        }
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }
        public DateTime Date
        {
            get => date;
            set => SetProperty(ref date, value);
        }

        public string FilterUSer
        {
            get => filterUser;
            set { SetProperty(ref filterUser, value); }
        }
        public string FilterAction
        {
            get => filterAction;
            set { SetProperty(ref filterAction, value); }
        }
        public string FilterDescription
        {
            get => filterDescription;
            set { SetProperty(ref filterDescription, value); }
        }
        public DateTime? FilterFromDate
        {
            get => filterFromDate;
            set { SetProperty(ref filterFromDate, value); }
        }
        public DateTime? FilterToDate
        {
            get => filterToDate;
            set { SetProperty(ref filterToDate, value); }
        }
        public ObservableCollection<History> HistoryList { get; set; } = new();
        public ObservableCollection<History> FilteredHistory { get; set; } = new();
        public List<string> ActionList { get; set; } = new List<string>();
        public void ApplyFilter()
        {
            FilteredHistory.Clear();
            var filteredItems = HistoryList.Where(history =>
                                (string.IsNullOrEmpty(FilterUSer) || history.UserName.Contains(FilterUSer, StringComparison.OrdinalIgnoreCase)) &&
                                (string.IsNullOrEmpty(filterAction) || history.Action.Contains(filterAction, StringComparison.OrdinalIgnoreCase)) &&
                                (string.IsNullOrEmpty(filterDescription) || history.Description.Contains(filterDescription, StringComparison.OrdinalIgnoreCase)) &&
                                (!FilterFromDate.HasValue || history.Date >= FilterFromDate.Value) &&
                                (!FilterToDate.HasValue || history.Date <= FilterToDate.Value));

            foreach (var item in filteredItems)
            {
                FilteredHistory.Add(item);
            }

        }
        public void ClearFilters()
        {
            FilterUSer = string.Empty;
            FilterAction = string.Empty;
            FilterDescription = string.Empty;
            FilterFromDate = DateTime.Now;
            FilterToDate = DateTime.Now;
            FilteredHistory.Clear();
            foreach (var item in HistoryList)
            {
                FilteredHistory.Add(item);
            }

        }
    }
}
