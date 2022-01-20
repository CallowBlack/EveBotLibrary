using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyFloat : PyObject, IValueChanged
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

        public event IValueChanged.ValueChangedHandler? ValueChanged;

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
            if (_value.HasValue && newValue != _value)
                ValueChanged?.Invoke(new ValueChangedArgs(this));
            _value = newValue;
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
