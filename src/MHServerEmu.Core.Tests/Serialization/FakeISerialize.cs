using MHServerEmu.Core.Serialization;

namespace MHServerEmu.Core.Tests.Serialization
{
    /// <summary>
    /// Fake <see cref="ISerialize"/> implementation for testing.
    /// </summary>
    internal class FakeISerialize : ISerialize, IEquatable<FakeISerialize>
    {
        private bool _boolField1;
        private int _intField1;
        private int _intField2;
        private uint _unencodedUIntField;
        private float _floatField1;
        private bool _boolField2;
        private float _floatField2;
        private bool _boolField3;
        private bool _boolField4;
        private string _stringField;
        private bool _boolField5;
        private ulong _ulongField;
        private bool _boolField6;

        public bool BoolField1 { get => _boolField1; set => _boolField1 = value; }
        public int IntField1 { get => _intField1; set => _intField1 = value; }
        public int IntField2 { get => _intField2; set => _intField2 = value; }
        public uint UnencodedUIntField { get => _unencodedUIntField; set => _unencodedUIntField = value; }
        public float FloatField1 { get => _floatField1; set => _floatField1 = value; }
        public bool BoolField2 { get => _boolField2; set => _boolField2 = value; }
        public float FloatField2 { get => _floatField2; set => _floatField2 = value; }
        public bool BoolField3 { get => _boolField3; set => _boolField3 = value; }
        public bool BoolField4 { get => _boolField4; set => _boolField4 = value; }
        public string StringField { get => _stringField; set => _stringField = value; }
        public bool BoolField5 { get => _boolField5; set => _boolField5 = value; }
        public ulong ULongField { get => _ulongField; set => _ulongField = value; }
        public bool BoolField6 { get => _boolField6; set => _boolField6 = value; }

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= archive.Transfer(ref _boolField1);
            success &= archive.Transfer(ref _intField1);
            success &= archive.Transfer(ref _intField2);

            if (archive.IsPacking)
                success &= archive.WriteUnencodedStream(_unencodedUIntField);
            else
                success &= archive.ReadUnencodedStream(ref _unencodedUIntField);

            success &= archive.Transfer(ref _floatField1);
            success &= archive.Transfer(ref _boolField2);
            success &= archive.Transfer(ref _floatField2);
            success &= archive.Transfer(ref _boolField3);
            success &= archive.Transfer(ref _boolField4);
            success &= archive.Transfer(ref _stringField);
            success &= archive.Transfer(ref _boolField5);
            success &= archive.Transfer(ref _ulongField);
            success &= archive.Transfer(ref _boolField6);
            return success;
        }

        public FakeISerialize Clone() => (FakeISerialize)MemberwiseClone();

        public bool Equals(FakeISerialize other)
        {
            bool equals = true;
            equals &= _boolField1 == other._boolField1;
            equals &= _intField1 == other._intField1;
            equals &= _intField2 == other._intField2;
            equals &= _unencodedUIntField == other._unencodedUIntField;
            equals &= _floatField1 == other._floatField1;
            equals &= _boolField2 == other._boolField2;
            equals &= _floatField2 == other._floatField2;
            equals &= _boolField3 == other._boolField3;
            equals &= _boolField4 == other._boolField4;
            equals &= _stringField == other._stringField;
            equals &= _boolField5 == other._boolField5;
            equals &= _ulongField == other._ulongField;
            equals &= _boolField6 == other._boolField6;
            return equals;
        }
    }
}
