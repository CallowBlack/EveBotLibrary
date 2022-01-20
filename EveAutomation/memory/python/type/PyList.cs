using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    using ListChangedArgs = CollectionChangedArgs<PyObject>;

    public class PyList : PyCollection, IListChanged
    {
        public PyList(ulong address) : base(address, 0x20) { }

        public event IListChanged.ListChangeHandler? ListChanged;

        protected override bool UpdateItems()
        {
            if (Count == 0)
            {
                if (_items.Count != 0)
                {
                    var rem = new List<PyObject>(_items);
                    _items.Clear();
                    ListChanged?.Invoke(new(this, null, rem, null));
                }
                return true;
            }

            var itemsPtr = ReadUInt64(Address + 0x18);
            if (!itemsPtr.HasValue)
            {
                _items.Clear();
                NotifyValueRemoved();
                return false;
            }

            var content = ReadBytes(itemsPtr.Value, Count * 0x8);
            if (content == null)
            {
                _items.Clear();
                NotifyValueRemoved();
                return false;
            }

            var added = new List<PyObject>();
            var removed = new HashSet<PyObject>(_items);
            _items.Clear();

            var reader = new BinaryReader(new MemoryStream(content));
            for (uint i = 0; i < Count; i++)
            {
                var itemPtr = reader.ReadUInt64();
                var item = PyObjectPool.Get(itemPtr);
                if (item == null) return false;
                _items.Add(item);

                if (removed.Contains(item))
                    removed.Remove(item);
                else
                    added.Add(item);
            }

            if (added.Count == 0 && removed.Count == 0)
                return true;

            foreach (var item in added)
                AddEventHandlers(item);

            foreach (var item in removed)
                RemoveEventHandlers(item);

            if (_isInitialized)
                ListChanged?.Invoke(new(this, added, removed.ToList(), null));

            return true;
        }

        private void OnValueChanged(ValueChangedArgs args)
        {
            var changedArgs = new ListChangedArgs(this, null, null, new() { args.Sender }, args);
            if (!changedArgs.IsLoop)
                ListChanged?.Invoke(changedArgs);
        }

        private void OnValueRemoved(object? sender, EventArgs args)
        {
            if (sender is not PyObject pyObject) return;

            try
            {
                RemoveEventHandlers(pyObject);
                if (_items.Remove(pyObject))
                    ListChanged?.Invoke(new ListChangedArgs(this, null, new() { pyObject }, null));
            }
            catch (InvalidOperationException) { }
        }

        private void AddEventHandlers(PyObject value)
        {
            value.ValueRemoved += OnValueRemoved;
            value.FieldChanged += OnValueChanged;
            if (value is IValueChanged vc)
                vc.ValueChanged += OnValueChanged;
            if (value is IDictionaryChanged dict)
                dict.DictionaryChanged += OnValueChanged;
            else if (value is IListChanged list)
                list.ListChanged += OnValueChanged;
        }

        private void RemoveEventHandlers(PyObject value)
        {
            value.ValueRemoved -= OnValueRemoved;
            value.FieldChanged -= OnValueChanged;
            if (value is IValueChanged vc)
                vc.ValueChanged -= OnValueChanged;
            if (value is IDictionaryChanged dict)
                dict.DictionaryChanged -= OnValueChanged;
            else if (value is IListChanged list)
                list.ListChanged -= OnValueChanged;
        }

        public override string ToString()
        {
            return $"list<0x{Address:X}> {Count} entries";
        }
    }
}
