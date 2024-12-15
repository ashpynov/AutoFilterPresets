using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace AutoFilterPresets.Setings.Models
{
    public class CompilationModel : ObservableObject
    {

        public string Id { get; set; }


        private string name;

        public string Name { get => name; set => SetValue(ref name, value); }

        public string Path { get; set; }

        public CompilationModel()
        {

        }
        public CompilationModel(string id, string filterImagesFolder, string filterBackgroundsFolder = null )
        {
            Id = id;
            FilterImagesFolder = filterImagesFolder;
            FilterBackgroundsFolder = filterBackgroundsFolder;
        }

        [DontSerialize]
        public bool IsTheme { get; set; }

        [DontSerialize]
        public bool IsGroup { get; set; } = false;

        public string FilterImagesFolder { get; set; }
        public string FilterBackgroundsFolder { get; set; }

        private static readonly ILogger logger = LogManager.GetLogger();
        public static CompilationModel FromThemeFile(string path)
        {
            if (path == null)
            {
                return null;
            }

            path = path.EndsWith("theme.yaml") ? path : System.IO.Path.Combine(path, "theme.yaml");

            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var compilation = Serialization.FromYamlFile<CompilationModel>(path);
                compilation.Path =  System.IO.Path.GetDirectoryName(path);
                compilation.IsTheme = true;
                return compilation;
            }
            catch (Exception e)
            {
                logger.Error($"Error loading compilation {path}: \n{e.Message}");
                return null;
            }
        }
        public static CompilationModel FromThemeId(string id)
        {
            return FromThemeFile(GetThemePath(id));
        }

        private static string GetThemePath(string id)
        {
            var compilation = FindCompilation(id);
            return compilation?.Path;
        }
        static public IEnumerable<CompilationModel> EnumThemes()
        {
            var themesRoot = new List<string>();
            if (!SettingsViewModel.PlayniteAPI.ApplicationInfo.IsPortable)
            {
                themesRoot.Add(SettingsViewModel.PlayniteAPI.Paths.ConfigurationPath);
            }

            themesRoot.AddMissing(SettingsViewModel.PlayniteAPI.Paths.ApplicationPath);

            foreach (var root in themesRoot)
            {
                var themesFolder = System.IO.Path.Combine(root, "Themes", "Fullscreen");
                if (Directory.Exists(themesFolder))
                {
                    foreach (var compilationPath in Directory.EnumerateDirectories(themesFolder))
                    {
                        CompilationModel compilation = CompilationModel.FromThemeFile(System.IO.Path.Combine(compilationPath, "theme.yaml"));
                        if (compilation != null) yield return compilation;
                    }
                }
            }
        }
        public static CompilationModel FindCompilation(string compilationId) => EnumThemes().FirstOrDefault(compilation => compilation.Id == compilationId);

        public string GetCompilationRelativePath(string path, bool check = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var fullPath = GetCompilationFullPath(path, check);
            return string.IsNullOrEmpty(fullPath)
                ? null
                : fullPath.ToLower().Trim('\\').StartsWith((Path ?? "").ToLower().Trim('\\'))
                    ? fullPath.Substring((Path ?? "").Trim('\\').Length).TrimStart('\\')
                    : fullPath;
        }

        public string GetCompilationFullPath(string path, bool check = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                return Path;
            }

            var fullPath = System.IO.Path.GetFullPath(path.Contains(":") ? path : System.IO.Path.Combine(Path?? "", path));
            return !check || Directory.Exists(fullPath) || File.Exists(fullPath) ? fullPath.TrimStart('\\') : null;
        }

    }
}