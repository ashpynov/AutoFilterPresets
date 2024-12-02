using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using AutoFilterPresets.Setings.Models;


namespace AutoFilterPresets.Settings.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            ((IComponentConnector)this).InitializeComponent();
        }
        private void FolderTB_ScrolltoEnd(object sender, EventArgs e)
        {
            if (sender is TextBox tb && !tb.IsFocused)
            {
                tb.ScrollToHorizontalOffset(tb.ActualWidth);
            }
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            (DataContext as SettingsViewModel).Settings.SelectedFilter = e.NewValue as SortingItem;
        }
    }
}