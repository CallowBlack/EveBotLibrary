using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyObject : CachebleObject
    {     
        public PyType Type {
            get {
                if (_type != null)
                    return _type;

                var typePtr = ReadUInt64(Address + 0x8);
                if (typePtr == null)
                    return PyType.EmptyType;

                this.typePtr = typePtr.Value;
                if (typePtr == Address)
                {
                    _type = PyType.EmptyType;
                    return _type;
                }

                if (PyObjectPool.Get(typePtr.Value) is not PyType pyObj)
                    return PyType.EmptyType;

                _type = pyObj;
                return pyObj;
            }

            protected set
            {
                _type = value;
            }
        }

        private PyType? _type;
        protected ulong typePtr = 0;

        public PyDict? Dict {
            get
            {
                var dictOffset = Type.DictOffset;
                if (dictOffset == 0)
                    return null;
                var dictPtr = ReadUInt64(Address + dictOffset);
                return PyObjectPool.Get(dictPtr ?? 0) as PyDict;
            } 
        }

        protected PyObject(ulong address, ulong size) : base(address, size) { }

        public PyObject(ulong address) : base(address, 0x10) { }

        public PyObject? GetMemberObject(PyType.MemberDef member)
        {
            var valuePtr = ReadUInt64(Address + member.Offset);
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
