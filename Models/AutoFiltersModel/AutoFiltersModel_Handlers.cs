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
        private void OnMainModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ActiveFilterPreset)
                || e.PropertyName == "IsExtraFilterActive" )
            {
                FilterPreset value = FindAutoPreset();
                if (value != activeFilterPreset)
                {
                    SetValue(ref activeFilterPreset, value, nameof(ActiveFilterPreset));
                }
                BringActivePresetIntoView();

            }
            else if (e.PropertyName == "SortedFilterFullscreenPresets")
            {
                RegisterHandlers();
                Update();
                UpdateFilterPresetSelector();
            }
        }

        private void OnItemsFilterPresetsSizeChanged(object sender, object e)
        {
            BringActivePresetIntoView();
        }

        private void OnDatabaseUpdated(object sender, object e)
        {
            Update();
        }
    }
}