using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    using DictionaryChangedArgs = CollectionChangedArgs<KeyValuePair<PyObject, PyObject>>;
    using ListChangedArgs = CollectionChangedArgs<PyObject>;

    public interface IValueChanged
    {
        public delegate void ValueChangedHandler(ValueChangedArgs args);
        public event ValueChangedHandler? ValueChanged;
    }

    public interface IFieldChanged
    {
        public delegate void FieldChangedHandler(FieldChangedArgs args);
        public event FieldChangedHandler? FieldChanged;
    }

    public interface IDictionaryChanged
    {   
        public delegate void DictionaryChangedHandler(DictionaryChangedArgs args);
        public event DictionaryChangedHandler? DictionaryChanged;
    }

    public interface IListChanged
    {
        public delegate void ListChangeHandler(ListChangedArgs args);
        public event ListChangeHandler? ListChanged;
    }

    public interface IValueRemoved
    {
        public event EventHandler ValueRemoved;
    }

    public class ValueChangedArgs
    {
        public PyObject Sender { get; private set; }
        public ValueChangedArgs? Child { get; private set; }

        protected HashSet<PyObject> visitedChilds;

        public bool IsLoop { get; private set; }

        public ValueChangedArgs(PyObject sender, ValueChangedArgs? child = null)
        {
            Sender = sender;
            Child = child;
            visitedChilds = child?.visitedChilds ?? new();
            IsLoop = visitedChilds.Contains(sender);
            visitedChilds.Add(sender);
        }
    }

    public class FieldChangedArgs : ValueChangedArgs
    {
        public string Name { get; private set; }

        public FieldChangedArgs(PyObject sender, string name, ValueChangedArgs? child = null) : base(sender, child)
        {
            Name = name;
        }
    }

    public class CollectionChangedArgs<T> : ValueChangedArgs
    {
        public List<T>? AddedItems { get; private set; }
        public List<T>? RemovedItems { get; private set; }
        public List<T>? ChangedItems { get; private set; }

        public CollectionChangedArgs(PyObject sender, List<T>? addedItems,
            List<T>? removedItems, List<T>? changedItems, ValueChangedArgs? callerArgs = null) : base(sender, callerArgs)
        {
            AddedItems = addedItems;
            RemovedItems = removedItems;
            ChangedItems = changedItems;
        }
    }
}
