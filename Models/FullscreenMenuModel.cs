using AutoFilterPresets.Setings.Models;
using AutoFilterPresets.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;


namespace AutoFilterPresets.Models
{
    public partial class FullscreenMenuModel : INotifyPropertyChanged
    {
        private readonly SettingsModel Settings;
        private readonly AutoFiltersModel AutoFilers;

        static string LastImagesPath = null;
        static string LastBackgroundsPath = null;

        public FullscreenMenuModel(AutoFiltersModel autoFilters, SettingsModel settings)
        {
            AutoFilers = autoFilters;
            Settings = settings;
                        EventManager.RegisterClassHandler(
                typeof(Control),
                Window.LoadedEvent,
                new RoutedEventHandler((object sender, RoutedEventArgs e) =>
                {
                    if (sender.GetType().Name == "SingleItemSelectionWindow"
                        && (sender as Control).DataContext?.GetType()?.Name == "SingleItemSelectionViewModel`1"
                        && (sender as Control).DataContext.GetType().GenericTypeArguments[0].Name == "FilterPreset")
                    {
                        var itemsControl = (sender as Window).FindName("PART_ItemsHost") as ItemsControl;
                        itemsControl.ItemContainerGenerator.StatusChanged += (s, ev) =>
                        {
                            if (itemsControl.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                            {
                                foreach (dynamic item in itemsControl.Items)
                                {
                                    if (itemsControl.ItemContainerGenerator.ContainerFromItem(item) is UIElement ctrl)
                                    {
                                        SetPresetMenuCommands(ctrl, item.Value as FilterPreset);
                                    }
                                }
                            }
                        };
                    }
                }
            ), true);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        void SetPresetMenuCommands(UIElement ctrl, FilterPreset preset)
        {
            var hasImages = !Settings.CurrentThemeImagesPath.IsNullOrEmpty();
            var hasBackgrounds = !Settings.CurrentThemeBackgroundsPath.IsNullOrEmpty();

            if (hasImages || hasBackgrounds)
            {
                ctrl.InputBindings.Add(
                    GameController.CreateInputBinding(
                        ControllerInput.Start,
                        (hasImages && hasBackgrounds)
                            ? SelectFilterImageOrBackgroundCommand
                            : hasImages
                                ? SelectFilterImageCommand
                                : SelectFilterBackgroundCommand,
                        preset
                ));

                if (hasImages)
                {
                    ctrl.InputBindings.Add(new KeyBinding(SelectFilterImageCommand, new KeyGesture(Key.F5)) { CommandParameter = preset });
                }
                if (hasBackgrounds)
                {
                    ctrl.InputBindings.Add(new KeyBinding(SelectFilterBackgroundCommand, new KeyGesture(Key.F4)) { CommandParameter = preset });
                }
            }

            return;
        }

        void ReplaceImage(string srcImage, string destImage)
        {

            if (srcImage.IsNullOrEmpty() || destImage.IsNullOrEmpty() || destImage.ToLower() == srcImage.ToLower())
            {
                return;
            }

            try
            {
                if (File.Exists(destImage))
                {
                    RemoveFromPlayniteImageCache(destImage);
                    RemoveFromWpfImageCache(destImage);
                }

                File.Copy(srcImage, destImage, overwrite: true);
                return;
            }
            catch
            {

            }
        }

        static void RemoveFromWpfImageCache(string destImage)
        {
            Type ImagingCache = typeof(BitmapImage).Assembly.GetType("System.Windows.Media.Imaging.ImagingCache");
            MethodInfo CheckDecoderCache = ImagingCache.GetMethod("CheckDecoderCache", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo RemoveFromDecoderCache = ImagingCache.GetMethod("RemoveFromDecoderCache", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo RemoveFromImageCache = ImagingCache.GetMethod("RemoveFromImageCache", BindingFlags.NonPublic | BindingFlags.Static);

            Uri destUri = new Uri(destImage, UriKind.Absolute);

            if ((CheckDecoderCache?.Invoke(null, new object[] { destUri }) as WeakReference)?.Target is BitmapDecoder decoder)
            {
                var method = typeof(Decoder).GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(decoder, null);
            }
            RemoveFromDecoderCache.Invoke(null, new object[] { destUri });
            RemoveFromImageCache.Invoke(null, new object[] { destUri });
        }

        static void RemoveFromPlayniteImageCache(string destImage)
        {
            Assembly assembly = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playnite.dll"));
            Type ImageSourceManager = assembly.GetType("Playnite.ImageSourceManager");
            dynamic Cache = ImageSourceManager.GetField("Cache", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            var removed = Cache.TryRemove(destImage);
        }


        public RelayCommand<object> SelectFilterImageCommand => new RelayCommand<object>(
            (a) =>
            {
                if (a is FilterPreset preset)
                {
                    SelectImage(preset);
                }
            }
        );

        public RelayCommand<object> SelectFilterBackgroundCommand => new RelayCommand<object>(
            (a) =>
            {
                if (a is FilterPreset preset)
                {
                    SelectBackground(preset);
                }
            }
        );

        public RelayCommand<object> SelectFilterImageOrBackgroundCommand => new RelayCommand<object>(
            (a) =>
            {
                if (a is FilterPreset preset)
                {
                    GameController.LongPressCommand(SelectFilterImageCommand, SelectFilterBackgroundCommand, a);
                }
            }
        );

        void SelectImage(FilterPreset preset)
        => SelectImage(
            preset,
            "LOC_AutoFilter_SelectImageTitle",
            "png",
            Settings.CurrentThemeImagesPath,
            ref LastImagesPath);


        void SelectBackground(FilterPreset preset)
        => SelectImage(
            preset,
            "LOC_AutoFilter_SelectBackgroundTitle",
            "jpg",
            Settings.CurrentThemeBackgroundsPath,
            ref LastBackgroundsPath);

        void SelectImage( FilterPreset preset, string titleFormat, string extension, string path, ref string lastPath)
        {
            var title = string.Format(ResourceProvider.GetString(titleFormat), preset.Name);
            var newImage = FullscreenFilePicker.SelectFile(
                title,
                $"{extension.ToUpper()} Image|*.{extension}",
                    lastPath ?? path
            );

            var destImage = Path.Combine(path, $"{preset.Name}.{extension}");

            lastPath = FullscreenFilePicker.LastPath;

            ReplaceImage( newImage, destImage);

            preset.OnPropertyChanged(nameof(preset.Name));
            if (AutoFilers.ActiveFilterPreset == preset )
            {
                AutoFilers.ActiveFilterPreset = preset;

                // ActiveFilterPreset = null;

                // System.Timers.Timer timer = new System.Timers.Timer(1000)
                // {
                //     AutoReset = false, // Set the timer to auto-reset (repeat)
                //     Enabled = true // Start the timer
                // };

                // // Hook up the Elapsed event for the timer inline
                // timer.Elapsed += (sender, e) =>
                // {
                //     timer.Dispose();
                //     ActiveFilterPreset = preset;
                // };
            }
        }
    }
}