﻿using AutoFilterPresets.Models;
using AutoFilterPresets.Setings.Models;
using AutoFilterPresets.Settings.Views;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AutoFilterPresets
{
    public partial class AutoFilterPresets : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        public static IPlayniteAPI PlayniteAPI { get; private set; }
        public static SettingsViewModel SettingsViewModel { get; set; }
        private static readonly string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public override Guid Id { get; } = Guid.Parse("1844176c-4d02-4bf8-b852-78b36a9de193");

        public static AutoFiltersModel AutoFilters;
        public static FullscreenMenuModel FullscreenMenu;

        public AutoFilterPresets(IPlayniteAPI api) : base(api)
        {
            PlayniteAPI = api;
            SettingsViewModel = new SettingsViewModel(this, api);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
            Localization.Load(PluginFolder, PlayniteAPI.ApplicationSettings.Language);

            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "AutoFilterPresets",
                SettingsRoot = $"{nameof(SettingsViewModel)}.{nameof(SettingsViewModel.Settings)}"
            });

            if (PlayniteAPI.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                AutoFilters = new AutoFiltersModel(PlayniteAPI, SettingsViewModel.Settings);
                FullscreenMenu = new FullscreenMenuModel(AutoFilters, SettingsViewModel.Settings);
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return SettingsViewModel;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SettingsView();
        }
    }
}