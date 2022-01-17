using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    // https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/object.h#L324
    public class PyType : PyObjectVar
    {

        // https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/methodobject.h#L37
        public class MethodDef { 
            public ulong Address { get; private set; }
            public string Name { get => ProcessMemory.Instance.ReadPointedString(Address, 255) ?? ""; }
            public ulong FunctionPtr { get => ProcessMemory.Instance.ReadUInt64(Address + 0x8) ?? 0; }
        
            public MethodDef(ulong address)
            {
                Address = address;
            }
        }

        // https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/structmember.h#L36
        public class MemberDef
        {
            public enum MemberType
            {
                Short, Int, Long, Float, Double, String, Object, Char, Byte, UByte, 
                UShort, UInt, ULong, StringInPlace, Bool, ObjectEx = 16, LongLong, ULongLong,
                Size_t, Undefined
            }

            public ulong Address { get; private set; }
            public string Name { get => ProcessMemory.Instance.ReadPointedString(Address, 255) ?? "";  }
            public MemberType Type { get => (MemberType)(ProcessMemory.Instance.ReadUInt64(Address + 0x8) ?? (int)MemberType.Undefined); }
            public ulong Offset { get => ProcessMemory.Instance.ReadUInt64(Address + 0x10) ?? 0; }
            public MemberDef(ulong address)
            {
                Address = address;
            }
        }

        // https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/object.h#L566
        [Flags] public enum TypeFlags
        {
            HaveGetCharBuffer = 1 << 0,
            HaveSequenceIn = 1 << 1,
            HaveInplaceOps = 1 << 3,
            HaveCheckTypes = 1 << 4,
            HaveRichCompare = 1 << 5,
            HaveWeakRefs = 1 << 6,
            HaveIter = 1 << 7,
            HaveClass = 1 << 8,
            HeapType = 1 << 9,
            BaseType = 1 << 10,
            Ready = 1 << 12,
            Readiying = 1 << 13,
            HaveGC = 1 << 14,
            HaveIndex = 1 << 17,
            HaveVersionTag = 1 << 18,
            ValidVersionTag = 1 << 19,
            IsAbstract = 1 << 20,
            HaveNewBuffer = 1 << 21,
            SubclassOfInt = 1 << 23,
            SubclassOfLong = 1 << 24,
            SubclassOfList = 1 << 25,
            SubclassOfTuple = 1 << 26,
            SubclassOfString = 1 << 27,
            SubclassOfUnicode = 1 << 28,
            SubclassOfDict = 1 << 29,
            SubclassOfBaseExc = 1 << 30,
            SubclassOfType = 1 << 31
        }

        public static PyType EmptyType = new PyType(0);

        public string Name { get => ProcessMemory.Instance.ReadPointedString(Address + 0x18, 255) ?? ""; }

        public TypeFlags Flags { get => (TypeFlags)(ProcessMemory.Instance.ReadUInt64(Address + 0xA8) ?? 0); }

        public IEnumerable<MethodDef> Methods { 
            get
            {
                var methodsPtr = ProcessMemory.Instance.ReadUInt64(Address + 0xE8);
                if (methodsPtr.HasValue && methodsPtr.Value != 0)
                {
                    ulong offset = 0;
                    var startPtr = ProcessMemory.Instance.ReadUInt64(methodsPtr.Value + offset);
                    while (startPtr.HasValue && startPtr.Value != 0)
                    {
                        yield return new MethodDef(methodsPtr.Value + offset);
                        offset += 0x20;
                        startPtr = ProcessMemory.Instance.ReadUInt64(methodsPtr.Value + offset);
                    }
                }

            } 
        }

        public IEnumerable<MemberDef> Members
        {
            get
            {
                var membersPtr = ProcessMemory.Instance.ReadUInt64(Address + 0xF0);
                if (membersPtr.HasValue && membersPtr.Value != 0)
                {
                    ulong offset = 0;
                    var startPtr = ProcessMemory.Instance.ReadUInt64(membersPtr.Value + offset);
                    while (startPtr.HasValue && startPtr.Value != 0)
                    {
                        yield return new MemberDef(membersPtr.Value + offset);
                        offset += 0x28;
                        startPtr = ProcessMemory.Instance.ReadUInt64(membersPtr.Value + offset);
                    }
                }

            }
        }

        public PyType? BaseType { get => PyObjectPool.Get(ProcessMemory.Instance.ReadUInt64(Address + 0x100) ?? 0) as PyType; }

        public new PyDict? Dict { get => PyObjectPool.Get(ProcessMemory.Instance.ReadUInt64(Address + 0x108) ?? 0) as PyDict; }

        public ulong DictOffset { get => ProcessMemory.Instance.ReadUInt64(Address + 0x120) ?? 0; }

        public bool IsReady { get => Flags.HasFlag(TypeFlags.Ready); }

        public bool IsClass { get => Flags.HasFlag(TypeFlags.HeapType); }

        public bool IsTypeType { get => this.Type == this; }

        public PyType(ulong address) : base(address) {}

        public override bool update()
        {
            if (!base.update())
                return false;

            if (typePtr == Address)
                Type = this;

            if (Name.Length == 0)
                return false;

            return true;
        }

        public override string ToString()
        {
            return $"type<0x{Address:X}> {Name}";
        }
    }
}
