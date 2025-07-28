using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MAM
{
	public sealed class AssetControl : Control
	{
		public AssetControl()
		{
			this.DefaultStyleKey = typeof(AssetControl);
		}
		public string Asset
		{
			get => (string)GetValue(AssetProperty);
			set => SetValue(AssetProperty, value);
		}
		DependencyProperty AssetProperty = DependencyProperty.Register(
	nameof(Asset),
	typeof(string),
	typeof(AssetControl),
	new PropertyMetadata(default(string), new PropertyChangedCallback(OnLabelChanged)));

		public bool HasLabelValue { get; set; }

		private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			AssetControl assetControl = d as AssetControl; //null checks omitted
			String s = e.NewValue as String; //null checks omitted
			if (s == String.Empty)
			{
				assetControl.HasLabelValue = false;
			}
			else
			{
				assetControl.HasLabelValue = true;
			}
		}
	}
}
