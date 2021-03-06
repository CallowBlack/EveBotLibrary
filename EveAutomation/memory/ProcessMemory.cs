using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory
{
    public class ProcessMemory : MemoryReader
    {
        public static ProcessMemory Instance { 
            get
            {
                if (_instance == null)
                    throw new Exception("Trying to access to unopened process.");
                return _instance;
            }
        }

        private static ProcessMemory? _instance;

        private readonly IntPtr _processHandle;
        private readonly Process _processObject;

        private ProcessMemory(Process process, IntPtr processHandle) {
            this._processHandle = processHandle;
            this._processObject = process;
        }

        ~ProcessMemory()
        {
            if (_processHandle != IntPtr.Zero)
                WinApi.CloseHandle(_processHandle);
        }

        public static ProcessMemory? Open(string processName)
        {
            foreach(Process process in Process.GetProcesses())
            {
                if (process.ProcessName.Equals(processName)) return Open(process);
            }
            return null;
        }

        public static ProcessMemory? Open(int processId)
        {
            var process = Process.GetProcessById(processId);
            if (process == null) return null;

            return Open(process);
        }

        public static ProcessMemory? Open(Process process)
        {
            IntPtr processHandle = WinApi.OpenProcess(
                (int)(WinApi.ProcessAccessFlags.VirtualMemoryRead | WinApi.ProcessAccessFlags.QueryInformation),
                false, process.Id);
            if (processHandle == IntPtr.Zero) return null;
            _instance = new ProcessMemory(process, processHandle);
            return _instance;
        }

        public override bool ReadBytes(ulong startAddress, ref byte[] buffer)
        {
            UIntPtr numberOfBytesReadAsPtr = UIntPtr.Zero;

            if (!WinApi.ReadProcessMemory(this._processHandle, startAddress, buffer, (UIntPtr)buffer.LongLength, ref numberOfBytesReadAsPtr))
                return false;

            var numberOfBytesRead = numberOfBytesReadAsPtr.ToUInt64();

            if (numberOfBytesRead == 0)
                return false;

            if (int.MaxValue < numberOfBytesRead)
                return false;

            if (numberOfBytesRead != (ulong)buffer.LongLength)
                return false;

            return true;
        }

        public IEnumerable<(ulong baseAddress, ulong length)> GetCommitedRegionsInfo(ulong start = 0, ulong end = 0x7fffffffffffffff, 
            WinApi.MemoryInformationProtection requiredProtectionFlags = 0)
        {
            ulong address = start;
            while (true)
            {
                WinApi.MEMORY_BASIC_INFORMATION64 m;
                int _ = WinApi.VirtualQueryEx(_processHandle, (IntPtr)address, out m, (uint)Marshal.SizeOf(typeof(WinApi.MEMORY_BASIC_INFORMATION64)));

                var regionProtection = (WinApi.MemoryInformationProtection)m.Protect;

                if (address == m.BaseAddress + m.RegionSize || address > end)
                    break;

                address = m.BaseAddress + m.RegionSize;

                if (m.State != (int)WinApi.MemoryInformationState.MEM_COMMIT)
                    continue;

                var protectionFlagsToSkip = WinApi.MemoryInformationProtection.PAGE_GUARD 
                    | WinApi.MemoryInformationProtection.PAGE_NOACCESS;

                var matchingFlagsToSkip = protectionFlagsToSkip & regionProtection;
                var userRequiredFlagsMatch = requiredProtectionFlags & regionProtection;

                if (matchingFlagsToSkip != 0 || userRequiredFlagsMatch != requiredProtectionFlags)
                    continue;

                yield return (m.BaseAddress, m.RegionSize);
            };
        }

        public (ulong baseAddress, ulong length) GetRegionInfo(ulong address)
        {
            WinApi.MEMORY_BASIC_INFORMATION64 m;
            int _ = WinApi.VirtualQueryEx(_processHandle, (IntPtr)address, out m, (uint)Marshal.SizeOf(typeof(WinApi.MEMORY_BASIC_INFORMATION64)));
            return (m.BaseAddress, m.RegionSize);
        }

        public IEnumerable<(ulong baseAddress, ulong length)>? GetModuleRegionsInfo(string moduleName, WinApi.MemoryInformationProtection requiredProtection = 0)
        {
            var module = FindModuleByName(moduleName);
            if (module == null)
                return null;

            return GetCommitedRegionsInfo((ulong)module.BaseAddress.ToInt64(), (ulong)(module.BaseAddress.ToInt64() + module.ModuleMemorySize), requiredProtection);
        }

        public ProcessModule? FindModuleByName(string moduleName)
        {
            foreach (ProcessModule module in _processObject.Modules)
            {
                if (module.ModuleName == moduleName)
                    return module;
            }
            return null;
        }

        public IEnumerable<(ulong baseAddress, byte[] content)> ReadCommitedRegionsContent()
        {
            foreach (var (baseAddress, length) in GetCommitedRegionsInfo())
            {
                var regionContent = ReadBytes(baseAddress, length);
                if (regionContent == null)
                    throw new Exception($"Failed to ReadProcessMemory at 0x{baseAddress:X}.");
                   
                yield return (baseAddress, regionContent);
            }
        }

    }
}
