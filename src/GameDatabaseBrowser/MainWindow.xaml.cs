using GameDatabaseBrowser.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MHServerEmu.Games.Properties;
using PropertyInfo = System.Reflection.PropertyInfo;
using PropertyCollection = MHServerEmu.Games.Properties.PropertyCollection;

namespace GameDatabaseBrowser
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Prototype Model hierarchy for treeView
        /// </summary>
        public ObservableCollection<PrototypeNode> PrototypeNodes { get; set; } = new();
        public ObservableCollection<PropertyNode> PropertyNodes { get; set; } = new();

        /// <summary>
        /// All prototypes loaded
        /// </summary>
        private List<PrototypeDetails> _prototypeDetails = new();

        private const int PrototypeMaxNumber = 93114;

        private string _currentFilter = "";

        private bool isReady = false;

        /// <summary>
        /// Stack for history of prototypes' fullname selected
        /// </summary>
        private Stack<string> _fullNameHistory = new();

        public MainWindow()
        {
            InitializeComponent();
            treeView.SelectedItemChanged += UpdatePropertiesSection;
            propertytreeView.MouseDoubleClick += OnPropertyDoubleClicked;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundWorker backgroundWorker = new() { WorkerReportsProgress = true };
            backgroundWorker.DoWork += Init;
            backgroundWorker.ProgressChanged += OnProgress;
            backgroundWorker.RunWorkerCompleted += OnPrototypeTreeLoaded;
            backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Init the prototype treeView
        /// </summary>
        private void Init(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            worker.ReportProgress(0);

            PakFileSystem.Instance.Initialize();
            bool isInitialized = GameDatabase.IsInitialized;
            DataDirectory dataDirectory = DataDirectory.Instance;

            InitPrototypesList(worker, out int counter);

            PrototypeNodes.Add(new() { PrototypeDetails = new("Prototypes", new()) });
            PropertyNodes.Add(new() { PropertyDetails = new() { Name = "Data" } });
            foreach (PrototypeDetails prototype in _prototypeDetails)
                AddPrototypeInHierarchy(prototype);

            worker.ReportProgress(100);
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

                List<PropertyDetails> properties = propertyInfo.Select(k => new PropertyDetails() { Name = k.Name, Value = k.GetValue(proto)?.ToString(), TypeName = k.PropertyType.Name }).ToList();
                _prototypeDetails.Add(new(fullName, properties));
                worker.ReportProgress((int)(++counter * 100 / ((float)PrototypeMaxNumber)));
            }

            _prototypeDetails = _prototypeDetails.OrderBy(k => k.FullName).ToList();
        }

        /// <summary>
        /// Update the progress bar
        /// </summary>
        private void OnProgress(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            UpdateLayout();
        }

        /// <summary>
        /// Refresh the treeView content
        /// </summary>
        private void OnPrototypeTreeLoaded(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Collapsed;
            if (treeView.Items.Count == 0)
                treeView.Items.Add(PrototypeNodes);
            PrototypeNodes[0].IsExpanded = true;
            treeView.Items.Refresh();
            UpdateLayout();
            isReady = true;
        }

        /// <summary>
        /// Select an element from fullName
        /// </summary>
        private void SelectFromName(string fullName)
        {
            _currentFilter = "";
            txtSearch.Text = "";
            RefreshPrototypeTree();
            int[] indexes = GetElementLocationInHierarchy(fullName);
            SelectTreeViewItem(indexes);
        }

        private void RefreshPrototypeTree()
        {
            isReady = false;
            Dispatcher.Invoke(() =>
            {
                ConstructPrototypeTree();
                OnPrototypeTreeLoaded(null, null);
            });
        }

        /// <summary>
        /// Construct the prototype hierarchy
        /// </summary>
        private void ConstructPrototypeTree()
        {
            PrototypeNodes[0].Childs.Clear();
            List<PrototypeDetails> prototypeToDisplay = _prototypeDetails;
            if (!string.IsNullOrEmpty(_currentFilter))
                prototypeToDisplay = _prototypeDetails.Where(k => k.FullName.ToLowerInvariant().Contains(_currentFilter)).ToList();

            foreach (PrototypeDetails prototype in prototypeToDisplay)
                AddPrototypeInHierarchy(prototype);
        }

        private void AddPrototypeInHierarchy(PrototypeDetails prototype)
        {
            string[] tokens = prototype.FullName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            ObservableCollection<PrototypeNode> pointer = PrototypeNodes[0].Childs;

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

        /// <summary>
        /// Return to the previous state
        /// </summary>
        private void OnBackButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!isReady)
                return;

            if (_fullNameHistory.Count < 2)
                return;

            _fullNameHistory.Pop();
            SelectFromName(_fullNameHistory.Peek());
        }

        /// <summary>
        /// Launch the search by PrototypeId or by keyword
        /// </summary>
        private void OnSearchButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!isReady)
                return;

            ulong.TryParse(txtSearch.Text, out ulong prototypeId);
            if (prototypeId != 0)
                _currentFilter = GameDatabase.GetPrototypeName((PrototypeId)prototypeId).ToLowerInvariant();
            else
                _currentFilter = txtSearch.Text.ToLowerInvariant();

            RefreshPrototypeTree();
        }

        /// <summary>
        /// When double click on a property
        /// Allow to travel to a prototype when double clicking on a prototypeId
        /// </summary>
        private void OnPropertyDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (!isReady)
                return;

            PropertyNode selected = ((FrameworkElement)e.OriginalSource).DataContext as PropertyNode;
            if (selected?.PropertyDetails?.Value == null)
                return;

            ulong.TryParse(selected.PropertyDetails.Value, out var prototypeId);
            if (prototypeId == 0)
                return;

            string name = GameDatabase.GetPrototypeName((PrototypeId)prototypeId);
            if (string.IsNullOrEmpty(name)) return;

            SelectFromName(name);
        }

        /// <summary>
        /// Called when context menu "Copy value" selected
        /// </summary>
        private void OnClickCopyValueMenuItem(object sender, RoutedEventArgs e)
        {
            PropertyNode selected = ((FrameworkElement)e.OriginalSource).DataContext as PropertyNode;
            if (selected?.PropertyDetails?.Value == null)
                return;

            ulong.TryParse(selected.PropertyDetails.Value, out var prototypeId);
            if (prototypeId == 0)
                Clipboard.SetText(selected.PropertyDetails.Value);
            else
                Clipboard.SetText(prototypeId.ToString());
        }

        /// <summary>
        /// Called when context menu "Copy name" selected
        /// </summary>
        private void OnClickCopyNameMenuItem(object sender, RoutedEventArgs e)
        {
            PropertyNode selected = ((FrameworkElement)e.OriginalSource).DataContext as PropertyNode;
            if (selected?.PropertyDetails?.Name == null)
                return;

            Clipboard.SetText(selected.PropertyDetails.Name);
        }

        /// <summary>
        /// Return an array of index that indicate the path to access the treeViewItem selected
        /// </summary>
        private int[] GetElementLocationInHierarchy(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            string[] tokens = fullName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            ObservableCollection<PrototypeNode> pointer = PrototypeNodes[0].Childs;
            int[] indexes = new int[tokens.Length];

            for (int i = 0; i < tokens.Length; i++)
            {
                indexes[i] = pointer.Select((p, i) => new { Index = i, PrototypeNode = p }).FirstOrDefault(k => k.PrototypeNode.PrototypeDetails?.Name == tokens[i]).Index;
                if (i < tokens.Length - 1)
                    pointer = pointer[indexes[i]].Childs;
            }

            return indexes;
        }

        /// <summary>
        /// Recursively construction of the property tree
        /// </summary>
        private void ConstructPropertyNodeHierarchy(PropertyNode node, object property)
        {
            if (property == null)
                return;
            PropertyInfo[] propertyInfo = property.GetType().GetProperties().Where(k => k.Name != "DataRef" && k.Name != "ParentDataRef" && k.Name != "DataRefRecord").OrderBy(k => k.Name).ToArray();

            foreach (PropertyInfo propInfo in propertyInfo)
            {
                if (propInfo.GetValue(property) is PropertyCollection)
                    node.Childs.Add(new() { PropertyDetails = new() { Name = propInfo.Name, Value = "", TypeName = propInfo.PropertyType.Name } });
                else
                    node.Childs.Add(new() { PropertyDetails = new() { Name = propInfo.Name, Value = propInfo.GetValue(property)?.ToString(), TypeName = propInfo.PropertyType.Name } });

                if (typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType))
                {
                    IEnumerable subPropertyInfo = (IEnumerable)propInfo.GetValue(property);
                    if (subPropertyInfo == null)
                        continue;

                    int index = 0;
                    foreach (var subPropInfo in subPropertyInfo)
                    {
                        if (subPropertyInfo is Array)
                        {
                            node.Childs.Last().Childs.Add(new() { PropertyDetails = new() { Index = index++, Name = "", Value = subPropInfo.ToString(), TypeName = subPropInfo.GetType().Name } });
                            if (subPropInfo.GetType().IsPrimitive)
                                continue;

                            ConstructPropertyNodeHierarchy(node.Childs.Last().Childs.Last(), subPropInfo);
                        }
                        else if (subPropertyInfo is PropertyCollection)
                        {
                            KeyValuePair<PropertyId, PropertyValue> kvp = (KeyValuePair<PropertyId, PropertyValue>)subPropInfo;
                            MHServerEmu.Games.Properties.PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
                            string val = info.DataType == PropertyDataType.Prototype ? kvp.Value.ToPrototypeId().ToString() : kvp.Value.Print(info.DataType).ToString();
                            string typeName = info.DataType == PropertyDataType.Prototype ? "PrototypeId" : info.DataType.ToString();
                            node.Childs.Last().Childs.Add(new() { PropertyDetails = new() { Name = kvp.Key.ToString(), Value = val, TypeName = typeName } });
                            continue;
                        }
                        else
                            ConstructPropertyNodeHierarchy(node.Childs.Last(), subPropInfo);
                    }
                }
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
                if (_fullNameHistory.Count == 0 || _fullNameHistory.Peek() != prototypeFullName)
                    _fullNameHistory.Push(prototypeFullName);
            }
            else txtDataRef.Text = dataRef;

            string parentDataRef = NodeSelected?.PrototypeDetails?.Properties?.FirstOrDefault(k => k.Name == "ParentDataRef")?.Value;

            if (ulong.TryParse(parentDataRef, out ulong parentPrototypeId))
                txtParentDataRef.Text = $"Parent : {GameDatabase.GetPrototypeName((PrototypeId)parentPrototypeId)} ({parentPrototypeId})";
            else txtParentDataRef.Text = parentDataRef;

            Prototype proto = GameDatabase.DataDirectory.GetPrototype<Prototype>((PrototypeId)prototypeId);
            PropertyNodes[0].Childs.Clear();
            ConstructPropertyNodeHierarchy(PropertyNodes[0], proto);
            PropertyNodes[0].IsExpanded = true;

            if (propertytreeView.Items.Count == 0)
                propertytreeView.Items.Add(PropertyNodes);
        }

        /// <summary>
        /// Expand or Collapse the node
        /// </summary>
        private void ExpandOrCollapseTreeViewItem(ItemsControl container, bool needToExpand = false)
        {
            if (container is not TreeViewItem)
                return;
            container.SetValue(TreeViewItem.IsExpandedProperty, needToExpand);
        }

        /// <summary>
        /// Load the container to be able to retrieve the treeViewItem
        /// </summary>
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
                itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                if (itemsPresenter == null)
                {
                    container.UpdateLayout();

                    itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                }
            }

            Panel itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);

            UIElementCollection children = itemsHostPanel.Children;

            MyVirtualizingStackPanel virtualizingPanel = itemsHostPanel as MyVirtualizingStackPanel;
            virtualizingPanel.BringIntoView(i);

            TreeViewItem childTreeView = (TreeViewItem)container.ItemContainerGenerator.ContainerFromIndex(i);
            ExpandOrCollapseTreeViewItem(childTreeView, needToExpand);
            return childTreeView;
        }

        /// <summary>
        /// Return a treeViewItem based on the array of indexes of its location in the hierarchy
        /// </summary>
        private void SelectTreeViewItem(int[] indexes)
        {
            ItemsControl container = treeView;
            TreeViewItem subContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromIndex(0);
            foreach (var index in indexes)
            {
                int childIndex = 0;
                for (int i = 0; i < subContainer.Items.Count; i++)
                {
                    if (i == index)
                        childIndex = i;
                    else
                        BringIntoView(subContainer, i);
                }
                subContainer = BringIntoView(subContainer, childIndex, true);
            }
            subContainer.Focus();
            subContainer.IsSelected = true;
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
