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
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Model hierarchy for treeView
        /// </summary>
        public ObservableCollection<PrototypeNode> Nodes { get; set; } = new();

        /// <summary>
        /// All prototypes loaded
        /// </summary>
        private List<PrototypeDetails> _prototypeDetails = new();

        private const int PrototypeMaxNumber = 93114;

        /// <summary>
        /// Stack for history of prototypes' fullname selected
        /// </summary>
        private Stack<string> _fullNameHistory = new();

        public MainWindow()
        {
            InitializeComponent();
            Nodes.Add(new() { PrototypeDetails = new("Prototypes", new()) });
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundWorker backgroundWorker = new();
            backgroundWorker.RunWorkerCompleted += OnLoadingCompleted;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.DoWork += LoadPrototypes;
            backgroundWorker.ProgressChanged += OnProgress;
            backgroundWorker.RunWorkerAsync();
        }

        private void OnBackButtonClicked(object sender, RoutedEventArgs e)
        {
            if (_fullNameHistory.Count < 2)
                return;
            _fullNameHistory.Pop();
            SelectFromName(_fullNameHistory.Peek());
        }

        private void OnProgress(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            UpdateLayout();
        }

        /// <summary>
        /// Create a list with all the prototypes and their properties
        /// </summary>
        private void InitPrototypesList(BackgroundWorker worker, out int counter)
        {
            counter = 0;
            foreach (PrototypeId prototypeId in GameDatabase.DataDirectory.IterateAllPrototypes())
            {
                string fullName = GameDatabase.GetPrototypeName(prototypeId);
                Prototype proto = GameDatabase.DataDirectory.GetPrototype<Prototype>(prototypeId);
                PropertyInfo[] propertyInfo = proto.GetType().GetProperties();

                List<Property> properties = propertyInfo.Select(k => new Property() { Name = k.Name, Value = k.GetValue(proto)?.ToString() }).ToList();
                _prototypeDetails.Add(new(fullName, properties));
                worker.ReportProgress((int)(++counter * 100 / ((float)PrototypeMaxNumber)));
            }

            _prototypeDetails = _prototypeDetails.OrderBy(k => k.FullName).ToList();
        }

        private void LoadPrototypes(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            worker.ReportProgress(0);

            PakFileSystem.Instance.Initialize();
            bool isInitialized = GameDatabase.IsInitialized;
            DataDirectory dataDirectory = DataDirectory.Instance;

            InitPrototypesList(worker, out int counter);

            foreach (PrototypeDetails prototype in _prototypeDetails)
                AddPrototypeInHierarchy(prototype);

            worker.ReportProgress(100);
        }

        private void AddPrototypeInHierarchy(PrototypeDetails prototype)
        {
            string[] tokens = prototype.FullName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            ObservableCollection<PrototypeNode> pointer = Nodes[0].Childs;

            string currentFullName = tokens.First();
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                if (pointer.FirstOrDefault(k => k.PrototypeDetails.Name == tokens[i]) == null)
                    pointer.Add(new() { PrototypeDetails = new(currentFullName, new()) });

                pointer = pointer.First(k => k.PrototypeDetails.Name == tokens[i]).Childs;
                currentFullName += $"/{tokens[i + 1]}";
            }

            pointer.Add(new() { PrototypeDetails = prototype });
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
                indexes[i] = pointer.Select((p, i) => new { Index = i, PrototypeNode = p }).FirstOrDefault(k => k.PrototypeNode.PrototypeDetails?.Name == tokens[i]).Index;
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
                item.IsSelected = (item.PrototypeDetails.Name == childName);
                item.IsExpanded = (item.PrototypeDetails.Name == childName);

                if (item.Childs.Count != 0)
                    SelectChildNode(item, childName);
            }
        }

        /// <summary>
        /// Update the properties based on the current selection
        /// </summary>
        private void UpdatePropertiesSection(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not PrototypeNode)
                return;

            PrototypeNode NodeSelected = (PrototypeNode)e.NewValue;
            string dataRef = NodeSelected?.PrototypeDetails?.Properties?.FirstOrDefault(k => k.Name == "DataRef")?.Value;
            if (dataRef == null)
                return;

            if (ulong.TryParse(dataRef, out ulong prototypeId))
            {
                string prototypeFullName = GameDatabase.GetPrototypeName((PrototypeId)prototypeId);
                txtDataRef.Text = $"{prototypeFullName} ({prototypeId})";
                _fullNameHistory.Push(prototypeFullName);
            }
            else txtDataRef.Text = dataRef;

            string parentDataRef = NodeSelected?.PrototypeDetails?.Properties?.FirstOrDefault(k => k.Name == "ParentDataRef")?.Value;

            if (ulong.TryParse(parentDataRef, out prototypeId))
                txtParentDataRef.Text = $"Parent : {GameDatabase.GetPrototypeName((PrototypeId)prototypeId)} ({prototypeId})";
            else txtParentDataRef.Text = parentDataRef;

            listView.ItemsSource = NodeSelected?.PrototypeDetails?.Properties?.Where(k => k.Name != "DataRef" && k.Name != "ParentDataRef" && k.Name != "DataRefRecord").OrderBy(k => k.Name);
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
