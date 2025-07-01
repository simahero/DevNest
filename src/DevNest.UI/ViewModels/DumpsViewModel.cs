using DevNest.Core.Dump;
using DevNest.UI.Models;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Dispatching;

namespace DevNest.UI.ViewModels
{
    public partial class DumpsViewModel : BaseViewModel
    {
        private readonly VarDumperServer _varDumperServer;
        private readonly DispatcherQueue _dispatcherQueue;
        private ObservableCollection<TreeNodeModel> _treeNodes = new();

        public DumpsViewModel(VarDumperServer varDumperServer)
        {
            _varDumperServer = varDumperServer;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            Title = "VarDumper Output";

            // Subscribe to collection changes
            _varDumperServer.Dumps.CollectionChanged += Dumps_CollectionChanged;

            // Populate existing items
            PopulateTreeNodes();
        }

        public ObservableCollection<object> Dumps => _varDumperServer.Dumps;

        public ObservableCollection<TreeNodeModel> TreeNodes
        {
            get => _treeNodes;
            set => SetProperty(ref _treeNodes, value);
        }

        private void Dumps_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Ensure this runs on the UI thread
            _dispatcherQueue.TryEnqueue(() => PopulateTreeNodes());
        }

        private void PopulateTreeNodes()
        {
            TreeNodes.Clear();

            for (int i = 0; i < Dumps.Count; i++)
            {
                var dump = Dumps[i];
                var rootNode = new TreeNodeModel()
                {
                    Content = $"Dump #{i + 1}",
                    IsExpanded = true
                };

                PopulateNode(rootNode, dump);
                TreeNodes.Add(rootNode);
            }
        }

        private void PopulateNode(TreeNodeModel node, object? obj)
        {
            if (obj == null)
            {
                node.Children.Add(new TreeNodeModel { Content = "null" });
                return;
            }

            if (obj is IDictionary dict)
            {
                foreach (DictionaryEntry entry in dict)
                {
                    var childNode = new TreeNodeModel()
                    {
                        Content = $"{entry.Key}: {GetValuePreview(entry.Value)}",
                        IsExpanded = false
                    };

                    if (IsComplexObject(entry.Value))
                    {
                        PopulateNode(childNode, entry.Value);
                    }

                    node.Children.Add(childNode);
                }
            }
            else if (obj is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var childNode = new TreeNodeModel()
                    {
                        Content = $"[{i}]: {GetValuePreview(list[i])}",
                        IsExpanded = false
                    };

                    if (IsComplexObject(list[i]))
                    {
                        PopulateNode(childNode, list[i]);
                    }

                    node.Children.Add(childNode);
                }
            }
            else if (obj.GetType().IsClass && obj.GetType() != typeof(string))
            {
                // Handle object properties using reflection
                var properties = obj.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    try
                    {
                        var value = prop.GetValue(obj);
                        var childNode = new TreeNodeModel()
                        {
                            Content = $"{prop.Name}: {GetValuePreview(value)}",
                            IsExpanded = false
                        };

                        if (IsComplexObject(value))
                        {
                            PopulateNode(childNode, value);
                        }

                        node.Children.Add(childNode);
                    }
                    catch
                    {
                        // Skip properties that can't be accessed
                    }
                }
            }
            else
            {
                node.Children.Add(new TreeNodeModel { Content = obj.ToString() ?? "" });
            }
        }

        private string GetValuePreview(object? value)
        {
            if (value == null) return "null";

            if (value is string str)
                return $"\"{str}\"";

            if (value is bool boolean)
                return boolean.ToString().ToLower();

            if (value is IDictionary dict)
                return $"Dictionary ({dict.Count} items)";

            if (value is IList list)
                return $"Array ({list.Count} items)";

            if (value.GetType().IsClass && value.GetType() != typeof(string))
                return $"Object ({value.GetType().Name})";

            return value.ToString() ?? "";
        }

        private bool IsComplexObject(object? value)
        {
            if (value == null) return false;

            return value is IDictionary ||
                   value is IList ||
                   (value.GetType().IsClass && value.GetType() != typeof(string));
        }
    }
}
