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
                DeepUpdate();
                await Task.Delay(1000);
            }
        }

        public async void DeepUpdate()
        {
            UpdateCount = 0;
            foreach (var item in PyObjectsWrapped)
                if (item.Origin is PyObject pyObject)
                    pyObject.Update(true, true);
        }

        public void SearchItems(string name, bool contains = true)
        {
            PyObjectPool.ScanProcessMemory(new List<String>() { name }, contains);
            PyObjectsWrapped = new ObservableCollection<PyObjectCollectionWrapper>(PyObjectPool.GetObjects()
                .Select(item => new PyObjectCollectionWrapper(item)));

            foreach(var item in PyObjectPool.GetObjects())
            {
                item.FieldChanged += Item_FieldChanged;
            }
            foreach(var obj in PyObjectsWrapped)
            {
                obj.Load();
            }
        }

        private void Item_FieldChanged(FieldChangedArgs args)
        {
            UpdateCount += 1;
        }

        public string SearchText { get; set; }

        public ObservableCollection<PyObjectCollectionWrapper> PyObjectsWrapped {
            get => _pyObjectWrapped;
            set
            {
                _pyObjectWrapped = value;
                NotifyPropertyChanged();
            }
        }

        public int UpdateCount { get => _updateCount;
        set { _updateCount = value;
                NotifyPropertyChanged();
            }
        }
        private int _updateCount;

        private ObservableCollection<PyObjectCollectionWrapper>? _pyObjectWrapped;

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

    [ValueConversion(typeof(PyObjectCollectionWrapper), typeof(PyObjectCollectionWrapper))]
    public class PyCollectionInitializerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PyObjectCollectionWrapper collection)
                collection.Load();

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(IWrapper), typeof(string))]
    public class PyObjectToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var origin = value;
            if (origin is PyDict dict)
                return $"dict[{dict.Length}]";
            else if (origin is PyList list)
                return $"list[{list.Length}]";
            else if (origin is PyTuple tuple)
                return $"tuple[{tuple.Length}]";
            else if (origin is PyString || origin is PyUnicode || origin is PyInt || origin is PyFloat || origin is PyBool)
                return origin?.ToString() ?? "<error>";
            else if (origin is PyObject obj) {
                if (obj.Type.Name == "NoneType")
                    return "None";

                if (obj.Dict == null)
                    return $"dict[0]";

                var name = obj.Dict.Get("_name") as PyString;
                if (name != null)
                    return $"dict[{obj.Dict.Length}]:{name.Value}";

                var _childrenObjects = obj.Dict.Get("_childrenObjects") as PyList;
                if (_childrenObjects != null)
                    return $"list[{_childrenObjects.Length}]";

                return $"dict[{obj.Dict.Length}]";
            }
            return "undefined";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class KeyValue
    {
        public string Key { get; set; }
        public IWrapper Value { get; set; }

        public KeyValue(string key, IWrapper value)
        {
            Key = key;
            Value = value;
        }
    }

    public interface IWrapper
    {
        public object Origin { get; set; }

        public string Key { get; set; }
    }


    public static class WrapperPool {
        public static Dictionary<object, IWrapper> wrappers = new();
    }
    public class PyKeyValueWrapper : INotifyPropertyChanged, IWrapper
    {
        public object Origin { get; set; }

        public PyObject Key { get; private set; }

        public IWrapper Value { get; private set; }
        string IWrapper.Key { get => Key.ToString(); set { } }

        public event PropertyChangedEventHandler? PropertyChanged;

        public PyKeyValueWrapper(KeyValuePair<PyObject, PyObject> pair)
        {
            Origin = pair;

            Key = pair.Key;

            if (WrapperPool.wrappers.ContainsKey(pair.Value))
            {
                Value = WrapperPool.wrappers[pair.Value];
            }
            else if (pair.Value is IValueChanged valueChanged)
            {
                Value = new PyObjectValueWrapper(valueChanged);
                valueChanged.ValueChanged += ValueChanged_ValueChanged;
            }
            else if (pair.Value is PyList pyList)
            {
                Value = new PyObjectCollectionWrapper(pyList);
            }
            else
            {
                Value = new PyObjectCollectionWrapper(pair.Value);
            }
        }

        private void ValueChanged_ValueChanged(ValueChangedArgs args)
        {
            PropertyChanged?.Invoke(this, new(null));
        }
    }

    public class PyObjectValueWrapper : INotifyPropertyChanged, IWrapper
    {
        public object Origin { get; set; }

        public string Key { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public PyObjectValueWrapper(IValueChanged obj, string? key = null)
        {
            Origin = obj;
            Key = key ?? "<NoName>";
            obj.ValueChanged += OnValueChanged;
        }

        private void OnValueChanged(ValueChangedArgs args)
        {
            PropertyChanged?.Invoke(this, new(""));
        }
    }

    public class PyObjectCollectionWrapper : ObservableCollection<IWrapper>, IWrapper
    {
        public object Origin { get; set; }

        public string Key { get; set; }

        public ObservableCollection<IWrapper> Wrappers { get => this; }


        private bool _isLoaded = false;

        public void Load()
        {
            if (_isLoaded)
                return;
            WrapperPool.wrappers[Origin] = this;
            if (Origin is PyList list)
            {
                AddItems(new List<PyObject>(list.Items));
                list.ListChanged += OnListChanged;
            }
            else if (Origin is PyTuple tuple)
                AddItems(tuple.Items);
            else if (Origin is PyDict dict)
            {
                AddItems(dict.Items);
                dict.DictionaryChanged += OnDictionaryChanged;
            }
            else if (Origin is PyObject obj && obj.Dict != null)
            {
                AddItems(obj.Dict.Items);
                obj.Dict.DictionaryChanged += OnDictionaryChanged;
            }
            _isLoaded = true;
        }

        public PyObjectCollectionWrapper(PyObject pyObject, string? name = null) {
            Key = name ?? "<NoName>";
            Origin = pyObject;
        }

        public void AddItems(IEnumerable<PyObject> items)
        {
            var i = 0;
            foreach (var item in items)
            {
                var currKey = $"[{i}]";
                if (WrapperPool.wrappers.ContainsKey(item))
                {
                    Add(WrapperPool.wrappers[item]);
                }
                else if (item is IValueChanged vc)
                {
                    Add(new PyObjectValueWrapper(vc, currKey));
                }
                else
                {
                    Add(new PyObjectCollectionWrapper(item, currKey));
                }
                i++;
            }
        }

        public void AddItems(IEnumerable<KeyValuePair<PyObject, PyObject>> items)
        {
            foreach (var item in items)
            {
                if (WrapperPool.wrappers.ContainsKey(item))
                {
                    Add(WrapperPool.wrappers[item]);
                }
                else
                {
                    Add(new PyKeyValueWrapper(item));
                }
            }
        }

        public void RemoveItems(IEnumerable items)
        {
            foreach (var item in items)
            {
                try
                {
                    var itemToRemove = this.First(value => value.Origin == item);
                    Remove(itemToRemove);
                }
                catch (InvalidOperationException) { }
            }
        }

        private void OnListChanged(CollectionChangedArgs<PyObject> args)
        {
            if (args.AddedItems != null)
            {
                AddItems(args.AddedItems);
            }

            if (args.RemovedItems != null)
            {
                RemoveItems(args.RemovedItems);
            }
        }

        private void OnDictionaryChanged(CollectionChangedArgs<KeyValuePair<PyObject, PyObject>> args)
        {
            if (args.AddedItems != null)
            {
                AddItems(args.AddedItems);
            }

            if (args.RemovedItems != null)
            {
                RemoveItems(args.RemovedItems);
            }
        }
    }
}
