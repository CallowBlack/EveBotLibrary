﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyObject
    {
        public ulong Address { get; } 
        public PyType Type { get; protected set; }
        public bool IsValid { get; protected set; }
        public PyDict? Dict {
            get
            {
                var dictOffset = Type.DictOffset;
                if (dictOffset == 0)
                    return null;
                var dictPtr = ProcessMemory.Instance.ReadUInt64(Address + dictOffset);
                return PyObjectPool.Get(dictPtr ?? 0) as PyDict;
            } 
        }

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

        public PyObject? GetMemberObject(PyType.MemberDef member)
        {
            var valuePtr = ProcessMemory.Instance.ReadUInt64(Address + member.Offset);
            return PyObjectPool.Get(valuePtr ?? 0);
        }

        public override string ToString()
        {
            if (Type.Name == "NoneType")
                return "None";

            return $"object<0x{Address:X}> with type {Type.Name}";
        }
    }
}
