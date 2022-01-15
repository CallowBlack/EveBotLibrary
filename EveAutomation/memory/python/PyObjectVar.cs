using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    internal class PyObjectVar : PyObject
    {
        public ulong Length { get; private set; }

        public PyObjectVar(ProcessMemory process, ulong address) : base(process, address) { }

        public override bool update()
        {
            if (!base.update())
                return false;

            var lengthTemp = Process.ReadUInt64(Address + 0x10);
            if (!lengthTemp.HasValue) return false;

            Length = lengthTemp.Value;
            return true;
        }
    }
}
