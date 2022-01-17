using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    public static class PyObjectTypeDeterminer
    {
        private static Dictionary<string, Func<ulong, PyObject>> _typeConstructors = new()
        {
            { "type",       address => new PyType(address) },
            { "str",        address => new PyString(address) },
            { "unicode",    address => new PyUnicode(address) },
            { "function",   address => new PyFunction(address) },
            { "dict",       address => new PyDict(address) },
            { "bool",       address => new PyBool(address) },
            { "float",      address => new PyFloat(address) },
            { "int",        address => new PyInt(address) },
            { "list",       address => new PyList(address) }
        };

        public static PyObject? CreateObject(ulong address)
        {
            if (address == 0) return null;

            var typeName = GetType(address);
            if (typeName == null) return null;

            if (_typeConstructors.ContainsKey(typeName))
                return _typeConstructors[typeName](address);

            return new PyObject(address);
        }

        public static void AddTypeConstructor(string type, Func<ulong, PyObject> constructor)
        {
            _typeConstructors[type] = constructor;
        }

        private static string? GetType(ulong address)
        {
            var typePtr = ProcessMemory.Instance.ReadUInt64(address + 0x8);
            if (!typePtr.HasValue) return null;

            var typeName = ProcessMemory.Instance.ReadPointedString(typePtr.Value + 0x18, 255);
            if (typeName == null) return null;

            return typeName;
        }
    }
}
