using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyBool : PyObject, IValueChanged
    {
        public bool Value { 
            get
            {
                UpdateValue();
                return _value ?? false;
            }
        }
        private bool? _value;

        public event IValueChanged.ValueChangedHandler? ValueChanged;

        public PyBool(ulong address) : base(address, 0x18) { }

        protected override bool UpdateObject(bool deep, HashSet<CachebleObject>? visited = null)
        {
            if (!base.UpdateObject(deep, visited))
                return false;

            UpdateValue();
            return true;
        }

        private void UpdateValue()
        {
            var newValue = ReadUInt64(Address + 0x10) != 0;
            var changed = _value.HasValue && _value != newValue;
            _value = newValue;

            if (changed)
                ValueChanged?.Invoke(new ValueChangedArgs(this));
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
