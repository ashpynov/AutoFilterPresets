using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace AutoFilterPresets.Models
{
    public class OrderDropHandler : IDropTarget
    {
        int GetItemIndex(UIElement item)
        => ItemsControl
            .ItemsControlFromItemContainer(item as TreeViewItem)
            .ItemContainerGenerator
            .IndexFromContainer(item as TreeViewItem);

        void SetProperty( object target, string propertyName, object value )
        => target.GetType().GetProperty(propertyName).SetValue(target, value);

        bool MatchType(SortingItemType a, SortingItemType b)
        {
            return a == b || a.ToString().StartsWith(b.ToString()) || b.ToString().StartsWith(a.ToString());
        }

        void UpdateDropInfo(IDropInfo dropInfo)
        {
            var targetItem = dropInfo.TargetItem as SortingItem;
            var dragItem = dropInfo.Data as SortingItem;

            if (targetItem == null)
            {
                var tv = dropInfo.VisualTarget as TreeView;
                var height = tv.ActualHeight;
                bool after = dropInfo.DropPosition.Y > 20;
                var index = after ? tv.Items.Count - 1 : 0;
                var TargetItem = tv.Items[index];

                index += after ? 1 : 0;

                SetProperty(dropInfo, nameof(dropInfo.TargetItem), TargetItem);
                SetProperty(dropInfo, nameof(dropInfo.InsertIndex), index);
                SetProperty(dropInfo, nameof(dropInfo.VisualTargetItem), tv.ItemContainerGenerator.ContainerFromItem(TargetItem));
                SetProperty(dropInfo, nameof(dropInfo.InsertPosition), after ? RelativeInsertPosition.AfterTargetItem : RelativeInsertPosition.BeforeTargetItem);
                return;
            }

            if (targetItem?.Parent != null)
            {
                var VisualTargetItem = dropInfo.VisualTargetItem.GetVisualAncestor<TreeViewItem>() ?? dropInfo.VisualTargetItem;
                var TargetItem = targetItem.Parent;

                var InsertPosition = targetItem.Parent.Items.IndexOf(targetItem) >= targetItem.Parent.Items.Count / 2
                    ? RelativeInsertPosition.AfterTargetItem
                    : RelativeInsertPosition.BeforeTargetItem;

                InsertPosition |= RelativeInsertPosition.TargetItemCenter;

                var InsertIndex = GetItemIndex(VisualTargetItem) + (InsertPosition.HasFlag(RelativeInsertPosition.AfterTargetItem) ? 1 : 0);

                SetProperty(dropInfo, nameof(dropInfo.VisualTargetItem), VisualTargetItem);
                SetProperty(dropInfo, nameof(dropInfo.TargetItem), TargetItem);
                SetProperty(dropInfo, nameof(dropInfo.InsertPosition), InsertPosition);
                SetProperty(dropInfo, nameof(dropInfo.InsertIndex), InsertIndex);

                targetItem = dropInfo.TargetItem as SortingItem;
            }
            else if (dropInfo.VisualTargetItem is TreeViewItem tvi && tvi.HasItems && tvi.IsExpanded && dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.AfterTargetItem))
            {
                var InsertPosition = dropInfo.InsertPosition & ~RelativeInsertPosition.AfterTargetItem | RelativeInsertPosition.BeforeTargetItem;
                SetProperty(dropInfo, nameof(dropInfo.InsertPosition), InsertPosition);
                SetProperty(dropInfo, nameof(dropInfo.InsertIndex), dropInfo.InsertIndex);
            }

            if (dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter))
            {
                var InsertIndex = GetItemIndex(dropInfo.VisualTargetItem)
                    + (dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.AfterTargetItem) ? 1 : 0);

                SetProperty(dropInfo, nameof(dropInfo.InsertIndex), InsertIndex);
            }

            dropInfo.Effects = DragDropEffects.Move;

            if ( dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter)
                && ( dragItem.IsGroup
                    || dragItem.Parent != null
                    || !targetItem.IsGroup
                    || !MatchType(dragItem.SortingType, targetItem.SortingType)
                )
            )
            {
                var InsertPosition = dropInfo.InsertPosition & ~RelativeInsertPosition.TargetItemCenter;
                SetProperty(dropInfo, nameof(InsertPosition), InsertPosition);
            }
        }

        void SelectDroppedItems(IDropInfo dropInfo,  object item)
        {
            if (dropInfo.VisualTarget is ItemsControl itemsControl)
            {
                var tvItem = dropInfo.VisualTargetItem as TreeViewItem;
                var tvItemIsExpanded = tvItem != null && tvItem.HasHeader && tvItem.HasItems && tvItem.IsExpanded;

                var itemsParent = tvItemIsExpanded
                    ? tvItem
                    : dropInfo.VisualTargetItem != null
                        ? ItemsControl.ItemsControlFromItemContainer(dropInfo.VisualTargetItem)
                        : itemsControl;
                itemsParent = itemsParent ?? itemsControl;

                (dropInfo.DragInfo.VisualSourceItem as TreeViewItem)?.ClearSelectedItems();
                itemsParent.ClearSelectedItems();
                itemsParent.SetItemSelected(item, true);
                bool expanded = (dropInfo.DragInfo.VisualSourceItem as TreeViewItem)?.IsExpanded == true;

                if (expanded
                && (ItemsControl.ItemsControlFromItemContainer(itemsParent) ?? itemsParent)
                    ?.ItemContainerGenerator
                    ?.ContainerFromItem(item) is TreeViewItem tvi )
                {
                    tvi.IsExpanded = true;
                }
            }
        }

        public void DragOver(IDropInfo dropInfo)
        {
            UpdateDropInfo(dropInfo);
            GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.DragOver(dropInfo);
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo?.DragInfo == null)
            {
                return;
            }

            UpdateDropInfo(dropInfo);

            var targetList = dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight
                ? (dropInfo.VisualTargetItem as TreeViewItem).ItemsSource as IList<SortingItem>
                : (dropInfo.VisualTarget as TreeView).ItemsSource as IList<SortingItem>;

            var dragItem = dropInfo.Data as SortingItem;
            var insertIndex = dropInfo.InsertIndex;

            if ( dropInfo.DropTargetAdorner == DropTargetAdorners.Highlight )
            {
                var targetItem = dropInfo.TargetItem as SortingItem;
                dragItem.Parent = targetItem;
                insertIndex = targetItem.Items.ToList().FindIndex(item => string.Compare(item.Name, dragItem.Name, StringComparison.Ordinal) > 0);;
            }
            else
            {
                dragItem.Parent = null;
            }

            var sourceList = dropInfo.DragInfo.SourceCollection.TryGetList();
            var oldIndex = sourceList.IndexOf(dropInfo.Data);

            if (sourceList == targetList && oldIndex < insertIndex)
            {
                insertIndex--;
            }

            sourceList.RemoveAt(oldIndex);
            if (insertIndex == -1 )
            {
                insertIndex = targetList.Count;
            }

            if (insertIndex < targetList.Count)
            {
                targetList.Insert(insertIndex, dragItem);
            }
            else
            {
                targetList.Add(dragItem);
            }

            SelectDroppedItems(dropInfo, dragItem);

        }
    }
}