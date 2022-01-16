using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyFunction : PyObject
    {
        public string Name { get; private set; }

        public PyFunction(ulong address) : base(address) { Name = ""; }

        public override bool update()
        {
            if (!base.update())
                return false;

            var strObjPtr = ProcessMemory.Instance.ReadUInt64(Address + 0x38);
            if (strObjPtr == null)
                return false;

            var strObj = PyObjectPool.Get(strObjPtr.Value) as PyString;
            if (strObj == null)
                return false;

            Name = strObj.Value;
            return true;
        }

        public override string ToString()
        {
            return $"function<0x{Address:X}> {Name}";
        }
    }
}
