using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyClass : PyObject
    {
        public PyTuple? Bases
        {
            get {
                var ptr = ReadUInt64(Address + 0x10);
                if (!ptr.HasValue)
                    return null;

                var tuple = PyObjectPool.Get(ptr.Value) as PyTuple;
                return tuple;
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

        public PyString? Name
        {
            get
            {
                var ptr = ReadUInt64(Address + 0x20);
                if (!ptr.HasValue)
                    return null;

                var name = PyObjectPool.Get(ptr.Value) as PyString;
                return name;
            }
        }

        public PyClass(ulong address) : base(address, 0x28)
        {
        }
    }
}
