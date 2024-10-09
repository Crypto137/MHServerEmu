using GameDatabaseBrowser.Models;
using GameDatabaseBrowser.Providers;
using GameDatabaseBrowser.Search;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Locales;
using MHServerEmu.Games.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PropertyCollection = MHServerEmu.Games.Properties.PropertyCollection;
using PropertyInfo = System.Reflection.PropertyInfo;

namespace GameDatabaseBrowser
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Prototype Model hierarchy for treeView
        /// </summary>
        public ObservableCollection<PrototypeNode> PrototypeNodes { get; set; } = new();

        /// <summary>
        /// Property Model hierarchy for treeView
        /// </summary>
        public ObservableCollection<PropertyNode> PropertyNodes { get; set; } = new();

        /// <summary>
        /// All prototypes loaded
        /// </summary>
        public List<PrototypeDetails> PrototypeDetails = new();

        /// <summary>
        /// Max prototype to load on init
        /// </summary>
        private const int PrototypeMaxNumber = 93114;

        /// <summary>
        /// Used as a semaphore to avoid action when we refresh the tree
        /// </summary>
        private bool isReady = false;

        /// <summary>
        /// Stack for history of prototypes' fullname selected used for Back action
        /// </summary>
        private Stack<string> _fullNameBackHistory = new();

        /// <summary>
        /// Stack for history of prototypes' fullname selected used for Forward action
        /// </summary>
        private Stack<string> _fullNameForwardHistory = new();

        /// <summary>
        /// Dictionary that contains all the prototype references. Used as cache to speed up the search
        /// </summary>
        public Dictionary<PrototypeId, List<PrototypeId>> CacheDictionary = new();

        private SearchHelper _searchHelper;

        public MainWindow()
        {
            InitializeComponent();
            SearchTypeComboBox.SelectedIndex = 0;
            treeView.SelectedItemChanged += UpdatePropertiesSection;
            propertytreeView.MouseDoubleClick += OnPropertyDoubleClicked;
        }

        /// <summary>
        /// Called when the main window is loaded
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundWorker backgroundWorker = new() { WorkerReportsProgress = true };
            backgroundWorker.DoWork += Init;
            backgroundWorker.ProgressChanged += OnProgress;
            backgroundWorker.RunWorkerCompleted += OnPrototypeTreeLoaded;
            backgroundWorker.RunWorkerAsync();
        }

        private void CleanPrototypeTree()
        {
            Dispatcher.Invoke(() =>
            {
                PrototypeNodes[0].Childs.Clear();
            });
        }

        /// <summary>
        /// Init the prototype treeView
        /// </summary>
        private void Init(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            worker.ReportProgress(0);

            if (!PakFileSystem.Instance.Initialize())
            {
                Dispatcher.Invoke(() =>
                {
                    NoSipMessage.Visibility = Visibility.Visible;
                });
                return;
            }

            bool isInitialized = GameDatabase.IsInitialized;
            DataDirectory dataDirectory = DataDirectory.Instance;

            int counter = 0;

            var currentLocale = LocaleManager.Instance.CurrentLocale;
            string[] filePaths = new string[]
            {
                "eng.all_FFFFFFFFFFFFFFFF.string",
                "eng.all_BFFFFFFFFFFFFFFF.string",
                "eng.all_7FFFFFFFFFFFFFFF.string",
                "eng.all_3FFFFFFFFFFFFFFF.string"
            };

            foreach (var filePath in filePaths)
            {
                currentLocale.LoadStringFile("Data\\Game\\Loco\\" + filePath);
                worker.ReportProgress(++counter * 100 / 4);
            }

            InitPrototypesList(worker, ref counter);
            GeneratePrototypeReferencesCache(worker, ref counter);


            PrototypeNodes.Add(new() { PrototypeDetails = new("Prototypes", new()) });
            PropertyNodes.Add(new() { PropertyDetails = new() { Name = "Data"}});

            RefreshPrototypeTree(GetSearchDetails());
            worker.ReportProgress(100);
        }

        private SearchDetails GetSearchDetails()
        {
            SearchDetails searchDetails = null;
            Dispatcher.Invoke(() =>
            {
                searchDetails = new SearchDetails(this);
            });
            return searchDetails;
        }

        /// <summary>
        /// Create a list with all the prototypes and their properties
        /// </summary>
        private void InitPrototypesList(BackgroundWorker worker, ref int counter)
        {

            foreach (PrototypeId prototypeId in GameDatabase.DataDirectory.IterateAllPrototypes())
            {
                string fullName = GameDatabase.GetPrototypeName(prototypeId);
                Prototype proto = GameDatabase.DataDirectory.GetPrototype<Prototype>(prototypeId);
                PropertyInfo[] propertyInfo = proto.GetType().GetProperties();

                List<PropertyDetails> properties = propertyInfo.Select(k => new PropertyDetails() { Name = k.Name, Value = k.GetValue(proto)?.ToString(), TypeName = k.PropertyType.Name }).ToList();
                PrototypeDetails.Add(new(fullName, properties));
                worker.ReportProgress((int)(++counter * 100 / ((float)PrototypeMaxNumber * 2)));
            }

            PrototypeDetails = PrototypeDetails.OrderBy(k => k.FullName).ToList();
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
            if (NoSipMessage.Visibility == Visibility.Visible)
                return;

            Dispatcher.Invoke(() =>
            {
                progressBar.Visibility = Visibility.Collapsed;

                if (treeView.Items.Count == 0)
                    treeView.Items.Add(PrototypeNodes);
                PrototypeNodes[0].IsExpanded = true;
                treeView.Items.Refresh();

                classAutoCompletionText.Provider = new PrototypeClassProvider();
                blueprintAutoCompletionText.Provider = new PrototypeBlueprintProvider();
                UpdateLayout();
                isReady = true;
            });
        }

        private bool NeedRefresh()
        {
            bool check = false;
            Dispatcher.Invoke(() =>
            {
                check = PrototypeNodes[0].Childs.Count != 34;
                if (check == false)
                    progressBar.Visibility = Visibility.Collapsed;
                return check;
            });
            return check;
        }

        /// <summary>
        /// Select an element from fullName
        /// </summary>
        private void SelectFromName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return;

            if (NeedRefresh()) RefreshPrototypeTree(GetSearchDetails());
            int[] indexes = GetElementLocationInHierarchy(fullName);
            SelectTreeViewItem(indexes);
        }

        /// <summary>
        /// Reload the prototype tree
        /// </summary>
        private void RefreshPrototypeTree(SearchDetails searchDetails, bool considerSearch = false)
        {
            isReady = false;
            ConstructPrototypeTree(searchDetails, considerSearch);
            OnPrototypeTreeLoaded(null, null);
        }

        /// <summary>
        /// Construct the prototype hierarchy
        /// </summary>
        private void ConstructPrototypeTree(SearchDetails searchDetails, bool considerSearch = false)
        {
            if (_searchHelper == null)
                _searchHelper = new(this);

            CleanPrototypeTree();
            List<PrototypeDetails> prototypeToDisplay = considerSearch ? _searchHelper.GetPrototypeToDisplay(searchDetails) : PrototypeDetails;

            if (prototypeToDisplay == null)
                return;

            PrototypeNodes[0].PrototypeDetails.Name = $"Prototypes [{prototypeToDisplay.Count}]";
            bool needExpand = searchDetails.NeedExpand || prototypeToDisplay.Count < 21;
            foreach (PrototypeDetails prototype in prototypeToDisplay)
                AddPrototypeInHierarchy(prototype, needExpand);
        }

        /// <summary>
        /// Add a prototype in the tree
        /// </summary>
        private void AddPrototypeInHierarchy(PrototypeDetails prototype, bool needExpand)
        {
            Dispatcher.Invoke(() =>
            {
                string[] tokens = prototype.FullName.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                ObservableCollection<PrototypeNode> pointer = PrototypeNodes[0].Childs;
                string currentFullName = tokens.First();
                for (int i = 0; i < tokens.Length - 1; i++)
                {
                    if (pointer.FirstOrDefault(k => k.PrototypeDetails.Name == tokens[i]) == null)
                        pointer.Add(new() { PrototypeDetails = new(currentFullName, new()), IsExpanded = needExpand });

                    pointer = pointer.First(k => k.PrototypeDetails.Name == tokens[i]).Childs;
                    currentFullName += $"/{tokens[i + 1]}";
                }

                pointer.Add(new() { PrototypeDetails = prototype, IsExpanded = needExpand });
            });
        }

        /// <summary>
        /// Return to the previous state
        /// </summary>
        private async void OnBackButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!isReady)
                return;

            if (_fullNameBackHistory.Count < 2)
                return;

            string fullname = _fullNameBackHistory.Pop();
            _fullNameForwardHistory.Push(fullname);

            try
            {
                progressBar.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true;
                await Task.Run(() => SelectFromName(_fullNameBackHistory.Peek()));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Return to the next state (basically it cancels a back action)
        /// </summary>
        private async void OnForwardButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!isReady)
                return;

            if (_fullNameForwardHistory.Count < 1)
                return;

            try
            {
                progressBar.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true;
                await Task.Run(() => SelectFromName(_fullNameForwardHistory.Pop()));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// When a touch is pressed
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OnSearchButtonClicked(sender, e);
        }

        /// <summary>
        /// Launch the search by PrototypeId or by keyword
        /// </summary>
        private async void OnSearchButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!isReady)
                return;

            try
            {
                SearchDetails searchDetails = GetSearchDetails();

                // Special search case: search currently selected prototype
                if (searchDetails.SearchType == SearchType.SelectedPrototype)
                {
                    SearchSelectedPrototype(searchDetails);
                    return;
                }

                // Default search: search the prototype tree
                progressBar.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true;
                await Task.Run(() => RefreshPrototypeTree(searchDetails, true));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Reset the current search
        /// </summary>
        private void OnResetButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!isReady)
                return;

            txtSearch.Text = "";
            classAutoCompletionText.Text = "";
            blueprintAutoCompletionText.Text = "";
            selectedPrototypeSearchText.Text = "";

            SearchDetails searchDetails = GetSearchDetails();
            if (searchDetails.SearchType == SearchType.SelectedPrototype)
            {
                PropertyNodes[0].ClearSearch();
                PropertyNodes[0].IsExpanded = true;
                propertytreeView.Items.Refresh();
            }   
            else
            {
                RefreshPrototypeTree(searchDetails);
            }
        }

        /// <summary>
        /// When double click on a property
        /// Allow to travel to a prototype when double clicking on a prototypeId
        /// </summary>
        private async void OnPropertyDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (!isReady)
                return;

            PropertyNode selected = ((FrameworkElement)e.OriginalSource).DataContext as PropertyNode;
            if (selected?.PropertyDetails?.Value == null)
                return;

            PrototypeId prototypeId = selected.PropertyDetails.GetPrototypeIdEquivalence();
            if (prototypeId == 0)
                return;

            try
            {
                progressBar.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true;
                await Task.Run(() => SelectFromName(GameDatabase.GetPrototypeName(prototypeId)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Called when context menu "Copy raw value" selected
        /// </summary>
        private void OnClickCopyRawValueMenuItem(object sender, RoutedEventArgs e)
        {
            object selected = ((FrameworkElement)e.OriginalSource).DataContext;

            string value = null;

            if (selected is PropertyNode propertyNode)
                value = propertyNode?.PropertyDetails?.Value;
            else if (selected is PrototypeNode prototypeNode)
            {
                if (prototypeNode?.PrototypeDetails?.FullName != null)
                    value = prototypeNode.PrototypeDetails.PrototypeId.ToString();
            }

            if (string.IsNullOrEmpty(value))
                return;

            if (ulong.TryParse(value, out var prototypeId))
                Clipboard.SetText(prototypeId.ToString());
            else
                Clipboard.SetText(value);
        }


        /// <summary>
        /// Called when context menu "Copy value to PrototypeId" selected
        /// </summary>
        private void OnClickCopyValueToPrototypeIdMenuItem(object sender, RoutedEventArgs e)
        {
            object selected = ((FrameworkElement)e.OriginalSource).DataContext;

            string value = "";
            PrototypeId prototypeId = 0;
            if (selected is PropertyNode propertyNode)
            {
                value = propertyNode?.PropertyDetails?.Value;
                if (string.IsNullOrEmpty(value))
                    return;

                prototypeId = propertyNode.PropertyDetails.GetPrototypeIdEquivalence();
            }
            else if (selected is PrototypeNode prototypeNode)
                prototypeId = prototypeNode.PrototypeDetails.PrototypeId;

            if (prototypeId == 0)
                Clipboard.SetText(value);
            else
                Clipboard.SetText(prototypeId.ToString());
        }

        private void OnClickCopyNameWithPrototypeIdMenuItem(object sender, RoutedEventArgs e)
        {
            object selected = ((FrameworkElement)e.OriginalSource).DataContext;

            string value = "";
            PrototypeId prototypeId = 0;
            if (selected is PropertyNode propertyNode)
            {
                value = propertyNode?.PropertyDetails?.Value;
                if (string.IsNullOrEmpty(value))
                    return;

                prototypeId = propertyNode.PropertyDetails.GetPrototypeIdEquivalence();
            }
            else if (selected is PrototypeNode prototypeNode)
                prototypeId = prototypeNode.PrototypeDetails.PrototypeId;

            if (prototypeId == 0)
                Clipboard.SetText(value);
            else
                Clipboard.SetText($"{GameDatabase.GetFormattedPrototypeName(prototypeId)} = {prototypeId},");
        }

        /// <summary>
        /// Called when context menu "Copy name" selected
        /// </summary>
        private void OnClickCopyNameMenuItem(object sender, RoutedEventArgs e)
        {
            object selected = ((FrameworkElement)e.OriginalSource).DataContext;

            string name = "";
            if (selected is PropertyNode propertyNode)
                name = propertyNode.PropertyDetails.Name;
            else if (selected is PrototypeNode prototypeNode)
                name = prototypeNode.PrototypeDetails.FullName;

            if (string.IsNullOrEmpty(name))
                return;

            Clipboard.SetText(name);
        }

        /// <summary>
        /// Called when search type changes
        /// </summary>
        private void OnSearchTypeSelected(object sender, SelectionChangedEventArgs e)
        {
            switch (SearchTypeComboBox.SelectedIndex)
            {
                case 0: // Search by text
                    SearchByTextField.Visibility = Visibility.Visible;
                    SearchByBlueprintField.Visibility = Visibility.Collapsed;
                    SearchByClassField.Visibility = Visibility.Collapsed;
                    SearchSelectedPrototypeField.Visibility = Visibility.Collapsed;

                    SearchByTextToggles.Visibility = Visibility.Visible;
                    SearchByClassAndBlueprintToggles.Visibility = Visibility.Collapsed;

                    break;

                case 1: // Search by class
                    SearchByTextField.Visibility = Visibility.Collapsed;
                    SearchByClassField.Visibility = Visibility.Visible;
                    SearchByBlueprintField.Visibility = Visibility.Collapsed;
                    SearchSelectedPrototypeField.Visibility = Visibility.Collapsed;

                    SearchByTextToggles.Visibility = Visibility.Collapsed;
                    SearchByClassAndBlueprintToggles.Visibility = Visibility.Visible;

                    break;

                case 2: // Search by blueprint
                    SearchByTextField.Visibility = Visibility.Collapsed;
                    SearchByClassField.Visibility = Visibility.Collapsed;
                    SearchByBlueprintField.Visibility = Visibility.Visible;
                    SearchSelectedPrototypeField.Visibility = Visibility.Collapsed;

                    SearchByTextToggles.Visibility = Visibility.Collapsed;
                    SearchByClassAndBlueprintToggles.Visibility = Visibility.Visible;

                    break;

                case 3: // Search selected prototype
                    SearchByTextField.Visibility = Visibility.Collapsed;
                    SearchByClassField.Visibility = Visibility.Collapsed;
                    SearchByBlueprintField.Visibility = Visibility.Collapsed;
                    SearchSelectedPrototypeField.Visibility = Visibility.Visible;

                    SearchByTextToggles.Visibility = Visibility.Collapsed;
                    SearchByClassAndBlueprintToggles.Visibility = Visibility.Collapsed;

                    break;
            }
        }

        /// <summary>
        /// Generate a dictionary to quicly retrieve the prototypes that reference an prototypeId in their properties
        /// </summary>
        private void GeneratePrototypeReferencesCache(BackgroundWorker worker, ref int counter)
        {
            foreach (PrototypeDetails prototypeDetails in PrototypeDetails)
            {
                string dataRef = prototypeDetails?.Properties?.FirstOrDefault(k => k.Name == "DataRef")?.Value;
                if (ulong.TryParse(dataRef, out ulong prototypeId))
                {
                    Prototype proto = GameDatabase.DataDirectory.GetPrototype<Prototype>((PrototypeId)prototypeId);
                    GeneratePrototypeReferencesCache(proto, (PrototypeId)prototypeId);
                }

                worker.ReportProgress((int)(++counter * 100 / ((float)PrototypeMaxNumber * 2)));
            }
        }

        /// <summary>
        /// Add an entry to the prototype references cache dictionary
        /// </summary>
        private void AddCacheEntry(PrototypeId key, PrototypeId value)
        {
            if (!CacheDictionary.ContainsKey(key))
                CacheDictionary[key] = new List<PrototypeId> { value };
            else if (CacheDictionary[key].Contains(value) == false)
                CacheDictionary[key].Add(value);
        }

        /// <summary>
        /// Browse all properties of the prototypes to construct the prototype references cache dictionary
        /// </summary>
        private void GeneratePrototypeReferencesCache(object property, PrototypeId parent)
        {
            if (property == null)
                return;

            PropertyInfo[] propertyInfo = property.GetType().GetProperties().Where(k => k.Name != "DataRef" && k.Name != "DataRefRecord").OrderBy(k => k.Name).ToArray();

            foreach (PropertyInfo propInfo in propertyInfo)
            {
                if (Attribute.IsDefined(propInfo, typeof(DoNotCopyAttribute)))
                    continue;

                var propValue = propInfo.GetValue(property);
                RegisterPrototypeIdIfNeeded(propValue, parent);

                if (IsTypeBrowsable(propInfo.PropertyType) == false)
                    continue;

                if (typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType))
                {
                    IEnumerable subPropertyInfo = (IEnumerable)propValue;
                    if (subPropertyInfo == null)
                        continue;

                    foreach (var subPropInfo in subPropertyInfo)
                    {
                        if (subPropertyInfo is Array)
                        {
                            RegisterPrototypeIdIfNeeded(subPropInfo, parent);

                            if (IsTypeBrowsable(subPropInfo.GetType()) == false)
                                continue;

                            GeneratePrototypeReferencesCache(subPropInfo, parent);
                        }
                        else if (subPropertyInfo is PropertyCollection)
                        {
                            KeyValuePair<PropertyId, PropertyValue> kvp = (KeyValuePair<PropertyId, PropertyValue>)subPropInfo;
                            MHServerEmu.Games.Properties.PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
                            if (info.DataType == PropertyDataType.Prototype)
                                AddCacheEntry(kvp.Value.ToPrototypeId(), parent);
                            else if (info.DataType == PropertyDataType.Asset)
                                AddCacheEntry(GameDatabase.GetDataRefByAsset(kvp.Value.ToAssetId()), parent);
                            continue;
                        }
                        else
                            GeneratePrototypeReferencesCache(subPropInfo, parent);
                    }
                }
                else if (propInfo.PropertyType == typeof(Prototype) || typeof(Prototype).IsAssignableFrom(propInfo.PropertyType))
                {
                    GeneratePrototypeReferencesCache(propValue, parent);
                }
            }
        }

        private void RegisterPrototypeIdIfNeeded(object propValue, PrototypeId parent)
        {
            if (propValue is PrototypeId prototypeId)
                AddCacheEntry(prototypeId, parent);
            else if (propValue is PrototypeGuid prototypeGuid)
                AddCacheEntry(GameDatabase.GetDataRefByPrototypeGuid(prototypeGuid), parent);
            else if (propValue is AssetId assetId)
                AddCacheEntry(GameDatabase.GetDataRefByAsset(assetId), parent);
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
        private void ConstructPropertyNodeHierarchy(PropertyNode node, object property, bool needExpand)
        {
            if (property == null)
                return;

            PropertyInfo[] propertyInfo = property.GetType().GetProperties().Where(k => k.Name != "DataRef" && k.Name != "DataRefRecord").OrderBy(k => k.Name).ToArray();

            foreach (PropertyInfo propInfo in propertyInfo)
            {
                if (Attribute.IsDefined(propInfo, typeof(DoNotCopyAttribute)))
                    continue;

                var propValue = propInfo.GetValue(property);

                if (propValue is PropertyCollection)
                    node.Childs.Add(new() { PropertyDetails = new() { Name = propInfo.Name, Value = "", TypeName = propInfo.PropertyType.Name }, IsExpanded = needExpand });
                else if (propInfo.Name != "ParentDataRef")
                    node.Childs.Add(new() { PropertyDetails = new() { Name = propInfo.Name, Value = propValue?.ToString(), TypeName = propInfo.PropertyType.Name }, IsExpanded = needExpand });

                if (IsTypeBrowsable(propInfo.PropertyType) == false)
                    continue;

                if (typeof(IEnumerable).IsAssignableFrom(propInfo.PropertyType))
                {
                    IEnumerable subPropertyInfo = (IEnumerable)propValue;
                    if (subPropertyInfo == null)
                        continue;

                    if (subPropertyInfo is Array)
                        node.Childs.Last().PropertyDetails.Value = node.Childs.Last().PropertyDetails.Value.Replace("[]", $"[{subPropertyInfo.Cast<object>().Count()}]");

                    int index = 0;
                    foreach (var subPropInfo in subPropertyInfo)
                    {
                        if (subPropertyInfo is Array)
                        {
                            string itemName;

                            switch (subPropInfo)
                            {
                                case EntityMarkerPrototype marker:
                                    itemName = GameDatabase.GetDataRefByPrototypeGuid(marker.EntityGuid).GetNameFormatted();
                                    break;

                                case MissionObjectivePrototype missionObjective:
                                    string objectiveName = LocaleManager.Instance.CurrentLocale.GetLocaleString(missionObjective.Name);
                                    itemName = string.IsNullOrWhiteSpace(objectiveName) == false ? objectiveName : subPropInfo.ToString();
                                    break;

                                default:
                                    itemName = subPropInfo.ToString();
                                    break;
                            }

                            node.Childs.Last().Childs.Add(new() { PropertyDetails = new() { Index = index++, Name = "", Value = itemName, TypeName = subPropInfo.GetType().Name }, IsExpanded = needExpand });

                            if (IsTypeBrowsable(subPropInfo.GetType()) == false)
                                continue;

                            if (subPropInfo is EvalPrototype evalProto && evalProto != null)
                            {
                                node.Childs.Last().Childs.Last().Childs.Add(new() { PropertyDetails = new() { Name = "", Value = evalProto.ExpressionString(), TypeName = "" }, IsExpanded = needExpand });
                            }
                            else
                            {
                                ConstructPropertyNodeHierarchy(node.Childs.Last().Childs.Last(), subPropInfo, needExpand);
                            }
                        }
                        else if (subPropertyInfo is PropertyCollection)
                        {
                            KeyValuePair<PropertyId, PropertyValue> kvp = (KeyValuePair<PropertyId, PropertyValue>)subPropInfo;
                            MHServerEmu.Games.Properties.PropertyInfo info = GameDatabase.PropertyInfoTable.LookupPropertyInfo(kvp.Key.Enum);
                            string val = info.DataType == PropertyDataType.Prototype ? kvp.Value.ToPrototypeId().ToString() : kvp.Value.Print(info.DataType).ToString();
                            string typeName = info.DataType == PropertyDataType.Prototype ? "PrototypeId" : info.DataType.ToString();
                            node.Childs.Last().Childs.Add(new() { PropertyDetails = new() { Name = kvp.Key.ToString(), Value = val, TypeName = typeName }, IsExpanded = needExpand });
                            continue;
                        }
                        else
                            ConstructPropertyNodeHierarchy(node.Childs.Last(), subPropInfo, needExpand);
                    }
                }
                else if (propInfo.PropertyType == typeof(EvalPrototype) || typeof(EvalPrototype).IsAssignableFrom(propInfo.PropertyType))
                {
                    EvalPrototype evalProto = (EvalPrototype)propValue;
                    if (evalProto != null)
                        node.Childs.Last().Childs.Add(new() { PropertyDetails = new() { Name = "", Value = evalProto.ExpressionString(), TypeName = "" }, IsExpanded = needExpand });
                }
                else if (propInfo.PropertyType == typeof(Prototype) || typeof(Prototype).IsAssignableFrom(propInfo.PropertyType))
                {
                    ConstructPropertyNodeHierarchy(node.Childs.Last(), propValue, needExpand);
                }
                else if (propInfo.PropertyType == typeof(LocaleStringId))
                {
                    LocaleStringId localeStringId = (LocaleStringId)propValue;

                    if (localeStringId != LocaleStringId.Invalid)
                    {
                        string localeString = LocaleManager.Instance.CurrentLocale.GetLocaleString(localeStringId);
                        if (localeString != string.Empty)
                        {
                            node.Childs.Last().Childs.Add(new() { PropertyDetails = new() { Name = "", Value = localeString, TypeName = "" }, IsExpanded = needExpand });
                            node.Childs.Last().IsExpanded = true;
                            node.Childs.Last().CollapseOnSearchClear = false;
                        }
                    }
                }
            }
        }

        private bool IsTypeBrowsable(Type type)
        {
            return !type.IsPrimitive && type != typeof(Vector3) && type != typeof(string);
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
                txtDataRef.DataContext = new PropertyNode() { PropertyDetails = new PropertyDetails() { Name = prototypeFullName, Value = prototypeId.ToString() } };

                if (_fullNameBackHistory.Count == 0 || _fullNameBackHistory.Peek() != prototypeFullName)
                    _fullNameBackHistory.Push(prototypeFullName);
            }
            else
            {
                txtDataRef.Text = dataRef;
                txtDataRef.DataContext = new PropertyNode() { PropertyDetails = new PropertyDetails() { Name = dataRef, Value = dataRef } };
            }

            string parentDataRef = NodeSelected?.PrototypeDetails?.Properties?.FirstOrDefault(k => k.Name == "ParentDataRef")?.Value;

            if (ulong.TryParse(parentDataRef, out ulong parentPrototypeId))
            {
                string prototypeFullName = GameDatabase.GetPrototypeName((PrototypeId)parentPrototypeId);
                txtParentDataRef.Text = $"Parent : {prototypeFullName} ({parentPrototypeId})";
                txtParentDataRef.DataContext = new PropertyNode() { PropertyDetails = new PropertyDetails() { Name = prototypeFullName, Value = parentPrototypeId.ToString(), TypeName = "PrototypeId" } };
            }
            else
            {
                txtParentDataRef.Text = $"Parent : {parentDataRef}";
                txtParentDataRef.DataContext = new PropertyNode() { PropertyDetails = new PropertyDetails() { Name = parentDataRef, Value = parentDataRef, TypeName = "PrototypeId" } };
            }

            Prototype proto = GameDatabase.DataDirectory.GetPrototype<Prototype>((PrototypeId)prototypeId);
            PropertyNodes[0].Childs.Clear();
            PropertyNodes[0].ClearSearch();
            propertytreeView.Items.Refresh();

            bool needExpand = expandResultToggle.IsChecked ?? false;
            ConstructPropertyNodeHierarchy(PropertyNodes[0], proto, needExpand);
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
            TreeViewItem childTreeView = null;

            Dispatcher.Invoke(() =>
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

                childTreeView = (TreeViewItem)container.ItemContainerGenerator.ContainerFromIndex(i);
                ExpandOrCollapseTreeViewItem(childTreeView, needToExpand);
            });

            return childTreeView;
        }

        /// <summary>
        /// Return a treeViewItem based on the array of indexes of its location in the hierarchy
        /// </summary>
        private void SelectTreeViewItem(int[] indexes)
        {
            Dispatcher.Invoke(() =>
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
            });
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

        private void SearchSelectedPrototype(SearchDetails searchDetails)
        {
            if (string.IsNullOrWhiteSpace(searchDetails.TextValue))
                return;

            if (PropertyNodes[0].Childs.Count == 0)
                return;

            List<PropertyNode> matches = new();
            PropertyNodes[0].ClearSearch();
            PropertyNodes[0].IsExpanded = true;
            PropertyNodes[0].SearchText(searchDetails.TextValue, matches);

            StringBuilder sb = new();
            sb.AppendLine($"Found {matches.Count} matches:\n");
            foreach (PropertyNode node in matches)
                sb.AppendLine(node.PropertyDetails.ToString());

            propertytreeView.Items.Refresh();

            MessageBox.Show(sb.ToString(), "Search Result");
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
