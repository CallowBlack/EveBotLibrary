using EveAutomation.memory.eve.type;
using EveAutomation.memory.python;
using EveAutomation.memory.python.type;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
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
        
        public void DeepUpdate()
        {
            foreach (var item in PyObjects)
                item.Object.Update(true, true);
        }

        public void SearchItems(string name, bool contains = true)
        {
            PyObjectPool.ScanProcessMemory(new List<String>() { name }, contains);
            PyObjects = new ObservableCollection<PyObjectNotify>(PyObjectPool.GetObjects().Select(obj => new PyObjectNotify(obj)));
        }

        public string SearchText { get; set; }

        public ObservableCollection<PyObjectNotify> PyObjects { 
            get => _pyObjects ?? new(); 
            set
            {
                _pyObjects = value;
                NotifyPropertyChanged();
            }
        }
        private ObservableCollection<PyObjectNotify>? _pyObjects;

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

    [ValueConversion(typeof(ObservableCollection<PyObjectNotify>), typeof(ObservableCollection<KeyValue>))]
    public class PyObjectNotifyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ObservableCollection<PyObjectNotify> objs)
            {
                ObservableCollection<KeyValue> newItems = 
                    new (objs.Select(obj => new KeyValue("object", obj.Object)));
                return newItems;
            }
                
            return new();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(PyObject), typeof(ObservableCollection<KeyValue>))]
    public class PyObjectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PyDict dict)
                return DictConvert(dict);
            else if (value is PyList list)
                return ListConvert(list.Items);
            else if (value is PyTuple tuple)
                return ListConvert(tuple.Items);
            else if (value is PyObject obj && obj.Dict != null)
            {
                var _childrenObjects = obj.Dict.Get("_childrenObjects") as PyList;
                if (_childrenObjects != null)
                    return ListConvert(_childrenObjects.Items);
                return DictConvert(obj.Dict);
            }
            return new();
        }

        private ObservableCollection<KeyValue> DictConvert(PyDict dict)
        {
            return new ObservableCollection<KeyValue>
                (
                    dict.Items.Select(item => new KeyValue((item.Key is PyString ps) ? ps.Value : item.ToString(), item.Value))
                );
        }
        private ObservableCollection<KeyValue> ListConvert(IEnumerable<PyObject> objects)
        {
            uint index = 0;
            var result = new ObservableCollection<KeyValue>();
            foreach (var item in objects)
            {
                result.Add(new KeyValue($"[{index}]", item));
                index++;
            }
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(PyObject), typeof(string))]
    public class PyObjectToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PyDict dict)
                return $"dict[{dict.Length}]";
            else if (value is PyList list)
                return $"list[{list.Length}]";
            else if (value is PyTuple tuple)
                return $"tuple[{tuple.Length}]";
            else if (value is PyString || value is PyUnicode || value is PyInt || value is PyFloat || value is PyBool)
                return value?.ToString() ?? "<error>";
            else if (value is PyObject obj) {
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
        public PyObject Value { get; set; }

        public KeyValue(string key, PyObject value)
        {
            Key = key;
            Value = value;
        }
    }

    public class PyObjectNotify : INotifyPropertyChanged
    {
        public PyObject Object { get; private set; }
        public PyObjectNotify(PyObject obj)
        {
            Object = obj;
            Object.FieldChanged += Object_MemberChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Object_MemberChanged(FieldChangedArgs args)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}
