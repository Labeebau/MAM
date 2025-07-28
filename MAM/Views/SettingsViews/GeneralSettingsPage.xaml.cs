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
using MAM.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM.Views.SettingsViews
{
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GeneralSettingsPage : Page
    {
        GeneralSettings viewModel;
        public GeneralSettingsPage()
        {
            this.InitializeComponent();
            NumberComboBox.ItemsSource = Enumerable.Range(1, 100).ToList();
            viewModel = new GeneralSettings();
            viewModel = GeneralSettings.LoadFromService();
            DataContext=viewModel;
        }

        private void ChbConfirmMovement_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChbSearchOnArchive_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChbshowStoryBoard_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ChbNeverTimeout_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SaveToService();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NumberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ChbMoveToRecycleBin_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
    public class GeneralSettings : ObservableObject
    {
        private string language;
        private bool confirmMovement;
        private bool searchOnArchive;
        private bool showStoryBoard;
        private int existingFileTimeOut;
        private int defaultPageSize;
        private bool moveToRecycleBin;

        public string Language
        {
            get => language;
            set => SetProperty(ref language, value);
        }
        public bool ConfirmMovement
        {
            get => confirmMovement;
            set => SetProperty(ref confirmMovement, value);
        }
        public bool MoveToRecycleBin
        {
            get => moveToRecycleBin;
            set => SetProperty(ref moveToRecycleBin, value);
        }
        public bool SearchOnArchive
        {
            get => searchOnArchive;
            set => SetProperty(ref searchOnArchive, value);
        }
        public bool ShowStoryBoard
        {
            get => showStoryBoard;
            set => SetProperty(ref showStoryBoard, value);
        }
        public int ExistingFileTimeOut
        {
            get => existingFileTimeOut;
            set => SetProperty(ref existingFileTimeOut, value);
        }
        public int DefaultPageSize
        {
            get => defaultPageSize;
            set => SetProperty(ref defaultPageSize, value);
        }
        public static GeneralSettings LoadFromService()
        {
            return new GeneralSettings
            {
                Language = SettingsService.Get(SettingKeys.Language, "en-US"),
                ConfirmMovement = SettingsService.Get(SettingKeys.ConfirmMovement, true),
                MoveToRecycleBin=SettingsService.Get(SettingKeys.MoveToRecycleBin,true),
                SearchOnArchive = SettingsService.Get(SettingKeys.SearchOnArchive, false),
                ShowStoryBoard = SettingsService.Get(SettingKeys.ShowStoryBoard, true),
                ExistingFileTimeOut = SettingsService.Get(SettingKeys.ExistingFileTimeOut, 30),
                DefaultPageSize = SettingsService.Get(SettingKeys.DefaultPageSize, 20)
            };
        }

        public void SaveToService()
        {
            SettingsService.Set(SettingKeys.Language, Language);
            SettingsService.Set(SettingKeys.ConfirmMovement, ConfirmMovement);
            SettingsService.Set(SettingKeys.MoveToRecycleBin, MoveToRecycleBin);
            SettingsService.Set(SettingKeys.SearchOnArchive, SearchOnArchive);
            SettingsService.Set(SettingKeys.ShowStoryBoard, ShowStoryBoard);
            SettingsService.Set(SettingKeys.ExistingFileTimeOut, ExistingFileTimeOut);
            SettingsService.Set(SettingKeys.DefaultPageSize, DefaultPageSize);
        }
    }
}
