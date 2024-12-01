using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace AutoFilterPresets.Models
{
    public partial class SettingsViewModel : ObservableObject, ISettings
    {
        private static readonly ILogger logger = LogManager.GetLogger();

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

        private CollectionModel primaryCollection;
        public CollectionModel PrimaryCollection { get => primaryCollection; set => SetValue( ref primaryCollection, value ); }

        private CollectionModel secondaryCollection;
        public CollectionModel SecondaryCollection { get => secondaryCollection; set => SetValue( ref secondaryCollection, value ); }


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
            FillImagesPlugin(Settings);

            Compilations =  GetAvailableCompilations().ToObservable();

            PrimaryCollection = new CollectionModel(PlayniteAPI, this)
            {
                SelectedCompilation = GetCurrentCompilation()
            };

            SecondaryCollection = new CollectionModel(PlayniteAPI, this)
            {
                SelectedCompilation = GetCurrentCompilation()
            };

            editingClone = Serialization.GetClone(Settings);
        }

        ObservableCollection<Compilation> GetGroupedCompilations( IEnumerable<Compilation> compilations)
        {
            var themes = compilations
                .Where(c => c.IsTheme && (!string.IsNullOrEmpty(c.FilterImagesFolder) || !string.IsNullOrEmpty(c.FilterBackgroundsFolder)))
                .OrderBy(c => c.Name)
                .ToList();

            var userCompilations = compilations
                .Where(c => !c.IsTheme)
                .OrderBy(c => c.Name)
                .ToList();

            var notConfigured = compilations
                .Where(c => c.IsTheme && string.IsNullOrEmpty(c.FilterImagesFolder) && string.IsNullOrEmpty(c.FilterBackgroundsFolder))
                .OrderBy(c => c.Name)
                .ToList();

            var result = new List<Compilation>();

            if (themes.Any())
            {
                result.Add(new Compilation { IsGroup = true, Name = "Themes:" });
                result.AddRange(themes);
            }

            if (userCompilations.Any())
            {
                result.Add(new Compilation { IsGroup = true, Name = "User Compilations:" });
                result.AddRange(userCompilations);
            }

            if (notConfigured.Any())
            {
                result.Add(new Compilation { IsGroup = true, Name = "Not Configured Themes:" });
                result.AddRange(notConfigured);
            }

            return result.ToObservable();
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
            Settings.Compilations = GetReconfiguredCompilations(Compilations);
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

        void FillImagesPlugin(AutoFilterPresetsSettings settings)
        {
            string imagesPath = Path.Combine(plugin.GetPluginUserDataPath(), "FiltersImages");
            string backgroundsPath = Path.Combine(plugin.GetPluginUserDataPath(), "FiltersBackgrounds");

            foreach (var f in settings.FilterList)
            {
                string image = Path.Combine(imagesPath, $"{f.Name}.png");
                if (File.Exists(image))
                {
                    f.ImagesPath = image;
                    f.ImagesPathIsChanged = false;
                }

                string background = Path.Combine(backgroundsPath, $"{f.Name}.jpg");
                if (File.Exists(background))
                {
                    f.BackgroundsPath = background;
                    f.BackgroundsPathIsChanged = false;
                }
            }
        }
   }
}