using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace AutoFilterPresets.Models
{
    public partial class SettingsViewModel : ObservableObject
    {
        private bool syncCompilationIsEnabled = false;
        public bool SyncCompilationIsEnabled { get => syncCompilationIsEnabled; set => SetValue( ref syncCompilationIsEnabled, value); }

        static private ObservableCollection<Compilation> compilations;
        public ObservableCollection<Compilation> Compilations
        {
            get => compilations;
            private set
            {
                SetValue(ref compilations, value);
                OnPropertyChanged(nameof(GroupedCompilations));
            }
        }
        public ObservableCollection<Compilation> GroupedCompilations { get => GetGroupedCompilations(Compilations); }

        public bool CompilationChanged {
            get
            {
                return Settings.FilterList.Any(f => f.CompilationImagesPathIsChanged || f.CompilationBackgroundsPathIsChanged );
            }
        }

        public bool FiltersChanged {
            get
            {
                return Settings.FilterList.Any(f => f.ImagesPathIsChanged || f.BackgroundsPathIsChanged );
            }
        }
    }
}