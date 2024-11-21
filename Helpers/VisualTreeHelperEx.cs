using System.Windows;
using System.Windows.Media;

namespace AutoFilterPresets.Helpers
{
    public static class VisualTreeHelperEx
    {
        public static T FindVisualChild<T>(DependencyObject parent, string typeName = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T && (typeName == null || child.GetType().Name == typeName))
                {
                    return (T)child;
                }
                else
                {
                    var childOfChild = FindVisualChild<T>(child, typeName);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }
    }
}