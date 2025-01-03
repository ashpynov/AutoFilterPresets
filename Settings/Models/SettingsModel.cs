using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AutoFilterPresets.Setings.Models
{
    public enum SortingOrder
    {
        Alphabet = 0,
        WithinGroups = 1,
        Custom = 2
    }
    public class SettingsModel : ObservableObject
    {
        private bool createSources = true;
        public bool CreateSources { get => createSources; set => SetValue(ref createSources, value); }

        private bool createPlatforms = true;
        public bool CreatePlatforms { get => createPlatforms; set => SetValue(ref createPlatforms, value); }

        private bool favoritesFirst = true;
        public bool FavoritesFirst { get => favoritesFirst; set => SetValue(ref favoritesFirst, value); }

        private bool loopSelection = true;
        public bool LoopSelection { get => loopSelection; set => SetValue(ref loopSelection, value); }

        private bool altBringIntoView = true;
        public bool AltBringIntoView { get => altBringIntoView; set => SetValue(ref altBringIntoView, value); }

        private SortingOrder orderBy = SortingOrder.Alphabet;
        public SortingOrder OrderBy { get => orderBy; set => SetValue(ref orderBy, value);}


        private bool? dontConfirmCopy = null;
        private bool dontDoubleAsk = false;
        public bool DontConfirmCopy
        {
            get => dontConfirmCopy == true;
            set
            {
                if (!dontConfirmCopy.HasValue)
                {
                    SetValue(ref dontConfirmCopy, value);
                }
                else
                {
                    dontDoubleAsk |= dontConfirmCopy == true;
                    if (value == true && !dontDoubleAsk)
                    {
                        var options = new List<MessageBoxOption>
                    {
                        new MessageBoxOption("LOC_AutoFilterSettings_DontConfirnButton", false),
                        new MessageBoxOption("LOC_AutoFilterSettings_ConfirmationCancel", true, true)
                    };

                        dontDoubleAsk = SettingsViewModel.PlayniteAPI.Dialogs.ShowMessage(
                            ResourceProvider.GetString("LOC_AutoFilterSettings_DontConfirnWarn"),
                            ResourceProvider.GetString("LOC_AutoFilterSettings_DontConfirnTitle"),
                            MessageBoxImage.Warning,
                            options) == options[0];
                    }
                    SetValue(ref dontConfirmCopy, dontDoubleAsk && value);
                }
            }
        }

        private ObservableCollection<SortingItem> sortedItems = new ObservableCollection<SortingItem>();
        public ObservableCollection<SortingItem> SortedItems { get => sortedItems; set => SetValue(ref sortedItems, value); }

        private SortingItem selectedFilter;

        private List<CompilationModel> compilations;
        public List<CompilationModel> Compilations { get => compilations; set => SetValue(ref compilations, value); }

        [DontSerialize]
        public SortingItem SelectedFilter { get => selectedFilter; set => SetValue(ref selectedFilter, value); }

        [DontSerialize]
        public static readonly ObservableCollection<SortingItem> defaultSortedItems = new ObservableCollection<SortingItem>()
        {
            new SortingItem() { Name = "<PRESETS>", SortingType = SortingItemType.Presets },
            new SortingItem() { Name = "<SOURCES>", SortingType = SortingItemType.Sources },
            new SortingItem() { Name = "<PLATFORMS>", SortingType = SortingItemType.Platforms },
            new SortingItem() { Name = "<HIDDEN>", SortingType = SortingItemType.Hidden }
        };

        [DontSerialize]
        public string CurrentThemeImagesPath = null;
        [DontSerialize]
        public string CurrentThemeBackgroundsPath = null;

        ObservableCollection<SortingItem> GetFilterGroupItems(SortingItem group, IEnumerable<SortingItem> existedItems)
        {
            if (!SettingsViewModel.PlayniteAPI.Database.IsOpen || Application.Current?.MainWindow?.DataContext == null)
            {
                return new ObservableCollection<SortingItem>();
            }

            List<string> items = new List<string>();
            SortingItemType itemSortingType = default;

            switch (group.SortingType)
            {
                case SortingItemType.Presets:
                    itemSortingType = SortingItemType.Preset;
                    items = (((dynamic)Application.Current.MainWindow.DataContext).SortedFilterFullscreenPresets as List<FilterPreset>)
                        .Select(p => p.Name)
                        .Where( name => !existedItems.Any(e => e.Name == name && e.SortingType==itemSortingType))
                        .ToList();
                    break;

                case SortingItemType.Sources:
                    itemSortingType = SortingItemType.Source;
                    items = SettingsViewModel.PlayniteAPI.Database.Games
                        .Where(g => g.Hidden == false && g.Source != null)
                        .Select(g => g.Source)
                        .Distinct()
                        .Select(source => Regex.Replace(Regex.Replace(source.Name, @"[<>:""/\\|?*]", " "), @"\s+", " ").Trim())
                        .Where( name => !existedItems.Any(e => e.Name == name && e.SortingType==itemSortingType))
                        .ToList();
                    break;

                case SortingItemType.Platforms:
                    itemSortingType = SortingItemType.Platform;
                    items = SettingsViewModel.PlayniteAPI.Database.Games
                        .Where(g => g.Hidden == false && g.Platforms?.HasItems() == true)
                        .SelectMany(g => g.Platforms)
                        .Distinct()
                        .Select(platform => Regex.Replace(Regex.Replace(platform.Name, @"[<>:""/\\|?*]", " "), @"\s+", " ").Trim())
                        .Where( name => !existedItems.Any(e => e.Name == name && e.SortingType==itemSortingType))
                        .ToList();
                    break;
            }

            items.Sort();
            return items.Select(name => new SortingItem() { Name = name, SortingType = itemSortingType, Parent = group  }).ToObservable();
        }

        [DontSerialize]
        public IEnumerable<SortingItem> HiddenItems
        { get => SortedItems.FirstOrDefault(g => g.SortingType == SortingItemType.Hidden)?.Items as IEnumerable<SortingItem> ?? new List<SortingItem>(); }

        bool IsHidden(SortingItem item) => item.IsFilter && IsHidden(item.Name);
        bool IsHidden(string name) => HiddenItems.Any(h => h.Name == name);

        public void AddMissingRemoveHiddenSortingItems()
        {
            AddMissingSortingItems();
            RemoveHiddenSortingItems();
        }
        void AddMissingSortingItems()
        {
            var saved = SortedItems.ToList();
            foreach (var sortingGroup in SettingsModel.defaultSortedItems)
            {
                var group = saved.FirstOrDefault(x => x.SortingType == sortingGroup.SortingType);
                if (group == null)
                {
                    group = sortingGroup;
                    SortedItems.Add(group);
                }
                if (group.Items == null)
                {
                    group.Items = new ObservableCollection<SortingItem>();
                }

                group.Items.AddMissing(GetFilterGroupItems(group, saved).Where(i => !group.Items.Any(x => x.Name == i.Name && x.SortingType == i.SortingType)));
                foreach( var i in group.Items)
                {
                    i.Parent = group;
                }
            }
        }

        void RemoveHiddenSortingItems()
        {
            RemoveHidden(SortedItems);
        }

        void RemoveHidden(ObservableCollection<SortingItem> items)
        {
            var itemsToRemove = items.Where(i => IsHidden(i)).ToList();
            foreach (var remove in itemsToRemove)
            {
                items.Remove(remove);
            }
            var groups = items.Where(i => i.SortingType != SortingItemType.Hidden && i.Items?.Count() > 0);
            foreach (var group in groups)
            {
                RemoveHidden(group.Items);
            }
        }
        public void RemoveHidden(List<FilterPreset> Filters)
        {
            Filters.RemoveAll(f => IsHidden(f.Name));
        }

        public void SortFilter(List<FilterPreset> Filters)
        {
            AddMissingRemoveHiddenSortingItems();

            var plainList = new List<SortingItem>();

            foreach(var item in SortedItems)
            {
                if (item.Items?.Count > 0)
                {
                    plainList.AddRange(item.Items);
                }
                else
                {
                    plainList.Add(item);
                }
            }
            var indexedItems = plainList.Select((item, index) => new { Item = item, Index = index });

            Filters.Sort((a, b) =>
            {
                var a_order = indexedItems.Where(x => x.Item.Name == a.Name).Select(x => x.Index).FirstOrDefault();
                var b_order = indexedItems.Where(x => x.Item.Name == b.Name).Select(x => x.Index).FirstOrDefault();
                return a_order - b_order;
            });
        }

        [DontSerialize]
        public IEnumerable<SortingItem> FilterList
        => SortedItems.SelectMany(x =>
            {
                var res = new List<SortingItem>();
                if (!x.IsGroup)
                {
                    res.Add(x);
                }
                if (x.Items?.Count > 0)
                {
                    res.AddRange(x.Items);
                }
                return res;
            } );
    }
}