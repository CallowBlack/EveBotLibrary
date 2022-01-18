using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyInt : PyObject
    {
        public int Value { get => ReadInt32(Address + 0x10) ?? 0; }
        public PyInt(ulong address) : base(address, 0x18) { }
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
