using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyTuple : PyCollection
    {
        protected override bool UpdateItems()
        {
            if (_isInitialized) return true;

            var content = ReadBytes(Address + 0x18, Count * 0x8);
            if (content == null)
                return false;

            var reader = new BinaryReader(new MemoryStream(content));
            for (uint i = 0; i < Count; i++)
            {
                var ptr = reader.ReadUInt64();
                var obj = PyObjectPool.Get(ptr);
                if (obj == null)
                    return false;
                _items.Add(obj);
            }
            return true;
        }

        public PyTuple(ulong address) : base(address, 0x18)
        {
            _updatePeriod = 0;
        }
    }
}
