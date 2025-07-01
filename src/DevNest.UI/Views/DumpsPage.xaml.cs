using Microsoft.UI.Xaml.Controls;
using DevNest.UI.ViewModels;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;

namespace DevNest.UI.Views;

public sealed partial class DumpsPage : Page
{
    public DumpsViewModel? ViewModel { get; set; }

    public DumpsPage()
    {
        this.InitializeComponent();
    }

    public void SetViewModel(DumpsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        // Subscribe to TreeNodes collection changes
        ViewModel.TreeNodes.CollectionChanged += TreeNodes_CollectionChanged;

        // Populate existing items
        PopulateTreeView();
    }

    private void TreeNodes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PopulateTreeView();
    }

    private void PopulateTreeView()
    {
        var treeView = this.FindName("DumpsTreeView") as TreeView;
        if (treeView == null) return;

        treeView.RootNodes.Clear();

        if (ViewModel?.TreeNodes == null) return;

        foreach (var treeNode in ViewModel.TreeNodes)
        {
            var rootNode = CreateTreeViewNode(treeNode);
            treeView.RootNodes.Add(rootNode);
        }
    }

    private TreeViewNode CreateTreeViewNode(DevNest.UI.Models.TreeNodeModel model)
    {
        var node = new TreeViewNode()
        {
            Content = model.Content,
            IsExpanded = model.IsExpanded
        };

        foreach (var child in model.Children)
        {
            node.Children.Add(CreateTreeViewNode(child));
        }

        return node;
    }
}