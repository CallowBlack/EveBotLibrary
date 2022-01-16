using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyInt : PyObject
    {
        public ulong Value { get => ProcessMemory.Instance.ReadUInt64(Address + 0x10) ?? 0; }
        public PyInt(ulong address) : base(address) { }
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
