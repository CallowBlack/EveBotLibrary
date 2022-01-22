using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyInstance : PyObject
    {
        public PyClass? Class
        {
            get
            {
                var ptr = ReadUInt64(Address + 0x10);
                if (!ptr.HasValue)
                    return null;

                var cls = PyObjectPool.Get(ptr.Value) as PyClass;
                return cls;
            }
        }

        public PyDict? Content
        {
            get
            {
                var ptr = ReadUInt64(Address + 0x18);
                if (!ptr.HasValue)
                    return null;

                var dict = PyObjectPool.Get(ptr.Value) as PyDict;
                return dict;
            }
        }

        public PyInstance(ulong address) : base(address, 0x20)
        {
        }
    }
}
