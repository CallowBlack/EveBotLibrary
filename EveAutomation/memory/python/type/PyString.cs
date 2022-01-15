using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyString : PyObjectVar
    {
        public PyString(ProcessMemory process, ulong address) : base(process, address)
        {
        }

        public override bool update()
        {
            if (!base.update())
                return false;

            var value = getValue();
            if (value == null)
                return false;

            return true;
        }

        public string? getValue()
        {
            return Process.ReadString(Address + 0x20, (uint)(Length + 1));
        }

    }
}
