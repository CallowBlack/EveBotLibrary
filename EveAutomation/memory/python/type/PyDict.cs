using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyDict : PyObjectVar, INotifyDictionaryChanged
    {
        public Dictionary<PyObject, PyObject> Items 
        { 
            get
            {
                UpdateDictionary();
                return _items;
            }
        }
        private Dictionary<PyObject, PyObject> _items;

        private ulong Mask { 
            get => ReadUInt64(Address + 0x20) ?? 0; 
        }

        public event EventHandler<DictionaryChangedEventArgs>? DictionaryChanged;

        public PyDict(ulong address) : base(address, 0x30) { }

        public PyObject? Get(string key)
        {
            foreach (var entry in Items)
            {
                var strKeyObj = entry.Key as PyString;
                if (strKeyObj == null)
                    continue;

                if (strKeyObj.Value == key)
                    return entry.Value;
            }
            return null;
        }

        private struct PyDictEntry
        {
            public ulong hash;
            public PyObject? key;
            public ulong valuePtr;

            public enum State { Unused, Active, Dummy };

            public PyDictEntry(PyObject? key, ulong valuePtr, ulong hash)
            {
                this.key = key;
                this.valuePtr = valuePtr;
                this.hash = hash;
            }

            public PyDictEntry()
            {
                this.key = null;
                this.valuePtr = 0;
                this.hash = 0;
            }

            public State GetState()
            {
                if (key == null)
                    return State.Unused;

                var keyAsStr = key as PyString;
                if (keyAsStr != null && keyAsStr.Value == "<dummy>")
                    return State.Dummy;

                return State.Active;
            }
        }

        private PyDictEntry ReadEntry(ref BinaryReader reader)
        {
            var hash = reader.ReadUInt64();
            var keyPtr = reader.ReadUInt64();
            var objPtr = reader.ReadUInt64();

            if (keyPtr == 0 || objPtr == 0) return new PyDictEntry();

            var keyObject = PyObjectPool.Get(keyPtr);
            if (keyObject == null) return new PyDictEntry();

            return new(keyObject, objPtr, hash);
        }

        private bool UpdateDictionary()
        {
            var isInitialized = _items != null;
            if (!isInitialized)
                _items = new();

            var tableAddr = ReadUInt64(Address + 0x28);
            if (!tableAddr.HasValue)
                throw new MemberAccessException($"Failed gain access to dict table address. Dict address: {Address:X}");

            var content = ReadBytes(tableAddr.Value, (Mask + 1) * 0x18);
            if (content == null)
            {
                _items.Clear();
                NotifyObjectRemoved();
                return false;
            }

            var addedItems = new List<KeyValuePair<PyObject, PyObject>>();
            var removedItems = new List<KeyValuePair<PyObject, PyObject>>();
            var updatedItems = new List<(PyObject key, PyObject oldValue, PyObject newValue)>();

            var oldKeys = new HashSet<PyObject>(_items.Keys);
            var reader = new BinaryReader(new MemoryStream(content));
            for (uint i = 0, u = 0; i <= Mask && u < Length; i++)
            {
                var entry = ReadEntry(ref reader);
                if (entry.GetState() != PyDictEntry.State.Active || entry.key == null) // Sometimes I hate VS2019
                    continue;

                var contains = _items.ContainsKey(entry.key);
                if (contains)
                    oldKeys.Remove(entry.key);

                if (contains && _items[entry.key].Address == entry.valuePtr)
                    continue;

                var newValue = PyObjectPool.Get(entry.valuePtr);
                if (newValue == null)
                    continue;

                if (contains)
                    updatedItems.Add((entry.key, _items[entry.key], newValue));
                else
                    addedItems.Add(new(entry.key, newValue));
                    
                u++;
            }

            if (addedItems.Count == 0 && removedItems.Count == 0 && updatedItems.Count == 0)
                return true;

            foreach (var removedItem in oldKeys)
            {
                var key = removedItem;
                var value = _items[removedItem];
                RemoveEventHandlers(key, value);

                removedItems.Add(new KeyValuePair<PyObject, PyObject>(key, value));

                _items.Remove(removedItem);
            }

            foreach (var updatedItem in updatedItems)
            {
                RemoveEventHandlers(null, updatedItem.oldValue);
                AddEventHandlers(null, updatedItem.newValue);

                _items[updatedItem.key] = updatedItem.newValue;
            }

            foreach (var addedItem in addedItems)
            {
                _items[addedItem.Key] = addedItem.Value;
                AddEventHandlers(addedItem.Key, addedItem.Value);
            }
            
            if (isInitialized)
                DictionaryChanged?.Invoke(this, new (
                    addedItems.Count == 0 ? null : addedItems, 
                    removedItems.Count == 0 ? null : removedItems,
                    updatedItems.Count == 0 ? null : updatedItems));

            return true;
        }

        public override string ToString()
        {
            return $"dict<0x{Address:X}> {Length} entries";
        }

        protected override bool UpdateObject(bool deep, HashSet<CachebleObject>? visited = null)
        {
            if (!base.UpdateObject(deep, visited))
                return false;

            if (!UpdateDictionary())
                return false;

            if (!deep)
                return true;
            
            foreach (var item in _items)
            {
                item.Key.Update(true, deep, visited);
                item.Value.Update(true, deep, visited);
            }

            return true;
        }

        private void OnKeyRemoved(object? sender, EventArgs args)
        {
            if (sender is not PyObject pyObject)
                return;

            if (!_items.ContainsKey(pyObject))
                return;

            UpdateDictionary();
        }

        private void OnValueRemoved(object? sender, EventArgs args)
        {
            if (sender is not PyObject pyObject)
                return;

            UpdateDictionary();
        }

        private void OnValueChanged(object? sender, ValueChangedEventArgs args)
        {
            if (sender is not PyObject value)
                return;

            try
            {
                var keyValue = _items.First(item => item.Value == value);
                DictionaryChanged?.Invoke(this, new (keyValue));
            }
            catch (InvalidOperationException) { }
        }

        private void AddEventHandlers(PyObject? key, PyObject value)
        {
            if (key is not null)
                key.ObjectRemoved += OnKeyRemoved;
            value.ObjectRemoved += OnValueRemoved;

            if (value is INotifyValueChanged vc)
                vc.ValueChanged += OnValueChanged;
        }

        private void RemoveEventHandlers(PyObject? key, PyObject value)
        {
            if (key is not null)
                key.ObjectRemoved -= OnKeyRemoved;
            value.ObjectRemoved -= OnValueRemoved;

            if (value is INotifyValueChanged vc)
                vc.ValueChanged -= OnValueChanged;
        }

    }
}
