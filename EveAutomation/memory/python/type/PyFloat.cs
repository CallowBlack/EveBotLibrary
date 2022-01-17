using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyFloat : PyObject
    {
        public double Value {
            get {
                var bytes = ProcessMemory.Instance.ReadBytes(Address + 0x10, 10);
                if (bytes == null)
                    return 0;

                return BitConverter.ToDouble(bytes);
            } 
        }

        public PyFloat(ulong address) : base(address) { }

        public override string ToString()
        {
            var strVal = Value.ToString();
            if (!strVal.Contains('.'))
                strVal += ".0";
            return strVal;
        }
    }
}
