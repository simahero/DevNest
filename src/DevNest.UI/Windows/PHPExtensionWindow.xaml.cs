using DevNest.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DevNest.UI.Windows
{
    public class PhpExtensionViewModel : INotifyPropertyChanged
    {
        private bool _enabled;

        public string Name { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string OriginalLine { get; set; } = string.Empty;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasComment => !string.IsNullOrEmpty(Comment);
        public Visibility CommentVisibility => HasComment ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed partial class PHPExtensionWindow : Window
    {
        private string iniFilePath;
        private List<string> originalLines;
        private ObservableCollection<PhpExtensionViewModel> extensions;

        public PHPExtensionWindow(string phpIniPath)
        {
            this.InitializeComponent();

            this.iniFilePath = phpIniPath;
            this.extensions = new ObservableCollection<PhpExtensionViewModel>();
            this.originalLines = File.ReadAllLines(phpIniPath).ToList();

            LoadExtensions();

            // Set the window to use custom title bar
            try
            {
                this.ExtendsContentIntoTitleBar = true;
            }
            catch
            {
                // Title bar setup might fail in some cases, continue without it
            }
        }

        private void LoadExtensions()
        {
            extensions.Clear();

            foreach (var line in originalLines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("extension=") || trimmed.StartsWith(";extension="))
                {
                    var isEnabled = trimmed.StartsWith("extension=");
                    var extensionPart = isEnabled ? trimmed.Substring("extension=".Length) : trimmed.Substring(";extension=".Length);

                    // Split by semicolon to separate extension name from comment
                    var parts = extensionPart.Split(';', 2);
                    var extensionName = parts[0].Trim();
                    var comment = parts.Length > 1 ? parts[1].Trim() : string.Empty;

                    // Skip if extension name is empty
                    if (string.IsNullOrEmpty(extensionName))
                        continue;

                    // Check if this extension already exists (in case of duplicates)
                    var existing = extensions.FirstOrDefault(e => e.Name.Equals(extensionName, System.StringComparison.OrdinalIgnoreCase));
                    if (existing == null)
                    {
                        extensions.Add(new PhpExtensionViewModel
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveExtensions();
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveExtensions()
        {
            var newLines = new List<string>();

            foreach (var line in originalLines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("extension=") || trimmed.StartsWith(";extension="))
                {
                    var isEnabled = trimmed.StartsWith("extension=");
                    var extensionPart = isEnabled ? trimmed.Substring("extension=".Length) : trimmed.Substring(";extension=".Length);

                    // Split by semicolon to separate extension name from comment
                    var parts = extensionPart.Split(';', 2);
                    var extensionName = parts[0].Trim();
                    var comment = parts.Length > 1 ? ";" + parts[1] : string.Empty;

                    // Find the corresponding extension in our collection
                    var ext = extensions.FirstOrDefault(e => e.Name.Equals(extensionName, System.StringComparison.OrdinalIgnoreCase));

                    if (ext != null)
                    {
                        // Reconstruct the line with the current enabled state and preserve comments
                        var prefix = ext.Enabled ? "extension=" : ";extension=";
                        var newLine = prefix + ext.Name + comment;
                        newLines.Add(newLine);
                    }
                    else
                    {
                        // Keep the original line if we don't have it in our collection
                        newLines.Add(line);
                    }
                }
                else
                {
                    // Keep non-extension lines as-is
                    newLines.Add(line);
                }
            }

            File.WriteAllLines(iniFilePath, newLines);
        }
    }
}
