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

                var content = ProcessMemory.Instance.ReadBytes(objectAddress, 0x20);
                if (content == null) return;

                BinaryReader binReader = new BinaryReader(new MemoryStream(content));

                var gcHead = readGCHead(ref binReader);

                binReader.BaseStream.Position += 0x8;

                var threshold = binReader.ReadUInt32();
                var count = binReader.ReadUInt32();

                var newGenerator = new PyGCGenerator(gcHead, threshold, count);
                _generators[i] = newGenerator;
            }
        }

        private PyGCHead readGCHead(ref BinaryReader reader)
        {
            var gcNext = reader.ReadUInt64();
            var gcPrev = reader.ReadUInt64();
            return new PyGCHead(gcNext, gcPrev);
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

                    var content = ProcessMemory.Instance.ReadBytes(current.nextGCHead, 0x10);
                    if (content == null) break;

                    BinaryReader binReader = new(new MemoryStream(content));
                    current = readGCHead(ref binReader);
                }
            }
        }
    }
}
