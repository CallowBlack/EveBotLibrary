using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    using ListChangedArgs = CollectionChangedArgs<PyObject>;

    public class PyList : PyObjectVar, IListChanged
    {

        public List<PyObject> Items
        {
            get
            {
                UpdateItems();
                return _items;
            }
        }

        private List<PyObject> _items;

        public PyList(ulong address) : base(address, 0x20) { }

        public event IListChanged.ListChangeHandler? ListChanged;

        protected override bool UpdateObject(bool deep, HashSet<CachebleObject>? visited = null)
        {
            if (!base.UpdateObject(deep, visited))
                return false;

            if (!UpdateItems())
                return false;

            foreach (var item in _items)
            {
                item.Update(true, deep, visited);
            }
            return true;
        }

        private bool UpdateItems()
        {
            var isInitialize = _items == null;
            if (isInitialize)
                _items = new();

            var itemsPtr = ReadUInt64(Address + 0x18);
            if (!itemsPtr.HasValue)
            {
                _items.Clear();
                NotifyValueRemoved();
                return false;
            }

            var content = ReadBytes(itemsPtr.Value, Length * 0x8);
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
            for (uint i = 0; i < Length; i++)
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

            if (!isInitialize)
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
            return $"list<0x{Address:X}> {Length} entries";
        }
    }
}
