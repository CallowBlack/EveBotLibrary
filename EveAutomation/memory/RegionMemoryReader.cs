using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory
{
    public class RegionMemoryReader
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

        public byte[] ReadBytes(uint length, uint offset = 0)
        {
            var start = _regionOffset + offset;
            if (start >= CurrentRegion.length) return Array.Empty<byte>();
            var end = start + length >= CurrentRegion.length ? CurrentRegion.length : start + length;

            var bytes = new byte[end - start];
            Buffer.BlockCopy(_regionContent, Convert.ToInt32(start), bytes, 0, Convert.ToInt32(end - start));
            return bytes;
        }

        public string ReadString(uint offset = 0, uint maxLength = 255)
        {
            byte[] bytes = ReadBytes(offset, maxLength);
            bytes = bytes.TakeWhile(character => 0 < character).ToArray();
            return Encoding.ASCII.GetString(bytes);
        }

        public bool IsPointer(uint offset = 0)
        {
            var ptr = ReadUInt64(offset);
            var value = ProcessMemory.Instance.ReadUInt64(ptr);
            return value != null;
        }

        public uint ReadUInt32(uint offset = 0)
        {
            var pos = _regionOffset + offset;
            if (pos + 4 >= CurrentRegion.length) return 0;
            return BitConverter.ToUInt32(_regionContent, (int)pos);
        }

        public string ReadStringPointer(uint offset = 0, uint maxLength = 255)
        {
            var strAddr = ReadUInt64(offset);
            if (strAddr == 0) return "";

            var result = ProcessMemory.Instance.ReadString(strAddr, maxLength);
            return result ?? "";
        }


        public ulong ReadUInt64(uint offset = 0)
        {
            var pos = _regionOffset + offset;
            if (pos + 8 >= CurrentRegion.length) return 0;
            return BitConverter.ToUInt64(_regionContent, (int)pos);
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
