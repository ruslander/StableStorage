using System.IO;
using System.Security.Cryptography;

namespace StableStorage
{
    public class OpData
    {
        public byte[] Payload { get; private set; }
        public byte[] Hash { get; private set; }
        public long Length { get; private set; }
        public long Position { get; private set; }

        public OpData(byte[] payload, long position)
        {
            Payload = payload;
            Hash = GetHashFor(payload);
            Length = Payload.Length + Hash.Length + 8;
            Position = position;
        }

        public OpData(byte[] payload, byte[] hash, long position)
        {
            Payload = payload;
            Hash = hash;
            Length = Payload.Length + Hash.Length + 8;
            Position = position;
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Position);
            writer.Write(Hash);
            writer.Write(Payload);
        }

        public static byte[] Seal(long gp, byte[]payload)
        {
            var op = new OpData(payload, gp);

            var storage = new MemoryStream();
            var writer = new BinaryWriter(storage);

            op.WriteTo(writer);

            return storage.ToArray();
        }

        public static OpData ReadFrom(byte[] record)
        {
            var reader = new BinaryReader(new MemoryStream(record));

            var position = reader.ReadInt64();
            var hash = reader.ReadBytes(16);

            var payloadLength = record.Length - hash.Length - 8;
            var payload = reader.ReadBytes(payloadLength);

            return new OpData(payload, hash, position);
        }

        public static byte[] GetHashFor(byte[] payload)
        {
            using (var md5 = MD5.Create())
                return md5.ComputeHash(new MemoryStream(payload));
        }
    }
}