using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyUnicode : PyObjectVar
    {
        public string Value { 
            get 
            {
                UpdateValue();
                return _value ?? "";
            }
        }

        private string? _value;
        public PyUnicode(ulong address) : base(address, 0x20) { }

        protected override bool UpdateObject(bool deep, HashSet<CachebleObject>? visited = null)
        {
            if (!base.UpdateObject(deep, visited))
                return false;

            if (!UpdateValue())
                return false;
            return true;
        }

        private bool UpdateValue()
        {
            if (Length > 0x1000)
                throw new Exception($"Unicode string length to long. Addr: 0x{Address:X}; Length: {Length}");

            var unicodeStringPtr = ReadUInt64(Address + 0x18);
            if (!unicodeStringPtr.HasValue)
            {
                NotifyValueRemoved();
                return false;
            }

            var byteLength = Length * 2;
            var rawContent = ReadBytes(unicodeStringPtr.Value, byteLength);
            if (rawContent == null || rawContent.Length < (int)byteLength)
                throw new Exception($"Failed to read unicode string content. Obj: 0x{Address:X}; Content: 0x{unicodeStringPtr:X}; " +
                    $"Length: {Length}");

            var newValue = Encoding.Unicode.GetString(rawContent, 0, (int)byteLength);
            _value = newValue;
            return true;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
