using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    internal class PyObject
    {
        public ulong Address { get; } 
        public PyType? Type { get; protected set; }
        public PyKind Kind { get; protected set; }
        public bool IsValid { get; protected set; }

        protected readonly ProcessMemory _process;

        public PyObject(ProcessMemory process, ulong address) 
        {
            this._process = process;
            Address = address;
            Kind = PyKind.Object;

            IsValid = update();
        }
        
        public virtual bool update()
        {
            var typePtr = _process.ReadUInt64(Address + 0x8);
            if (typePtr == null)
                return false;

            if (!PyObjectPool.ContainsType((ulong)typePtr))
                return false;

            var pyObj = PyObjectPool.GetTypeByAddress((ulong)typePtr);
            if (pyObj == null)
                return false;

            if (pyObj.Kind != PyKind.Type)
                return false;

            Type = pyObj;

            return true;
        }

        public override string ToString()
        {
            var header = $"0x{Address:X}: PyObject ";
            if (Type == null)
                return header + "Non valid";

            return header + $"[Type {Type.Name}]";
        }
    }
}
