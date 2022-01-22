using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyInt : PyObject, INotifyValueChanged
    {
        public int Value {
            get
            {
                UpdateValue();
                return _value ?? 0;
            }
        }
        private int? _value;

        public event EventHandler<ValueChangedEventArgs>? ValueChanged;

        public PyInt(ulong address) : base(address, 0x18) { }

        protected override bool UpdateObject(bool deep, HashSet<CachebleObject>? visited = null)
        {
            if (!base.UpdateObject(deep, visited))
                return false;

            UpdateValue();
            return true;
        }

        private void UpdateValue()
        {
            var newValue = ReadInt32(Address + 0x10) ?? 0;
            var oldValue = _value;
            _value = newValue;

            if (oldValue.HasValue && oldValue != newValue)
                NotifyObjectRemoved();
                //ValueChanged?.Invoke(this, new ValueChangedEventArgs(oldValue, newValue));   
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
