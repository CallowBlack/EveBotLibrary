using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyString : PyObjectVar
    {

        public string Value { get => ProcessMemory.Instance.ReadString(Address + 0x20, (uint) (Length + 1)) ?? ""; }
        public uint Hash { get => ProcessMemory.Instance.ReadUInt32(Address + 0x18) ?? 0; }

        public PyString(ulong address) : base(address) { }

        public override string ToString()
        {
            return Value;
        }

    }
}
