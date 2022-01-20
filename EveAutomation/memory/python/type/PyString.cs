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
            if (_value != null)
                return true;

            var newValue = ReadString(Address + 0x20, (uint)(Length + 1)) ?? "";
            if (newValue == null)
            {
                NotifyValueRemoved();
                return false;
            }

            _value = newValue;
            return true;
        }

        public override string ToString()
        {
            return Value;
        }

    }
}
