using EveAutomation.memory.python.type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EveAutomation.memory.python
{
    public class PyGC
    {
        struct PyGCHead
        {
            public ulong nextGCHead;
            public ulong prevGCHead;

            public PyGCHead(ulong nextHead, ulong prevHead)
            {
                this.nextGCHead = nextHead;
                this.prevGCHead = prevHead;
            }
        }

        struct PyGCGenerator
        {
            public PyGCHead GCHead;
            public readonly uint threshold;
            public uint count;

            public PyGCGenerator(PyGCHead GCHead, uint threshold, uint count)
            {
                this.GCHead = GCHead;
                this.threshold = threshold;
                this.count = count;
            }
        }

        private PyGCGenerator[] _generators = new PyGCGenerator[3];

        private ulong _address;

        public PyGC(ulong address)
        {
            this._address = address;
            readGenerators();
        }

        private void readGenerators()
        {
            for (uint i = 0; i < _generators.Length; i++)
            {
                ulong objectAddress = GetGeneratorAddress(i);

                var threshold = ProcessMemory.Instance.ReadUInt32(objectAddress + 0x18);
                if (!threshold.HasValue) return;

                var count = ProcessMemory.Instance.ReadUInt32(objectAddress + 0x1C);
                if (!count.HasValue) return;

                var gcHead = readGCHead(objectAddress);
                if (!gcHead.HasValue) return;

                var newGenerator = new PyGCGenerator(gcHead.Value, threshold.Value, count.Value);
                _generators[i] = newGenerator;
            }
        }

        private PyGCHead? readGCHead(ulong objectAddress)
        {
            var gcNext = ProcessMemory.Instance.ReadUInt64(objectAddress);
            if (!gcNext.HasValue) return null;

            var gcPrev = ProcessMemory.Instance.ReadUInt64(objectAddress + 0x8);
            if (!gcPrev.HasValue) return null;

            return new PyGCHead(gcNext.Value, gcPrev.Value);
        }

        private ulong GetGeneratorAddress(uint id)
        {
            return _address + id * 0x20;
        }
 
        public IEnumerable<ulong> GetObjectAddresses()
        {
            for (uint i = 0; i < _generators.Length; i++)
            {
                var genAddr = GetGeneratorAddress(i);
                var generator = _generators[i];
                var current = generator.GCHead;
                while (current.nextGCHead != genAddr)
                {
                    yield return current.nextGCHead + 0x18;

                    var result = readGCHead(current.nextGCHead);
                    if (!result.HasValue) break;
                    current = result.Value;
                }
            }
        }
    }
}
