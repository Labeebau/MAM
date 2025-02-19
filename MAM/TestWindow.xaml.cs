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
using Microsoft.UI.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Common;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class TestWindow : Window
    {

        //public ObservableCollection<FileSystemItem> FileSystemItems { get; set; }

        public TestWindow()
        {
            this.InitializeComponent();
            //FileSystemItems = new ObservableCollection<FileSystemItem>();

            //// Load the file system hierarchy
            //var rootDirectory = @"F:\MAM"; // Change this to the desired root directory
            //var rootItem = CreateFileSystemItem(rootDirectory);
            //FileSystemItems.Add(rootItem);

            //FileTreeView.DataContext = this;
        }

        //private FileSystemItem CreateFileSystemItem(string path)
        //{
        //    var item = new FileSystemItem
        //    {
        //        Name = System.IO.Path.GetFileName(path),
        //        Path = path,
        //        IsDirectory = Directory.Exists(path)
        //    };

        //    if (item.IsDirectory)
        //    {
        //        try
        //        {
        //            foreach (var dir in Directory.GetDirectories(path))
        //            {
        //                item.Children.Add(CreateFileSystemItem(dir));
        //            }

        //            foreach (var file in Directory.GetFiles(path))
        //            {
        //                item.Children.Add(new FileSystemItem
        //                {
        //                    Name = System.IO.Path.GetFileName(file),
        //                    Path = file,
        //                    IsDirectory = false
        //                });
        //            }
        //        }
        //        catch (UnauthorizedAccessException)
        //        {
        //            // Handle access exceptions
        //        }
        //    }

        //    return item;
        //}
        //private void FileTreeView_Expanded(object sender, TreeViewExpandingEventArgs e)
        //{
        //    if (e.Item is FileSystemItem item && item.IsDirectory && !item.IsLoaded)
        //    {
        //        // Use a temporary list to avoid modifying the collection directly during iteration
        //        var newChildren = new List<FileSystemItem>();

        //        try
        //        {
        //            foreach (var dir in Directory.GetDirectories(item.Path))
        //            {
        //                newChildren.Add(new FileSystemItem
        //                {
        //                    Name = System.IO.Path.GetFileName(dir),
        //                    Path = dir,
        //                    IsDirectory = true
        //                });
        //            }

        //            foreach (var file in Directory.GetFiles(item.Path))
        //            {
        //                newChildren.Add(new FileSystemItem
        //                {
        //                    Name = System.IO.Path.GetFileName(file),
        //                    Path = file,
        //                    IsDirectory = false
        //                });
        //            }
        //        }
        //        catch (UnauthorizedAccessException)
        //        {
        //            // Handle access exceptions gracefully
        //        }

        //        // Update the ObservableCollection outside the event loop
        //        DispatcherQueue.TryEnqueue(() =>
        //         {
        //             item.Children.Clear(); // Safe to clear now
        //             foreach (var child in newChildren)
        //             {
        //                 item.Children.Add(child);
        //             }
        //             item.IsLoaded = true;
        //         });
        //    }
        //}



    }
    //public class FileSystemItem
    //{
    //    public string Name { get; set; }
    //    public string Path { get; set; }
    //    public bool IsDirectory { get; set; }
    //    public bool IsLoaded { get; set; }

    //    public ObservableCollection<FileSystemItem> Children { get; set; }

    //    public FileSystemItem()
    //    {
    //        Children = new ObservableCollection<FileSystemItem>();
    //    }
    //}
    //public class BoolToGlyphConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, string language)
    //    {
    //        if (value is bool isDirectory)
    //        {
    //            return isDirectory ? "\uE8B7" : "\uE8A5"; // Folder glyph and file glyph
    //        }

    //        return "\uE10E"; // Default glyph (error/unknown)
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, string language)
    //    {
    //        throw new NotImplementedException(); // One-way binding only
    //    }
    //}
}