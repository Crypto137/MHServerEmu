using Xunit.Abstractions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Tests.Serialization
{
    public class ArchiveTests
    {
        const ulong TestReplicationPolicy = 0xEF;

        private readonly ITestOutputHelper _testOutputHelper;

        public ArchiveTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(64)]
        public void Transfer_Bool_PacksAndUnpacks(int numBools)
        {
            bool[] testBools = new bool[numBools];
            for (int i = 0; i < testBools.Length; i++)
                testBools[i] = i % 2 == 0;  // true/false/true/false pattern

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                for (int i = 0; i < testBools.Length; i++)
                {
                    bool boolToEncode = testBools[i];
                    success &= archive.Transfer(ref boolToEncode);
                }

                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                for (int i = 0; i < testBools.Length; i++)
                {
                    bool boolToDecode = false;
                    success &= archive.Transfer(ref boolToDecode);
                    Assert.Equal(testBools[i], boolToDecode);
                }

                Assert.True(success);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void Transfer_UShort_PacksAndUnpacks(ushort testUShort)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                ushort ushortToPack = testUShort;
                success &= archive.Transfer(ref ushortToPack);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                ushort ushortToUnpack = 0;
                success &= archive.Transfer(ref ushortToUnpack);
                Assert.True(success);

                Assert.Equal(testUShort, ushortToUnpack);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(-200)]
        public void Transfer_Int_PacksAndUnpacks(int testInt)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                int intToPack = testInt;
                success &= archive.Transfer(ref intToPack);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                int intToUnpack = 0;
                success &= archive.Transfer(ref intToUnpack);
                Assert.True(success);

                Assert.Equal(testInt, intToUnpack);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void Transfer_UInt_PacksAndUnpacks(uint testUInt)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                uint uintToPack = testUInt;
                success &= archive.Transfer(ref uintToPack);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                uint uintToUnpack = 0;
                success &= archive.Transfer(ref uintToUnpack);
                Assert.True(success);

                Assert.Equal(testUInt, uintToUnpack);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(429496729600)]
        [InlineData(-200)]
        public void Transfer_Long_PacksAndUnpacks(long testLong)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                long longToPack = testLong;
                success &= archive.Transfer(ref longToPack);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                long longToUnpack = 0;
                success &= archive.Transfer(ref longToUnpack);
                Assert.True(success);

                Assert.Equal(testLong, longToUnpack);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(429496729600)]
        public void Transfer_ULong_PacksAndUnpacks(ulong testULong)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                ulong ulongToPack = testULong;
                success &= archive.Transfer(ref ulongToPack);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                ulong ulongToUnpack = 0;
                success &= archive.Transfer(ref ulongToUnpack);
                Assert.True(success);

                Assert.Equal(testULong, ulongToUnpack);
            }
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(100f)]
        [InlineData(222.222f)]
        [InlineData(-333.33f)]
        public void Transfer_Float_PacksAndUnpacks(float testFloat)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                float floatToPack = testFloat;
                success &= archive.Transfer(ref floatToPack);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                float floatToUnpack = 0;
                success &= archive.Transfer(ref floatToUnpack);
                Assert.True(success);

                Assert.Equal(testFloat, floatToUnpack);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("hello world")]
        [InlineData("привет мир")]
        [InlineData("1234567890")]
        public void Transfer_String_PacksAndUnpacks(string testString)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                string stringToPack = testString;
                success &= archive.Transfer(ref stringToPack);

                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                string stringToUnpack = null;
                success &= archive.Transfer(ref stringToUnpack);
                Assert.True(success);

                Assert.Equal(testString, stringToUnpack);
            }
        }

        [Theory]
        [InlineData(2000f, 1250f, 750f)]
        [InlineData(128.333f, -524.12f, 423.1253f)]
        public void Transfer_Vector_PacksAndUnpacks(float x, float y, float z)
        {
            Vector3 testVector = new(x, y, z);

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                Vector3 vectorToPack = new(testVector);
                success &= archive.Transfer(ref vectorToPack);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                Vector3 vectorToUnpack = Vector3.Zero;
                success &= archive.Transfer(ref vectorToUnpack);
                Assert.True(success);

                Assert.Equal(testVector, vectorToUnpack);
            }
        }

        [Theory]
        [InlineData(1f, 0)]
        [InlineData(2.125f, 3)]
        [InlineData(3.328125f, 6)]
        [InlineData(-3.328125f, 6)]
        public void Transfer_FixedFloat_PacksAndUnpacks(float testFloat, int precision)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                float floatToPack = testFloat;
                success &= archive.TransferFloatFixed(ref floatToPack, precision);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                float floatToUnpack = 0f;
                success &= archive.TransferFloatFixed(ref floatToUnpack, precision);
                Assert.True(success);

                Assert.Equal(testFloat, floatToUnpack);
            }
        }

        [Theory]
        [InlineData(4.375f, 5.500f, 6.625f, 3)]
        [InlineData(-4.375f, -5.500f, -6.625f, 3)]
        public void Transfer_FixedVector_PacksAndUnpacks(float x, float y, float z, int precision)
        {
            Vector3 testVector = new(x, y, z);

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                Vector3 vectorToPack = new(testVector);
                success &= archive.TransferVectorFixed(ref vectorToPack, precision);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                Vector3 vectorToUnpack = Vector3.Zero;
                success &= archive.TransferVectorFixed(ref vectorToUnpack, precision);
                Assert.True(success);

                Assert.Equal(testVector, vectorToUnpack);
            }
        }

        [Theory]
        [InlineData(0.765625f, 0.875000f, 0.984375f, true, 6)]
        [InlineData(0.765625f, 0.875000f, 0.984375f, false, 6)]
        [InlineData(-0.765625f, -0.875000f, -0.984375f, false, 6)]
        public void Transfer_FixedOrientation_PacksAndUnpacks(float yaw, float pitch, float roll, bool yawOnly, int precision)
        {
            Orientation testOrientation = new(yaw, pitch, roll);

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                Orientation orientationToPack = new(testOrientation);
                success &= archive.TransferOrientationFixed(ref orientationToPack, yawOnly, precision);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                Orientation orientationToUnpack = Orientation.Zero;
                success &= archive.TransferOrientationFixed(ref orientationToUnpack, yawOnly, precision);
                Assert.True(success);

                if (yawOnly)
                    Assert.Equal(testOrientation.Yaw, orientationToUnpack.Yaw);
                else
                    Assert.Equal(testOrientation, orientationToUnpack);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(111)]
        public void WriteUnencoded_Byte_PacksAndUnpacks(byte testByte)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                byte byteToWrite = testByte;
                success &= archive.WriteSingleByte(byteToWrite);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                byte byteToRead = 0;
                success &= archive.ReadSingleByte(ref byteToRead);
                Assert.True(success);

                Assert.Equal(testByte, byteToRead);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(111)]
        [InlineData(7274496)]
        public void WriteUnencoded_UInt_PacksAndUnpacks(uint testUInt)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                uint uintToWrite = testUInt;
                success &= archive.WriteUnencodedStream(uintToWrite);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                uint uintToRead = 0;
                success &= archive.ReadUnencodedStream(ref uintToRead);
                Assert.True(success);

                Assert.Equal(testUInt, uintToRead);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(111)]
        [InlineData(429496729600)]
        public void WriteUnencoded_ULong_PacksAndUnpacks(ulong testULong)
        {
            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                ulong ulongToWrite = testULong;
                success &= archive.WriteUnencodedStream(ulongToWrite);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                ulong ulongToRead = 0;
                success &= archive.ReadUnencodedStream(ref ulongToRead);
                Assert.True(success);

                Assert.Equal(testULong, ulongToRead);
            }
        }

        [Fact]
        public void Serialize_FakeISerialize_PacksAndUnpacks()
        {
            FakeISerialize fakeISerialize = new()
            {
                BoolField1 = true,
                IntField1 = 100,
                IntField2 = -200,
                FloatField1 = 33.333f,
                BoolField2 = false,
                FloatField2 = -44.44f,
                BoolField3 = true,
                BoolField4 = false,
                StringField = "test",
                BoolField5 = false,
                ULongField = 5555ul << 32,
                BoolField6 = true
            };

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;
                ISerialize iserializeToPack = fakeISerialize.Clone();
                success &= archive.Transfer(ref iserializeToPack);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                ISerialize iserializeToUnpack = new FakeISerialize();
                success &= archive.Transfer(ref iserializeToUnpack);
                Assert.True(success);

                Assert.Equal(fakeISerialize, (FakeISerialize)iserializeToUnpack);
            }
        }

        [Fact]
        public void Serialize_UpdateAvatarState_UnpacksWithMouseInput()
        {
            byte[] buffer = Convert.FromHexString("0100C9F7FD0601012CF453FE02801605010102F453FE02801000AC3A81030600");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(0x1ul, archive.ReplicationPolicy);     // Proximity

                int avatarIndex = 0;
                success &= archive.Transfer(ref avatarIndex);
                Assert.Equal(0, avatarIndex);

                ulong entityId = 0;
                success &= archive.Transfer(ref entityId);
                Assert.Equal(14646217ul, entityId);

                bool isUsingGamepadInput = false;
                success &= archive.Transfer(ref isUsingGamepadInput);
                Assert.False(isUsingGamepadInput);

                uint avatarWorldInstanceId = 0;
                success &= archive.Transfer(ref avatarWorldInstanceId);
                Assert.Equal(1u, avatarWorldInstanceId);

                uint fieldFlags = 0;
                success &= archive.Transfer(ref fieldFlags);
                Assert.Equal(0x2cu, fieldFlags);    // Flag2, HasLocomotionFlags, UpdatePathNodes

                Vector3 position = Vector3.Zero;
                success &= archive.TransferVectorFixed(ref position, 3);
                Assert.Equal(671.25f, position.X);
                Assert.Equal(23.875f, position.Y);
                Assert.Equal(176f, position.Z);

                Orientation orientation = Orientation.Zero;
                success &= archive.TransferOrientationFixed(ref orientation, true, 6);
                Assert.Equal(-0.046875f, orientation.Yaw);
                Assert.Equal(0f, orientation.Pitch);
                Assert.Equal(0f, orientation.Roll);

                ulong locomotionFlags = 0;
                success &= archive.Transfer(ref locomotionFlags);
                Assert.Equal(0x1ul, locomotionFlags);   // Flag0

                uint pathGoalNodeIndex = 0;
                success &= archive.Transfer(ref pathGoalNodeIndex);
                Assert.Equal(0x1u, pathGoalNodeIndex);

                uint numPathNodes = 0;
                success &= archive.Transfer(ref numPathNodes);
                Assert.Equal(0x2u, numPathNodes);

                Vector3 vertex0 = Vector3.Zero;
                success &= archive.TransferVectorFixed(ref vertex0, 3);
                Assert.Equal(671.25f, vertex0.X);
                Assert.Equal(23.875f, vertex0.Y);
                Assert.Equal(128f, vertex0.Z);

                int vertexSideRadius0 = 0;
                success &= archive.Transfer(ref vertexSideRadius0);
                Assert.Equal(0, vertexSideRadius0);

                Vector3 vertex1 = Vector3.Zero;
                success &= archive.TransferVectorFixed(ref vertex1, 3);
                Assert.Equal(466.75f, vertex1.X);
                Assert.Equal(-24.125f, vertex1.Y);
                Assert.Equal(0.375f, vertex1.Z);

                int vertexSideRadius1 = 0;
                success &= archive.Transfer(ref vertexSideRadius1);
                Assert.Equal(0, vertexSideRadius1);

                Assert.True(success);
            }
        }

        [Fact]
        public void Serialize_UpdateAvatarState_UnpacksWithGamepadInput()
        {
            byte[] buffer = Convert.FromHexString("0100C9F7FD068101248A4AD50180167401028A4AD501801600BC329641FD0500");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(0x1ul, archive.ReplicationPolicy);     // Proximity

                int avatarIndex = 0;
                success &= archive.Transfer(ref avatarIndex);
                Assert.Equal(0, avatarIndex);

                ulong entityId = 0;
                success &= archive.Transfer(ref entityId);
                Assert.Equal(14646217ul, entityId);

                bool isUsingGamepadInput = false;
                success &= archive.Transfer(ref isUsingGamepadInput);
                Assert.True(isUsingGamepadInput);

                uint avatarWorldInstanceId = 0;
                success &= archive.Transfer(ref avatarWorldInstanceId);
                Assert.Equal(1u, avatarWorldInstanceId);

                uint fieldFlags = 0;
                success &= archive.Transfer(ref fieldFlags);
                Assert.Equal(0x24u, fieldFlags);    // Flag2, UpdatePathNodes

                Vector3 position = Vector3.Zero;
                success &= archive.TransferVectorFixed(ref position, 3);
                Assert.Equal(592.625f, position.X);
                Assert.Equal(-13.375f, position.Y);
                Assert.Equal(176f, position.Z);

                Orientation orientation = Orientation.Zero;
                success &= archive.TransferOrientationFixed(ref orientation, true, 6);
                Assert.Equal(0.90625f, orientation.Yaw);
                Assert.Equal(0f, orientation.Pitch);
                Assert.Equal(0f, orientation.Roll);

                uint pathGoalNodeIndex = 0;
                success &= archive.Transfer(ref pathGoalNodeIndex);
                Assert.Equal(0x1u, pathGoalNodeIndex);

                uint numPathNodes = 0;
                success &= archive.Transfer(ref numPathNodes);
                Assert.Equal(0x2u, numPathNodes);

                Vector3 vertex0 = Vector3.Zero;
                success &= archive.TransferVectorFixed(ref vertex0, 3);
                Assert.Equal(592.625f, vertex0.X);
                Assert.Equal(-13.375f, vertex0.Y);
                Assert.Equal(176f, vertex0.Z);

                int vertexSideRadius0 = 0;
                success &= archive.Transfer(ref vertexSideRadius0);
                Assert.Equal(0, vertexSideRadius0);

                Vector3 vertex1 = Vector3.Zero;
                success &= archive.TransferVectorFixed(ref vertex1, 3);
                Assert.Equal(403.75f, vertex1.X);
                Assert.Equal(521.375f, vertex1.Y);
                Assert.Equal(-47.875f, vertex1.Z);

                int vertexSideRadius1 = 0;
                success &= archive.Transfer(ref vertexSideRadius1);
                Assert.Equal(0, vertexSideRadius1);

                Assert.True(success);
            }
        }
    }
}