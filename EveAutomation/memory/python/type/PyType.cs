using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyType : PyObjectVar
    {
        public static PyType EmptyType = new PyType(0);

        public string Name { get; private set; }

        public bool IsTypeType { get => this.Type == this; }

        public PyType(ulong address) : base(address) {}

        public override bool update()
        {
            if (!base.update())
                return false;

            if (typePtr == Address)
                Type = this;

            var name = ProcessMemory.Instance.ReadPointedString(Address + 0x18, 255);
            if (name == null)
                return false;

            Name = name;
            return true;
        }

        public override string ToString()
        {
            return $"0x{Address:X}: PyType [Name {Name}]{(IsTypeType ? " TT!" : "" )}";
            return $"type<0x{Address:X}> {Name}";
        }
    }
}
