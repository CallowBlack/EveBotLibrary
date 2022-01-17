using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory
{
    public abstract class MemoryReader
    {
        public abstract bool ReadBytes(ulong startAddress, ref byte[] buffer);

        public virtual byte[]? ReadBytes(ulong startAddress, ulong length)
        {
            byte[] buffer = new byte[length];
            if (!ReadBytes(startAddress, ref buffer)) return null;
            return buffer;
        }

        public virtual string? ReadString(ulong address, uint maxLength)
        {
            var bytes = ReadBytes(address, maxLength);
            if (bytes == null) return null;

            bytes = bytes.TakeWhile(character => 0 < character).ToArray();
            return Encoding.ASCII.GetString(bytes);
        }

        public virtual string? ReadPointedString(ulong ptrAddr, uint maxLength = 256)
        {
            var strPtr = ReadUInt64(ptrAddr);
            return strPtr == null ? null : ReadString((ulong)strPtr, maxLength);
        }

        public virtual uint? ReadUInt32(ulong address)
        {
            var bytes = ReadBytes(address, 4);
            if (bytes == null) return null;
            return BitConverter.ToUInt32(bytes);
        }

        public virtual int? ReadInt32(ulong address)
        {
            var bytes = ReadBytes(address, 4);
            if (bytes == null) return null;
            return BitConverter.ToInt32(bytes);
        }

        public virtual ulong? ReadUInt64(ulong address)
        {
            var bytes = ReadBytes(address, 8);
            if (bytes == null) return null;
            return BitConverter.ToUInt64(bytes);
        }

        public virtual long? ReadInt64(ulong address)
        {
            var bytes = ReadBytes(address, 8);
            if (bytes == null) return null;
            return BitConverter.ToInt64(bytes);
        }

        public virtual bool IsPointer(ulong address)
        {
            var ptr = ReadUInt64(address);
            if (!ptr.HasValue) return false;

            var value = ReadBytes(ptr.Value, 8);
            return value != null;
        }

    }
}
