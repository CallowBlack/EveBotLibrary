﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyObject
    {
        public ulong Address { get; } 
        public PyType Type { get; protected set; }
        public bool IsValid { get; protected set; }

        protected ulong typePtr = 0;

        public PyObject(ulong address) 
        {
            Address = address;
            Type = PyType.EmptyType;
            IsValid = update();
        }
        
        public virtual bool update()
        {
            var typePtr = ProcessMemory.Instance.ReadUInt64(Address + 0x8);
            if (typePtr == null)
                return false;

            this.typePtr = typePtr.Value;

            if (typePtr == Address)
                return true;

            var pyObj = PyObjectPool.Get(typePtr.Value) as PyType;
            if (pyObj == null)
                return false;

            Type = pyObj;
            return true;
        }

        public override string ToString()
        {
            var header = $"0x{Address:X}: PyObject ";
            if (Type == null)
                return header + "Non valid";
            if (Type.Name == "NoneType")
                return "None";

            return header + $"[Type {Type.Name}]";
            return $"object<0x{Address:X}> with type {Type.Name}";
        }
    }
}
