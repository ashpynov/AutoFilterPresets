using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace AutoFilterPresets.Helpers
{
    public static class VisualTreeHelperEx
    {
        public static T FindVisualChild<T>(DependencyObject parent, string typeName = null,string name = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null
                    && child is T
                    && (typeName == null || child.GetType().Name == typeName)
                    && (name == null || (child as FrameworkElement)?.Name == name)
                )
                {
                    return (T)child;
                }
                else
                {
                    var childOfChild = FindVisualChild<T>(child, typeName, name);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}