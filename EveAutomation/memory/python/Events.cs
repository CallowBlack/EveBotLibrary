using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    public interface INotifyValueChanged
    {
        public event EventHandler<ValueChangedEventArgs>? ValueChanged;
    }

    public interface INotifyDictionaryChanged
    {
        public event EventHandler<DictionaryChangedEventArgs>? DictionaryChanged;
    }

    public interface INotifyListChanged
    {
        public event EventHandler<ListChangedEventArgs>? ListChanged;
    }

    public interface INotifyObjectRemoved
    {
        public event EventHandler? ObjectRemoved;
    }

    public class ValueChangedEventArgs : EventArgs
    {
        public object OldValue { get; private set; }
        public object NewValue { get; private set; }

        public ValueChangedEventArgs(object oldValue, object newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public enum CollectionChangeType
    {
        ContainerChanged,
        ItemChanged
    }

    public class ListChangedEventArgs : EventArgs
    {
        public CollectionChangeType ChangeType { get; private set; }

        public List<PyObject>? AddedItems { get; private set; }
        public List<PyObject>? RemovedItems { get; private set; }

        public PyObject? ChangedItem { get; private set; }

        public ListChangedEventArgs(List<PyObject >? addedItems,
            List<PyObject>? removedItems)
        {
            ChangeType = CollectionChangeType.ContainerChanged;

            AddedItems = addedItems;
            RemovedItems = removedItems;
        }

        public ListChangedEventArgs(PyObject changedItem)
        {
            ChangeType = CollectionChangeType.ItemChanged;

            ChangedItem = changedItem;
        }
    }

    public class DictionaryChangedEventArgs : EventArgs
    {
        public CollectionChangeType ChangeType { get; private set; }

        public List<KeyValuePair<PyObject, PyObject>>? AddedItems { get; private set; }
        public List<KeyValuePair<PyObject, PyObject>>? RemovedItems { get; private set; }
        public List<(PyObject key, PyObject oldValue, PyObject newValue)>? ChangedValues { get; private set; }

        public KeyValuePair<PyObject, PyObject>? ChangedItem { get; private set; }

        public DictionaryChangedEventArgs(
            List<KeyValuePair<PyObject, PyObject>>? addedItems,
            List<KeyValuePair<PyObject, PyObject>>? removedItems,
            List<(PyObject key, PyObject oldValue, PyObject newValue)>? changedValues)
        {
            ChangeType = CollectionChangeType.ContainerChanged;

            AddedItems = addedItems;
            RemovedItems = removedItems;
            ChangedValues = changedValues;
        }

        public DictionaryChangedEventArgs(KeyValuePair<PyObject, PyObject>? keyValue)
        {
            ChangeType = CollectionChangeType.ItemChanged;

            ChangedItem = keyValue;
        }
    }
}
