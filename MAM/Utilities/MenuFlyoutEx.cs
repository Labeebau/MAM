using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Globalization;

namespace MAM.Utilities
{
    public class MenuFlyoutEx:MenuFlyout
    {
        public static readonly DependencyProperty ItemsSourceProperty =
       DependencyProperty.Register(
           nameof(ItemsSource),
           typeof(IEnumerable),
           typeof(MenuFlyoutEx),
           new PropertyMetadata(default, OnItemsSourcePropertyChanged));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(MenuFlyoutEx),
                new PropertyMetadata(default));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not MenuFlyoutEx menuFlyoutEx ||
                e.NewValue is not IEnumerable itemsSource)
            {
                return;
            }

            menuFlyoutEx.Items.Clear();

            foreach (object item in itemsSource)
            {
                MenuFlyoutItem menuFlyoutItem = new()
                {
                    Text = item.ToString(),
                    Command = menuFlyoutEx.Command,
                    CommandParameter = item,
                };

                menuFlyoutEx.Items.Add(menuFlyoutItem);
            }
        }
    }
    public partial class DropDownViewModel : ObservableObject
    {

        // The CommunityToolkit.Mvvm will generate a "ChangeLanguageCommand" for you.
       // [RelayCommand]
        //private void ChangeLanguage(Languages language)
        //{
        //    // Do your logic here...
        //}
        //// The CommunityToolkit.Mvvm will generate a "LanguageOptions" property for you.
        //[ObservableProperty]
        //private Languages[] _languageOptions = Enum.GetValues<Languages>();
    }
}
