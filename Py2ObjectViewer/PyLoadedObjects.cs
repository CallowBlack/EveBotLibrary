using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Py2ObjectViewer
{
    public static class PyLoadedObjects
    {
        private static Dictionary<PyObject, int> _loadedObjects = new();

        public static void LoadObject(PyObject pyObject)
        {
            if (!_loadedObjects.ContainsKey(pyObject))
                _loadedObjects[pyObject] = 0;
            _loadedObjects[pyObject]++;
        }

        public static void UnloadObject(PyObject pyObject)
        {
            if (!_loadedObjects.ContainsKey(pyObject))
                return;
            _loadedObjects[pyObject]--;

            if (_loadedObjects[pyObject] <= 0)
                _loadedObjects.Remove(pyObject);
        }

        public static List<PyObject> GetObjects()
        {
            return _loadedObjects.Keys.ToList();
        }

        public static void Clear()
        {
            _loadedObjects.Clear();
        }
    }
}
