using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Google.ProtocolBuffers;
using Google.ProtocolBuffers.Descriptors;
using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Core.Serialization
{
    /// <summary>
    /// A more memory efficient version of <see cref="CodedOutputStream"/>.
    /// </summary>
    public sealed class RecyclableCodedOutputStream : ICodedOutputStreamEx, IDisposable
    {
        private static readonly ConcurrentBag<RecyclableCodedOutputStream> Instances = new();

        private readonly byte[] _primaryBuffer = new byte[CodedOutputStream.DefaultBufferSize];
        private readonly byte[] _floatBuffer = new byte[sizeof(float)];

        private CodedOutputStream _cos;

        private RecyclableCodedOutputStream() { }

        private void Initialize(Stream stream)
        {
            _cos = ProtobufHelper.CodedOutputStreamEx.CreateInstance(stream, _primaryBuffer);
        }

        public static RecyclableCodedOutputStream CreateInstance(Stream stream)
        {
            if (Instances.TryTake(out RecyclableCodedOutputStream cos) == false)
                cos = new();

            cos.Initialize(stream);
            return cos;
        }

        #region IDisposable

        public void Dispose()
        {
            if (_cos != null)
            {
                _cos.Flush();
                _cos = null;
            }

            Instances.Add(this);
        }

        #endregion

        #region ICodedOutputStream

        // For most of this we just pass everything to the default implementation.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush()
        {
            _cos.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteArray(FieldType fieldType, int fieldNumber, string fieldName, IEnumerable list)
        {
            _cos.WriteArray(fieldType, fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(int fieldNumber, string fieldName, bool value)
        {
            _cos.WriteBool(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBoolArray(int fieldNumber, string fieldName, IEnumerable<bool> list)
        {
            _cos.WriteBoolArray(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(int fieldNumber, string fieldName, ByteString value)
        {
            _cos.WriteBytes(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytesArray(int fieldNumber, string fieldName, IEnumerable<ByteString> list)
        {
            _cos.WriteBytesArray(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(int fieldNumber, string fieldName, double value)
        {
            _cos.WriteDouble(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDoubleArray(int fieldNumber, string fieldName, IEnumerable<double> list)
        {
            _cos.WriteDoubleArray(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteEnum(int fieldNumber, string fieldName, int value, object rawValue)
        {
            _cos.WriteEnum(fieldNumber, fieldName, value, rawValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteEnumArray<T>(int fieldNumber, string fieldName, IEnumerable<T> list) where T : struct, IComparable, IFormattable
        {
            _cos.WriteEnumArray(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteField(FieldType fieldType, int fieldNumber, string fieldName, object value)
        {
            _cos.WriteField(fieldType, fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixed32(int fieldNumber, string fieldName, uint value)
        {
            _cos.WriteFixed32(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixed32Array(int fieldNumber, string fieldName, IEnumerable<uint> list)
        {
            _cos.WriteFixed32Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixed64(int fieldNumber, string fieldName, ulong value)
        {
            _cos.WriteFixed64(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFixed64Array(int fieldNumber, string fieldName, IEnumerable<ulong> list)
        {
            _cos.WriteFixed64Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloat(int fieldNumber, string fieldName, float value)
        {
            //_cos.WriteFloat(fieldNumber, fieldName, value);

            _cos.WriteTag(fieldNumber, WireFormat.WireType.Fixed32);

            MemoryMarshal.Cast<byte, float>(_floatBuffer)[0] = value;
            _cos.WriteRawBytes(_floatBuffer, 0, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloatArray(int fieldNumber, string fieldName, IEnumerable<float> list)
        {
            _cos.WriteFloatArray(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteGroup(int fieldNumber, string fieldName, IMessageLite value)
        {
            _cos.WriteGroup(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteGroupArray<T>(int fieldNumber, string fieldName, IEnumerable<T> list) where T : IMessageLite
        {
            _cos.WriteGroupArray(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int fieldNumber, string fieldName, int value)
        {
            _cos.WriteInt32(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32Array(int fieldNumber, string fieldName, IEnumerable<int> list)
        {
            _cos.WriteInt32Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(int fieldNumber, string fieldName, long value)
        {
            _cos.WriteInt64(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64Array(int fieldNumber, string fieldName, IEnumerable<long> list)
        {
            _cos.WriteInt64Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMessage(int fieldNumber, string fieldName, IMessageLite value)
        {
            _cos.WriteMessage(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMessageArray<T>(int fieldNumber, string fieldName, IEnumerable<T> list) where T : IMessageLite
        {
            _cos.WriteMessageArray(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMessageEnd()
        {
            _cos.Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMessageSetExtension(int fieldNumber, string fieldName, IMessageLite value)
        {
            _cos.WriteMessageSetExtension(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMessageSetExtension(int fieldNumber, string fieldName, ByteString value)
        {
            _cos.WriteMessageSetExtension(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteMessageStart()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedArray(FieldType fieldType, int fieldNumber, string fieldName, IEnumerable list)
        {
            _cos.WritePackedArray(fieldType, fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedBoolArray(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<bool> list)
        {
            _cos.WritePackedBoolArray(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedDoubleArray(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<double> list)
        {
            _cos.WritePackedDoubleArray(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedEnumArray<T>(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<T> list) where T : struct, IComparable, IFormattable
        {
            _cos.WritePackedEnumArray(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedFixed32Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<uint> list)
        {
            _cos.WritePackedFixed32Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedFixed64Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<ulong> list)
        {
            _cos.WritePackedFixed64Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedFloatArray(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<float> list)
        {
            _cos.WritePackedFloatArray(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedInt32Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<int> list)
        {
            _cos.WritePackedInt32Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedInt64Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<long> list)
        {
            _cos.WritePackedInt64Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedSFixed32Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<int> list)
        {
            _cos.WritePackedSFixed32Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedSFixed64Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<long> list)
        {
            _cos.WritePackedSFixed64Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedSInt32Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<int> list)
        {
            _cos.WritePackedSInt32Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedSInt64Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<long> list)
        {
            _cos.WritePackedSInt64Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedUInt32Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<uint> list)
        {
            _cos.WritePackedUInt32Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WritePackedUInt64Array(int fieldNumber, string fieldName, int calculatedSize, IEnumerable<ulong> list)
        {
            _cos.WritePackedUInt64Array(fieldNumber, fieldName, calculatedSize, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSFixed32(int fieldNumber, string fieldName, int value)
        {
            _cos.WriteSFixed32(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSFixed32Array(int fieldNumber, string fieldName, IEnumerable<int> list)
        {
            _cos.WriteSFixed32Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSFixed64(int fieldNumber, string fieldName, long value)
        {
            _cos.WriteSFixed64(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSFixed64Array(int fieldNumber, string fieldName, IEnumerable<long> list)
        {
            _cos.WriteSFixed64Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSInt32(int fieldNumber, string fieldName, int value)
        {
            _cos.WriteSInt32(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSInt32Array(int fieldNumber, string fieldName, IEnumerable<int> list)
        {
            _cos.WriteSInt32Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSInt64(int fieldNumber, string fieldName, long value)
        {
            _cos.WriteSInt64(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSInt64Array(int fieldNumber, string fieldName, IEnumerable<long> list)
        {
            _cos.WriteSInt64Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(int fieldNumber, string fieldName, string value)
        {
            _cos.WriteString(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteStringArray(int fieldNumber, string fieldName, IEnumerable<string> list)
        {
            _cos.WriteStringArray(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(int fieldNumber, string fieldName, uint value)
        {
            _cos.WriteUInt32(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32Array(int fieldNumber, string fieldName, IEnumerable<uint> list)
        {
            _cos.WriteUInt32Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64(int fieldNumber, string fieldName, ulong value)
        {
            _cos.WriteUInt64(fieldNumber, fieldName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64Array(int fieldNumber, string fieldName, IEnumerable<ulong> list)
        {
            _cos.WriteUInt64Array(fieldNumber, fieldName, list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUnknownBytes(int fieldNumber, ByteString value)
        {
            _cos.WriteUnknownBytes(fieldNumber, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUnknownField(int fieldNumber, WireFormat.WireType wireType, ulong value)
        {
            _cos.WriteUnknownField(fieldNumber, wireType, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Obsolete]
        public void WriteUnknownGroup(int fieldNumber, IMessageLite value)
        {
            _cos.WriteUnknownGroup(fieldNumber, value);
        }

        #endregion

        #region ICodedOutputStreamEx

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRawVarint32(uint value)
        {
            _cos.WriteRawVarint32(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRawVarint64(ulong value)
        {
            _cos.WriteRawVarint64(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRawByte(byte value)
        {
            _cos.WriteRawByte(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRawBytes(byte[] value)
        {
            _cos.WriteRawBytes(value);
        }

        #endregion
    }
}
