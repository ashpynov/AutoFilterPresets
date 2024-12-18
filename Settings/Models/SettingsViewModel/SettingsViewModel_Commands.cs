

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace AutoFilterPresets.Setings.Models
{
    public partial class SettingsViewModel
    {
        private static ILogger Logger = LogManager.GetLogger();
        bool HasDifference(CollectionModel fromCollection, CollectionModel toCollection, bool MissingOnly=false)
        {
            bool hasDiff = false;

            foreach (var name in Settings.FilterList.Select(f => f.Name))
            {
                var from = fromCollection.ImagesCollection.FirstOrDefault(f => f.Name == name);
                var to = toCollection.ImagesCollection.FirstOrDefault(f => f.Name == name);
                if (from != null && to == null)
                {
                    hasDiff = true;
                }
                else
                {
                    hasDiff = hasDiff
                    || ((!MissingOnly || (to?.Image).IsNullOrEmpty()) && from?.Image != to?.Image)
                    || ((!MissingOnly || (to?.Background).IsNullOrEmpty()) && from?.Background != to?.Background);
                }
                if (hasDiff)
                    return true;
            }
            return hasDiff;

        }

        string CopyValue(string from, string to, bool MissingOnly) => !MissingOnly || to.IsNullOrEmpty() ? from : to;
        void SyncCompilations(CollectionModel fromCollection, CollectionModel toCollection, bool MissingOnly=false)
        {
            foreach(var name in Settings.FilterList.Select(f => f.Name))
            {
                var from = fromCollection.ImagesCollection.FirstOrDefault(f => f.Name == name);
                var to = toCollection.ImagesCollection.FirstOrDefault(f => f.Name == name);

                if (from != null && to == null)
                {
                    to = new FilterImages(name);
                    toCollection.ImagesCollection.Add(to);
                }

                if (to == null)
                {
                    continue;
                }
                to.Image = CopyValue( from?.Image, to?.Image, MissingOnly );
                to.Background = CopyValue( from?.Background, to?.Background, MissingOnly );
            }
            toCollection.OnFilesChanged();
        }

        public RelayCommand CopyCompilationFullToRightCommand => new RelayCommand(
            () => SyncCompilations(PrimaryCollection, SecondaryCollection, false),
            () =>
                PrimaryCollection.SelectedCompilation is CompilationModel pfi
                && SecondaryCollection.SelectedCompilation is CompilationModel sfi
                && pfi != sfi
                && HasDifference(PrimaryCollection, SecondaryCollection, false)
        );

        public RelayCommand CopyCompilationFullToLeftCommand => new RelayCommand(
            () => SyncCompilations( SecondaryCollection, PrimaryCollection, false),
            () =>
                PrimaryCollection.SelectedCompilation is CompilationModel pfi
                && SecondaryCollection.SelectedCompilation is CompilationModel sfi
                && pfi != sfi
                && HasDifference(SecondaryCollection, PrimaryCollection, false)
        );

        public RelayCommand CopyCompilationToRightCommand => new RelayCommand(
            () => SyncCompilations(PrimaryCollection, SecondaryCollection,true),
            () =>
                PrimaryCollection.SelectedCompilation is CompilationModel pfi
                && SecondaryCollection.SelectedCompilation is CompilationModel sfi
                && pfi != sfi
                && HasDifference(PrimaryCollection, SecondaryCollection, true)
        );

        public RelayCommand CopyCompilationToLeftCommand => new RelayCommand(
            () => SyncCompilations( SecondaryCollection, PrimaryCollection, true),
            () =>
                PrimaryCollection.SelectedCompilation is CompilationModel pfi
                && SecondaryCollection.SelectedCompilation is CompilationModel sfi
                && pfi != sfi
                && HasDifference(SecondaryCollection, PrimaryCollection, true)
        );
        public RelayCommand CopyImageToRightCommand => new RelayCommand(
            () =>
            {
                SecondaryCollection.SelectedfilterImages.Image = PrimaryCollection.SelectedfilterImages.Image;
                SecondaryCollection.OnFilesChanged();
            },
            () =>
                PrimaryCollection.SelectedfilterImages is FilterImages pfi
                && SecondaryCollection.SelectedfilterImages is FilterImages sfi
                && pfi.Image != sfi.Image
        );

        public RelayCommand CopyImageToLeftCommand => new RelayCommand(
            () =>
            {
                PrimaryCollection.SelectedfilterImages.Image = SecondaryCollection.SelectedfilterImages.Image;
                PrimaryCollection.OnFilesChanged();
            },
            () =>
                PrimaryCollection.SelectedfilterImages is FilterImages pfi
                && SecondaryCollection.SelectedfilterImages is FilterImages sfi
                && pfi.Image != sfi.Image
        );

        public RelayCommand CopyBackgroundToRightCommand => new RelayCommand(
            () =>
            {
                SecondaryCollection.SelectedfilterImages.Background = PrimaryCollection.SelectedfilterImages.Background;
                SecondaryCollection.OnFilesChanged();
            },
            () =>
                PrimaryCollection.SelectedfilterImages is FilterImages pfi
                && SecondaryCollection.SelectedfilterImages is FilterImages sfi
                && pfi.Background != sfi.Background
        );

        public RelayCommand CopyBackgroundToLeftCommand => new RelayCommand(
            () =>
            {
                PrimaryCollection.SelectedfilterImages.Background = SecondaryCollection.SelectedfilterImages.Background;
                PrimaryCollection.OnFilesChanged();
            },
            () =>
                PrimaryCollection.SelectedfilterImages is FilterImages pfi
                && SecondaryCollection.SelectedfilterImages is FilterImages sfi
                && pfi.Background != sfi.Background
        );
    }
}