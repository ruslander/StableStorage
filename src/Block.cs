using System;
using System.Collections.Generic;
using System.Linq;

namespace StableStorage
{
    public class BlockFullException : Exception { }
    public class Block
    {
        public const long Size = Units.KILO*4;
        public readonly byte[] Payload;

        public Block(byte[] payload)
        {
            Payload = payload;
        }

        public static Block New()
        {
            return new Block(new byte[Size]);
        }

        public int IndexOfNextFreeByte()
        {
            return Records().Sum();
        }

        public int Append(byte[] record)
        {
            var usedCapacity = Records().Sum() + Records().Count * 4;

            if(usedCapacity + 4 + record.Length >= Payload.Length)
                throw new BlockFullException();

            var appendStartingWith = Records().Sum();
            Array.Copy(record, 0, Payload, appendStartingWith, record.Length);

            var size = BitConverter.GetBytes(record.Length);
            var sizeIdx = Payload.Length - Records().Count * 4;
            Array.Copy(size, 0, Payload, sizeIdx - 4, 4);

            return appendStartingWith;
        }

        public List<int> Records()
        {
            var records = new List<int>();

            if (Payload.Length == 0)
            {
                return records;
            }

            var idx = Payload.Length - 4;
            var last = 0;

            while ((last = BitConverter.ToInt32(Payload, idx)) != 0)
            {
                idx = idx - 4;
                records.Add(last);
            }

            return records;
        }

        public IEnumerable<byte[]> Forward()
        {
            var records = Records();
            var idx = 0;

            for (int i = 0; i < records.Count; i++)
            {
                var size = records[i];
                var payload = new byte[size];

                Array.Copy(Payload, idx, payload, 0, size);

                idx = idx + size;

                yield return payload;
            }
        }

        public override string ToString()
        {
            return "Records " + Records().Count;
        }
    }
}