using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyBool : PyObject
    {
        public bool Value { get => ProcessMemory.Instance.ReadUInt64(Address + 0x10) != 0; }
        public PyBool(ulong address) : base(address) { }
    }
}
