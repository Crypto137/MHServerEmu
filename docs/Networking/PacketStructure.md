# Packet Structure

Packets are routed to various services using what is called mux. All packets have a six-byte header with the following structure:

```csharp
ushort MuxId;
UInt24 BodySize;
byte MuxCommand;
```

There are a total of five known mux commands:

```csharp
enum MuxCommand : byte
{
    Connect = 0x01,
    ConnectAck = 0x02,      // Expected response to Connect
    Disconnect = 0x03,
    ConnectWithData = 0x04, // Doesn't seem to be used, might be server <-> server only
    Data = 0x05
}
```

In data packets the header is followed by a body of length defined in the header that consists of serialized [Protocol Buffer (protobuf)](https://protobuf.dev/) messages. It is possible to extract protobuf schemas (.proto) needed to deserialize these messages from the main client executable using [protod](https://github.com/dennwc/protod). You can then use these schemas to generate the code needed for deserialization using, for example, the protogen tool included with [protobuf-csharp-port](https://github.com/jskeet/protobuf-csharp-port).

Each message has the following structure:

```csharp
varint MessageId;          // Same as index in its .proto schema
varint MessageSize;
byte[MessageSize] ProtobufPayload;
```

Varint, or variable-width integer, is an unsigned 64-bit integer encoded using the protobuf wire format. Each varint can take anywhere between one and ten bytes, with small values using fewer bytes. Varints are heavily used for encoding all sorts of data in the game's network protocol. For more information see [protobuf documentation on encoding](https://protobuf.dev/programming-guides/encoding/).
