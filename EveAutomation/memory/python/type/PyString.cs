using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyString : PyObjectVar
    {

        public string Value { 
            get
            {
                UpdateValue();
                return _value ?? "";
            }
        }
        private string? _value;

        public PyString(ulong address) : base(address, 0x18) 
        {
            _updatePeriod = 0;
            SetSize(0x20 + Length + 1);
        }

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
            var newValue = ReadString(Address + 0x20, (uint)(Length + 1)) ?? "";
            if (_value != null && newValue != _value)
            {
                NotifyValueRemoved();
                return false;
            }

            if (_value == null)
                _value = newValue;
            return true;
        }

        public override string ToString()
        {
            return Value;
        }

    }
}
