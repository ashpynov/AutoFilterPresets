

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace AutoFilterPresets.Models
{
    public partial class SettingsViewModel
    {
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
                    || ((!MissingOnly || string.IsNullOrEmpty(to?.Image)) && from?.Image != to?.Image)
                    || ((!MissingOnly || string.IsNullOrEmpty(to?.Background)) && from?.Background != to?.Background);
                }
                if (hasDiff)
                    return true;
            }
            return hasDiff;

        }

        string CopyValue(string from, string to, bool MissingOnly) => !MissingOnly || string.IsNullOrEmpty(to) ? from : to;
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

                to.Image = CopyValue( from?.Image, to?.Image, MissingOnly );
                to.Background = CopyValue( from?.Background, to?.Background, MissingOnly );
            }
            toCollection.OnFilesChanged();
        }

        public RelayCommand CopyCompilationFullToRightCommand => new RelayCommand(
            () => SyncCompilations(PrimaryCollection, SecondaryCollection, false),
            () =>
                PrimaryCollection.SelectedCompilation is Compilation pfi
                && SecondaryCollection.SelectedCompilation is Compilation sfi
                && pfi != sfi
                && HasDifference(PrimaryCollection, SecondaryCollection, false)
        );

        public RelayCommand CopyCompilationFullToLeftCommand => new RelayCommand(
            () => SyncCompilations( SecondaryCollection, PrimaryCollection, false),
            () =>
                PrimaryCollection.SelectedCompilation is Compilation pfi
                && SecondaryCollection.SelectedCompilation is Compilation sfi
                && pfi != sfi
                && HasDifference(SecondaryCollection, PrimaryCollection, false)
        );

        public RelayCommand CopyCompilationToRightCommand => new RelayCommand(
            () => SyncCompilations(PrimaryCollection, SecondaryCollection,true),
            () =>
                PrimaryCollection.SelectedCompilation is Compilation pfi
                && SecondaryCollection.SelectedCompilation is Compilation sfi
                && pfi != sfi
                && HasDifference(PrimaryCollection, SecondaryCollection, true)
        );

        public RelayCommand CopyCompilationToLeftCommand => new RelayCommand(
            () => SyncCompilations( SecondaryCollection, PrimaryCollection, true),
            () =>
                PrimaryCollection.SelectedCompilation is Compilation pfi
                && SecondaryCollection.SelectedCompilation is Compilation sfi
                && pfi != sfi
                && HasDifference(SecondaryCollection, PrimaryCollection, true)
        );
        public RelayCommand CopyImageToRightCommand => new RelayCommand(
            () => SecondaryCollection.SelectedfilterImages.Image = PrimaryCollection.SelectedfilterImages.Image,
            () =>
                PrimaryCollection.SelectedfilterImages is FilterImages pfi
                && SecondaryCollection.SelectedfilterImages is FilterImages sfi
                && pfi.Image != sfi.Image
        );

        public RelayCommand CopyImageToLeftCommand => new RelayCommand(
            () => PrimaryCollection.SelectedfilterImages.Image = SecondaryCollection.SelectedfilterImages.Image,
            () =>
                PrimaryCollection.SelectedfilterImages is FilterImages pfi
                && SecondaryCollection.SelectedfilterImages is FilterImages sfi
                && pfi.Image != sfi.Image
        );

        public RelayCommand CopyBackgroundToRightCommand => new RelayCommand(
            () => SecondaryCollection.SelectedfilterImages.Background = PrimaryCollection.SelectedfilterImages.Background,
            () =>
                PrimaryCollection.SelectedfilterImages is FilterImages pfi
                && SecondaryCollection.SelectedfilterImages is FilterImages sfi
                && pfi.Background != sfi.Background
        );

        public RelayCommand CopyBackgroundToLeftCommand => new RelayCommand(
            () => PrimaryCollection.SelectedfilterImages.Background = SecondaryCollection.SelectedfilterImages.Background,
            () =>
                PrimaryCollection.SelectedfilterImages is FilterImages pfi
                && SecondaryCollection.SelectedfilterImages is FilterImages sfi
                && pfi.Background != sfi.Background
        );
    }
}