using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyDict : PyObject
    {
        public ulong Count { get => ProcessMemory.Instance.ReadUInt64(Address + 0x18) ?? 0; }
        public IEnumerable<(PyObject key, PyObject value)> Items { get => GetItems(); }

        private ulong Mask { get => ProcessMemory.Instance.ReadUInt64(Address + 0x20) ?? 0; }
        
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

            public PyDictEntry() {
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

        public PyObject? Get(string key)
        {
            foreach ((PyObject keyObj, PyObject valueObj) in Items)
            {
                var strKeyObj = keyObj as PyString;
                if (strKeyObj == null)
                    continue;

                if (strKeyObj.Value == key)
                    return valueObj;
            }
            return null;
        }

        public PyDict(ulong address) : base(address) { }

        private PyDictEntry ReadEntry(ulong address)
        {
            var hash = ProcessMemory.Instance.ReadUInt64(address);
            if (!hash.HasValue) return new PyDictEntry();

            var keyPtr = ProcessMemory.Instance.ReadUInt64(address + 0x8);
            if (!keyPtr.HasValue || keyPtr == 0) return new PyDictEntry();

            var objPtr = ProcessMemory.Instance.ReadUInt64(address + 0x10);
            if (!objPtr.HasValue || objPtr == 0) return new PyDictEntry();

            var keyObject = PyObjectPool.Get(keyPtr.Value);
            if (keyObject == null) return new PyDictEntry();

            return new(keyObject, objPtr.Value, hash.Value);

        }

        private IEnumerable<(PyObject key, PyObject value)> GetItems()
        {
            var tableAddr = ProcessMemory.Instance.ReadUInt64(Address + 0x28);
            if (!tableAddr.HasValue)
                throw new MemberAccessException($"Failed gain access to dict table address. Dict address: {Address:X}");

            for (uint i = 0, u = 0 ; i <= Mask && u < Count; i++)
            {
                var currAddr = tableAddr.Value + i * 0x18;
                var entry = ReadEntry(currAddr);
                if (entry.GetState() == PyDictEntry.State.Active && entry.key != null) // Sometimes I hate VS2019
                {
                    var value = PyObjectPool.Get(entry.valuePtr);
                    if (value == null)
                        continue;
                    
                    u++;
                    yield return (entry.key, value);
                }
            }
        }

        public override string ToString()
        {
            return $"dict<0x{Address:X}> {Count} entries";
        }

    }
}
