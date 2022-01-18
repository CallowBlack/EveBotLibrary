using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyTuple : PyObjectVar
    {
        public IEnumerable<PyObject> Items {
            get
            {
                for (uint i = 0; i < Length; i++)
                {
                    var ptr = ReadUInt64(Address + 0x18 + i * 0x8);
                    if (!ptr.HasValue) continue;

                    var obj = PyObjectPool.Get(ptr.Value);
                    if (obj == null) continue;

                    yield return obj;
                }
            }
        }

        public PyTuple(ulong address) : base(address, 0x18)
        {
            _updatePeriod = 0;
            SetSize(0x18 + Length * 0x8);
        }
    }
}
