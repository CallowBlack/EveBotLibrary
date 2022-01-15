using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    internal static class PyObjectTypeConverter
    {
        private static Dictionary<string, Func<PyObject, PyObject>> _conventers = new()
        {
            { "type", pyObject => new PyType(pyObject.Process, pyObject.Address) },
            { "str", pyObject => new PyString(pyObject.Process, pyObject.Address) },
            { "function", pyObject => new PyFunction(pyObject.Process, pyObject.Address) },
        };

        public static PyObject ConvertToCorrectType(PyObject pyObject)
        {
            if (!pyObject.IsValid)
                return pyObject;

            if (pyObject.Type == null)
                return new PyType(pyObject.Process, pyObject.Address);

            if (_conventers.ContainsKey(pyObject.Type.Name))
            {
                return _conventers[pyObject.Type.Name](pyObject);
            }

            return pyObject;
        }

        public static void AddTypeConverter(string type, Func<PyObject, PyObject> converter)
        {
            _conventers[type] = converter;
        }

    }
}
