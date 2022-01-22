using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyFloat : PyObject, INotifyValueChanged
    {

        public double Value
        {
            get
            {
                UpdateValue();
                return _value ?? 0;
            }
        }
        private double? _value;

        public event EventHandler<ValueChangedEventArgs>? ValueChanged;

        public PyFloat(ulong address) : base(address, 0x18) { }

        protected override bool UpdateObject(bool deep, HashSet<CachebleObject>? visited = null)
        {
            if (!base.UpdateObject(deep, visited))
                return false;

            UpdateValue();
            return true;
        }

        private void UpdateValue()
        {
            var bytes = ReadBytes(Address + 0x10, 10);
            var newValue = BitConverter.ToDouble(bytes);

            var oldValue = _value;
            _value = newValue;

            if (oldValue.HasValue && oldValue != newValue)
                ValueChanged?.Invoke(this, new ValueChangedEventArgs(oldValue, newValue));
        }

        public override string ToString()
        {
            var strVal = Value.ToString();
            if (!strVal.Contains('.'))
                strVal += ".0";
            return strVal;
        }
    }
}
