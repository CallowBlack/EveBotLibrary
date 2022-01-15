using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyObject
    {
        public ulong Address { get; } 
        public PyType? Type { get; protected set; }
        public PyKind Kind { get; protected set; }
        public bool IsValid { get; protected set; }
        public ProcessMemory Process { get; }

        protected ulong typePtr = 0;

        public PyObject(ProcessMemory process, ulong address) 
        {
            Process = process;
            Address = address;
            Kind = PyKind.Object;

            IsValid = update();
        }
        
        public virtual bool update()
        {
            var typePtr = Process.ReadUInt64(Address + 0x8);
            if (typePtr == null)
                return false;

            this.typePtr = typePtr.Value;

            if (typePtr == Address)
                return true;

            var pyObj = PyObjectPool.GetTypeByAddress(typePtr.Value);
            if (pyObj == null)
            {
                if (!PyObjectPool.AddType(Process, typePtr.Value))
                    return false;
                pyObj = PyObjectPool.GetTypeByAddress(typePtr.Value);
                if (pyObj == null)
                    return false;
            }

            if (pyObj.Name == "String")
                Console.WriteLine("Found string!");

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
