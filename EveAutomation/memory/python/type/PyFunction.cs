using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyFunction : PyObject
    {
        public string Name { 
            get
            {
                if (_nameObj != null)
                    return _nameObj.Value;

                var strObjPtr = ReadUInt64(Address + 0x38);
                if (strObjPtr == null)
                    return "";

                var strObj = PyObjectPool.Get(strObjPtr.Value) as PyString;
                if (strObj == null)
                    return "";

                _nameObj = strObj;
                return strObj.Value;
            } 
        }

        private PyString? _nameObj;

        public PyFunction(ulong address) : base(address, 0x40) { }

        public override string ToString()
        {
            return $"function<0x{Address:X}> {Name}";
        }
    }
}
