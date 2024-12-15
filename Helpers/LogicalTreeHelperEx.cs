using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

public static class LogicalTreeHelperEx
{
    public static IEnumerable<T> FindLogicalChildren<T>(DependencyObject parent, string typeName = null, string name = null) where T : DependencyObject
    {
        if (parent == null) yield break;

        foreach (var child in LogicalTreeHelper.GetChildren(parent))
        {
            if (child is DependencyObject dependencyChild)
            {
                if ( child is T
                && ( typeName == null || child.GetType().Name == typeName)
                && ( name == null || (child as FrameworkElement)?.Name == name))
                {
                    yield return child as T;
                }

                foreach (var descendant in FindLogicalChildren<T>(dependencyChild, typeName, name))
                {
                    yield return descendant;
                }
            }
        }
    }
}