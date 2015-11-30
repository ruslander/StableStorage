using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LightRail.Core
{
    public class HotSegmentFullException : Exception { }

    public interface ISegment
    {
        long Position { get; set; }
        void Dispose();

        IEnumerable<Block> FetchForward();
        IEnumerable<Block> FetchBackward();
    }

    public class HotSegment : ISegment
    {
        public HotSegmentBurner Burner { get; set; }
        public long Position { get; set; }
        public List<Block> Blocks { get; set; }

        public IEnumerable<Block> FetchForward()
        {
            return Blocks.AsEnumerable();
        }

        public IEnumerable<Block> FetchBackward()
        {
            return Enumerable.Reverse(Blocks);
        }

        public int RecordsCount()
        {
            return Blocks
                .Select(x => x.Records().Count)
                .Sum();
        }

        public void Dispose()
        {
            Burner.Dispose();
        }

        private readonly long _capacity;

        Block _current;

        public HotSegment(long capacity, List<Block> blocks)
        {
            _capacity = capacity;

            Blocks = blocks;
            _current = blocks.Last();
        }

        public HotSegment(long capacity)
        {
            _capacity = capacity;
            
            Blocks = new List<Block>();

            RollCurrentBlock();
        }

        public long Append(byte[] next)
        {
            var op = new Op(next);

            var storage = new MemoryStream();
            var writer = new BinaryWriter(storage);

            op.WriteTo(writer);

            try
            {
                return BurnCurrentBlock(storage.ToArray());
            }
            catch (BlockFullException)
            {
                if ((Blocks.Count + 1) * _current.Payload.Length > _capacity)
                    throw new HotSegmentFullException();

                RollCurrentBlock();

                return BurnCurrentBlock(storage.ToArray());
            }
        }

        private long BurnCurrentBlock(byte[] record)
        {
            var positionInBlock = _current.Append(record);
            Burner.Burn(_current, Blocks.Count);

            return positionInBlock + (Blocks.Count - 1) * Block.Size;
        }

        private void RollCurrentBlock()
        {
            _current = Block.New();
            Blocks.Add(_current);
        }

        public override string ToString()
        {
            return string.Format("Blocks {0} Records {1}", Blocks.Count, Blocks.Select(x=>x.Records().Count).Sum());
        }
    }
}