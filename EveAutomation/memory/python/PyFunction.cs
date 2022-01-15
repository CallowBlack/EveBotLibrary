using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    internal class PyFunction : PyObject
    {
        public string Name { get; private set; }

        public PyFunction(ProcessMemory process, ulong address) : base(process, address)
        {
        }

        public override bool update()
        {
            if (!base.update())
                return false;

            var strObjPtr = Process.ReadUInt64(Address + 0x38);
            if (strObjPtr == null)
                return false;

            var pyObj = new PyObject(Process, strObjPtr.Value);
            var strObj = PyObjectTypeConverter.ConvertToCorrectType(pyObj) as PyString;
            if (strObj == null)
                return false;

            var strVal = strObj.getValue();
            if (strVal == null)
                return false;

            Name = strVal;
            return true;
        }
    }
}
