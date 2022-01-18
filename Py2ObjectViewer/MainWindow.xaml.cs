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
            PyObjectPool.ScanProcessMemory(new List<String>() { "UIRoot" }, true);
            PyObjects = new ObservableCollection<KeyValue>(PyObjectPool.GetObjects().Select(obj => new KeyValue($"object<{obj.Type.Name}>", obj)));
            DataContext = this;
            SearchText = "";
        }

        public string SearchText { get; set; }

        public ObservableCollection<KeyValue> PyObjects { 
            get => _pyObjects; 
            set
            {
                _pyObjects = value;
                NotifyPropertyChanged();
            }
        }
        private ObservableCollection<KeyValue> _pyObjects;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            PyObjectPool.ScanProcessMemory(new List<String>() { SearchText }, true);
            PyObjects = new ObservableCollection<KeyValue>(PyObjectPool.GetObjects().Select(obj => new KeyValue($"object<{obj.Type.Name}>", obj)));
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
            return null;
        }

        private ObservableCollection<KeyValue> DictConvert(PyDict dict)
        {
            return new ObservableCollection<KeyValue>
                (
                    dict.Items.Select(item => new KeyValue((item.key is PyString ps) ? ps.Value : item.ToString(), item.value))
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
}
