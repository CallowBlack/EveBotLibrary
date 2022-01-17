using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory
{
    public class RegionMemoryReader : MemoryReader
    {
        public ulong Address { 
            get => CurrentRegion.baseAddress + _regionOffset;
            set {
                if (!CanRead)
                    return;

                _regionOffset = value - CurrentRegion.baseAddress;
                if (_regionOffset < CurrentRegion.length)
                    return;

                while (CanRead && _regionOffset >= CurrentRegion.length)
                {
                    _regionOffset -= CurrentRegion.length;
                    _currentRegion++;
                }
                LoadCurrentRegion();
            }
        }
        
        public bool CanRead { get => _currentRegion < _regionsInfo.Count; }

        public (ulong baseAddress, ulong length) CurrentRegion { get => _regionsInfo[_currentRegion]; }

        private IReadOnlyList<(ulong baseAddress, ulong length)> _regionsInfo;

        private int _currentRegion = 0;
        private ulong _regionOffset = 0;
        private byte[] _regionContent = Array.Empty<byte>();

        public RegionMemoryReader(IEnumerable<(ulong baseAddress, ulong length)>? regions = null)
        {

            if (regions == null)
                this._regionsInfo = ProcessMemory.Instance.GetCommitedRegionsInfo().ToList();
            else 
                this._regionsInfo = regions.ToList();

            LoadCurrentRegion();
        }

        public override bool ReadBytes(ulong address, ref byte[] buffer)
        {
            if (address < CurrentRegion.baseAddress || address > CurrentRegion.baseAddress + CurrentRegion.length)
                return ProcessMemory.Instance.ReadBytes(address, ref buffer);

            var offset = address - CurrentRegion.baseAddress;

            var start = _regionOffset + offset;
            if (start >= CurrentRegion.length) return false;

            var length = (ulong)buffer.LongLength;
            if (start + length > CurrentRegion.length) return false;

            Buffer.BlockCopy(_regionContent, Convert.ToInt32(start), buffer, 0, buffer.Length);
            return true;
        }

        public new string ReadString(ulong offset = 0, uint maxLength = 255)
        {
            return base.ReadString(CurrentRegion.baseAddress + offset, maxLength) ?? "";
        }

        public new uint ReadUInt32(ulong offset = 0)
        {
            return base.ReadUInt32(CurrentRegion.baseAddress + offset) ?? 0;
        }

        public string ReadStringPointer(ulong offset = 0, uint maxLength = 255)
        {
            return base.ReadPointedString(CurrentRegion.baseAddress + offset, maxLength) ?? string.Empty;
        }

        public new ulong ReadUInt64(ulong offset = 0)
        {
            return base.ReadUInt64(CurrentRegion.baseAddress + offset) ?? 0;
        }

        public new bool IsPointer(ulong offset)
        {
            return base.IsPointer(CurrentRegion.baseAddress + offset);
        }

        private void LoadCurrentRegion()
        {
            if (!CanRead)
                return;

            var result = ProcessMemory.Instance.ReadBytes(CurrentRegion.baseAddress, CurrentRegion.length);
            if (result == null)
                throw new Exception($"Failed to ReadProcessMemory.InstanceMemory at 0x{CurrentRegion.baseAddress:X}.");
            _regionContent = result;
        }
    }
}
