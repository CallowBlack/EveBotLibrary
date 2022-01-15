using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    internal class PyType : PyObject
    {
        public string Name { get; private set; }

        public PyType(ProcessMemory process, ulong address) : base(process, address) {}

        public override bool update()
        {
            Kind = PyKind.Type;

            var typePtr = _process.ReadUInt64(Address + 0x8);
            if (typePtr == null)
                return false;

            if (typePtr == Address)
                Kind = PyKind.TypeType;
            else if (!PyObjectPool.IsTypeType((ulong)typePtr))
            {
                if (!PyObjectPool.AddType(_process, typePtr.Value))
                    return false;
                if (!PyObjectPool.IsTypeType((ulong)typePtr))
                    return false;
            }

            var name = _process.ReadPointedString(Address + 0x18, 255);
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
