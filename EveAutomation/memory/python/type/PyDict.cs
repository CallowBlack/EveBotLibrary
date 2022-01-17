using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyDict : PyObject
    {
        public ulong Count { 
            get => ReadUInt64(Address + 0x18) ?? 0; 
        }
        
        public IEnumerable<(PyObject key, PyObject value)> Items 
        { 
            get => GetItems(); 
        }

        private ulong Mask { 
            get => ReadUInt64(Address + 0x20) ?? 0; 
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

        public PyDict(ulong address) : base(address, 0x30) { }

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

        private IEnumerable<(PyObject key, PyObject value)> GetItems()
        {
            var tableAddr = ReadUInt64(Address + 0x28);
            if (!tableAddr.HasValue)
                throw new MemberAccessException($"Failed gain access to dict table address. Dict address: {Address:X}");

            var content = ReadBytes(tableAddr.Value, Mask * 0x18);
            if (content == null)
                yield break;

            var reader = new BinaryReader(new MemoryStream(content));
            for (uint i = 0, u = 0; i <= Mask && u < Count; i++)
            {
                var entry = ReadEntry(ref reader);
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
