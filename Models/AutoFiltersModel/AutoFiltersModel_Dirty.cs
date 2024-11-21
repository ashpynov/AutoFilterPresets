using AutoFilterPresets.Helpers;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;


namespace AutoFilterPresets.Models
{
    public partial class AutoFiltersModel : INotifyPropertyChanged
    {
        private void SuppressNativeUpdate(Control sender)
        {
            Logger.Debug("Injecting into FilterPresetSelector...");

            FilterPresetSelector = sender
                .Template
                .FindName("PART_FilterPresetSelector", sender as FrameworkElement)
                as Control
                ?? VisualTreeHelperEx.FindVisualChild<Control>(sender as Control, "FilterPresetSelector");

            if (FilterPresetSelector == null)
            {
                Logger.Error("FilterPresetSelector not found");
                return;
            }

            ItemsFilterPresets = FilterPresetSelector
                .GetType()
                .GetField(nameof(ItemsFilterPresets), BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(FilterPresetSelector) as ItemsControl;

            if (ItemsFilterPresets == null)
            {
                Logger.Error("ItemsFilterPresets property not found");
                return;
            }

            var methodInfo = FilterPresetSelector
                .GetType()
                .GetMethod("MainModel_PropertyChanged", BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
            {
                Logger.Error("MainModel_PropertyChanged method not found");
                return;
            }

            var handler = (PropertyChangedEventHandler)Delegate.CreateDelegate(typeof(PropertyChangedEventHandler), FilterPresetSelector, methodInfo);
            mainModel.PropertyChanged -= handler;

            Logger.Debug("Native MainModel_PropertyChanged method unsubscribed");
        }

        private void ReplaceCommand(string name)
        {
            try
            {
                object mainModel = PlayniteAPI.MainView
                    .GetType()
                    .GetField("mainModel", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(PlayniteAPI.MainView);

                Logger.Debug($"Replacing {name}");

                var property = mainModel
                    .GetType()
                    .GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (property == null)
                {
                    Logger.Error($"Not found property {name}");
                    return;
                }

                dynamic command = property.GetGetMethod()?.Invoke(mainModel, null);
                if (command == null)
                {
                    Logger.Error($"Cannot get property {name} value");
                    return;
                };

                dynamic newCommand = GetType().GetProperty(name).GetValue(this);
                newCommand.Gesture = command.Gesture;

                property.GetSetMethod(true)?.Invoke(mainModel, new[] { newCommand });
                return;
            }
            catch
            {
                Logger.Error($"Cannot replace {name} connamd");
            }
        }

        private void UpdateFilterPresetSelector()
        {
            if (ItemsFilterPresets == null) return;

            Assembly playnite = Assembly.LoadFrom(Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "playnite.dll"));
            Type ObjectEqualityToBoolConverterType = playnite.GetType("Playnite.Converters.ObjectEqualityToBoolConverter");

            Assembly assembly = Assembly.GetEntryAssembly();
            Type CheckBoxExType = assembly.GetType("Playnite.FullscreenApp.Controls.CheckBoxEx");

            ItemsFilterPresets.Items.Clear();
            foreach (var preset in AutoPresets)
            {
                CheckBox item = Activator.CreateInstance(CheckBoxExType) as CheckBox;
                item.Style = ResourceProvider.GetResource<Style>("ItemFilterQuickPreset");
                item.Command = mainModel.ApplyFilterPresetCommand;
                item.CommandParameter = preset;
                item.DataContext = preset;

                BindingOperations.SetBinding(
                    item,
                    AutomationProperties.NameProperty,
                    new Binding
                    {
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.Default,
                        Source = preset,
                        Path = new PropertyPath(nameof(preset.Name))
                    }
                );

                BindingOperations.SetBinding(
                    item,
                    CheckBox.IsCheckedProperty,
                    new Binding
                    {
                        Mode = BindingMode.OneWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.Default,
                        Source = this,
                        Path = new PropertyPath(nameof(mainModel.ActiveFilterPreset)),
                        Converter = Activator.CreateInstance(ObjectEqualityToBoolConverterType) as IValueConverter,
                        ConverterParameter = preset,
                    }
                );

                item.SizeChanged += (o, e) => BringActivePresetIntoView();

                ItemsFilterPresets.Items.Add(item);
            }
            BringActivePresetIntoView();
        }

        private void SelectPreset()
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "playnite.dll"));
                Type genericTypeDefinition = assembly.GetType("System.SelectableNamedObject`1");

                if (genericTypeDefinition == null) return;

                Type constructedType = genericTypeDefinition.MakeGenericType(typeof(FilterPreset));

                var selectableNamedObjects = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(genericTypeDefinition.MakeGenericType(typeof(FilterPreset))));
                foreach (var filterPreset in AutoPresets)
                {
                    dynamic instance = Activator.CreateInstance(constructedType, filterPreset);
                    selectableNamedObjects.Add(instance);
                }

                // Get the ItemSelector type and SelectSingle method
                Type itemSelectorType = assembly.GetType("Playnite.ItemSelector");
                MethodInfo selectSingleMethod = itemSelectorType.GetMethod("SelectSingle", BindingFlags.Static | BindingFlags.Public);

                // Prepare the parameters for the SelectSingle method
                object[] parameters = new object[]
                {
                    ResourceProvider.GetString("LOCSettingsTopPanelFilterPresetsItem"),
                    "",
                    selectableNamedObjects,
                    null
                };

                // Call the SelectSingle method
                bool result = (bool)selectSingleMethod.MakeGenericMethod(typeof(FilterPreset)).Invoke(null, parameters);

                if (result)
                {
                    ActiveFilterPreset = (FilterPreset)parameters[3];
                }

            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message);
            };

        }
    }
}