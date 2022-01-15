using EveAutomation.memory;
using EveAutomation.memory.python;
using EveAutomation.memory.python.type;

var process = ProcessMemory.Open("exefile");
if (process == null) {
    Console.WriteLine("Program not found.");
    return;
}

var moduleRegions = process.GetModuleRegionsInfo("python27.dll");
if (moduleRegions == null)
{
    Console.WriteLine("Python27 module not found.");
    return;
}

foreach (var region in moduleRegions) {
    Console.WriteLine($"Region 0x{region.baseAddress:X} - 0x{region.baseAddress + region.length:X}");
}

PyObjectPool.ScanProcessMemory(process);

foreach (var pyObject in PyObjectPool.GetObjects().Where(pyobject => pyobject is PyDict))
{
    Console.WriteLine(pyObject);
}