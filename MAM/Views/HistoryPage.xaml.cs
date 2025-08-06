using MAM.Data;
using MAM.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using System.Data;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Drawing.Layout;
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
            GlobalFontSettings.FontResolver = new CustomFontResolver();


        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel = new History();
            GetHistory();
            GetActions();
            GetUsers();
            // ? Set date constraints in code
            FromDateSearchBox.MinDate = new DateTimeOffset(new DateTime(2000, 1, 1));
            FromDateSearchBox.MaxDate = new DateTimeOffset(new DateTime(2099, 12, 31));
            ToDateSearchBox.MinDate = new DateTimeOffset(new DateTime(2000, 1, 1));
            ToDateSearchBox.MaxDate = new DateTimeOffset(new DateTime(2099, 12, 31));
            FromDateSearchBox.Date = DateTime.Now;
            ToDateSearchBox.Date = DateTime.Now;
            ViewModel.ApplyFilter();
            DataContext = ViewModel;

        }
        private void GetUsers()
        {
            ViewModel.UserList.Add("All Users");
            DataTable dt = new DataTable();
            dt = dataAccess.GetData($"select user_name FROM user;");
            foreach (DataRow row in dt.Rows)
            {
                ViewModel.UserList.Add(row[0].ToString());
            }

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
            ViewModel.ActionList.Add("All");
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
        private async void ExportToExcel()
        {
            var data = ViewModel.FilteredHistory;

            if (data == null || !data.Any())
            {
                await GlobalClass.Instance.ShowDialogAsync("No data to export.", this.XamlRoot);
                return;
            }
            // Initialize the picker with the current window
            var hwnd = WindowNative.GetWindowHandle(App.MainAppWindow);
            var savePicker = new FileSavePicker();
            InitializeWithWindow.Initialize(savePicker, hwnd);
            savePicker.SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("CSV file", new List<string>() { ".csv" });
            savePicker.SuggestedFileName = "HistoryExport";

            var file = await savePicker.PickSaveFileAsync();

            if (file != null)
            {
                var csvLines = new List<string>
                {
                    "User Name,Action,Description,Date"
                };

                foreach (var item in data)
                {
                    string line = $"\"{item.UserName}\",\"{item.Action}\",\"{item.Description}\",\"{item.Date}\"";
                    csvLines.Add(line);
                }

                await FileIO.WriteLinesAsync(file, csvLines);
                await GlobalClass.Instance.ShowDialogAsync("Export complete.", this.XamlRoot);
            }
        }
        public async Task ExportToPdfAsync(ObservableCollection<History> data)
        {
            // Show File Save Picker
            var savePicker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainAppWindow); // Or pass your window
            InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("PDF Files", new List<string>() { ".pdf" });
            savePicker.SuggestedFileName = "ExportedHistory";

            var file = await savePicker.PickSaveFileAsync();
            if (file == null)
                return; // user cancelled

            // Export PDF using PDFsharp
            var document = new PdfSharp.Pdf.PdfDocument();
            var fontBold = new XFont("Arial", 12, XFontStyleEx.Bold);
            var font = new XFont("Arial", 10, XFontStyleEx.Regular);
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            double margin = 40;
            double y = margin;
            double tableWidth = Width - 2 * margin;

            // Define column widths
            double userWidth = 60;
            double actionWidth = 100;
            double descriptionWidth = 300;
            double dateWidth = tableWidth - userWidth - actionWidth - descriptionWidth;

            // Draw headers
            void DrawHeader()
            {
                gfx.DrawString("User", fontBold, XBrushes.Black, new XRect(margin, y, userWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString("Action", fontBold, XBrushes.Black, new XRect(margin + userWidth, y, actionWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString("Description", fontBold, XBrushes.Black, new XRect(margin + userWidth + actionWidth, y, descriptionWidth, 20), XStringFormats.TopLeft);
                gfx.DrawString("Date", fontBold, XBrushes.Black, new XRect(margin + userWidth + actionWidth + descriptionWidth, y, dateWidth, 20), XStringFormats.TopLeft);
                y += 25;
            }

            DrawHeader();

            foreach (var item in data)
            {
                string description = item.Description;

                // Estimate line count from the longest text field (Description here)
                double blockHeight = EstimateBlockHeight(description, font, descriptionWidth, gfx);


                // Page break if needed
                if (y + blockHeight > page.Height.Point - margin)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    y = margin;
                    DrawHeader();
                }

                // Define column rectangles
                var userRect = new XRect(margin, y, userWidth, blockHeight);
                var actionRect = new XRect(margin + userWidth, y, actionWidth, blockHeight);
                var descriptionRect = new XRect(margin + userWidth + actionWidth, y, descriptionWidth, blockHeight);
                var dateRect = new XRect(margin + userWidth + actionWidth + descriptionWidth, y, dateWidth, blockHeight);

                // Draw using text formatter for all columns to keep alignment consistent
                var tf = new XTextFormatter(gfx);
                tf.DrawString(item.UserName, font, XBrushes.Black, userRect, XStringFormats.TopLeft);
                tf.DrawString(item.Action, font, XBrushes.Black, actionRect, XStringFormats.TopLeft);
                tf.DrawString(description, font, XBrushes.Black, descriptionRect, XStringFormats.TopLeft);
                tf.DrawString(item.Date.ToString("yyyy-MM-dd HH:mm"), font, XBrushes.Black, dateRect, XStringFormats.TopLeft);

                // Draw light horizontal separator
                y += blockHeight + 5;
                gfx.DrawLine(XPens.LightGray, margin, y, margin + tableWidth, y);
            }


            // Save using a stream from StorageFile
            using (var stream = await file.OpenStreamForWriteAsync())
            {
                document.Save(stream, false);
                await GlobalClass.Instance.ShowDialogAsync("Export complete.", this.XamlRoot);

            }
        }
        double EstimateBlockHeight(string text, XFont font, double width, XGraphics gfx)
        {
            var words = text.Split(' ');
            var line = "";
            int lines = 1;

            foreach (var word in words)
            {
                var testLine = line.Length == 0 ? word : line + " " + word;
                var size = gfx.MeasureString(testLine, font);
                if (size.Width > width)
                {
                    lines++;
                    line = word;
                }
                else
                {
                    line = testLine;
                }
            }

            double lineHeight = gfx.MeasureString("A", font).Height + 2;
            return lines * lineHeight;
        }


        //private async Task ExportToPdfAsync(ObservableCollection<History> historyList)
        //{
        //    var document = new PdfDocument();
        //    var page = document.AddPage();
        //    var gfx = XGraphics.FromPdfPage(page);

        //    double margin = 40;
        //    double y = margin;

        //    // Fonts
        //    var headerFont = new XFont("Arial", 14, XFontStyleEx.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));

        //    var font = new XFont("Arial", 12, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));

        //    // Column headers
        //    string[] headers = { "User", "Action", "Description", "Date" };
        //    double[] columnWidths = { 80, 100, 250, 120 }; // adjust as needed
        //    XSize textSize = gfx.MeasureString("Sample", font);
        //    double lineHeight = textSize.Height + 4;

        //    // Draw headers
        //    double x = margin;
        //    for (int i = 0; i < headers.Length; i++)
        //    {
        //        gfx.DrawString(headers[i], headerFont, XBrushes.Black, new XRect(x, y, columnWidths[i], lineHeight), XStringFormats.TopLeft);
        //        x += columnWidths[i];
        //    }
        //    y += lineHeight;
        //    foreach (var item in historyList)
        //    {
        //        x = margin;

        //        string[] row = {
        //                            item.UserName ?? "",
        //                            item.Action ?? "",
        //                            item.Description ?? "",
        //                            item.Date.ToString("yyyy-MM-dd HH:mm")  
        //                        };


        //        // Measure max row height
        //        double rowHeight = row.Select((text, i) =>
        //        {
        //            var layout = gfx.MeasureString(text, font);
        //            double lines = Math.Ceiling(layout.Width / columnWidths[i]);
        //            return lines * lineHeight;
        //        }).Max();

        //        // Check for page overflow
        //        if (y + rowHeight > page.Height.Point - margin)
        //        {
        //            page = document.AddPage();
        //            gfx = XGraphics.FromPdfPage(page);
        //            y = margin;
        //        }

        //        // Draw each cell with wrapping
        //        for (int i = 0; i < row.Length; i++)
        //        {
        //            var tf = new XTextFormatter(gfx);
        //            var rect = new XRect(x, y, columnWidths[i], rowHeight);
        //            tf.DrawString(row[i], font, XBrushes.Black, rect, XStringFormats.TopLeft);
        //            x += columnWidths[i];
        //        }

        //        y += rowHeight;
        //    }

        //    // Save file dialog
        //    var picker = new FileSavePicker();
        //    picker.SuggestedStartLocation = PickerLocationId.Desktop;
        //    picker.FileTypeChoices.Add("PDF Document", new List<string>() { ".pdf" });
        //    picker.SuggestedFileName = "ExportedHistory";
        //    picker.DefaultFileExtension = ".pdf";

        //    IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow);
        //    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        //    var file = await picker.PickSaveFileAsync();
        //    if (file != null)
        //    {
        //        using var stream = await file.OpenStreamForWriteAsync();
        //        document.Save(stream);
        //        await GlobalClass.Instance.ShowDialogAsync("Export complete.", this.XamlRoot);

        //    }
        //}

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            //ViewModel.FilterUser = UserDropDown.Content.ToString();
            // ViewModel.FilterAction = testListView.SelectedItem.ToString();
            ViewModel.FilterDescription = DescriptionTextBox.Text;
            ViewModel.FilterFromDate = FromDateSearchBox.Date?.DateTime;
            ViewModel.FilterToDate = ToDateSearchBox.Date?.DateTime;
            ViewModel.ApplyFilter();
        }

        private void OnActionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActionDropDown.Content = ViewModel.FilterAction;
            if (ViewModel.FilterAction == ("All"))
                ViewModel.FilterAction = string.Empty;
            ActionDropDown.Flyout.Hide();
        }

        private void OnUserSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserDropDown.Content = ViewModel.FilterUser;
            if (ViewModel.FilterUser == ("All Users"))
                ViewModel.FilterUser = string.Empty;
            UserDropDown.Flyout.Hide();
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private async void PdfMenuFlyout_Click(object sender, RoutedEventArgs e)
        {
            await ExportToPdfAsync(ViewModel.FilteredHistory);
        }

        private void ExcelMenuFlyout_Click(object sender, RoutedEventArgs e)
        {
            ExportToExcel();
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


        public string FilterUser
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
        public List<string> UserList { get; set; } = new List<string>();

        public void ApplyFilter()
        {
            FilteredHistory.Clear();

            // Default From and To date to today if not set
            var fromDate = FilterFromDate ?? DateTime.Today;
            var toDate = FilterToDate ?? DateTime.Today;

            var filteredItems = HistoryList.Where(history =>
                                (string.IsNullOrEmpty(FilterUser) || history.UserName.Contains(FilterUser, StringComparison.OrdinalIgnoreCase)) &&
                                (string.IsNullOrEmpty(FilterAction) || history.Action.Contains(FilterAction, StringComparison.OrdinalIgnoreCase)) &&
                                (string.IsNullOrEmpty(FilterDescription) || history.Description.Contains(FilterDescription, StringComparison.OrdinalIgnoreCase)) &&
                                history.Date.Date >= fromDate.Date &&
                                history.Date.Date <= toDate.Date);

            foreach (var item in filteredItems)
            {
                FilteredHistory.Add(item);
            }
        }

        //public void ApplyFilter()
        //{
        //    FilteredHistory.Clear();
        //    var filteredItems = HistoryList.Where(history =>
        //                        (string.IsNullOrEmpty(FilterUSer) || history.UserName.Contains(FilterUSer, StringComparison.OrdinalIgnoreCase)) &&
        //                        (string.IsNullOrEmpty(filterAction) || history.Action.Contains(filterAction, StringComparison.OrdinalIgnoreCase)) &&
        //                        (string.IsNullOrEmpty(filterDescription) || history.Description.Contains(filterDescription, StringComparison.OrdinalIgnoreCase)) &&
        //                        (!FilterFromDate.HasValue || history.Date >= FilterFromDate.Value) &&
        //                        (!FilterToDate.HasValue || history.Date <= FilterToDate.Value));

        //    foreach (var item in filteredItems)
        //    {
        //        FilteredHistory.Add(item);
        //    }

        //}
        public void ClearFilters()
        {
            FilterUser = string.Empty;
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
    public class CustomFontResolver : IFontResolver
    {
        public byte[] GetFont(string faceName)
        {
            string fontPath = @"C:\Windows\Fonts\arial.ttf"; // Or Verdana, etc.
            return File.ReadAllBytes(fontPath);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase))
            {
                return new FontResolverInfo("Arial#");
            }

            return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
        }
    }


}
