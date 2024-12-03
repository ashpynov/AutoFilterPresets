using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Playnite.SDK.Data;

namespace AutoFilterPresets.Setings.Models
{
    public partial class SettingsViewModel : ObservableObject
    {
        static readonly List<CompilationModel> KnownThemeFolders = new List<CompilationModel>()
        {
            new CompilationModel("AllyNite_e1f711b2-2e8e-41a7-a843-c931045cfaa6", @"Icons\Filter", @"Icons\FilterBackground"),
            new CompilationModel("Aniki_ReMake_bb8728bd-ac83-4324-88b1-ee5c586527d1", @"Icons\Filter"),
            new CompilationModel("Aniki_Lite", @"Icons\Filter"),
            new CompilationModel("Aniki_Remix_df9e4536-addb-46a6-9a2c-4550c7cff461", @"Icons\Filter"),
            new CompilationModel("PS5reborn_saVantCZ", @"Icons\Filters"),
            new CompilationModel("DashX_saVantCZ", @"Images\FilterPresetsDash"),
            new CompilationModel("(u)biquity_e71482ce-33e2-4b23-aaea-de576975d3a9", @"Images\Icons\FilterIcons"),
            new CompilationModel("Anthem_0dd7c65b-909e-49ec-b19b-7277cb28ec0f", @"Images\Icons\FilterIcons"),
            new CompilationModel("ClarityLike_97ac384b-d6a2-4e5d-85d7-bb10b02ad2c0", @"Images\Filter"),
            new CompilationModel("Hero_b490d300-ff04-4f7d-905e-a3ee6e421ecf", @"Icons\_Filter"),
            new CompilationModel("J-Hero_c0fabc1d-aadb-41e8-be6c-82e27147f6e4", @"Icons\Filter"),
            new CompilationModel("McHERO", @"Icons\Filter"),
            new CompilationModel("Nintendo Switch-ish_0d020ed5-0f3c-4c1a-bf9a-c983ef7d74b7", @"FilterIcons"),
            new CompilationModel("Playhub", @"Icons\RenameTo'Filters'"),
            new CompilationModel("Vapour_dd1ccb2e-7025-4b6a-8b80-ddad9bdff48f", @"Images\FilterLogos"),
        };

        void UpdateKnownThemesFolders(IEnumerable<CompilationModel> compilations, IEnumerable<CompilationModel> compilationWithFolders)
        {
            foreach (var compilation in compilations)
            {
                var compilationFolders = compilationWithFolders?.FirstOrDefault(t => t.Id?.ToLower() == compilation.Id?.ToLower());
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
                    ( string.IsNullOrEmpty(compilation.FilterImagesFolder)
                        || compilation.FilterImagesFolder.ToLower().Trim('\\') == @"Icons\Filter".ToLower()
                    ) && string.IsNullOrEmpty(compilation.FilterBackgroundsFolder))
                {
                    continue;
                }

                var known = KnownThemeFolders.FirstOrDefault(t => t.Id?.ToLower() == compilation.Id?.ToLower());
                if (known == null
                    || compilation.GetCompilationRelativePath(known.FilterImagesFolder)?.ToLower() != compilation.FilterImagesFolder?.ToLower()
                    || compilation.GetCompilationRelativePath(known.FilterBackgroundsFolder)?.ToLower() != compilation.FilterBackgroundsFolder?.ToLower()
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
                    if (result.FirstOrDefault(t => t.Id?.ToLower() == c.Id?.ToLower()) is CompilationModel compilation)
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
            UpdateKnownThemesFolders(result, KnownThemeFolders);
            return result;
        }

        static CompilationModel GetCurrentCompilation() =>
            compilations.FirstOrDefault(t => t.Id == PlayniteAPI.ApplicationSettings.FullscreenTheme)
            ?? compilations.FirstOrDefault( t => t.Id == null);

    }
}