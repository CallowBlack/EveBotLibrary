using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyList : PyCollection, INotifyListChanged
    {
        public PyList(ulong address) : base(address, 0x20) { }

        public event EventHandler<ListChangedEventArgs>? ListChanged;

        protected override bool UpdateItems()
        {
            if (Count == 0)
            {
                if (_items.Count == 0)
                    return true;

                var clone = new List<PyObject>(_items);
                _items.Clear();
                ListChanged?.Invoke(this, new(null, clone));

                return true;
            }

            var itemsPtr = ReadUInt64(Address + 0x18);
            if (!itemsPtr.HasValue)
            {
                _items.Clear();
                NotifyObjectRemoved();
                return false;
            }

            var content = ReadBytes(itemsPtr.Value, Count * 0x8);
            if (content == null)
            {
                _items.Clear();
                NotifyObjectRemoved();
                return false;
            }

            var addedItems = new List<PyObject>();

            var oldItems = new Dictionary<PyObject, uint>(_items.Count);
            foreach (var item in _items)
            {
                if (!oldItems.ContainsKey(item))
                    oldItems[item] = 0;
                oldItems[item]++;
            }

            _items.Clear();

            var reader = new BinaryReader(new MemoryStream(content));
            for (uint i = 0; i < Count; i++)
            {
                var itemPtr = reader.ReadUInt64();
                var item = PyObjectPool.Get(itemPtr);
                if (item == null) return false;
                _items.Add(item);

                if (oldItems.ContainsKey(item))
                {
                    if (oldItems[item]-- == 0)
                        oldItems.Remove(item);
                }
                else
                    addedItems.Add(item);
            }


            if (addedItems.Count == 0 && oldItems.Count == 0)
                return true;

            var removedItems = new List<PyObject>(oldItems.Count);
            foreach (var entry in oldItems)
            {
                for (var i = 0; i < entry.Value; i++)
                    removedItems.Add(entry.Key);
            }

            foreach (var item in addedItems)
                AddEventHandlers(item);

            foreach (var item in removedItems)
                RemoveEventHandlers(item);

            if (_isInitialized)
            {
                ListChanged?.Invoke(this, new(
                    addedItems.Count == 0 ? null : addedItems, 
                    removedItems.Count == 0 ? null : removedItems
                    )); 
            }
                

            return true;
        }

        private void OnValueChanged(object? sender, ValueChangedEventArgs args)
        {
            if (sender is not PyObject pyObject)
                return;

            ListChanged?.Invoke(this , new(pyObject));
        }

        private void OnValueRemoved(object? sender, EventArgs args)
        {
            if (sender is not PyObject pyObject) return;

            try
            {
                RemoveEventHandlers(pyObject);
                if (_items.Remove(pyObject))
                    ListChanged?.Invoke(this, new (null, new() { pyObject }));
            }
            catch (InvalidOperationException) { }
        }

        private void AddEventHandlers(PyObject value)
        {
            value.ObjectRemoved += OnValueRemoved;
            if (value is INotifyValueChanged vc)
                vc.ValueChanged += OnValueChanged;
        }

        private void RemoveEventHandlers(PyObject value)
        {
            value.ObjectRemoved -= OnValueRemoved;
            if (value is INotifyValueChanged vc)
                vc.ValueChanged -= OnValueChanged;
        }

        public override string ToString()
        {
            return $"list<0x{Address:X}> {Count} entries";
        }
    }
}
