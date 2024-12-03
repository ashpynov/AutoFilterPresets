using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Resources;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace AutoFilterPresets.Setings.Models
{
    public enum SortingItemType
    {
        Preset,
        Presets,
        Source,
        Sources,
        Platform,
        Platforms,
    }
    public class SortingItem : ObservableObject
    {
        public string Name { get; set; }
        public SortingItemType SortingType { get; set; }
        public ObservableCollection<SortingItem> Items { get; set; }

        [DontSerialize]
        public string TranslatedName
        {
            get => new[] {SortingItemType.Presets, SortingItemType.Sources, SortingItemType.Platforms }.Contains(SortingType)
                ? ResourceProvider.GetString($"LOC_AutoFilterSettings_{SortingType.ToString().ToUpper()}")
                : Name;
        }

        [DontSerialize]
        public SortingItem Parent { get; set; } = null;

        [DontSerialize]
        public bool IsGroup
        {
            get => !IsFilter;
        }
        [DontSerialize]
        public bool IsFilter
        {
            get => new[] { SortingItemType.Presets, SortingItemType.Sources, SortingItemType.Platforms }.Contains(SortingType) == false;
        }

        private string imagePath;
        [DontSerialize]
        public string ImagesPath { get => imagePath; set => SetValue(ref imagePath, value); }

        private string compilationImagesPath;
        [DontSerialize]
        public string CompilationImagesPath { get => compilationImagesPath; set => SetValue(ref compilationImagesPath, value); }

        private string backgroundsPath;
        [DontSerialize]
        public string BackgroundsPath  { get => backgroundsPath; set => SetValue(ref backgroundsPath, value); }

        private string compilationBackgroundsPath;
        [DontSerialize]
        public string CompilationBackgroundsPath { get => compilationBackgroundsPath; set => SetValue(ref compilationBackgroundsPath, value); }

        [DontSerialize]
        public bool ImagesPathIsChanged = false;

        [DontSerialize]
        public bool BackgroundsPathIsChanged = false;
        [DontSerialize]
        public bool CompilationImagesPathIsChanged = false;

        [DontSerialize]
        public bool CompilationBackgroundsPathIsChanged = false;
    }
}