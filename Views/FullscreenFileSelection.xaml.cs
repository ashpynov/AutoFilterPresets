using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using AutoFilterPresets.Helpers;
using AutoFilterPresets.Setings.Models;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace AutoFilterPresets.Views
{

    public class FilePickerItem: ObservableObject
    {
        public string Name { get; set;}

        private string filePath;
        public string FilePath
        {
            get => filePath;
            set
            {
                SetValue(ref filePath, value);
                OnPropertyChanged(nameof(IsImage));
            }
        }
        public string Icon { get; set;}
        public bool IsFolder { get; set;}

        private bool selected = false;
        public bool Selected
        {
             get => selected;
             set => SetValue(ref selected, value);
        }

        public bool IsImage { get => new string[] { ".png", ".jpg" }.Contains(Path.GetExtension(FilePath).ToLower()); }
    }

    public class FullscreenFilePickerModel : ObservableObject
    {
        private readonly FullscreenFilePicker Window;
        private readonly string FileIcon = "\uefb2";
        private readonly string ImageIcon = "\uef4b";
        private readonly string ForderIcon = "\uef36";
        private readonly string DriveIcon = "\uef43";
        private readonly string UpIcon = "\uefd4";

        private DispatcherTimer _tooltipTimer;

        private string Filter;

        private ItemsControl PART_ItemsHost;
        private string prevPath = "";
        private string currentPath = "";
        public string CurrentPath
        {
            get => currentPath;
            set
            {
                if (Directory.Exists(value))
                {
                    prevPath = currentPath;
                    SetValue(ref currentPath, value);
                    ReadDirectory();
                }
            }
        }
        private ObservableCollection<FilePickerItem> items = new ObservableCollection<FilePickerItem>();
        public ObservableCollection<FilePickerItem> Items { get => items; set => SetValue(ref items, value); }

        private FilePickerItem selected = null;
        public FilePickerItem Selected { get => selected; set => SetValue(ref selected, value); }

        public string SelectedFilePath { get; private set; }

        public FullscreenFilePickerModel(FullscreenFilePicker window, string filter, string startFolder)
        {
            Filter = filter;
            Window = window;
            Window.DataContext = this;
            SelectedFilePath = null;
            CurrentPath = (startFolder is string && Directory.Exists(startFolder))
                ? startFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            CreateItemsControlEx();
        }

        public void ReadDirectory()
        {
            if (string.IsNullOrEmpty(currentPath))
            {
                Items.Clear();
                Items.AddMissing(DriveInfo
                    .GetDrives()
                    .Select(d => new FilePickerItem()
                    {
                        IsFolder = true,
                        Icon = DriveIcon,
                        FilePath = d.Name,
                        Name = d.Name
                    })); ;

                return;
            }

            var directories = Directory
                .GetDirectories(currentPath)
                .Select(d => new FilePickerItem()
                {
                    IsFolder = true,
                    Icon = ForderIcon,
                    FilePath = d,
                    Name = System.IO.Path.GetFileName(d)
                })
                .OrderBy(d => d.Name.ToLower())
                .ToList();


            directories.Insert(0, new FilePickerItem()
            {
                IsFolder = true,
                Icon = UpIcon,
                FilePath = Path.GetDirectoryName(currentPath),
                Name = ".."
            });

            string[] parts = Filter.Split('|');
            string[] patterns = parts[1].Split(';');

            var files = patterns
                .SelectMany(pattern => Directory.GetFiles(currentPath, pattern))
                .Select(f =>
                {
                    var i = new FilePickerItem()
                    {
                        IsFolder = false,
                        FilePath = f,
                        Name = System.IO.Path.GetFileName(f)
                    };
                    i.Icon = i.IsImage ? ImageIcon : FileIcon;
                    return i;
                })
                .OrderBy(f => f.Name.ToLower());

            Items.Clear();
            Items.AddMissing(directories.Concat(files));

            FilePickerItem selected = null;
            if ((Items.FirstOrDefault(i => i.FilePath?.ToLower() == prevPath?.ToLower()) ?? Items.FirstOrDefault()) is FilePickerItem item)
            {
                item.Selected = true;
                selected = item;
            }

            if ( selected is FilePickerItem
                && PART_ItemsHost is ItemsControl
                && VisualTreeHelperEx.FindVisualChild<VirtualizingStackPanel>(PART_ItemsHost) is VirtualizingStackPanel itemsPanel)
            {
                var index = PART_ItemsHost.Items.IndexOf(selected);
                if (index != -1)
                {
                    itemsPanel.BringIndexIntoViewPublic(index);
                }
            }
        }

        public RelayCommand SelectCommand => new RelayCommand(
            () =>
            {
                if (Selected == null)
                {
                    return;
                }

                if (Selected.IsFolder)
                {
                    CurrentPath = selected.FilePath;
                }
                else
                {
                    SelectedFilePath = selected.FilePath;
                    Window.DialogResult = true;
                }
            }
        );

        public RelayCommand<object> CancelCommand => new RelayCommand<object>((a) =>
        {
            if (a is TextBox textBox)
            {
                textBox.Text = CurrentPath;
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                return;
            }
            else
            {
                Window.DialogResult = false;
            }
        });

        public RelayCommand UpCommand => new RelayCommand(
        () => { CurrentPath = Path.GetDirectoryName(currentPath); },
        () => Path.GetDirectoryName(currentPath) is string
        );

        public RelayCommand<object> EditCommand => new RelayCommand<object>((a) =>
        {
            if (a is TextBox textBox)
            {
                textBox.Text = TextInput(textBox.Text);
                CurrentPath = textBox.Text;
            }
        });

        static Button currentFocusedButton = null;
        private void TooltipTimer_Tick(object sender, EventArgs e)
        {
            _tooltipTimer.Stop();
            if (currentFocusedButton is Button button && button.Content is FilePickerItem item && item.IsImage)
            {
                var toolTip = new ToolTip()
                {
                    Style = Application.Current.TryFindResource("FilePickerFilePreviewTooltip") as Style
                            ?? Window.FindResource("FilePickerFilePreviewTooltip") as Style,
                    Content = item,
                    PlacementTarget = button
                };
                button.ToolTip = toolTip;
                toolTip.IsOpen = true;
            }
        }
        public void FileButton_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is FilePickerItem item)
            {
                foreach (var i in items)
                {
                    var selected = i == item;
                    if (i.Selected != selected)
                    {
                        i.Selected = selected;
                    };
                }
                Selected = item;
                currentFocusedButton = button;

                if (item.IsImage)
                {
                    _tooltipTimer.Start();
                }
            }
        }

        public void FileButton_LostFocus(object sender, RoutedEventArgs e)
        {
            _tooltipTimer.Stop();
            if (sender is Button button && button.ToolTip is ToolTip tooltip)
            {
                currentFocusedButton = null;
                tooltip.StaysOpen = false;
                tooltip.IsOpen = false;
                button.ToolTip = null;
            }
        }

        public RelayCommand<object> ToggleCapsCommand
        {
            get => new RelayCommand<object>((a) =>
            {
                if (a is Window window && window.FindName("GridInput") is Grid GridInput)
                {
                    var capsEnabledField = window.GetType().GetField("capsEnabled", BindingFlags.Instance | BindingFlags.NonPublic);
                    bool capsEnabled = (bool) capsEnabledField.GetValue(window);
                    foreach (var child in GridInput.Children)
                    {
                        if (child is Button button)
                        {
                            var cont = button.Content?.ToString();
                            if (cont.IsNullOrEmpty() || cont.Length > 1)
                            {
                                continue;
                            }
                            var symbolLower = "-!:;1234567890";
                            var symbolUpper = "_/.,~@#$%^&*()";

                            var from = capsEnabled ? symbolUpper : symbolLower;
                            var to = !capsEnabled ? symbolUpper : symbolLower;

                            var ch = button.Content?.ToString();
                            if (from.Contains(ch))
                            {
                                button.Content = to[from.IndexOf(ch)];
                            }
                            else
                            {
                                button.Content = capsEnabled ? cont.ToLower() : cont.ToUpper();
                            }

                        }
                    }

                    capsEnabledField.SetValue( window,  !capsEnabled);
                }
            });
        }

        void HackScreenKeyboard(Window inputWindow)
        {
            try
            {
                if (inputWindow.FindName("GridInput") is Grid GridInput)
                {

                    foreach (var child in GridInput.Children)
                    {
                        if (child is Button button && Grid.GetRow(button) == 4 && Grid.GetColumn(button) == 0 )
                        {
                            button.Command = ToggleCapsCommand;
                            button.CommandParameter = inputWindow;
                        }
                    }
                }

                for (int i = 0; i < inputWindow.InputBindings.Count; i++)
                {
                    dynamic binding = inputWindow.InputBindings[i];
                    if ((int)binding.Button == (int)ControllerInput.RightStick)
                    {
                        inputWindow.InputBindings.RemoveAt(i);
                        break;
                    }
                }
                inputWindow.InputBindings.Add(GameController.CreateInputBinding(ControllerInput.RightStick, ToggleCapsCommand, inputWindow));
            }
            catch
            {
            }
        }

        public string TextInput(string input)
        {
            var assembly = Application.Current.GetType().Assembly;
            Type type = assembly.GetType("Playnite.FullscreenApp.Windows.TextInputWindow");
            dynamic inputWindow = Activator.CreateInstance(type);
            HackScreenKeyboard(inputWindow);
            var oldValue = input;
            try
            {
                Window.Opacity = 0;
                var result = inputWindow.ShowInput(Window, "", "", input);
                return result.Result ? result.SelectedString : oldValue;
            }
            finally
            {
                Window.Opacity = 1;
            }
        }
        public void CreateItemsControlEx()
        {
            _tooltipTimer = new DispatcherTimer();
            _tooltipTimer.Interval = TimeSpan.FromMilliseconds(300); // Set debounce delay
            _tooltipTimer.Tick += TooltipTimer_Tick;

            // Use reflection to get the ButtonEx Type
            Assembly assembly = Assembly.GetEntryAssembly();
            Assembly playnite = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playnite.dll"));

            Type buttonExType = assembly.GetType("Playnite.FullscreenApp.Controls.ButtonEx");
            Type itemsControlExType = assembly.GetType("Playnite.FullscreenApp.Controls.ItemsControlEx");

            Type focusBehaviorsType = playnite.GetType("Playnite.Behaviors.FocusBahaviors");

            FieldInfo focusBindingPropertyField = focusBehaviorsType.GetField("FocusBindingProperty", BindingFlags.Static | BindingFlags.Public);
            DependencyProperty focusBindingProperty = (DependencyProperty)focusBindingPropertyField.GetValue(null);

            if (itemsControlExType == null || buttonExType == null)
            {
                return;
            }

            // Create a FrameworkElementFactory for the ButtonEx
            var buttonExFactory = new FrameworkElementFactory(buttonExType);

            // Set bindings for the ButtonEx properties using Button's DependencyProperties
            buttonExFactory.SetBinding(Button.ContentProperty, new Binding());
            buttonExFactory.SetBinding(Button.CommandProperty, new Binding("DataContext.SelectCommand") { ElementName = "PART_DockHost" });
            buttonExFactory.SetBinding(Button.CommandParameterProperty, new Binding());
            buttonExFactory.SetBinding(focusBindingProperty, new Binding("Selected") { Mode = BindingMode.TwoWay });

            buttonExFactory.SetValue(FrameworkElement.StyleProperty, Application.Current.FindResource("GameMenuButton"));
            buttonExFactory.SetValue(Button.ContentTemplateProperty, Application.Current.TryFindResource("FilePickerFileButtonTemplate")
                                                                        ?? Window.FindResource("FilePickerFileButtonTemplate"));


            buttonExFactory.SetValue(Button.WidthProperty, Double.NaN);
            buttonExFactory.SetValue(Button.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            buttonExFactory.SetValue(Button.HorizontalContentAlignmentProperty, HorizontalAlignment.Left);
            buttonExFactory.SetValue(Button.VerticalContentAlignmentProperty, VerticalAlignment.Center);

            buttonExFactory.AddHandler(Button.GotFocusEvent, new RoutedEventHandler(FileButton_GotFocus));
            buttonExFactory.AddHandler(Button.LostFocusEvent, new RoutedEventHandler(FileButton_LostFocus));

            // Create the DataTemplate and set its VisualTree
            var dataTemplate = new DataTemplate
            {
                VisualTree = buttonExFactory
            };

            // Create the ControlTemplate for ItemsControlEx
            var scrollViewerFactory = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewerFactory.SetValue(ScrollViewer.FocusableProperty, false);
            scrollViewerFactory.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            scrollViewerFactory.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            scrollViewerFactory.SetValue(ScrollViewer.CanContentScrollProperty, true);

            var itemsPresenterFactory = new FrameworkElementFactory(typeof(ItemsPresenter));
            scrollViewerFactory.AppendChild(itemsPresenterFactory);

            var controlTemplate = new ControlTemplate();
            controlTemplate.VisualTree = scrollViewerFactory;

            // Create the ItemsControlEx
            var ItemsControlEx = Activator.CreateInstance(itemsControlExType) as ItemsControl;
            ItemsControlEx.Name = "PART_ItemsHost";
            PART_ItemsHost = ItemsControlEx;

            // Set ItemsPanel to VirtualizingStackPanel
            ItemsPanelTemplate itemsPanelTemplate = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            ItemsControlEx.ItemsPanel = itemsPanelTemplate;

            // Enable Virtualization and set Virtualization Mode
            VirtualizingStackPanel.SetIsVirtualizing(ItemsControlEx, true);
            VirtualizingStackPanel.SetVirtualizationMode(ItemsControlEx, VirtualizationMode.Recycling);

            ItemsControlEx.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Items"));

            ItemsControlEx.SetValue(focusBindingProperty, true);

            ItemsControlEx.Focusable = false;
            ItemsControlEx.ItemTemplate = dataTemplate;
            ItemsControlEx.Template = controlTemplate;

            // Create the DockPanel and add ItemsControlEx to it
            var dockPanel = Window.FindName("PART_DockHost") as DockPanel;
            dockPanel.Children.Add(ItemsControlEx);

            DependencyProperty inputHintProperty =
                 (DependencyProperty)buttonExType
                    .GetField("InputHintProperty", BindingFlags.Static | BindingFlags.Public)
                    .GetValue(null);

            Button upFooterButton = Activator.CreateInstance(buttonExType) as Button;
            upFooterButton.Style = Application.Current.TryFindResource("ButtonBottomMenu") as Style;
            upFooterButton.Content = ResourceProvider.GetString("LOC_FilePicker_Footer_UpCommand");
            upFooterButton.SetBinding(Button.CommandProperty, new Binding("DataContext.UpCommand") { ElementName = "PART_DockHost" });
            upFooterButton.SetValue(inputHintProperty, ResourceProvider.GetResource("ButtonPromptY"));

            var footerStackPanel = Window.FindName("PART_FooterPanel") as StackPanel;
            footerStackPanel.Children.Add(upFooterButton);

            if (Window.FindName("PART_CurrentPath") is TextBox textBox)
            {
                textBox.InputBindings.Add(GameController.CreateInputBinding(GameController.ConfirmationBinding, EditCommand, textBox));
                textBox.InputBindings.Add(GameController.CreateInputBinding(GameController.CancellationBinding, CancelCommand, textBox));
            }

            Window.InputBindings.Add(GameController.CreateInputBinding(ControllerInput.Y, UpCommand));
            Window.InputBindings.Add(GameController.CreateInputBinding(GameController.CancellationBinding, CancelCommand));
        }
    }


    public partial class FullscreenFilePicker : Window
    {
        public static string LastPath = null;

        public FullscreenFilePicker(string title) : base()
        {
            ((IComponentConnector)this).InitializeComponent();
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Title = title;

            foreach (Window window in Application.Current.Windows)
            {
                if (window.IsActive)
                {
                    Owner = window;
                    if (Owner != Application.Current.MainWindow)
                    {
                        Owner.Opacity = 0;
                    }
                    break;
                }
            }

            dynamic model = SettingsViewModel.PlayniteAPI.MainView
                    .GetType()
                    .GetField("mainModel", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(SettingsViewModel.PlayniteAPI.MainView);

            if (model != null)
            {
                Width = model.WindowWidth;
                Height = model.WindowHeight;
                if (FindName("GridMain") is Grid mainGrid)
                {
                    mainGrid.Width = model.ViewportWidth;
                }
            }

            //Background = new SolidColorBrush(Color.FromArgb(1,0,0,0));
            AllowsTransparency = true;
        }

        private void TextBox_ScrolltoEnd(object sender, EventArgs e)
        {
            if (sender is TextBox tb && !tb.IsFocused)
            {
                tb.ScrollToHorizontalOffset(tb.ActualWidth);
            }
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            if (e.Key == Key.Up)
            {
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Down)
            {
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Return)
            {
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Escape)
            {
                textBox.Text = (DataContext as FullscreenFilePickerModel).CurrentPath;
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
                return;
            }
        }
        public static string SelectFile(string title, string filter, string startFolder = null)
        {
            string selectedFilePath = null;
            Application.Current.Dispatcher.Invoke(() =>
            {

                var window = new FullscreenFilePicker(title);
                try
                {
                    var model = new FullscreenFilePickerModel(window, filter, startFolder ?? LastPath);
                    if (window.ShowDialog() == true)
                    {
                        selectedFilePath = model.SelectedFilePath;
                    }
                    LastPath = model.CurrentPath;
                }
                finally
                {
                    if (window.Owner != null && window.Owner != Application.Current.MainWindow)
                    {
                        window.Owner.Opacity = 1;
                    }
                }
            });
            return selectedFilePath;
        }
    }
}
