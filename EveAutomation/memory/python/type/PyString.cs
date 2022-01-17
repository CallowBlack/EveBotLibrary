using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyString : PyObjectVar
    {

        public string Value { get => ReadString(Address + 0x20, (uint) (Length + 1)) ?? ""; }

        public uint Hash { get => ReadUInt32(Address + 0x18) ?? 0; }

        public PyString(ulong address) : base(address, 0x18) 
        {
            _updatePeriod = 0;
            SetSize(0x20 + Length + 1);
        }

        public override string ToString()
        {
            return Value;
        }

    }
}
