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
using System.Windows;

namespace Py2ObjectViewer
{
    public class PyObjectWrapper : INotifyPropertyChanged
    {
        public PyObject Origin { get; protected set; }
        public string Key { get; protected set; }
        public PyObject? KeyObject { get; protected set; }

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
            
            if (Origin is INotifyValueChanged vc)
            {
                vc.ValueChanged += OnValueChanged;
            }

            PyLoadedObjects.LoadObject(Origin);
        }

        ~PyObjectWrapper()
        {
            PyLoadedObjects.UnloadObject(Origin);
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
            else if (Origin is PyClass cls)
                return $"class '{cls.Name}'";
            else if (Origin is PyInstance instance)
            {
                if (instance.Class == null)
                    return $"inst of undefined";
                return $"inst of class '{instance.Class.Name}'";
            }   
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

            if (Origin is PyClass cls)
            {
                if (cls.Bases != null) yield return new PyObjectWrapper(cls.Bases, "Bases");
                if (cls.Content != null) yield return new PyObjectWrapper(cls.Content, "Content");

                yield break;
            }

            if (Origin is PyInstance instance)
            {
                if (instance.Class != null) yield return new PyObjectWrapper(instance.Class, "Class");
                if (instance.Content != null) yield return new PyObjectWrapper(instance.Content, "Content");

                yield break;
            }

            if (KeyObject != null && 
                KeyObject is not INotifyValueChanged && KeyObject is not PyString && KeyObject is not PyUnicode)
            {
                yield return new PyObjectWrapper(KeyObject, "Key");
                yield return new PyObjectWrapper(Origin, "Value");
                yield break;
            }

            if (Origin is INotifyValueChanged)
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
                    PyObjectWrapper wrapper;
                    if (item.Value.Dict != null && item.Value.Dict.Get("_childrenObjects") is PyList _childrenObjects)
                        wrapper = new PyObjectWrapper(_childrenObjects, item.Key.ToString());
                    else
                        wrapper = new PyObjectWrapper(item.Value, item.Key.ToString());
                    wrapper.KeyObject = item.Key;
                    yield return wrapper;
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

        private void OnValueChanged(object? sender, ValueChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, new(null));
        }

        private void OnDictChanged(object? sender, DictionaryChangedEventArgs args)
        {
            if (args.ChangeType != CollectionChangeType.ContainerChanged)
                return;

            if (args.RemovedItems != null) RemoveItems(args.RemovedItems.Select(pair => pair.Key).ToList(), true);

            if (args.AddedItems != null) AddItems(args.AddedItems);

            if (args.ChangedValues != null) ChangeObjects(args.ChangedValues);

            PropertyChanged?.Invoke(this, new(null));
        }

        private void OnListChanged(object? sender, EveAutomation.memory.python.ListChangedEventArgs args)
        {
            if (args.ChangeType != CollectionChangeType.ContainerChanged)
                return;

            if (args.RemovedItems != null) RemoveItems(args.RemovedItems);

            if (args.AddedItems != null) AddItems(args.AddedItems);

            PropertyChanged?.Invoke(this, new(null));

        }

        private void ChangeObjects(List<(PyObject key, PyObject oldItem, PyObject newItem)> items)
        {
            var dict = items.ToDictionary(entry => entry.key, entry => entry.newItem);
            for (int i = 0; i < Wrappers.Count; i++)
            {
                var wrapper = Wrappers[i];
                if (wrapper.KeyObject == null || !dict.ContainsKey(wrapper.KeyObject))
                    continue;

                var replaceWrapper = new PyObjectWrapper(dict[wrapper.KeyObject], wrapper.Key);
                replaceWrapper.KeyObject = wrapper.KeyObject;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Wrappers.RemoveAt(i);
                    Wrappers.Insert(i, replaceWrapper);
                });
            }
        }

        private void RemoveItems(List<PyObject> pyObjects, bool byKey = false)
        {
            var indexes = FindIndexes(pyObjects, byKey);
            foreach (var index in indexes)
                Application.Current.Dispatcher.Invoke(() => Wrappers.RemoveAt(index));

            if (Origin is PyCollection && indexes.Count > 0)
                ReindexWrappers(indexes.Min());
        }

        private void AddItems(IEnumerable<PyObject> entries)
        {
            var isCollection = Origin is PyCollection;
            foreach (PyObject item in entries)
            {
                Application.Current.Dispatcher.Invoke(() => 
                    Wrappers.Add(new PyObjectWrapper(item, isCollection ? $"[{Wrappers.Count}]" : null)));
            }
                
        }

        private void AddItems(IEnumerable<KeyValuePair<PyObject, PyObject>> entries)
        {
            foreach (var entry in entries)
            {
                var wrapper = new PyObjectWrapper(entry.Value, entry.Key.ToString());
                wrapper.KeyObject = entry.Key;
                Application.Current.Dispatcher.Invoke(() => Wrappers.Add(wrapper));
            }
        }

        private List<int> FindIndexes(List<PyObject> pyObjects, bool byKey = false)
        {
            var result = new List<int>(pyObjects.Count);
            var set = new HashSet<PyObject>(pyObjects);
            for (int i = Wrappers.Count - 1; i >= 0; i--)
            {
                var wrapper = Wrappers[i];
                if (!byKey && set.Contains(wrapper.Origin) || 
                    byKey && wrapper.KeyObject != null && set.Contains(wrapper.KeyObject))
                    result.Add(i);
            }
            return result;
        }

        private void ReindexWrappers(int startIndex)
        {
            for (int i = startIndex; i < Wrappers.Count; i++)
                Wrappers[i].Key = $"[{i}]";
        }
    }
}
