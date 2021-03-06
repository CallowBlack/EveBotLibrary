using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Configuration;
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
            newObject.ObjectRemoved += OnObjectRemoved;
            return newObject;
        }

        public static IList<PyObject> GetObjects()
        {
            return _objects.Values.ToList();
        }

        public static void Clear()
        {
            _objects.Clear();
            _garbageCollector = null;
        }

        public static IEnumerable<PyObject> ScanPythonObjects(
            bool skipBuildinType = true,
            Func<ulong, string, bool>? typeFilter = null, 
            Func<PyObject, bool>? objectFilter = null)
        {

            if (_garbageCollector == null && !ScanForgarbageCollector())
                throw new DllNotFoundException("Failed to find garbage collector.");

            _objects.Clear();

            // Skiping buildin types allows gain performance increase
            var buildinTypes = new HashSet<string> { 
                "object", "NoneType", "int", "long", 
                "string", "unicode", "weakref", "bool", 
                "float", "function", "type" };

            // Adding collector objects to the pool.
            foreach (var address in _garbageCollector.GetObjectAddresses())
            {
                // If object already exists
                if (_objects.ContainsKey(address))
                    continue;

                if (skipBuildinType || typeFilter != null)
                {
                    var tn = PyObjectTypeDeterminer.GetType(address);
                    if (tn == null)
                        continue;

                    if (skipBuildinType && buildinTypes.Contains(tn))
                        continue;

                    if (typeFilter != null && !typeFilter(address, tn))
                        continue;
                }

                var newObject = PyObjectTypeDeterminer.CreateObject(address);
                if (newObject == null)
                    continue;

                if (objectFilter != null && !objectFilter(newObject))
                    continue;

                _objects.Add(address, newObject);
                newObject.ObjectRemoved += OnObjectRemoved;

                yield return newObject;
            }
        }

        private static void OnObjectRemoved(object? sender, EventArgs e)
        {
            if (sender is PyObject pyObject)
                _objects.Remove(pyObject.Address);
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

        static bool GetCollectorFromSavedOffset()
        {
            var appSettings = ConfigurationManager.AppSettings;
            var savedOffsetString = appSettings["GCOffset"] ?? null;
            if (savedOffsetString == null) return false;

            if (!UInt64.TryParse(savedOffsetString, out var offset)) return false;

            var module = ProcessMemory.Instance.FindModuleByName("python27.dll");
            if (module == null) return false;

            var gcAddress = (ulong)module.BaseAddress.ToInt64() + offset;
            var reader = new RegionMemoryReader(new List<(ulong, ulong)>() { (gcAddress, 0x60) });
            if (!IsGarbageCollector(reader))
                return false;

            _garbageCollector = new PyGC(gcAddress);
            return true;
        }

        static void SaveGCOffset(ulong address)
        {
            try
            {
                var module = ProcessMemory.Instance.FindModuleByName("python27.dll");
                if (module == null) return;

                var offset = address - (ulong)module.BaseAddress.ToInt64();

                var confingFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = confingFile.AppSettings.Settings;
                
                var offsetSetting = settings["GCOffset"];
                if (offsetSetting != null)
                    offsetSetting.Value = offset.ToString();
                else
                    settings.Add("GCOffset", offset.ToString());

                confingFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(confingFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Failed to save GCOffset to settings.");
            }

        }

        static bool ScanForgarbageCollector()
        {

            if (GetCollectorFromSavedOffset())
                return true;

            // Garbage collector must be in image of python27.dll module.
            var pythonRegions = ProcessMemory.Instance.GetModuleRegionsInfo("python27.dll", WinApi.MemoryInformationProtection.PAGE_READWRITE);
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
                    SaveGCOffset(reader.Address);
                    _garbageCollector = new PyGC(reader.Address);
                    return true;
                }
                reader.Address += 8;
            }
            return false;
        }
    }
}
