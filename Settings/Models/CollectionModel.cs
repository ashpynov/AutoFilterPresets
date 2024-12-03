using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using Playnite.SDK;

namespace AutoFilterPresets.Setings.Models
{
    public class CollectionModel : ObservableObject
    {
        private readonly IPlayniteAPI PlayniteAPI;

        private readonly SettingsViewModel SettingsView;
        private readonly SettingsModel Settings;

        public readonly List<FilterImages> ImagesCollection = new List<FilterImages>();

        private FilterImages selectedFilterImages;
        public FilterImages SelectedfilterImages {get => selectedFilterImages;  set => SetValue( ref selectedFilterImages, value ); }

        public CollectionModel(IPlayniteAPI PlayniteAPI, SettingsViewModel SettingsView)
        {
            this.PlayniteAPI = PlayniteAPI;
            this.SettingsView = SettingsView;

            Settings = SettingsView.Settings;
            Settings.PropertyChanged += OnSettingsPropertyChanged;

            LoadImagesCollection(true);
        }

        void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.SelectedFilter))
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

        private CompilationModel selectedCompilation;
        public CompilationModel SelectedCompilation
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
                if (SelectedCompilation is CompilationModel compilation && (string.IsNullOrEmpty(path) || (path = compilation.GetCompilationRelativePath(path)) is string))
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
                if (SelectedCompilation is CompilationModel compilation && compilation.Path?.ToLower() != path?.ToLower() && (string.IsNullOrEmpty(path) || Directory.Exists(path)))
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
                if (SelectedCompilation is CompilationModel compilation && (string.IsNullOrEmpty(path) || (path = compilation.GetCompilationRelativePath(path)) is string))
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
                } while (SettingsView.Compilations.FirstOrDefault(c => c.Name == name) != null);

                var compilation = new CompilationModel(Guid.NewGuid().ToString(), null, null)
                {
                    Name = name
                };
                SettingsView.Compilations.Add(compilation);
                SettingsView.OnPropertyChanged(nameof(SettingsView.Compilations));
                SettingsView.OnPropertyChanged(nameof(SettingsView.GroupedCompilations));

                SelectedCompilation = compilation;

            },
            () => true
        );

        public RelayCommand DeleteCompilationCommand => new RelayCommand(
            () =>
            {
                var index = SettingsView.Compilations.IndexOf(SelectedCompilation);
                SettingsView.Compilations.RemoveAt(index);
                if (SettingsView.Compilations.Count > 0)
                {
                    SelectedCompilation = SettingsView.Compilations[Math.Min(index, SettingsView.Compilations.Count - 1)];
                }

                SettingsView.OnPropertyChanged(nameof(SettingsView.Compilations));
                SettingsView.OnPropertyChanged(nameof(SettingsView.GroupedCompilations));
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

        public void RevertChanges()
        {
            foreach (var x in ImagesCollection)
            {
                x.Image = x.OriginalImage;
                x.Background = x.OriginalBackground;
            }
            OnFilesChanged();
        }

        public RelayCommand RevertAllChangesCommand
        => new RelayCommand(
            () => RevertChanges(),
            () => CompilationChanged
        );


        public void SaveImages()
        {
            var imagesFolder = SelectedCompilation.GetCompilationFullPath(SelectedCompilation.FilterImagesFolder);
            var backgroundsFolder = SelectedCompilation.GetCompilationFullPath(SelectedCompilation.FilterImagesFolder);

            var images = ImagesCollection
                .Where(i => i.Image != i.OriginalImage)
                .Select(i => new Tuple<string,string>( i.Image, i.OriginalImage ?? Path.Combine(imagesFolder, $"{i.Name}.png")))
                .ToList();

            images.AddRange( ImagesCollection
                .Where(i => i.Background != i.OriginalBackground)
                .Select(i => new Tuple<string,string>( i.Background, i.OriginalBackground ?? Path.Combine(backgroundsFolder, $"{i.Name}.jpg")))
                .ToList());

            var copyToTemp = new List<Tuple<string, string>>();
            var copyToTarget = new List<Tuple<string, string>>();
            var toDelete = new List<string>();

            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            foreach (var image in images)
            {
                var hasConflict = images.Select(i => i.Item2).Contains(image.Item1);
                if (hasConflict)
                {
                    var tempFileName = Path.Combine(tempDir, Path.GetRandomFileName() + Path.GetExtension(image.Item2) ?? "");
                    copyToTemp.Add(new Tuple<string, string>(image.Item1, tempFileName));
                    copyToTarget.Add(new Tuple<string, string>(tempFileName, image.Item2));
                }
                else if (image.Item1 == null)
                {
                    toDelete.Add(image.Item2);
                }
                else
                {
                    copyToTarget.Add(image);
                }
            }

            try
            {
                if (copyToTemp.Count > 0)
                {
                    Directory.CreateDirectory(tempDir);
                    foreach (var image in copyToTemp)
                    {
                        File.Copy(image.Item1, image.Item2, overwrite: true);
                    }
                }

                foreach (var delete in toDelete)
                {
                    File.Delete(delete);
                }

                foreach (var image in copyToTarget)
                {
                    File.Copy(image.Item1, image.Item2, overwrite: true);
                }

                if (copyToTemp.Count > 0)
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                PlayniteAPI.Dialogs.ShowErrorMessage($"Something goes wrong during files copying:\n{ex.Message}");
            }

            LoadCompilationImages();
            LoadCompilationBackgrounds();
        }

        public RelayCommand SaveAllChangesCommand
        => new RelayCommand(
            () =>
            {
                if (SettingsView.ImageSaveConfirmationDialog( withRevert:false ) == ConfirmationResult.Save)
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