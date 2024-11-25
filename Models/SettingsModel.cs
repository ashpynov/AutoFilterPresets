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
    public enum SortingOrder
    {
        Alphabet = 0,
        WithinGroups = 1,
        Custom = 2
    }
    public class AutoFilterPresetsSettings : ObservableObject
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

        private ObservableCollection<SortingItem> sortedItems = new ObservableCollection<SortingItem>();
        public ObservableCollection<SortingItem> SortedItems { get => sortedItems; set => SetValue(ref sortedItems, value); }

        [DontSerialize]
        public static readonly ObservableCollection<SortingItem> defaultSortedItems = new ObservableCollection<SortingItem>()
        {
            new SortingItem() { Name = "<PRESETS>", SortingType = SortingItemType.Presets },
            new SortingItem() { Name = "<SOURCES>", SortingType = SortingItemType.Sources },
            new SortingItem() { Name = "<PLATFORMS>", SortingType = SortingItemType.Platforms }
        };

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

        public void AddMissingSortingItems()
        {
            var saved = SortedItems.ToList();
            foreach (var sortingGroup in AutoFilterPresetsSettings.defaultSortedItems)
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

        public void SortFilter(List<FilterPreset> Filters)
        {
            AddMissingSortingItems();

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
    }

    public class SettingsViewModel : ObservableObject, ISettings
    {
        public OrderDropHandler DropHandler { get; }

        private readonly AutoFilterPresets plugin;
        public static IPlayniteAPI PlayniteAPI;
        private AutoFilterPresetsSettings editingClone { get; set; }

        private AutoFilterPresetsSettings settings;
        public AutoFilterPresetsSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public SettingsViewModel(AutoFilterPresets plugin, IPlayniteAPI PlayniteAPI)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;
            SettingsViewModel.PlayniteAPI = PlayniteAPI;


            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<AutoFilterPresetsSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new AutoFilterPresetsSettings();
            }

            DropHandler = new OrderDropHandler();
         }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            Settings.AddMissingSortingItems();
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }

    }
}