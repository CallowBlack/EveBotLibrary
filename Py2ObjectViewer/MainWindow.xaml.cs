using EveAutomation.memory.eve.type;
using EveAutomation.memory.python;
using EveAutomation.memory.python.type;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public MainWindow()
        {
            InitializeComponent();
            EveAutomation.memory.ProcessMemory.Open("exefile");
            EveAutomation.memory.eve.TypeLoader.Initialize();
            SearchItems("UIRoot", false);
            DataContext = this;
            SearchText = "";
            DoUpdate();
        }

        public async void DoUpdate()
        {
            while (true)
            {
                UpdateLoaded();
                await Task.Delay(1000);
            }
        }

        public void UpdateLoaded()
        {
            UpdateCount = 0;
            List<PyObject> loadedClone = new(PyObjectWrapper.loadedObjects);
            foreach (var item in loadedClone)
                item.Update(true);
        }

        public void SearchItems(string name, bool contains = true)
        {
            PyObjectWrapper.loadedObjects.Clear();

            PyObjectPool.ScanProcessMemory(new List<String>() { name }, contains);
            PyObjectsWrapped = new ObservableCollection<PyObjectWrapper>(PyObjectPool.GetObjects()
                .Select(item => new PyObjectWrapper(item)));

            foreach(var item in PyObjectPool.GetObjects())
            {
                item.FieldChanged += Item_FieldChanged;
            }
        }

        private void Item_FieldChanged(FieldChangedArgs args)
        {
            UpdateCount += 1;
        }

        public string SearchText { get; set; }

        public ObservableCollection<PyObjectWrapper> PyObjectsWrapped 
        {
            get => _pyObjectWrapped;
            set
            {
                _pyObjectWrapped = value;
                NotifyPropertyChanged();
            }
        }
        private ObservableCollection<PyObjectWrapper>? _pyObjectWrapped;

        public int UpdateCount 
        { 
            get => _updateCount;

            set 
            { 
                _updateCount = value;
                NotifyPropertyChanged();
            }
        }
        private int _updateCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SearchItems(SearchText);
        }
    }
}
