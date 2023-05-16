using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace XHexEditor.Wpf.App
{
    static class AppData
    {
        class Settings
        {
            public string[] RecentFiles { get; set; } = new string[0];
        }

        #region Construction

        static AppData()
        {
            _filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), typeof(AppData).Namespace!, "settings.json");
            Load();
        }

        #endregion

        #region Fields

        private static readonly string _filePath;
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        private static Settings _settings = new Settings();

        #endregion

        #region Properties

        public static string[] RecentFiles => _settings.RecentFiles;

        #endregion

        #region Methods

        public static void AddToRecentFiles(string filePath)
        {
            // if the file is already in the recent list, then move it to the top
            for (int i = 0; i < _settings.RecentFiles.Length; i++)
            {
                if (String.Equals(_settings.RecentFiles[i], filePath, StringComparison.OrdinalIgnoreCase))
                {
                    _settings.RecentFiles = new[] { filePath }.Concat(_settings.RecentFiles.Except(new[] { filePath })).ToArray();
                    Save();
                    return;
                }
            }

            // otherwise add it to the top
            _settings.RecentFiles = new[] { filePath }.Concat(_settings.RecentFiles).Take(4).ToArray();
            Save();
        }

        private static void Load()
        {
            try
            {
                string json = File.ReadAllText(_filePath);

                _settings = JsonSerializer.Deserialize<Settings>(json, _jsonSerializerOptions)!;
            }
            catch
            { }
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_filePath)!);

                string json = JsonSerializer.Serialize(_settings, _jsonSerializerOptions);
                File.WriteAllText(_filePath, json);
            }
            catch 
            { }
        }

        #endregion
    }
}
