using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    internal static class PyObjectPool
    {
        private static Dictionary<ulong, PyObject> _objects = new();
        private static Dictionary<ulong, PyType> _types = new();
        private static Dictionary<ulong, PyType> _typeTypes = new();

        public static bool AddType(PyType pyObject)
        {
            if (ContainsType(pyObject.Address))
                return false;

            _types.Add(pyObject.Address, pyObject);
            return true;
        }

        public static bool AddType(ProcessMemory processMemory, ulong address)
        {
            if (ContainsType(address))
                return false;

            var newType = new PyType(processMemory, address);
            if (!newType.IsValid)
                return false;

            if (newType.Kind == PyKind.TypeType)
                _typeTypes.Add(address, newType);
            else
                _types.Add(address, newType);

            return true;
        }

        public static bool ContainsType(ulong typeAddress)
        {
            return _types.ContainsKey(typeAddress);
        }

        public static IEnumerable<PyType> GetTypes()
        {
            return _types.Values;
        }

        public static PyType? GetTypeByAddress(ulong typeAddress)
        {
            if (!ContainsObject(typeAddress)) return null;
            return _types[typeAddress];
        }

        public static bool IsTypeType(ulong typeAddress)
        {
            return _typeTypes.ContainsKey(typeAddress);
        }

        public static void AddObject(PyObject pyObject)
        {
            if (ContainsObject(pyObject.Address))
                return;
            
            if (pyObject.Kind == PyKind.Type || pyObject.Kind == PyKind.TypeType)
                return;

            _objects.Add(pyObject.Address, pyObject);
        }

        public static bool ContainsObject(ulong objectAddress)
        {
            return _objects.ContainsKey(objectAddress);
        }

        public static IEnumerable<PyObject> GetObjects()
        {
            return _objects.Values;
        }

        public static PyObject? GetObjectByAddress(ulong objectAddress)
        {
            if (!ContainsObject(objectAddress)) return null;
            return _objects[objectAddress];
        }

        public static void ScanProcessMemory(ProcessMemory process)
        {
            //ScanForPyType_Type(process);
            //ScanForPyTypes(process);
            //ScanForPyObjects(process); 
        }

        static bool IsPyType_Type(RegionMemoryReader reader)
        {
            // https://docs.python.org/2/c-api/typeobj.html?highlight=ob_type#c.PyObject.ob_type
            // https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Objects/typeobject.c#L2879
            // For PyType_Type "ob_type" is pointer to PyType_Type itself.
            // ob_type offset is 0x8 (8) for x64 system.
            // https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/object.h#L78
            var ob_type_addr = reader.ReadUInt64(0x8);
            if (ob_type_addr != reader.Address) return false;

            // For PyType_Type tp_name = "type".
            // tp_name offset is 0x18 (24) for x64 system.
            var tp_name = reader.ReadStringPointer(0x18, 5);
            if (tp_name != "type") return false;

            return true;
        }

        static bool IsPyType(RegionMemoryReader reader)
        {
            // See IsPyType_Type
            var ob_type_addr = reader.ReadUInt64(0x8);
            if (!_typeTypes.ContainsKey(ob_type_addr)) return false;

            var tp_name = reader.ReadStringPointer(0x18, 8);
            if (tp_name == null || tp_name.Length < 3) return false;

            return true;
        }

        static bool IsPyObject(RegionMemoryReader reader)
        {
            // See IsPyType_Type
            var ob_type_addr = reader.ReadUInt64(0x8);
            return _types.ContainsKey(ob_type_addr);
        }

        static void ScanForPyType_Type(ProcessMemory process)
        {
            RegionMemoryReader reader = new(process);
            while (reader.CanRead)
            {
                if (IsPyType_Type(reader))
                    _typeTypes.Add(reader.Address, new PyType(process, reader.Address));
                reader.Address += 8;
            }
        }

        static void ScanForPyTypes(ProcessMemory process)
        {
            RegionMemoryReader reader = new(process);
            while (reader.CanRead)
            {
                if (IsPyType(reader))
                    AddType(new PyType(process, reader.Address));
                reader.Address += 8;
            }
        }

        static void ScanForPyObjects(ProcessMemory process)
        {
            RegionMemoryReader reader = new(process);
            while (reader.CanRead)
            {
                if (IsPyObject(reader))
                    AddObject(new PyObject(process, reader.Address));
                reader.Address += 8;
            }
        }
    }
}
