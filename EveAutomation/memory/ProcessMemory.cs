using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory
{
    internal class ProcessMemory
    {

        private readonly IntPtr _processHandle;

        private ProcessMemory(IntPtr processHandle) {
            this._processHandle = processHandle;
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
                if (process.ProcessName.Equals(processName)) return Open(process.Id);
            }
            return null;
        }

        public static ProcessMemory? Open(int processId)
        {
            IntPtr processHandle = WinApi.OpenProcess(
                (int)(WinApi.ProcessAccessFlags.VirtualMemoryRead | WinApi.ProcessAccessFlags.QueryInformation),
                false, processId);
            if (processHandle == IntPtr.Zero) return null;
            return new ProcessMemory(processHandle);
        }

        public byte[]? ReadBytes(ulong startAddress, ulong length)
        {
            var buffer = new byte[length];

            UIntPtr numberOfBytesReadAsPtr = UIntPtr.Zero;

            if (!WinApi.ReadProcessMemory(this._processHandle, startAddress, buffer, (UIntPtr)buffer.LongLength, ref numberOfBytesReadAsPtr))
                return null;

            var numberOfBytesRead = numberOfBytesReadAsPtr.ToUInt64();

            if (numberOfBytesRead == 0)
                return null;

            if (int.MaxValue < numberOfBytesRead)
                return null;

            if (numberOfBytesRead == (ulong)buffer.LongLength)
                return buffer;

            return buffer.AsSpan(0, (int)numberOfBytesRead).ToArray();
        }

        public string? ReadString(ulong address, uint maxLength)
        {
            var bytes = ReadBytes(address, maxLength);
            if (bytes == null) return null;

            bytes = bytes.TakeWhile(character => 0 < character).ToArray();
            return Encoding.ASCII.GetString(bytes);
        }

        public string? ReadPointedString(ulong ptrAddr, uint maxLength)
        {
            var strPtr = ReadUInt64(ptrAddr);
            return strPtr == null ? null : ReadString((ulong)strPtr, maxLength);
        }

        public ulong? ReadUInt64(ulong address)
        {
            var bytes = ReadBytes(address, 8);
            if (bytes == null) return null;
            return BitConverter.ToUInt64(bytes, 0);
        }

        public IEnumerable<(ulong baseAddress, ulong length)> GetCommitedRegionsInfo()
        {
            ulong address = 0;
            while (true)
            {
                WinApi.MEMORY_BASIC_INFORMATION64 m;
                int _ = WinApi.VirtualQueryEx(_processHandle, (IntPtr)address, out m, (uint)Marshal.SizeOf(typeof(WinApi.MEMORY_BASIC_INFORMATION64)));

                var regionProtection = (WinApi.MemoryInformationProtection)m.Protect;

                // logLine($"{m.BaseAddress}-{(uint)m.BaseAddress + (uint)m.RegionSize - 1} : {m.RegionSize} bytes result={result}, state={(WinApi.MemoryInformationState)m.State}, type={(WinApi.MemoryInformationType)m.Type}, protection={regionProtection}");

                if (address == m.BaseAddress + m.RegionSize)
                    break;

                address = m.BaseAddress + m.RegionSize;

                if (m.State != (int)WinApi.MemoryInformationState.MEM_COMMIT)
                    continue;

                var protectionFlagsToSkip = WinApi.MemoryInformationProtection.PAGE_GUARD 
                    | WinApi.MemoryInformationProtection.PAGE_NOACCESS 
                    | WinApi.MemoryInformationProtection.PAGE_EXECUTE;

                var matchingFlagsToSkip = protectionFlagsToSkip & regionProtection;

                if (matchingFlagsToSkip != 0)
                {
                    // logLine($"Skipping region beginning at {m.BaseAddress:X} as it has flags {matchingFlagsToSkip}.");
                    continue;
                }
                yield return (m.BaseAddress, m.RegionSize);
            };
        }

        public (ulong baseAddress, ulong length) GetRegionInfo(ulong address)
        {
            WinApi.MEMORY_BASIC_INFORMATION64 m;
            int _ = WinApi.VirtualQueryEx(_processHandle, (IntPtr)address, out m, (uint)Marshal.SizeOf(typeof(WinApi.MEMORY_BASIC_INFORMATION64)));
            return (m.BaseAddress, m.RegionSize);
        }

        public (ulong baseAddress, ulong length) GetModuleRegionInfo(string moduleName)
        {

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
