using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using System.Windows;



namespace AutoFilterPresets.Setings.Models
{
    public enum ConfirmationResult
    {
        Cancel = 0,
        Save = 1,
        Revert = 2,
    }

    public partial class SettingsViewModel : ObservableObject, ISettings
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public OrderDropHandler DropHandler { get; }

        private readonly AutoFilterPresets plugin;
        public static IPlayniteAPI PlayniteAPI;
        private SettingsModel editingClone { get; set; }

        private SettingsModel settings;
        public SettingsModel Settings
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

        private bool syncCompilationIsEnabled = false;
        public bool SyncCompilationIsEnabled { get => syncCompilationIsEnabled; set => SetValue( ref syncCompilationIsEnabled, value); }

        private ConfirmationResult confirmationResult;

        static private ObservableCollection<CompilationModel> compilations;
        public ObservableCollection<CompilationModel> Compilations
        {
            get => compilations;
            private set
            {
                SetValue(ref compilations, value);
                OnPropertyChanged(nameof(GroupedCompilations));
            }
        }
        public ObservableCollection<CompilationModel> GroupedCompilations { get => GetGroupedCompilations(Compilations); }


        public SettingsViewModel(AutoFilterPresets plugin, IPlayniteAPI PlayniteAPI)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;
            SettingsViewModel.PlayniteAPI = PlayniteAPI;


            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<SettingsModel>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new SettingsModel();
            }

            DropHandler = new OrderDropHandler();
         }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            Settings.AddMissingSortingItems();

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

            Settings.SelectedFilter = null;
        }

        ObservableCollection<CompilationModel> GetGroupedCompilations( IEnumerable<CompilationModel> compilations)
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

            var result = new List<CompilationModel>();

            if (themes.Any())
            {
                result.Add(new CompilationModel { IsGroup = true, Name = "Themes:" });
                result.AddRange(themes);
            }

            if (userCompilations.Any())
            {
                result.Add(new CompilationModel { IsGroup = true, Name = "User Compilations:" });
                result.AddRange(userCompilations);
            }

            if (notConfigured.Any())
            {
                result.Add(new CompilationModel { IsGroup = true, Name = "Not Configured Themes:" });
                result.AddRange(notConfigured);
            }

            return result.ToObservable();
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            if( PrimaryCollection.CompilationChanged )
            {
                if (confirmationResult == ConfirmationResult.Save)
                {
                    PrimaryCollection.SaveImages();
                }
                else
                {
                    PrimaryCollection.RevertChanges();
                }
            }
            if ( SyncCompilationIsEnabled && SecondaryCollection.CompilationChanged)
            {
                if (confirmationResult == ConfirmationResult.Save)
                {
                    SecondaryCollection.SaveImages();
                }
                else
                {
                    SecondaryCollection.RevertChanges();
                }
            }
            Settings.Compilations = GetReconfiguredCompilations(Compilations);
            plugin.SavePluginSettings(Settings);
        }


        public ConfirmationResult ImageSaveConfirmationDialog(bool withRevert)
        {
            if (Settings.DontConfirmCopy)
            {
                return ConfirmationResult.Save;
            }

            var options = new List<MessageBoxOption>
            {
                new MessageBoxOption(withRevert ? "LOC_AutoFilterSettings_ConfirmationSaveWithSettings" : "LOC_AutoFilterSettings_ConfirmationSave",true),
                new MessageBoxOption("LOC_AutoFilterSettings_ConfirmationCancel", false, true)
            };

            if (withRevert)
            {
                options.Insert(1, new MessageBoxOption("LOC_AutoFilterSettings_ConfirmationRevert", false));
            }

            var result = PlayniteAPI.Dialogs.ShowMessage(
                ResourceProvider.GetString("LOC_AutoFilterSettings_ConfirmationText"),
                ResourceProvider.GetString("LOC_AutoFilterSettings_ConfirmationCaption"),
                MessageBoxImage.Warning,
                options);
            if (result == options[0])
            {
                return ConfirmationResult.Save;
            }
            else if (withRevert && result == options[1])
            {
                return ConfirmationResult.Revert;
            }
            return ConfirmationResult.Cancel;
        }
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            confirmationResult = ConfirmationResult.Cancel;

            if ( PrimaryCollection.CompilationChanged || ( SyncCompilationIsEnabled && SecondaryCollection.CompilationChanged) )
            {
                confirmationResult = ImageSaveConfirmationDialog(withRevert: true);
                if (confirmationResult == ConfirmationResult.Cancel)
                {
                    errors.Add(ResourceProvider.GetString("LOC_AutoFilterSettings_SaveCanceled"));
                    return false;
                }
            }
            return true;
        }
   }
}