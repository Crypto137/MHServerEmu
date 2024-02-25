using GameDatabaseBrowser.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GameDatabaseBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<PrototypeNode> Nodes { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();
            Nodes.Add(new PrototypeNode() { Name = "Prototypes" });
            txtDataRef.Text = "";
            txtParentDataRef.Text = "";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.RunWorkerCompleted += OnLoadingCompleted;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.DoWork += LoadPrototypes;
            backgroundWorker.ProgressChanged += OnProgress;
            backgroundWorker.RunWorkerAsync();
        }

        private void OnProgress(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            UpdateLayout();
        }

        private void LoadPrototypes(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            worker.ReportProgress(0);

            PakFileSystem.Instance.Initialize();

            bool isInitialized = GameDatabase.IsInitialized;
            DataDirectory dataDirectory = DataDirectory.Instance;

            int p = 0; // Max 93114

            foreach (PrototypeId prototypeId in GameDatabase.DataDirectory.IterateAllPrototypes())
            {
                AddPrototypeInHierarchy(prototypeId);

                worker.ReportProgress((int)(++p * (100 / 93114f)));
            }

            worker.ReportProgress(100);
        }

        private void AddPrototypeInHierarchy(PrototypeId prototypeId)
        {
            Prototype proto = GameDatabase.DataDirectory.GetPrototype<Prototype>(prototypeId);
            Type type = proto.GetType();
            PropertyInfo[] properties = type.GetProperties();

            string name = GameDatabase.GetPrototypeName(prototypeId);
            string[] tokens = name.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            ObservableCollection<PrototypeNode> pointer = Nodes[0].Childs;

            for (int i = 0; i < tokens.Length - 1; i++)
            {
                if (pointer.FirstOrDefault(k => k.Name == tokens[i]) == null)
                {
                    pointer.Add(new()
                    {
                        Name = tokens[i],
                        Properties = new()
                    });
                }

                pointer = pointer.First(k => k.Name == tokens[i]).Childs;
            }

            pointer.Add(new()
            {
                Name = tokens.Last(),
                Properties = properties
                    .Select(k => new Property() { Name = k.Name, Value = k.GetValue(proto)?.ToString() }).ToList()
            });
        }

        private void OnLoadingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Collapsed;

            treeView.Items.Add(Nodes);

            treeView.SelectedItemChanged += UpdatePropertiesSection;
            listView.MouseDoubleClick += OnPropertySelected;
            UpdateLayout();
        }

        private void OnPropertySelected(object sender, MouseButtonEventArgs e)
        {
            Property selected = ((FrameworkElement)e.OriginalSource).DataContext as Property;
            ulong.TryParse(selected.Value, out var prototypeId);
            if (prototypeId == 0)
                return;

            string name = GameDatabase.GetPrototypeName((PrototypeId)prototypeId);
            if (name == null) return;

            SelectFromName(name);
        }

        private void SelectFromName(string name)
        {
            int[] indexes = GetElementLocationInHierarchy(name);
            if (indexes == null) return;

            TreeViewItem item = GetTreeViewItem(indexes);
            ObservableCollection<PrototypeNode> p = (ObservableCollection<PrototypeNode>)treeView.Items[0];
            SelectChildNode(p.First(), name.Split('/').Last());
        }

        private int[] GetElementLocationInHierarchy(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            string[] tokens = name.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            ObservableCollection<PrototypeNode> pointer = Nodes[0].Childs;
            int[] indexes = new int[tokens.Length];

            for (int i = 0; i < tokens.Length; i++)
            {
                indexes[i] = pointer.Select((p, i) => new { Index = i, PrototypeNode = p }).FirstOrDefault(k => k.PrototypeNode.Name == tokens[i]).Index;
                if (i < tokens.Length - 1)
                    pointer = pointer[indexes[i]].Childs;
            }
            pointer[indexes.Last()].IsSelected = true;
            return indexes;
        }

        private void SelectChildNode(PrototypeNode parentItem, string childName)
        {
            foreach (var item in parentItem.Childs)
            {
                item.IsSelected = (item.Name == childName);
                item.IsExpanded = (item.Name == childName);

                if (item.Childs.Count != 0)
                    SelectChildNode(item, childName);
            }
        }

        private void UpdatePropertiesSection(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not PrototypeNode)
                return;

            PrototypeNode NodeSelected = (PrototypeNode)e.NewValue;
            string val = NodeSelected?.Properties?.FirstOrDefault(k => k.Name == "DataRef")?.Value;
            if (val == null)
                return;

            PrototypeId prototypeId = (PrototypeId)ulong.Parse(val);
            txtDataRef.Text = $"{GameDatabase.GetPrototypeName(prototypeId)} ({prototypeId})";
            string parent = NodeSelected?.Properties?.FirstOrDefault(k => k.Name == "ParentDataRef")?.Value;
            prototypeId = (PrototypeId)ulong.Parse(parent);
            txtParentDataRef.Text = $"Parent : {GameDatabase.GetPrototypeName(prototypeId)} ({prototypeId})";

            listView.ItemsSource = NodeSelected?.Properties?.Where(k => k.Name != "DataRef" && k.Name != "ParentDataRef" && k.Name != "DataRefRecord").OrderBy(k => k.Name);
        }

        private void ExpandOrCollapseTreeViewItem(ItemsControl container, bool needToExpand = false)
        {
            if (container is not TreeViewItem)
                return;
            container.SetValue(TreeViewItem.IsExpandedProperty, needToExpand);
        }

        private TreeViewItem BringIntoView(ItemsControl container, int i, bool needToExpand = false)
        {
            ExpandOrCollapseTreeViewItem(container, true);

            container.ApplyTemplate();
            ItemsPresenter itemsPresenter = (ItemsPresenter)container.Template.FindName("ItemsHost", container);
            if (itemsPresenter != null)
            {
                itemsPresenter.ApplyTemplate();
            }
            else
            {
                // The Tree template has not named the ItemsPresenter,
                // so walk the descendents and find the child.
                itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                if (itemsPresenter == null)
                {
                    container.UpdateLayout();

                    itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                }
            }

            Panel itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);

            // Ensure that the generator for this panel has been created.
            UIElementCollection children = itemsHostPanel.Children;

            MyVirtualizingStackPanel virtualizingPanel = itemsHostPanel as MyVirtualizingStackPanel;
            virtualizingPanel.BringIntoView(i);

            TreeViewItem childTreeView = (TreeViewItem)container.ItemContainerGenerator.ContainerFromIndex(i);
            ExpandOrCollapseTreeViewItem(childTreeView, needToExpand);
            return childTreeView;
        }

        private TreeViewItem GetTreeViewItem(int[] indexes)
        {
            ItemsControl container = treeView;
            TreeViewItem subContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromIndex(0);
            TreeViewItem nextContainer = null;
            foreach (var index in indexes)
            {
                for (int i = 0; i < subContainer.Items.Count; i++)
                {
                    if (i == index)
                        nextContainer = BringIntoView(subContainer, i, true);
                    else
                        BringIntoView(subContainer, i);
                }

                subContainer = nextContainer;
            }

            return null;
        }

        private T FindVisualChild<T>(Visual visual) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual child = (Visual)VisualTreeHelper.GetChild(visual, i);
                if (child != null)
                {
                    T correctlyTyped = child as T;
                    if (correctlyTyped != null)
                    {
                        return correctlyTyped;
                    }

                    T descendent = FindVisualChild<T>(child);
                    if (descendent != null)
                    {
                        return descendent;
                    }
                }
            }

            return null;
        }
    }

    public class MyVirtualizingStackPanel : VirtualizingStackPanel
    {
        public void BringIntoView(int index)
        {
            this.BringIndexIntoView(index);
        }
    }

}
