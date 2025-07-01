using DevNest.Core.Dump;
using DevNest.UI.Models;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;

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

            _varDumperServer.Dumps.CollectionChanged += Dumps_CollectionChanged;

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
                node.Content += " = null";
                return;
            }

            if (obj is JsonElement jsonElement)
            {
                PopulateJsonElement(node, jsonElement);
            }
            else
            {
                node.Content += $" = {obj}";
            }
        }

        private void PopulateJsonElement(TreeNodeModel parentNode, JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    PopulateJsonObject(parentNode, element);
                    break;

                case JsonValueKind.Array:
                    PopulateJsonArray(parentNode, element);
                    break;

                case JsonValueKind.String:
                    parentNode.Content = $"\"{element.GetString()}\" (string)";
                    break;

                case JsonValueKind.Number:
                    if (element.GetRawText().Contains('.'))
                        parentNode.Content = $"{element.GetRawText()} (float)";
                    else
                        parentNode.Content = $"{element.GetRawText()} (integer)";
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    parentNode.Content = $"{element.GetBoolean().ToString().ToLower()} (boolean)";
                    break;

                case JsonValueKind.Null:
                    parentNode.Content = "null (null)";
                    break;

                default:
                    parentNode.Content = $"{element.GetRawText()} (unknown)";
                    break;
            }
        }

        private void PopulateJsonObject(TreeNodeModel parentNode, JsonElement obj)
        {
            if (obj.TryGetProperty("__type", out var typeProperty))
            {
                var type = typeProperty.GetString();
                parentNode.Content += $" ({type})";

                if (obj.TryGetProperty("__class", out var classProperty))
                {
                    parentNode.Content += $" {classProperty.GetString()}";
                }

                if (obj.TryGetProperty("__value", out var valueProperty))
                {
                    if (valueProperty.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in valueProperty.EnumerateObject())
                        {
                            var childNode = new TreeNodeModel
                            {
                                Content = prop.Name,
                                IsExpanded = false
                            };

                            if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                PopulateJsonElement(childNode, prop.Value);
                            }
                            else
                            {
                                // Store the property name and set the content to just the value
                                var propName = childNode.Content;
                                PopulateJsonElement(childNode, prop.Value);
                                childNode.Content = $"{propName} = {childNode.Content}";
                            }

                            parentNode.Children.Add(childNode);
                        }
                    }
                    else
                    {
                        var valueNode = new TreeNodeModel
                        {
                            Content = "value",
                            IsExpanded = false
                        };

                        if (valueProperty.ValueKind == JsonValueKind.Object || valueProperty.ValueKind == JsonValueKind.Array)
                        {
                            PopulateJsonElement(valueNode, valueProperty);
                        }
                        else
                        {
                            var propName = valueNode.Content;
                            PopulateJsonElement(valueNode, valueProperty);
                            valueNode.Content = $"{propName} = {valueNode.Content}";
                        }

                        parentNode.Children.Add(valueNode);
                    }
                }

                if (obj.TryGetProperty("__resourceType", out var resourceTypeProperty))
                {
                    var resourceNode = new TreeNodeModel
                    {
                        Content = $"resourceType = \"{resourceTypeProperty.GetString()}\"",
                        IsExpanded = false
                    };
                    parentNode.Children.Add(resourceNode);
                }
            }
            else
            {
                parentNode.Content += " (object)";

                foreach (var prop in obj.EnumerateObject())
                {
                    var childNode = new TreeNodeModel
                    {
                        Content = prop.Name,
                        IsExpanded = false
                    };

                    if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        PopulateJsonElement(childNode, prop.Value);
                    }
                    else
                    {
                        var propName = childNode.Content;
                        PopulateJsonElement(childNode, prop.Value);
                        childNode.Content = $"{propName} = {childNode.Content}";
                    }

                    parentNode.Children.Add(childNode);
                }
            }
        }

        private void PopulateJsonArray(TreeNodeModel parentNode, JsonElement array)
        {
            parentNode.Content += $" (array[{array.GetArrayLength()}])";

            int index = 0;
            foreach (var item in array.EnumerateArray())
            {
                var childNode = new TreeNodeModel
                {
                    Content = $"[{index}]",
                    IsExpanded = false
                };
                PopulateJsonElement(childNode, item);
                parentNode.Children.Add(childNode);
                index++;
            }
        }
    }
}
