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
            var unicodeStringPtr = ReadUInt64(Address + 0x18);
            if (Length > 0x1000 || !unicodeStringPtr.HasValue)
            {
                NotifyValueRemoved();
                return false;
            }

            var byteLength = Length * 2;
            var rawContent = ReadBytes(unicodeStringPtr.Value, byteLength);
            if (rawContent == null || rawContent.Length < (int)byteLength)
            {
                NotifyValueRemoved();
                return false;
            }

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
