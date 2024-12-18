using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Playnite.SDK.Data;

namespace AutoFilterPresets.Setings.Models
{
    public partial class SettingsViewModel : ObservableObject
    {
        void UpdateKnownThemesFolders(IEnumerable<CompilationModel> compilations, IEnumerable<CompilationModel> compilationWithFolders)
        {
            foreach (var compilation in compilations)
            {
                var compilationFolders = compilationWithFolders?.FirstOrDefault(t => t.Id.IsNoCaseEqual(compilation.Id));
                compilation.FilterImagesFolder = compilation.GetCompilationRelativePath(compilationFolders?.FilterImagesFolder ?? @"Icons\Filter") ?? compilation.FilterImagesFolder ;
                compilation.FilterBackgroundsFolder = compilation.GetCompilationRelativePath(compilationFolders?.FilterBackgroundsFolder) ?? compilation.FilterBackgroundsFolder;
            }
        }

        List<CompilationModel> GetReconfiguredCompilations(IEnumerable<CompilationModel> compilations)
        {
            var configured = new List<CompilationModel>();
            foreach (var compilation in compilations)
            {
                if ( compilation.IsTheme &&
                    ( compilation.FilterImagesFolder.IsNullOrEmpty()
                        || compilation.FilterImagesFolder.ToLower().Trim('\\') == @"Icons\Filter".ToLower()
                    ) && compilation.FilterBackgroundsFolder.IsNullOrEmpty())
                {
                    continue;
                }

                var known = AutoFilterPresets.DefaultThemeFolders.FirstOrDefault(t => t.Id.IsNoCaseEqual(compilation.Id));
                if (known == null
                    || !compilation.GetCompilationRelativePath(known.FilterImagesFolder).IsNoCaseEqual(compilation.FilterImagesFolder)
                    || !compilation.GetCompilationRelativePath(known.FilterBackgroundsFolder).IsNoCaseEqual(compilation.FilterBackgroundsFolder)
                )
                {
                    configured.Add(Serialization.GetClone(compilation));
                }
            }
            return (configured.Count > 0) ? configured : null;
        }

        List<CompilationModel> GetAvailableCompilations()
        {
            var result = CompilationModel.EnumThemes().ToList();
            result.Sort((a,b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            if (Settings.Compilations?.Count > 0)
            {
                foreach (var c in settings.Compilations)
                {
                    if (result.FirstOrDefault(t => t.Id.IsNoCaseEqual(c.Id)) is CompilationModel compilation)
                    {
                        c.FilterImagesFolder =  compilation.FilterImagesFolder ?? c.FilterImagesFolder;
                        c.FilterBackgroundsFolder = compilation.FilterBackgroundsFolder ?? c.FilterBackgroundsFolder;
                    }
                    else
                    {
                        result.Add(c);
                    }
                }
            }
            UpdateKnownThemesFolders(result, AutoFilterPresets.DefaultThemeFolders);
            return result;
        }

        static CompilationModel GetCurrentCompilation() =>
            compilations.FirstOrDefault(t => t.Id == PlayniteAPI.ApplicationSettings.FullscreenTheme)
            ?? compilations.FirstOrDefault( t => t.Id == null);

    }
}