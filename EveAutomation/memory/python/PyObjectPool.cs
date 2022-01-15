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
        private static PyType? _typeType = null;
        private static PyGC? _garbageCollector = null;

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
                _typeType = newType;
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
            if (!ContainsType(typeAddress)) return null;
            return _types[typeAddress];
        }

        public static bool IsTypeType(ulong typeAddress)
        {
            if (_typeType == null) return false;
            return _typeType.Address == typeAddress;
        }

        public static void AddObject(PyObject pyObject)
        {
            if (ContainsObject(pyObject.Address))
                return;
            
            if (pyObject.Kind == PyKind.Type || pyObject.Kind == PyKind.TypeType)
            {
                AddType((PyType)pyObject);
                return;
            }

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
            // Clear all data what was found ago.
            _objects.Clear();
            _types.Clear();
            _typeType = null;

            if (!ScanForgarbageCollector(process) || _garbageCollector == null) {
                Console.WriteLine("Failed to find garbage collector.");
                return;
            }

            // Adding collector objects to the pool.
            foreach (var pyObj in _garbageCollector.GetObjects())
            {
                AddObject(pyObj);
            }
        }

        static bool IsgarbageCollector(RegionMemoryReader region)
        {
            // garbage collector consists of 3 generators which structure defined in
            // https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Modules/gcmodule.c#L43

            // PyGC_Head defined in
            // https://github.com/python/cpython/blob/362ede2232107fc54d406bb9de7711ff7574e1d4/Include/objimpl.h#L266

            uint objectOffset = 0;

            // We use thresholdes like a pattern for find. Threasholdes is defined where garbage collector is.
            uint[] thresholdes = { 700, 10, 10};

            foreach (uint targetThreshold in thresholdes)
            {
                // PyGC_Head.gc.gc_next must be pointer
                var gcNext = region.IsPointer(objectOffset);
                if (!gcNext) return false;

                // PyGC_Head.gc.gc_prev must be pointer
                var gcPrev = region.IsPointer(objectOffset + 0x8);
                if (!gcPrev) return false;

                // threshold must be certain number integer
                var threshold = region.ReadUInt32(objectOffset + 0x18);
                if (threshold != targetThreshold) return false;

                objectOffset += 0x20;
            }

            return true;

        }

        static bool ScanForgarbageCollector(ProcessMemory process)
        {
            // Garbage collector must be in image of python27.dll module.
            var pythonRegions = process.GetModuleRegionsInfo("python27.dll", WinApi.MemoryInformationProtection.PAGE_READWRITE);
            if (pythonRegions == null)
            {
                Console.WriteLine("Python27.dll not found in the process.");
                return false;
            }

            RegionMemoryReader reader = new(process, pythonRegions);
            while (reader.CanRead)
            {
                if (IsgarbageCollector(reader))
                {
                    _garbageCollector = new PyGC(process, reader.Address);
                    return true;
                }
                reader.Address += 8;
            }
            return false;
        }
    }
}
