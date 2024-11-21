using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    }

    public class SettingsViewModel : ObservableObject, ISettings
    {
        private readonly AutoFilterPresets plugin;
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

        public SettingsViewModel(AutoFilterPresets plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

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
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
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