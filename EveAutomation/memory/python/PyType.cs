using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    internal class PyType : PyObjectVar
    {
        public string Name { get; private set; }

        public PyType(ProcessMemory process, ulong address) : base(process, address) {}

        public override bool update()
        {
            this.Kind = PyKind.Type;
            if (!base.update())
                return false;

            if (typePtr == Address)
            {
                Type = this;
                this.Kind = PyKind.TypeType;
            }

            var name = Process.ReadPointedString(Address + 0x18, 255);
            if (name == null)
                return false;

            Name = name;
            return true;
        }

        public override string ToString()
        {
            return $"0x{Address:X}: PyType [Name {Name}]{(Kind == PyKind.TypeType ? " TT!" : "" )}";
        }
    }
}
