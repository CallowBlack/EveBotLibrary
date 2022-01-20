using EveAutomation.memory.python;
using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Py2ObjectViewer
{
    public class PyObjectWrapper : INotifyPropertyChanged
    {
        public static readonly HashSet<PyObject> loadedObjects = new();

        public PyObject Origin { get; private set; }
        public string Key { get; protected set; }

        public string Presentation { get => GetPresentation(); }

        public ObservableCollection<PyObjectWrapper> Wrappers {
            get
            {
                if (_wrappers == null)
                    _wrappers = new(LoadWrappers());

                return _wrappers;
            }
        }
        private ObservableCollection<PyObjectWrapper>? _wrappers;

        public event PropertyChangedEventHandler? PropertyChanged;

        public PyObjectWrapper(PyObject pyObject, string? key = null)
        {
            Key = key ?? "object";
            Origin = pyObject;
            
            if (Origin is IValueChanged vc)
            {
                vc.ValueChanged += OnValueChanged;
            }

            loadedObjects.Add(Origin);
        }

        private string GetPresentation()
        {
            if (Origin is PyDict dict)
                return $"dict[{dict.Length}]";
            else if (Origin is PyList list)
                return $"list[{list.Count}]";
            else if (Origin is PyTuple tuple)
                return $"tuple[{tuple.Count}]";
            else if (Origin is PyString || Origin is PyUnicode || Origin is PyInt || Origin is PyFloat || Origin is PyBool)
                return Origin?.ToString() ?? "<error>";
            else if (Origin is PyObject obj)
            {
                if (obj.Type.Name == "NoneType")
                    return "None";

                if (obj.Dict == null)
                    return $"dict[0]";

                if (obj.Dict.Get("_name") is PyString name)
                    return $"dict[{obj.Dict.Length}]:{name.Value}";

                if (obj.Dict.Get("_childrenObjects") is PyList _childrenObjects)
                    return $"list[{_childrenObjects.Count}]";

                return $"dict[{obj.Dict.Length}]";
            }
            return "undefined";
        }

        private IEnumerable<PyObjectWrapper> LoadWrappers()
        {
            if (Origin is IValueChanged)
                yield break;

            if (Origin is PyCollection list)
            {
                uint i = 0;
                foreach (var item in list.Items)
                {
                    yield return new PyObjectWrapper(item, $"[{i}]");
                    i++;
                }
            }
            else
            {
                var dict = Origin is PyDict pyDict ? pyDict : Origin.Dict;
                if (dict == null)
                    yield break;

                foreach (var item in dict.Items)
                {
                    yield return new PyObjectWrapper(item.Value, item.Key.ToString());
                }
            }

            UpdateEvent();
        }

        private void UpdateEvent()
        {
            if (Origin is PyList list)
            {
                list.ListChanged += OnListChanged;
            }
            else if (Origin is PyDict dict)
            {
                dict.DictionaryChanged += OnDictChanged;
            }
            else if (Origin.Dict != null)
            {
                Origin.Dict.DictionaryChanged += OnDictChanged;
            }
        }

        private void OnValueChanged(ValueChangedArgs args)
        {
            PropertyChanged?.Invoke(this, new(null));
        }

        private void OnDictChanged(CollectionChangedArgs<KeyValuePair<PyObject, PyObject>> args)
        {
            if (args.AddedItems != null) AddItems(args.AddedItems.Select(pair => pair.Value));

            if (args.RemovedItems != null) RemoveItems(args.RemovedItems.Select(pair => pair.Value));
        }

        private void OnListChanged(CollectionChangedArgs<PyObject> args)
        {
            if (args.AddedItems != null) AddItems(args.AddedItems);

            if (args.RemovedItems != null) RemoveItems(args.RemovedItems);
        }

        private void RemoveItems(IEnumerable<PyObject> pyObjects)
        {
            var indexes = FindIndexes(pyObjects);
            foreach (var index in indexes)
                Wrappers.RemoveAt(index);

            if (Origin is PyCollection && indexes.Any())
                ReindexWrappers(indexes.Min());
        }

        private void AddItems(IEnumerable<PyObject> pyObjects)
        {
            var isCollection = Origin is PyCollection;
            foreach (var pyObject in pyObjects)
                Wrappers.Add(new PyObjectWrapper(pyObject, isCollection ? $"[{Wrappers.Count}]" : null));
        }

        private IEnumerable<int> FindIndexes(IEnumerable<PyObject> pyObjects)
        {
            var set = new HashSet<PyObject>(pyObjects);
            for (int i = 0; i < Wrappers.Count; i++)
            {
                if (set.Contains(Wrappers[i].Origin))
                    yield return i;
            }
        }

        private void ReindexWrappers(int startIndex)
        {
            for (int i = startIndex; i < Wrappers.Count; i++)
                Wrappers[i].Key = $"[{i}]";
        }
    }
}
