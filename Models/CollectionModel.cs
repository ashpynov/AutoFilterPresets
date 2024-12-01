using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Playnite.SDK;

namespace AutoFilterPresets.Models
{
    public class CollectionModel : ObservableObject
    {
        private readonly IPlayniteAPI PlayniteAPI;

        private readonly SettingsViewModel SettingsModel;
        private readonly AutoFilterPresetsSettings Settings;

        public readonly List<FilterImages> ImagesCollection = new List<FilterImages>();

        private FilterImages selectedFilterImages;
        public FilterImages SelectedfilterImages {get => selectedFilterImages;  set => SetValue( ref selectedFilterImages, value ); }

        public CollectionModel(IPlayniteAPI PlayniteAPI, SettingsViewModel SettingsModel)
        {
            this.PlayniteAPI = PlayniteAPI;
            this.SettingsModel = SettingsModel;

            Settings = SettingsModel.Settings;
            Settings.PropertyChanged += OnSettingsPropertyChanged;

            LoadImagesCollection(true);
        }

        void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AutoFilterPresetsSettings.SelectedFilter))
            {
                SetSelectedFilterImages(Settings.SelectedFilter?.Name);
            }
        }

        public void LoadImagesCollection(bool refresh = false)
        {
            var missed = Settings
                .FilterList
                .Where(f => f.IsFilter && ImagesCollection.FirstOrDefault(i => i.Name.ToLower() == f.Name.ToLower()) is null)
                .Select(f => new FilterImages(f.Name));

            if (refresh == false)
            {
                LoadCompilationImages(missed);
                LoadCompilationBackgrounds(missed);
            }

            ImagesCollection.AddRange(missed);

            if (refresh == true)
            {
                LoadCompilationImages(missed);
                LoadCompilationBackgrounds(missed);
            }

            SetSelectedFilterImages(Settings.SelectedFilter?.Name);
        }

        public void SetSelectedFilterImages(string FilterName)
        {
            SelectedfilterImages = string.IsNullOrEmpty(FilterName) ? null : ImagesCollection.FirstOrDefault( i => i.Name.ToLower() == FilterName.ToLower());
        }

        private Compilation selectedCompilation;
        public Compilation SelectedCompilation
        {
            get => selectedCompilation;
            set
            {
                SetValue(ref selectedCompilation, value);
                CompilationRootFolder = selectedCompilation?.Path;
                CompilationImagesFolder = selectedCompilation?.FilterImagesFolder;
                CompilationBackgroundsFolder = selectedCompilation?.FilterBackgroundsFolder;
            }
        }

        private string compilationBackgroundsFolder;
        public string CompilationBackgroundsFolder
        {
            get => compilationBackgroundsFolder;
            set
            {
                compilationBackgroundsFolder = value;
                var path = compilationBackgroundsFolder;
                if (SelectedCompilation is Compilation compilation && (string.IsNullOrEmpty(path) || (path = compilation.GetCompilationRelativePath(path)) is string))
                {
                    compilation.FilterBackgroundsFolder = compilationBackgroundsFolder = path;
                    LoadCompilationBackgrounds();
                    OnPropertyChanged(nameof(CompilationChanged));
                }
                OnPropertyChanged();
            }
        }

        private string compilationRootFolder;
        public string CompilationRootFolder
        {
            get => compilationRootFolder;
            set
            {
                compilationRootFolder = value;
                var path = compilationRootFolder;
                if (SelectedCompilation is Compilation compilation && compilation.Path?.ToLower() != path?.ToLower() && (string.IsNullOrEmpty(path) || Directory.Exists(path)))
                {
                    var images = compilation.GetCompilationFullPath(compilation.FilterImagesFolder);
                    var backgrounds = compilation.GetCompilationFullPath(compilation.FilterBackgroundsFolder);
                    compilation.Path = compilationRootFolder = path;
                    CompilationImagesFolder = compilation.GetCompilationRelativePath(images);
                    CompilationBackgroundsFolder = compilation.GetCompilationRelativePath(backgrounds);
                    // LoadCompilationImages();
                    // LoadCompilationBackgrounds();
                    OnPropertyChanged(nameof(CompilationChanged));
                }
                OnPropertyChanged();
            }
        }

        private string compilationImagesFolder;
        public string CompilationImagesFolder
        {
            get => compilationImagesFolder;
            set
            {
                compilationImagesFolder = value;
                var path = compilationImagesFolder;
                if (SelectedCompilation is Compilation compilation && (string.IsNullOrEmpty(path) || (path = compilation.GetCompilationRelativePath(path)) is string))
                {
                    compilation.FilterImagesFolder = compilationImagesFolder = path;
                    LoadCompilationImages();
                    OnPropertyChanged(nameof(CompilationChanged));
                }
                OnPropertyChanged();
            }
        }

        bool imagesPathIsChanged;
        bool backgroundsPathIsChanged;

        public bool CompilationChanged
        {
            get
            {
                return imagesPathIsChanged || backgroundsPathIsChanged;
            }
        }
        void LoadCompilationImages(IEnumerable<FilterImages> filterImages = null)
        {
            var imagesPath = SelectedCompilation?.GetCompilationFullPath(SelectedCompilation.FilterImagesFolder);
            foreach (var f in filterImages ?? ImagesCollection)
            {
                string image = string.IsNullOrEmpty(imagesPath) ? null : Path.Combine(imagesPath, $"{f.Name}.png");
                f.OriginalImage = f.Image = (image != null && File.Exists(image)) ? image : null;
            }
            imagesPathIsChanged = false;
            OnFilesChanged();
        }

        void LoadCompilationBackgrounds(IEnumerable<FilterImages> filterImages = null)
        {
            var backgroundsPath = SelectedCompilation?.GetCompilationFullPath(SelectedCompilation.FilterBackgroundsFolder);
            foreach (var f in filterImages ?? ImagesCollection)
            {
                string background = string.IsNullOrEmpty(backgroundsPath) ? null : Path.Combine(backgroundsPath, $"{f.Name}.jpg");
                f.OriginalBackground = f.Background = (background != null && File.Exists(background)) ? background : null;
            }
            backgroundsPathIsChanged = false;
            OnFilesChanged();
        }

        public RelayCommand SelectRootFolderCommand => new RelayCommand(
            () =>
            {
                var folder = PlayniteAPI.Dialogs.SelectFolder();
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    CompilationRootFolder = folder;
                }
            },
            () => !SelectedCompilation.IsTheme && !SelectedCompilation.IsGroup
        );

        public RelayCommand SelectImagesFolderCommand => new RelayCommand(
            () =>
            {
                var folder = PlayniteAPI.Dialogs.SelectFolder();
                if (!string.IsNullOrEmpty(folder) && SelectedCompilation.GetCompilationRelativePath(folder) is string relPath)
                {
                    CompilationImagesFolder = relPath;
                }
            },
            () => !CompilationChanged
        );

        public RelayCommand AddCompilationCommand => new RelayCommand(
            () =>
            {
                // Add Compilation;
                var name = "";
                int id = 0;
                do
                {
                    name = $"{SelectedCompilation?.Name} ({id++})";
                } while (SettingsModel.Compilations.FirstOrDefault(c => c.Name == name) != null);

                var compilation = new Compilation(Guid.NewGuid().ToString(), null, null)
                {
                    Name = name
                };
                SettingsModel.Compilations.Add(compilation);
                SettingsModel.OnPropertyChanged(nameof(SettingsModel.Compilations));
                SettingsModel.OnPropertyChanged(nameof(SettingsModel.GroupedCompilations));

                SelectedCompilation = compilation;

            },
            () => true
        );

        public RelayCommand DeleteCompilationCommand => new RelayCommand(
            () =>
            {
                var index = SettingsModel.Compilations.IndexOf(SelectedCompilation);
                SettingsModel.Compilations.RemoveAt(index);
                if (SettingsModel.Compilations.Count > 0)
                {
                    SelectedCompilation = SettingsModel.Compilations[Math.Min(index, SettingsModel.Compilations.Count - 1)];
                }

                SettingsModel.OnPropertyChanged(nameof(SettingsModel.Compilations));
                SettingsModel.OnPropertyChanged(nameof(SettingsModel.GroupedCompilations));
            },
            () => SelectedCompilation != null && SelectedCompilation.IsTheme == false && SelectedCompilation.IsGroup == false
        );

        public void OnFilesChanged()
        {
            var ic = ImagesCollection.Any(i => i.Image?.ToLower() != i.OriginalImage?.ToLower());
            var bc = ImagesCollection.Any(i => i.Background?.ToLower() != i.OriginalBackground?.ToLower());

            if (imagesPathIsChanged != ic || backgroundsPathIsChanged != bc)
            {
                imagesPathIsChanged = ic;
                backgroundsPathIsChanged = bc;
                OnPropertyChanged(nameof(CompilationChanged));
            }
        }

        public RelayCommand SelectBackgroundsFolderCommand => new RelayCommand(
            () =>
            {
                var folder = PlayniteAPI.Dialogs.SelectFolder();
                if (!string.IsNullOrEmpty(folder) && SelectedCompilation.GetCompilationRelativePath(folder) is string relPath)
                {
                    CompilationBackgroundsFolder = relPath;
                }
            },
            () => !CompilationChanged
        );

        public RelayCommand RevertAllChangesCommand => new RelayCommand(
            () =>
            {
                foreach (var x in ImagesCollection)
                {
                    x.Image = x.OriginalImage;
                    x.Background = x.OriginalBackground;
                }
                OnFilesChanged();
            },
            () => CompilationChanged
        );


        public void SaveImages()
        {

        }
        public RelayCommand SaveAllChangesCommand => new RelayCommand(
            () =>
            {
                if (PlayniteAPI.Dialogs.ShowMessage(
                    ResourceProvider.GetString("LOC_AutoFilterSettings_ConfirmationText"),
                    ResourceProvider.GetString("LOC_AutoFilterSettings_ConfirmationCaption"),
                    MessageBoxButton.OKCancel) == MessageBoxResult.OK )
                {
                    SaveImages();
                }
            },
            () => CompilationChanged
        );

        public RelayCommand<object> SelectFilterImageCommand => new RelayCommand<object>(
            (a) =>
            {
                var image = PlayniteAPI.Dialogs.SelectFile("PNG image|*.png");
                if (!string.IsNullOrEmpty(image))
                {
                    (a as FilterImages).Image = image;
                }
                OnFilesChanged();
            },
            (a) => a is FilterImages
        );
        public RelayCommand<object> RemoveFilterImageCommand => new RelayCommand<object>(
            (a) => { (a as FilterImages).Image = null; OnFilesChanged(); },
            (a) => a is FilterImages fi && !string.IsNullOrEmpty(fi.Image)
        );
        public RelayCommand<object> RevertFilterImageCommand => new RelayCommand<object>(
            (a) => { (a as FilterImages).Image = (a as FilterImages).OriginalImage; OnFilesChanged(); },
            (a) =>  a is FilterImages fi && fi.Image?.ToLower() != fi.OriginalImage?.ToLower()
        );
        public RelayCommand<object> SelectFilterBackgroundCommand => new RelayCommand<object>(
            (a) =>
            {
                var image = PlayniteAPI.Dialogs.SelectFile("JPG image|*.jpg");
                if (!string.IsNullOrEmpty(image))
                {
                    (a as FilterImages).Background = image;
                }
                OnFilesChanged();
            },
            (a) => a is FilterImages
        );
        public RelayCommand<object> RemoveFilterBackgroundCommand => new RelayCommand<object>(
            (a) => { (a as FilterImages).Background = null; OnFilesChanged(); },
            (a) => a is FilterImages fi && !string.IsNullOrEmpty(fi.Background)
        );
        public RelayCommand<object> RevertFilterBackgroundCommand => new RelayCommand<object>(
            (a) => { (a as FilterImages).Background = (a as FilterImages).OriginalBackground; OnFilesChanged(); },
            (a) =>  a is FilterImages fi && fi.Background?.ToLower() != fi.OriginalBackground?.ToLower()
        );
    }
}