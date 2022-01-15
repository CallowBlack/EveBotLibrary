using EveAutomation.memory;
using EveAutomation.memory.python;

var process = ProcessMemory.Open("exefile");
if (process == null) {
    Console.WriteLine("Program not found.");
    return;
}

var regionCount = new Dictionary<ulong, int>();

var regions = process.GetCommitedRegionsInfo().ToList();
PyObjectPool.ScanProcessMemory(process);

foreach (var item in PyObjectPool.GetTypes()) 
{
    var baseAddress = process.GetRegionInfo(item.Address).baseAddress;
    if (!regionCount.ContainsKey(baseAddress))
        regionCount[baseAddress] = 0;
    regionCount[baseAddress]++;
    Console.WriteLine(item);
}

Console.WriteLine("Regions for types: ");
foreach (var item in regionCount.OrderBy((entry) => entry.Value))
{
    (ulong baseAddress, ulong length) = process.GetRegionInfo(item.Key);
    Console.WriteLine($"0x{baseAddress:X} - 0x{baseAddress + length:X}: {item.Value}");
}

//regionCount.Clear();

//foreach (var item in PyObjectPool.GetObjects())
//{
//    var baseAddress = process.GetRegionInfo(item.Address).baseAddress;
//    if (!regionCount.ContainsKey(baseAddress))
//        regionCount[baseAddress] = 0;
//    regionCount[baseAddress]++;
//}

//Console.WriteLine("Regions for objects: ");
//foreach (var item in regionCount.OrderBy((entry) => entry.Value))
//{
//    (ulong baseAddress, ulong length) = process.GetRegionInfo(item.Key);
//    Console.WriteLine($"0x{baseAddress:X} - 0x{baseAddress + length:X}: {item.Value}");
//}