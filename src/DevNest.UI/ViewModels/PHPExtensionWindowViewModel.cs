using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevNest.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevNest.Core.Models;

namespace DevNest.UI.ViewModels
{
    public partial class PHPExtensionWindowViewModel : BaseViewModel
    {
        private readonly string _iniFilePath;
        private List<string> _originalLines;

        public ObservableCollection<PhpExtension> Extensions { get; } = new();

        public event EventHandler? CloseRequested;

        public PHPExtensionWindowViewModel(string phpIniPath)
        {
            _iniFilePath = phpIniPath;
            _originalLines = new List<string>();
            Title = "PHP Extensions";
        }

        protected override async Task OnLoadedAsync()
        {
            await LoadExtensionsAsync();
        }

        [RelayCommand]
        private Task LoadExtensionsAsync()
        {
            try
            {
                if (!File.Exists(_iniFilePath))
                    return Task.CompletedTask;

                _originalLines = File.ReadAllLines(_iniFilePath).ToList();
                Extensions.Clear();

                foreach (var line in _originalLines)
                {
                    var trimmed = line.Trim();

                    if (trimmed.StartsWith("extension=") || trimmed.StartsWith(";extension="))
                    {
                        var isEnabled = trimmed.StartsWith("extension=");
                        var extensionPart = isEnabled ? trimmed.Substring("extension=".Length) : trimmed.Substring(";extension=".Length);

                        var parts = extensionPart.Split(';', 2);
                        var extensionName = parts[0].Trim();
                        var comment = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                        if (string.IsNullOrEmpty(extensionName))
                            continue;

                        var existing = Extensions.FirstOrDefault(e => e.Name.Equals(extensionName, StringComparison.OrdinalIgnoreCase));
                        if (existing == null)
                        {
                            Extensions.Add(new PhpExtension
                            {
                                Name = extensionName,
                                Enabled = isEnabled,
                                Comment = comment,
                                OriginalLine = line
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading PHP extensions: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task SaveExtensionsAsync()
        {
            try
            {
                var newLines = new List<string>();

                foreach (var line in _originalLines)
                {
                    var trimmed = line.Trim();

                    if (trimmed.StartsWith("extension=") || trimmed.StartsWith(";extension="))
                    {
                        var isEnabled = trimmed.StartsWith("extension=");
                        var extensionPart = isEnabled ? trimmed.Substring("extension=".Length) : trimmed.Substring(";extension=".Length);

                        var parts = extensionPart.Split(';', 2);
                        var extensionName = parts[0].Trim();
                        var comment = parts.Length > 1 ? ";" + parts[1] : string.Empty;

                        var ext = Extensions.FirstOrDefault(e => e.Name.Equals(extensionName, StringComparison.OrdinalIgnoreCase));

                        if (ext != null)
                        {
                            var prefix = ext.Enabled ? "extension=" : ";extension=";
                            var newLine = prefix + ext.Name + comment;
                            newLines.Add(newLine);
                        }
                        else
                        {
                            newLines.Add(line);
                        }
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }

                await File.WriteAllLinesAsync(_iniFilePath, newLines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving PHP extensions: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private async Task SaveAndClose()
        {
            await SaveExtensionsAsync();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
