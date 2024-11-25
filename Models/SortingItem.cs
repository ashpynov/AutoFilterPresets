using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Resources;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace AutoFilterPresets.Models
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
    public class SortingItem
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
    }
}