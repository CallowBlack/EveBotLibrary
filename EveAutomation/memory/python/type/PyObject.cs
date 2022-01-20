﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EveAutomation.memory.python.IValueChanged;

namespace EveAutomation.memory.python.type
{
    public class PyObject : CachebleObject, IFieldChanged, IValueRemoved
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

        public event EventHandler? ValueRemoved;
        public event IFieldChanged.FieldChangedHandler? FieldChanged;

        public PyDict? Dict {
            get
            {
                if (_dict != null)
                    return _dict;

                var dictOffset = Type.DictOffset;
                if (dictOffset == 0)
                    return null;

                var dictPtr = ReadUInt64(Address + dictOffset);
                _dict = PyObjectPool.Get(dictPtr ?? 0) as PyDict;
                if (_dict != null)
                    _dict.DictionaryChanged += OnDictChanged;
                return _dict;
            } 
        }
        private PyDict? _dict; 

        protected PyObject(ulong address, ulong size) : base(address, size) { }

        public PyObject(ulong address) : base(address, 0x10) { }

        public PyObject? GetMemberObject(PyType.MemberDef member)
        {
            var valuePtr = ReadUInt64(Address + member.Offset);
            return PyObjectPool.Get(valuePtr ?? 0);
        }

        protected override bool UpdateObject(bool deep, HashSet<CachebleObject>? visited = null)
        {
            if (!base.UpdateObject(deep, visited))
                return false;

            if (typePtr != 0 && typePtr != ReadUInt64(Address + 0x8))
            {
                NotifyValueRemoved();
                return false;
            }

            if (_dict != null && Type.DictOffset != 0 && _dict.Address != ReadUInt64(Address + Type.DictOffset))
            {
                NotifyValueRemoved();
                return false;
            }

            if (deep && _dict != null)
                _dict.Update(true, deep, visited);
            
            return true;
        }

        protected void OnDictChanged(CollectionChangedArgs<KeyValuePair<PyObject, PyObject>> args)
        {
            if (args.ChangedItems == null)
                return;

            foreach(var updatedItem in args.ChangedItems)
            {
                if (updatedItem.Key is PyString keyStr)
                {
                    var nextArgs = new FieldChangedArgs(this, keyStr.Value, args);
                    if (!nextArgs.IsLoop)
                        FieldChanged?.Invoke(nextArgs);
                }
            }
        }

        protected void NotifyValueRemoved()
        {
            ValueRemoved?.Invoke(this, new EventArgs());
        }

        public override string ToString()
        {
            if (Type.Name == "NoneType")
                return "None";

            return $"object<0x{Address:X}> with type {Type.Name}";
        }

    }
}
