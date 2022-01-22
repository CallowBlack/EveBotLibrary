using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class PyCollection : PyObject
    {
        public ulong Count { get => ReadUInt64(Address + 0x10) ?? 0; }

        public PyCollection(ulong address) : base(address, 0x18) { }

        protected PyCollection(ulong address, ulong size) : base(address, size) { }

        public List<PyObject> Items
        {
            get
            {
                UpdateItems();
                _isInitialized = true;
                return _items;
            }
        }

        protected List<PyObject> _items = new();
        protected bool _isInitialized = false;

        protected virtual bool UpdateItems()
        {
            throw new NotImplementedException();
        }

        protected override bool UpdateObject(bool deep, HashSet<CachebleObject>? visited = null)
        {
            if (!base.UpdateObject(deep, visited))
                return false;

            if (!UpdateItems())
                return false;
            _isInitialized = true;

            if (!deep)
                return true;

            foreach (var item in _items)
            {
                item.Update(true, deep, visited);
            }

            return true;
        }
    }
}
