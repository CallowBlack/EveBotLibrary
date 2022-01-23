using EveAutomation.memory.eve.type;
using EveAutomation.memory.python;
using EveAutomation.memory.python.type;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Py2ObjectViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<PyObjectWrapper> PyObjectsWrapped
        {
            get => _pyObjectWrapped;
            set
            {
                _pyObjectWrapped = value;
                NotifyPropertyChanged();
            }
        }
        private ObservableCollection<PyObjectWrapper> _pyObjectWrapped;

        public bool CanSearch
        {
            get => _canSearch;
            set
            {
                _canSearch = value;
                NotifyPropertyChanged();
            }
        }
        private bool _canSearch;

        public string SearchText {
            get => _searchText;
            set {
                _searchText = value;
                UpdateConfig("SearchText", value);
            }
        }
        private string _searchText;

        public bool Contains { 
            get => _contains; 
            set
            {
                _contains = value;
                UpdateConfig("Contains", value.ToString());
            }
        }
        private bool _contains;

        public int DeepLevel {
            get => _deepLevel;
            set
            {
                _deepLevel = value;
                UpdateConfig("DeepLevel", value.ToString());
            }
        }
        private int _deepLevel;

        // Search Info
        public int SearchFound
        {
            get => _searchFound;
            set
            {
                _searchFound = value;
                NotifyPropertyChanged();
            }
        }
        private int _searchFound = 0;

        public int SearchChecked
        {
            get => _searchChecked;
            set
            {
                if (value > SearchMax)
                    SearchMax = value;

                _searchChecked = value;
                NotifyPropertyChanged();
            }
        }
        private int _searchChecked = 0;

        public int SearchMax
        {
            get => _searchMax;
            set
            {
                _searchMax = value;
                NotifyPropertyChanged();
            }
        }
        private int _searchMax = 0;

        // Update info
        public int UpdatingCount { 
            get => _updatingCount; 
            set
            {
                _updatingCount = value;
                NotifyPropertyChanged();
            }
        }
        private int _updatingCount = 0;

        public int LoadedObjects
        {
            get => _loadedObjects;
            set
            {
                _loadedObjects = value;
                NotifyPropertyChanged();
            }
        }
        private int _loadedObjects = 0;

        public bool IsAttachedToProcess
        {
            get => _isAttachedToProcess;
            set
            {
                _isAttachedToProcess = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isAttachedToProcess = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly BackgroundWorker updateWorker;
        private readonly BackgroundWorker searchWorker;
        private Process? _process;

        public MainWindow()
        {
            LoadConfig();

            updateWorker = new BackgroundWorker();
            updateWorker.WorkerSupportsCancellation = true;
            updateWorker.DoWork += UpdateDoWork;

            searchWorker = new BackgroundWorker();
            searchWorker.WorkerSupportsCancellation = true;
            searchWorker.DoWork += SearchDoWork;

            EveAutomation.memory.eve.TypeLoader.Initialize();

            InitializeComponent();

            Task.Run(async () =>
            {
                await AttachToProcess();

                if (!IsAttachedToProcess)
                {
                    MessageBox.Show("Cannot find eve process automaticaly. Select process manual.");
                    ShowProcessSelector();
                }
            });
        }

        private void SearchDoWork(object? sender, DoWorkEventArgs e)
        {
            SearchChecked = 0;
            SearchFound = 0;

            Func<ulong, string, bool> filter =
                (address, typeName) =>
            {
                SearchChecked++;
                return Contains ? typeName.Contains(SearchText) : typeName.Equals(SearchText);
            };


            var newCollection = new ObservableCollection<PyObjectWrapper>();
            
            try
            {
                foreach (var item in PyObjectPool.ScanPythonObjects(true, filter))
                {
                    if (searchWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    SearchFound++;
                    newCollection.Add(new PyObjectWrapper(item));
                }
                SearchMax = SearchChecked;

                PyObjectsWrapped = newCollection;
            } 
            catch (DllNotFoundException)
            {
                MessageBox.Show("Cannot find GCCollector from process. Try select process again.", "GCCollector not found");
                DeattachFromProccess();
                return;
            }

        }

        private async void UpdateDoWork(object? sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (updateWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                if (searchWorker.IsBusy)
                {
                    await Task.Delay(1000);
                    continue;
                }

                var loadedObjectList = PyLoadedObjects.GetObjects();
                LoadedObjects = loadedObjectList.Count;
                UpdatingCount = 0;

                foreach (var item in loadedObjectList)
                {
                    item.Update(true);
                    UpdatingCount++;
                }
                    
                await Task.Delay(1000);
            }
        }

        private void SearchButtonClicked(object sender, RoutedEventArgs e)
        {
            searchWorker.RunWorkerAsync();
        }

        private void CopyContextMenuClicked(object sender, RoutedEventArgs e)
        {
            if (e.Source is not MenuItem menuItem)
                return;

            if (menuItem.CommandParameter is not ContextMenu contextMenu)
                return;

            if (contextMenu.PlacementTarget is not TextBlock textBlock)
                return;

            Clipboard.SetText(textBlock.Text);
        }

        private void MenuItemOpenProcessClicked(object sender, RoutedEventArgs e)
        {
            ShowProcessSelector();
        }

        private void ShowProcessSelector()
        {
            ProcessSelector processSelector = new ProcessSelector();
            processSelector.ProcessSelected += OnProcessSelected;
            processSelector.ShowDialog();
        }

        private bool OpenSelectedProcess()
        {
            if (_process is null)
            {
                var result = EveAutomation.memory.ProcessMemory.Open("exefile");
                if (result == null)
                    return false;
                return true;
            }

            var opened = EveAutomation.memory.ProcessMemory.Open(_process);
            return opened != null;
        }

        private async Task AttachToProcess()
        {
            IsAttachedToProcess = false;
            if (!OpenSelectedProcess())
                return;

            while (searchWorker.IsBusy || updateWorker.IsBusy)
                await Task.Delay(25);

            searchWorker.RunWorkerAsync();
            updateWorker.RunWorkerAsync();
            IsAttachedToProcess = true;
        }

        private void DeattachFromProccess()
        {
            if (updateWorker.IsBusy)
                updateWorker.CancelAsync();

            PyLoadedObjects.Clear();
            PyObjectPool.Clear();
            if (PyObjectsWrapped != null)
                Application.Current.Dispatcher?.Invoke(() => PyObjectsWrapped.Clear());

            IsAttachedToProcess = false;
        }

        private async void OnProcessSelected(object? sender, System.Diagnostics.Process e)
        {
            if (IsAttachedToProcess)
            {
                if (searchWorker.IsBusy)
                    searchWorker.CancelAsync();
                DeattachFromProccess();
            }
            
            _process = e;
            await AttachToProcess();
        }

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadConfig()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                _searchText = appSettings["SearchText"] ?? "UIRoot";
                _contains = Boolean.Parse(appSettings["Contains"] ?? "False");
                _deepLevel = Int32.Parse(appSettings["DeepLevel"] ?? "0");
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
            }
        }

        private void UpdateConfig(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
    }
}
