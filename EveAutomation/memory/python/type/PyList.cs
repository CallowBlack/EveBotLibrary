using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyList : PyObjectVar
    {
        public IEnumerable<PyObject> Items
        {
            get
            {
                var itemsPtr = ProcessMemory.Instance.ReadUInt64(Address + 0x18);
                if (!itemsPtr.HasValue) yield break;

                for (uint i = 0; i < Length; i++)
                {
                    ulong itemPtrAddress = itemsPtr.Value + i * 8;
                    var itemPtr = ProcessMemory.Instance.ReadUInt64(itemPtrAddress);
                    if (!itemPtr.HasValue) yield break;
                    
                    var item = PyObjectPool.Get(itemPtr.Value);
                    if (item == null) yield break;
                    yield return item;
                }
            }
        }
        public PyList(ulong address) : base(address, 0x20) { }

        public override string ToString()
        {
            return $"list<0x{Address:X}> {Length} entries";
        }
    }
}
