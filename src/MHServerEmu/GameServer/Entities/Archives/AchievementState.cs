using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class AchievementState
    {
        public ulong Id { get; set; }
        public ulong Count { get; set; }
        public ulong CompletionDate { get; set; }
        public bool ModifiedSinceCheckpoint { get; set; }

        public AchievementState(CodedInputStream stream, BoolBuffer boolBuffer)
        {
            Id = stream.ReadRawVarint64();
            Count = stream.ReadRawVarint64();
            CompletionDate = stream.ReadRawVarint64();
            if (boolBuffer.IsEmpty) boolBuffer.SetBits(stream.ReadRawByte());
            ModifiedSinceCheckpoint = boolBuffer.ReadBool();
        }

        public AchievementState(ulong id, ulong count, ulong completionDate, bool modifiedSinceCheckpoint)
        {
            Id = id;
            Count = count;
            CompletionDate = completionDate;
            ModifiedSinceCheckpoint = modifiedSinceCheckpoint;
        }

        public byte[] Encode()
        {
            return Array.Empty<byte>();
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Id: 0x{Id.ToString("X")}");
                streamWriter.WriteLine($"Count: 0x{Count.ToString("X")}");
                streamWriter.WriteLine($"CompletionDate: 0x{CompletionDate.ToString("X")}");
                streamWriter.WriteLine($"ModifiedSinceCheckpoint: {ModifiedSinceCheckpoint}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
