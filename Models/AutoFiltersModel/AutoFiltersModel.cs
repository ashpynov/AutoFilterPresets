using AutoFilterPresets.Helpers;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;


namespace AutoFilterPresets.Setings.Models
{
    public partial class AutoFiltersModel: INotifyPropertyChanged
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private bool eventsAreRegistered = false;

        private List<FilterPreset> AutoPresets;

        private readonly IPlayniteAPI PlayniteAPI;

        private readonly dynamic mainModel;

        private ItemsControl ItemsFilterPresets;

        private Control FilterPresetSelector;
        private readonly SettingsModel Settings;

        public AutoFiltersModel( IPlayniteAPI playniteAPI, SettingsModel settings )
        {
            PlayniteAPI = playniteAPI;
            Settings = settings;

            mainModel = PlayniteAPI.MainView
                    .GetType()
                    .GetField("mainModel", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(PlayniteAPI.MainView);

            (mainModel as INotifyPropertyChanged).PropertyChanged += OnMainModelChanged;
            ReplaceCommand(nameof(SelectFilterPresetCommand));
            ReplaceCommand(nameof(PrevFilterViewCommand));
            ReplaceCommand(nameof(NextFilterViewCommand));

            Logger.Debug("AutoFiltersModel created");

            EventManager.RegisterClassHandler(
                typeof(Control),
                Window.LoadedEvent,
                new RoutedEventHandler((object sender, RoutedEventArgs e)=>
                {
                    if (sender.GetType().Name != "Main") return;
                    SuppressNativeUpdate(sender as Control);

                }
            ), true);
        }

        private List<FilterPreset> PlatformsAutoFilter()
        {
            var platforms = PlayniteAPI.Database.Games
                .Where(g => g.Hidden == false && g.Platforms?.HasItems() == true)
                .SelectMany(g => g.Platforms)
                .Distinct()
                .Select(platform =>
                {
                    var safeName = Regex.Replace(Regex.Replace(platform.Name, @"[<>:""/\\|?*]", " "), @"\s+", " ").Trim();
                    FilterPreset filter = new FilterPreset()
                    {
                        Name = safeName,
                        SortingOrder = Settings.FavoritesFirst ? SortOrder.Favorite : SortOrder.Name,
                        SortingOrderDirection =  Settings.FavoritesFirst ? SortOrderDirection.Descending : SortOrderDirection.Ascending,
                        Settings = new FilterPresetSettings()
                        {
                            Platform = new IdItemFilterItemProperties(platform.Id)
                        }
                    };
                    return filter;
                })
                .ToList();

            platforms.Sort((x, y) => x.Name.CompareTo(y.Name));
            return platforms;
        }

        private List<FilterPreset> SourcesAutoFilter()
        {
            var sources = PlayniteAPI.Database.Games
                .Where(g => g.Hidden == false && g.Source != null)
                .Select(g => g.Source)
                .Distinct()
                .Select(source =>
                {
                    var safeName = Regex.Replace(Regex.Replace(source.Name, @"[<>:""/\\|?*]", " "), @"\s+", " ").Trim();
                    FilterPreset filter = new FilterPreset()
                    {
                        Name = safeName,
                        SortingOrder = Settings.FavoritesFirst ? SortOrder.Favorite : SortOrder.Name,
                        SortingOrderDirection =  Settings.FavoritesFirst ? SortOrderDirection.Descending : SortOrderDirection.Ascending,
                        Settings = new FilterPresetSettings()
                        {
                            Source = new IdItemFilterItemProperties(source.Id)
                        }
                    };
                    return filter;
                })
                .ToList();

            sources.Sort((x, y) => x.Name.CompareTo(y.Name));
            return sources;
        }


        private void UpdateAutoPresets()
        {
            dynamic model = Application.Current?.MainWindow?.DataContext;
            if (model == null)
            {
                return;
            }
            var presets = model.SortedFilterFullscreenPresets as List<FilterPreset>;
            if (Settings.CreateSources)
            {
                presets.AddRange(SourcesAutoFilter().Where(s => presets.FindIndex(p => AreFiltersEqual(s, p)) == -1));
            }

            if (Settings.CreatePlatforms)
            {
                presets.AddRange(PlatformsAutoFilter().Where(s => presets.FindIndex(p => AreFiltersEqual(s, p)) == -1));
            }

            Settings.RemoveHidden(presets);

            if (Settings.OrderBy == SortingOrder.Alphabet)
            {
                presets.Sort((x, y) => x.Name.CompareTo(y.Name));
            }

            if (Settings.OrderBy == SortingOrder.Custom)
            {
                Settings.SortFilter(presets);
            }

            AutoPresets = presets;

            UpdateFilterPresetSelector();

            Logger.Debug($"Automatic Filters rebuilded with {AutoPresets.Count} items");
        }

        private bool AreFiltersEqual(FilterPreset filter1, FilterPreset filter2)
        {
            if (filter1 == null || filter2 == null)
                return filter1 == filter2;

            return (filter1.Name is string && filter2.Name is string && filter1.Name.ToLower() == filter2.Name.ToLower())
            || Serialization.AreObjectsEqual(filter1.Settings,filter2.Settings);
        }

        private bool AreFiltersEqual(FilterPresetSettings settings1, FilterPresetSettings settings2)
        {
            if (settings1 == null || settings2 == null)
                return settings1 == settings2;

            return Serialization.AreObjectsEqual(settings1,settings2);
        }

        private void SelectFilterView(bool next)
        {
            if (!AutoPresets.HasItems())
            {
                return;
            }

            int offset = next ? 1 : -1;

            if (ActiveFilterPreset == null)
            {
                ActiveFilterPreset = next ? AutoPresets[0] : AutoPresets[AutoPresets.Count - 1];
            }
            else
            {
                var nextIndex = AutoPresets.FindIndex(p => AreFiltersEqual(p,ActiveFilterPreset)) + (next ? 1 : -1);

                if (nextIndex < 0)
                {
                    nextIndex = Settings.LoopSelection ? AutoPresets.Count - 1 : 0;
                }
                else if (nextIndex > AutoPresets.Count - 1)
                {
                    nextIndex = Settings.LoopSelection ? 0 : AutoPresets.Count - 1;
                }

                ActiveFilterPreset = AutoPresets[nextIndex];
            }

        }
        static private FilterPreset activeFilterPreset = null;
        public FilterPreset ActiveFilterPreset
        {
            get
            {
                return activeFilterPreset;
            }
            set
            {
                PlayniteAPI.MainView.ApplyFilterPreset(value);
                SetValue(ref activeFilterPreset, value);
            }
        }

        private FilterPreset FindAutoPreset()
        {
            var currentPresetId = PlayniteAPI.MainView.GetActiveFilterPreset();
            return AutoPresets.FirstOrDefault(p => p.Id == currentPresetId)
                ?? AutoPresets.FirstOrDefault(p => AreFiltersEqual(p.Settings, PlayniteAPI.MainView.GetCurrentFilterSettings()));
        }

        private void BringActivePresetIntoView()
        {
            if ( activeFilterPreset == null || ItemsFilterPresets == null || ItemsFilterPresets.Items.Count == 0)
            {
                return;
            }

            int index = ItemsFilterPresets.Items
                .Cast<FrameworkElement>()
                .Select((itm, idx) => new { itm, idx })
                .Where(x => x.itm.DataContext == ActiveFilterPreset)
                .Select(x => x.idx + 1 )
                .FirstOrDefault() - 1;

            if (index == -1) return;
            CheckBox item = ItemsFilterPresets.Items[index] as CheckBox;

            if (!Settings.AltBringIntoView)
            {
                item.BringIntoView();
                return;
            }

            if (item.ActualWidth == 0 || item.ActualHeight == 0) return;

            var scrollViewer = VisualTreeHelperEx.FindVisualChild<ScrollViewer>(FilterPresetSelector);
            if (scrollViewer == null) return;


            if (scrollViewer.CanContentScroll)
            {
                var startIdx = (int)scrollViewer.HorizontalOffset;
                var width = (int)scrollViewer.ViewportWidth;
                var viewPortMid = (int)(startIdx + width / 2);

                if (index > viewPortMid)
                {
                    if (index < ItemsFilterPresets.Items.Count - 1)
                    {
                        CheckBox nextItem = ItemsFilterPresets.Items[index + 1] as CheckBox;
                        nextItem.BringIntoView();
                    }
                    else
                    {
                        scrollViewer.ScrollToRightEnd();
                    }
                }
                else if (index <= viewPortMid)
                {
                    if (index > 0)
                    {
                        (ItemsFilterPresets.Items[index - 1] as CheckBox).BringIntoView() ;
                    }
                    else
                    {
                        scrollViewer.ScrollToLeftEnd();
                    }
                }
            }
            else
            {
                var left = (index > 0) ? (ItemsFilterPresets.Items[index - 1] as CheckBox)?.ActualWidth ?? 0 : 0;
                var right = (index < ItemsFilterPresets.Items.Count - 1) ? (ItemsFilterPresets.Items[index + 1] as CheckBox)?.ActualWidth ?? 0 : 0;
                var top = (index > 0) ? (ItemsFilterPresets.Items[index - 1] as CheckBox)?.ActualHeight ?? 0 : 0;
                var bottom = (index < ItemsFilterPresets.Items.Count - 1) ? (ItemsFilterPresets.Items[index + 1] as CheckBox)?.ActualHeight ?? 0 : 0;
                item.BringIntoView(new Rect(-left, -top, item.ActualWidth + left + right, item.ActualHeight + top + bottom));
            }
        }

        private void Update()
        {
            UpdateAutoPresets();
            var currentPreset = FindAutoPreset();
            if (!AreFiltersEqual(activeFilterPreset, currentPreset))
            {
                PlayniteAPI.MainView.ApplyFilterPreset(currentPreset);
                SetValue(ref activeFilterPreset, currentPreset, nameof(ActiveFilterPreset));

                Logger.Debug($"Automatic Filter {currentPreset?.Name} Applied");
            }

        }

        private void RegisterHandlers()
        {
            if (!eventsAreRegistered)
            {
                Logger.Debug("Registering database events handlers...");
                PlayniteAPI.Database.Games.ItemUpdated += OnDatabaseUpdated;
                PlayniteAPI.Database.Games.ItemCollectionChanged += OnDatabaseUpdated;
                PlayniteAPI.Database.DatabaseOpened += OnDatabaseUpdated;

                eventsAreRegistered = true;
                Logger.Debug($"Database Events registered");
            }
        }


        public RelayCommand<object> PrevFilterViewCommand => new RelayCommand<object>(
            (a) => SelectFilterView(next: false),
            (a) => PlayniteAPI.Database?.IsOpen == true
        );
        public RelayCommand<object> NextFilterViewCommand => new RelayCommand<object>(
           (a) => SelectFilterView(next: true),
           (a) => PlayniteAPI.Database?.IsOpen == true
        );

        public RelayCommand SelectFilterPresetCommand => new RelayCommand(
           () => SelectPreset(),
           () => PlayniteAPI.Database?.IsOpen == true
        );
    }
}

