using GameDatabaseBrowser.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace GameDatabaseBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PrototypeNode Root = new() { Name = "Prototypes" };

        public MainWindow()
        {
            InitializeComponent();
            txtDataRef.Text = "DataRef : ";
            txtParentDataRef.Text = "ParentDataRef : ";
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
                Prototype proto = dataDirectory.GetPrototype<Prototype>(prototypeId);
                Type type = proto.GetType();

                PropertyInfo[] properties = type.GetProperties();

                Root.Childs.Add(new()
                {
                    Name = GameDatabase.GetPrototypeName(prototypeId),
                    Properties = properties
                    .Select(k => new Property() { Name = k.Name, Value = k.GetValue(proto)?.ToString() }).ToList()
                });

                worker.ReportProgress((int)(++p * (100 / 93114f)));
            }

            worker.ReportProgress(100);
        }

        private void OnLoadingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Collapsed;

            treeView.Items.Add(Root);

            treeView.SelectedItemChanged += UpdatePropertiesSection;

            UpdateLayout();
        }

        private void UpdatePropertiesSection(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            PrototypeNode NodeSelected = (PrototypeNode)e.NewValue;
            txtDataRef.Text = "DataRef : " + NodeSelected?.Properties?.First(k => k.Name == "DataRef")?.Value;
            txtParentDataRef.Text = "Parent : " + NodeSelected?.Properties?.First(k => k.Name == "ParentDataRef")?.Value;
            listView.ItemsSource = NodeSelected?.Properties?.Where(k => k.Name != "DataRef" && k.Name != "ParentDataRef" && k.Name != "DataRefRecord");
        }
    }
}
