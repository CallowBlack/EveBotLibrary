using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python.type
{
    public class CachebleObject : MemoryReader
    {
        public ulong Address { get; private set; }

        public ulong Size { get; private set; }

        // If equals zero means that cache updated only once.
        protected long _updatePeriod = 1000; // In ms

        private (long time, ulong address, string value)? _pointedStringCache;
        private byte[] _cache;

        private long _lastTimeUpdate = 0;

        public CachebleObject(ulong address, ulong size)
        {
            Address = address;
            Size = size;

            _cache = Array.Empty<byte>();
        }

        public override bool ReadBytes(ulong startAddress, ref byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public override byte[]? ReadBytes(ulong startAddress, ulong length)
        {
            if (length > 0xFFFF)
                return null;

            if (startAddress < Address || startAddress + length > Address + Size)
                return ProcessMemory.Instance.ReadBytes(startAddress, length);

            UpdateCache();

            var cachedOffset = startAddress - Address;

            var buffer = new byte[length];
            Buffer.BlockCopy(_cache, Convert.ToInt32(cachedOffset), buffer, 0, (int)length);

            return buffer;
        }

        public override string? ReadPointedString(ulong ptrAddr, uint maxLength = 256)
        {
            var strPtr = ReadUInt64(ptrAddr);
            if (!strPtr.HasValue) return null;

            var milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (_pointedStringCache.HasValue && _pointedStringCache.Value.address == strPtr.Value
                && (_updatePeriod == 0 || _pointedStringCache.Value.time + _updatePeriod > milliseconds))
                return _pointedStringCache.Value.value;

            var str = ReadString(strPtr.Value, maxLength);
            if (str == null) return null;

            _pointedStringCache = new (milliseconds, strPtr.Value, str);
            return str;
        }

        public async void Update(bool forced = false, bool deep = false, HashSet<CachebleObject>? visited = null)
        {
            if (forced && _updatePeriod == 0)
                UpdateCache(forced);

            UpdateObject(deep, visited ?? (deep ? new() : null));
            await Task.Yield();
        }

        protected virtual bool UpdateObject(bool deep, HashSet<CachebleObject>? visited = null)
        {
            if (visited != null && visited.Contains(this))
                return false;
            
            visited?.Add(this);
            
            return true;
        }

        private void UpdateCache(bool forced = false)
        {
            long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (_lastTimeUpdate == 0)
                _cache = new byte[Size];

            if (forced || _lastTimeUpdate == 0 || (_updatePeriod > 0 && _lastTimeUpdate + _updatePeriod < milliseconds))
            {
                ProcessMemory.Instance.ReadBytes(Address, ref _cache);
                _lastTimeUpdate = milliseconds;
            }
        }

        protected void SetSize(ulong newSize)
        {
            _lastTimeUpdate = 0;
            Size = newSize;
        }

    }
}
