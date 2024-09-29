using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Helpers
{
    /// <summary>
    /// Dedicated place for protobuf-csharp-port reflection hackery.
    /// </summary>
    public static class ProtobufHelper
    {
        public static class CodedOutputStreamEx
        {
            private static readonly Func<Stream, byte[], CodedOutputStream> CreateInstanceDelegate;

            static CodedOutputStreamEx()
            {
                // protobuf-csharp-port is dumb and hides the constructor that accepts external buffers,
                // so we have to emit the code to access it at runtime. Essentially, we are creating an
                // extra CodedOutputStream.CreateInstance() overload here.

                Type[] argTypes = new Type[] { typeof(Stream), typeof(byte[]) };

                DynamicMethod dm = new("CreateInstance", typeof(CodedOutputStream), argTypes);
                ILGenerator il = dm.GetILGenerator();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Newobj, typeof(CodedOutputStream).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, argTypes));
                il.Emit(OpCodes.Ret);

                CreateInstanceDelegate = dm.CreateDelegate<Func<Stream, byte[], CodedOutputStream>>();
            }

            /// <summary>
            /// Creates a new <see cref="CodedOutputStream"/> that uses the provided buffer.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static CodedOutputStream CreateInstance(Stream stream, byte[] buffer)
            {
                return CreateInstanceDelegate(stream, buffer);
            }
        }
    }
}
