using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyBool : PyObject
    {
        public bool Value { get => ReadUInt64(Address + 0x10) != 0; }
        public PyBool(ulong address) : base(address, 0x18) { }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
