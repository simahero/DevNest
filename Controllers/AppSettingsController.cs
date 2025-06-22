using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DevNest.Controllers
{

    public class AppSettingsController : INotifyPropertyChanged
    {
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToSystemTray { get; set; } = false;
        public bool AutoVirtualHosts { get; set; } = false;
        public bool AutoCreateDatabase { get; set; } = false;
        public string InstallDirectory { get; set; } = @"C:\DevNest";
        public ObservableCollection<CategoryVersion> Versions { get; set; } = new ObservableCollection<CategoryVersion>();

        private static readonly string SettingsFilePath = @"C:\DevNest\settings.json";
        private static readonly string DevNestBinPath = @"C:\DevNest\bin";
        private static AppSettingsController? _instance;

        public AppSettingsController()
        {
            LoadCategories();
        }

        public static AppSettingsController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }
        public static AppSettingsController Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath); var settings = JsonSerializer.Deserialize<AppSettingsController>(json);
                    if (settings != null)
                    {
                        // Initialize the Versions collection if it's null (due to deserialization)
                        if (settings.Versions == null)
                        {
                            settings.Versions = new ObservableCollection<CategoryVersion>();
                        }
                        else
                        {
                            // Re-attach PropertyChanged event handlers after deserialization
                            foreach (var categoryVersion in settings.Versions)
                            {
                                categoryVersion.PropertyChanged += settings.CategoryVersion_PropertyChanged;
                            }
                        }
                        settings.LoadCategories();
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }

            return new AppSettingsController();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
        public void LoadCategories()
        {
            bool needsSave = false;
            var savedVersions = new Dictionary<string, string>();

            // Preserve existing version selections if any
            if (Versions != null)
            {
                savedVersions = Versions.ToDictionary(v => v.Name, v => v.SelectedVersion);
                // Clear existing event handlers to avoid duplicates
                foreach (var version in Versions)
                {
                    version.PropertyChanged -= CategoryVersion_PropertyChanged;
                }
            }

            // Initialize or clear the Versions collection
            if (Versions == null)
            {
                Versions = new ObservableCollection<CategoryVersion>();
            }
            else
            {
                Versions.Clear();
            }

            if (!Directory.Exists(DevNestBinPath))
            {
                Directory.CreateDirectory(DevNestBinPath);
                return;
            }

            var categoryDirectories = Directory.GetDirectories(DevNestBinPath)
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .OrderBy(name => name);

            foreach (var categoryName in categoryDirectories)
            {
                if (string.IsNullOrEmpty(categoryName)) continue;

                var categoryPath = Path.Combine(DevNestBinPath, categoryName);
                var versionDirectories = Directory.GetDirectories(categoryPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .OrderBy(name => name)
                    .ToList();

                if (versionDirectories.Any())
                {
                    var categoryVersion = new CategoryVersion
                    {
                        Name = categoryName
                    };

                    foreach (var version in versionDirectories)
                    {
                        if (!string.IsNullOrEmpty(version))
                        {
                            categoryVersion.AvailableVersions.Add(version);
                        }
                    }

                    // Set selected version from saved settings or first available version
                    if (savedVersions.TryGetValue(categoryName, out var savedVersion) &&
                        categoryVersion.AvailableVersions.Contains(savedVersion))
                    {
                        categoryVersion.SelectedVersion = savedVersion;
                    }
                    else if (categoryVersion.AvailableVersions.Any())
                    {
                        categoryVersion.SelectedVersion = categoryVersion.AvailableVersions.First();
                        needsSave = true;
                    }

                    categoryVersion.PropertyChanged += CategoryVersion_PropertyChanged;

                    Versions.Add(categoryVersion);
                }
            }
            if (needsSave)
            {
                Save();
            }
        }

        private void CategoryVersion_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CategoryVersion.SelectedVersion) && sender is CategoryVersion categoryVersion)
            {
                Save();
            }
        }
        public string GetSelectedVersionPath(string category)
        {
            var categoryVersion = Versions.FirstOrDefault(c => c.Name == category);
            if (categoryVersion != null && !string.IsNullOrEmpty(categoryVersion.SelectedVersion))
            {
                return Path.Combine(DevNestBinPath, category, categoryVersion.SelectedVersion);
            }
            return string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class CategoryVersion : INotifyPropertyChanged
    {
        private string _selectedVersion = string.Empty;

        public required string Name { get; set; }
        public ObservableCollection<string> AvailableVersions { get; set; } = new();

        public string SelectedVersion
        {
            get => _selectedVersion;
            set
            {
                if (_selectedVersion != value)
                {
                    _selectedVersion = value;
                    OnPropertyChanged(nameof(SelectedVersion));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
