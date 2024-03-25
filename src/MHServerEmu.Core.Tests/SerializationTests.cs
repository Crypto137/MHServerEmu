using Xunit.Abstractions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Core.Tests
{
    public class SerializationTests
    {
        const ulong TestReplicationPolicy = 0xEF;

        private readonly ITestOutputHelper _testOutputHelper;

        public SerializationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Archive_Transfer_PacksAndUnpacksPrimitives()
        {
            const ushort TestUShort = 1111;
            const int TestInt = 2222;
            const uint TestUInt = 3333;
            const long TestLong = 4444;
            const ulong TestULong = 5555;
            const float TestFloat = 6666.666f;

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                ushort ushortToPack = TestUShort;
                success &= archive.Transfer(ref ushortToPack);

                int intToPack = TestInt;
                success &= archive.Transfer(ref intToPack);

                uint uintToPack = TestUInt;
                success &= archive.Transfer(ref uintToPack);

                long longToPack = TestLong;
                success &= archive.Transfer(ref longToPack);

                ulong ulongToPack = TestULong;
                success &= archive.Transfer(ref ulongToPack);

                float floatToPack = TestFloat;
                success &= archive.Transfer(ref floatToPack);

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
                Assert.Equal(TestUShort, ushortToUnpack);

                int intToUnpack = 0;
                success &= archive.Transfer(ref intToUnpack);
                Assert.Equal(TestInt, intToUnpack);

                uint uintToUnpack = 0;
                success &= archive.Transfer(ref uintToUnpack);
                Assert.Equal(TestUInt, uintToUnpack);

                long longToUnpack = 0;
                success &= archive.Transfer(ref longToUnpack);
                Assert.Equal(TestLong, longToUnpack);

                ulong ulongToUnpack = 0;
                success &= archive.Transfer(ref ulongToUnpack);
                Assert.Equal(TestULong, ulongToUnpack);

                float floatToUnpack = 0f;
                success &= archive.Transfer(ref floatToUnpack);
                Assert.Equal(TestFloat, floatToUnpack);

                Assert.True(success);
            }
        }

        [Fact]
        public void Archive_Transfer_PacksAndUnpacksBools()
        {
            const bool TestBool1 = true;
            const bool TestBool2 = false;
            const bool TestBool3 = true;
            const bool TestBool4 = false;
            const bool TestBool5 = true;
            const bool TestBool6 = false;

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                bool bool1 = TestBool1;
                success &= archive.Transfer(ref bool1);

                bool bool2 = TestBool2;
                success &= archive.Transfer(ref bool2);

                bool bool3 = TestBool3;
                success &= archive.Transfer(ref bool3);

                bool bool4 = TestBool4;
                success &= archive.Transfer(ref bool4);

                bool bool5 = TestBool5;
                success &= archive.Transfer(ref bool5);

                bool bool6 = TestBool6;
                success &= archive.Transfer(ref bool6);

                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                bool bool1 = false;
                success &= archive.Transfer(ref bool1);
                Assert.Equal(TestBool1, bool1);

                bool bool2 = false;
                success &= archive.Transfer(ref bool2);
                Assert.Equal(TestBool2, bool2);

                bool bool3 = false;
                success &= archive.Transfer(ref bool3);
                Assert.Equal(TestBool3, bool3);

                bool bool4 = false;
                success &= archive.Transfer(ref bool4);
                Assert.Equal(TestBool4, bool4);

                bool bool5 = false;
                success &= archive.Transfer(ref bool5);
                Assert.Equal(TestBool5, bool5);

                bool bool6 = false;
                success &= archive.Transfer(ref bool6);
                Assert.Equal(TestBool6, bool6);

                Assert.True(success);
            }
        }

        [Fact]
        public void Archive_Transfer_PacksAndUnpacksVectors()
        {
            Vector3 TestVector1 = new(2000f, 1250f, 750f);
            Vector3 TestVector2 = new(128.333f, 524.12f, 423.1253f);

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                success &= archive.Transfer(ref TestVector1);
                success &= archive.Transfer(ref TestVector2);

                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                Vector3 vector1 = Vector3.Zero;
                success &= archive.Transfer(ref vector1);
                Assert.Equal(TestVector1.X, vector1.X);
                Assert.Equal(TestVector1.Y, vector1.Y);
                Assert.Equal(TestVector1.Z, vector1.Z);

                Vector3 vector2 = Vector3.Zero;
                success &= archive.Transfer(ref vector2);
                Assert.Equal(TestVector2.X, vector2.X);
                Assert.Equal(TestVector2.Y, vector2.Y);
                Assert.Equal(TestVector2.Z, vector2.Z);

                Assert.True(success);
            }
        }

        [Fact]
        public void Archive_Transfer_PacksAndUnpacksFixedFloat()
        {
            const float TestFloatPrecision0 = 1f;
            const float TestFloatPrecision3 = 2.125f;
            const float TestFloatPrecision6 = 3.328125f;
            Vector3 TestVector = new(4.375f, 5.500f, 6.625f);
            Orientation TestOrientation = new(0.765625f, 0.875000f, 0.984375f);

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                float floatPrecision0 = TestFloatPrecision0;
                success &= archive.TransferFloatFixed(ref floatPrecision0, 0);

                float floatPrecision3 = TestFloatPrecision3;
                success &= archive.TransferFloatFixed(ref floatPrecision3, 3);

                float floatPrecision6 = TestFloatPrecision6;
                success &= archive.TransferFloatFixed(ref floatPrecision6, 6);

                success &= archive.TransferVectorFixed(ref TestVector, 3);
                success &= archive.TransferOrientationFixed(ref TestOrientation, true, 6);
                success &= archive.TransferOrientationFixed(ref TestOrientation, false, 6);

                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                float floatPrecision0 = 0f;
                success &= archive.TransferFloatFixed(ref floatPrecision0, 0);
                Assert.Equal(TestFloatPrecision0, floatPrecision0);

                float floatPrecision3 = 0f;
                success &= archive.TransferFloatFixed(ref floatPrecision3, 3);
                Assert.Equal(TestFloatPrecision3, floatPrecision3);

                float floatPrecision6 = 0f;
                success &= archive.TransferFloatFixed(ref floatPrecision6, 6);
                Assert.Equal(TestFloatPrecision6, floatPrecision6);

                Vector3 vector = Vector3.Zero;
                success &= archive.TransferVectorFixed(ref vector, 3);
                Assert.Equal(TestVector.X, vector.X);
                Assert.Equal(TestVector.Y, vector.Y);
                Assert.Equal(TestVector.Z, vector.Z);

                Orientation orientationYawOnly = Orientation.Zero;
                success &= archive.TransferOrientationFixed(ref orientationYawOnly, true, 6);
                Assert.Equal(TestOrientation.Yaw, orientationYawOnly.Yaw);
                Assert.Equal(0f, orientationYawOnly.Pitch);
                Assert.Equal(0f, orientationYawOnly.Roll);

                Orientation orientationFull = Orientation.Zero;
                success &= archive.TransferOrientationFixed(ref orientationFull, false, 6);
                Assert.Equal(TestOrientation.Yaw, orientationFull.Yaw);
                Assert.Equal(TestOrientation.Pitch, orientationFull.Pitch);
                Assert.Equal(TestOrientation.Roll, orientationFull.Roll);

                Assert.True(success);
            }
        }

        [Fact]
        public void Archive_Transfer_PacksAndUnpacksISerialize()
        {
            TestISerialize TestISerialize = new()
            {
                BoolField1 = true,
                IntField1 = 100,
                IntField2 = -200,
                FloatField1 = 33.333f,
                BoolField2 = false,
                FloatField2 = -44.44f,
                BoolField3 = true,
                BoolField4 = false,
                BoolField5 = false,
                ULongField = 5555 << 33,
                BoolField6 = true
            };

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;
                ISerialize iserializeToPack = TestISerialize;
                success &= archive.Transfer(ref iserializeToPack);
                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                ISerialize iserializeToUnpack = new TestISerialize();
                success &= archive.Transfer(ref iserializeToUnpack);
                TestISerialize testISerialize = (TestISerialize)iserializeToUnpack;

                Assert.Equal(TestISerialize.BoolField1, testISerialize.BoolField1);
                Assert.Equal(TestISerialize.IntField1, testISerialize.IntField1);
                Assert.Equal(TestISerialize.IntField2, testISerialize.IntField2);
                Assert.Equal(TestISerialize.FloatField1, testISerialize.FloatField1);
                Assert.Equal(TestISerialize.BoolField2, testISerialize.BoolField2);
                Assert.Equal(TestISerialize.FloatField2, testISerialize.FloatField2);
                Assert.Equal(TestISerialize.ULongField, testISerialize.ULongField);
                Assert.Equal(TestISerialize.BoolField3, testISerialize.BoolField3);
                Assert.Equal(TestISerialize.BoolField4, testISerialize.BoolField4);
                Assert.Equal(TestISerialize.BoolField5, testISerialize.BoolField5);
                Assert.Equal(TestISerialize.BoolField6, testISerialize.BoolField6);

                Assert.True(success);
            }
        }

        [Fact]
        public void Archive_Transfer_PacksAndUnpacksUnencoded()
        {
            const byte TestByte = 111;
            const uint TestUInt = 2222;
            const ulong TestULong = 3333 << 33;

            byte[] buffer;

            using (Archive archive = new(ArchiveSerializeType.Replication, TestReplicationPolicy))
            {
                bool success = true;

                success &= archive.WriteSingleByte(TestByte);
                success &= archive.WriteSingleByte(TestByte);
                success &= archive.WriteUnencodedStream(TestUInt);
                success &= archive.WriteSingleByte(TestByte);
                success &= archive.WriteSingleByte(TestByte);
                success &= archive.WriteUnencodedStream(TestULong);
                success &= archive.WriteSingleByte(TestByte);
                success &= archive.WriteSingleByte(TestByte);

                Assert.True(success);

                buffer = archive.AccessAutoBuffer().ToArray();
            }

            _testOutputHelper.WriteLine($"ArchiveData: {buffer.ToHexString()}");

            using (Archive archive = new(ArchiveSerializeType.Replication, buffer))
            {
                bool success = true;

                Assert.Equal(TestReplicationPolicy, archive.ReplicationPolicy);

                byte testByte1 = 0;
                success &= archive.ReadSingleByte(ref testByte1);
                Assert.Equal(TestByte, testByte1);

                byte testByte2 = 0;
                success &= archive.ReadSingleByte(ref testByte2);
                Assert.Equal(TestByte, testByte2);

                uint testUInt = 0;
                success &= archive.ReadUnencodedStream(ref testUInt);
                Assert.Equal(TestUInt, testUInt);

                byte testByte3 = 0;
                success &= archive.ReadSingleByte(ref testByte3);
                Assert.Equal(TestByte, testByte3);

                byte testByte4 = 0;
                success &= archive.ReadSingleByte(ref testByte4);
                Assert.Equal(TestByte, testByte4);

                ulong testULong = 0;
                success &= archive.ReadUnencodedStream(ref testULong);
                Assert.Equal(TestULong, testULong);

                byte testByte5 = 0;
                success &= archive.ReadSingleByte(ref testByte5);
                Assert.Equal(TestByte, testByte5);

                byte testByte6 = 0;
                success &= archive.ReadSingleByte(ref testByte6);
                Assert.Equal(TestByte, testByte6);

                Assert.True(success);
            }
        }

        [Fact]
        public void Archive_Transfer_UnpacksAvatarStateUpdates()
        {
            byte[] MouseInputUpdate = Convert.FromHexString("0100C9F7FD0601012CF453FE02801605010102F453FE02801000AC3A81030600");
            byte[] GamepadInputUpdate = Convert.FromHexString("0100C9F7FD068101248A4AD50180167401028A4AD501801600BC329641FD0500");

            using (Archive archive = new(ArchiveSerializeType.Replication, MouseInputUpdate))
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

            using (Archive archive = new(ArchiveSerializeType.Replication, GamepadInputUpdate))
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
            }
        }

        class TestISerialize : ISerialize
        {
            private bool _boolField1;
            private int _intField1;
            private int _intField2;
            private float _floatField1;
            private bool _boolField2;
            private float _floatField2;
            private bool _boolField3;
            private bool _boolField4;
            private bool _boolField5;
            private ulong _ulongField;
            private bool _boolField6;

            public bool BoolField1 { get => _boolField1; set => _boolField1 = value; }
            public int IntField1 { get => _intField1; set => _intField1 = value; }
            public int IntField2 { get => _intField2; set => _intField2 = value; }
            public float FloatField1 { get => _floatField1; set => _floatField1 = value; }
            public bool BoolField2 { get => _boolField2; set => _boolField2 = value; }
            public float FloatField2 { get => _floatField2; set => _floatField2 = value; }
            public bool BoolField3 { get => _boolField3; set => _boolField3 = value; }
            public bool BoolField4 { get => _boolField4; set => _boolField4 = value; }
            public bool BoolField5 { get => _boolField5; set => _boolField5 = value; }
            public ulong ULongField { get => _ulongField; set => _ulongField = value; }
            public bool BoolField6 { get => _boolField6; set => _boolField6 = value; }

            public bool Serialize(Archive archive)
            {
                bool success = true;
                success &= archive.Transfer(ref _boolField1);
                success &= archive.Transfer(ref _intField1);
                success &= archive.Transfer(ref _intField2);
                success &= archive.Transfer(ref _floatField1);
                success &= archive.Transfer(ref _boolField2);
                success &= archive.Transfer(ref _floatField2);
                success &= archive.Transfer(ref _boolField3);
                success &= archive.Transfer(ref _boolField4);
                success &= archive.Transfer(ref _boolField5);
                success &= archive.Transfer(ref _ulongField);
                success &= archive.Transfer(ref _boolField6);

                return success;
            }
        }
    }
}