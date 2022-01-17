using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    public static class PyObjectPool
    {
        private static Dictionary<ulong, PyObject> _objects = new();
        private static PyGC? _garbageCollector = null;

        public static PyObject? Get(ulong address)
        {
            if (_objects.ContainsKey(address))
                return _objects[address];

            var newObject = PyObjectTypeDeterminer.CreateObject(address);
            if (newObject == null)
                return null;

            _objects.Add(address, newObject);
            return newObject;
        }

        public static IList<PyObject> GetObjects()
        {
            return _objects.Values.ToList();
        }

        public static void ScanProcessMemory(ProcessMemory process)
        {
            // Clear all data what was found ago.
            _objects.Clear();

            if (!ScanForgarbageCollector(process) || _garbageCollector == null) {
                Console.WriteLine("Failed to find garbage collector.");
                return;
            }

            // Adding collector objects to the pool.
            foreach (var address in _garbageCollector.GetObjectAddresses())
            {
                if (_objects.ContainsKey(address))
                    continue;

                var newObject = PyObjectTypeDeterminer.CreateObject(address);
                if (newObject == null)
                    continue;

                
                _objects.Add(address, newObject);
            }
        }

        static bool IsGarbageCollector(RegionMemoryReader region)
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

            RegionMemoryReader reader = new(pythonRegions);
            while (reader.CanRead)
            {
                if (IsGarbageCollector(reader))
                {
                    _garbageCollector = new PyGC(reader.Address);
                    return true;
                }
                reader.Address += 8;
            }
            return false;
        }
    }
}
