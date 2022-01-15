using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    internal class PyUnicode : PyObject
    {
        public string Value { 
            get 
            {
                var unicodeStringLength = ProcessMemory.Instance.ReadUInt64(Address + 0x10);
                if (!unicodeStringLength.HasValue) return "";

                if (unicodeStringLength > 0x1000 )
                    throw new Exception($"Unicode string length to long. Addr: 0x{Address:X}; Length: {unicodeStringLength}");

                var unicodeStringPtr = ProcessMemory.Instance.ReadUInt64(Address + 0x18);
                if (!unicodeStringPtr.HasValue) return "";

                var byteLength = unicodeStringLength.Value * 2;
                var rawContent = ProcessMemory.Instance.ReadBytes(unicodeStringPtr.Value, byteLength);
                if (rawContent == null || rawContent.Length < (int)byteLength)
                    throw new Exception($"Failed to read unicode string content. Obj: 0x{Address:X}; Content: 0x{unicodeStringPtr:X}; " +
                        $"Length: {unicodeStringLength}");

                return Encoding.Unicode.GetString(rawContent, 0, (int)byteLength);

            }
        }

        public override string ToString()
        {
            return Value;
        }

        public PyUnicode(ulong address) : base(address) { }
    }
}
