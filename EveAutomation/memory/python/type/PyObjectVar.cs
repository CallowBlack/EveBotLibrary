using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyObjectVar : PyObject
    {
        public ulong Length { get => ProcessMemory.Instance.ReadUInt64(Address + 0x10) ?? 0; }

        public PyObjectVar(ulong address) : base(address) { }

    }
}
