# Overview

*NOTE: This overview is somewhat outdated and is not representative of the latest research. Feel free to ask on Discord if you have questions.*

Marvel Heroes uses [Protocol Buffers](https://protobuf.dev/) for network message serialization. It is possible to extract protobuf schemas (.proto) from the main client executable using [protod](https://github.com/dennwc/protod) and then use them with tools such as protogen included with [protobuf-csharp-port](https://github.com/jskeet/protobuf-csharp-port).

Protobuf payloads have the following structure:

```
varint MessageId          // same as index in its .proto schema
varint MessageSize        // in bytes
byte[] EncodedMessage
```

For more information on varint see [protobuf documentation on encoding](https://protobuf.dev/programming-guides/encoding/).

When a player tries to log in, the client sends an HTTP POST request to the auth server specified in SiteConfig.xml over https. The auth server responds with either an error message via an HTTP status code or an AuthTicket payload that contains session info (id, token, AES-256 encryption key), as well as the address and port of the frontend server that the client proceeds to connect to.

The frontend server routes messages to various services using what is called mux. Communication with the frontend server requires a 6 byte header with the following structure:

```
ushort MuxId        // used to determine where to route the message on the backend
UInt24 BodySize     // in bytes
byte Command
```

There are a total of 5 known mux commands:

```
enum MuxCommand
{
    Connect = 0x01,
    ConnectAck = 0x02,      // Expected response to Connect
    Disconnect = 0x03,
    ConnectWithData = 0x04, // Doesn't seem to be used, might be server <-> server only
    Data = 0x05             // Requires one or more protobuf payloads as body
}
```

After establishing a mux connection with the frontend server, the client sends a ClientCredentials message that contains a session id and a session token encrypted using the AES-256 key provided in the AuthTicket message along with a specified random IV.

The frontend server can respond with either a LoginQueueStatus to display a login queue screen in the client, or a SessionEncryptionChanged to proceed with logging in. It's currently unclear how SessionEncryptionChanged functions.

The client responds to the SessionEncryptionChanged message with two messages in a row: InitialClientHandshake with PLAYERMGR_SERVER_FRONTEND followed by  NetMessageReadyForGameJoin that is most likely supposed to be routed to the server specified in the handshake.

After that roughly the following sequence happens:

- The server responds with NetMessageReadyAndLoggedIn.

- The client connects on mux id 2 and sends InitialClientHandshake for GROUPING_MANAGER_FRONTEND. It appears mux id 2 is used for communicating with GROUPING_MANAGER_FRONTEND.

- The server initiates time sync with NetMessageInitialTimeSync.

- After syncing time the client starts sending NetMessagePing periodically.

- The server queues a loading screen with NetMessageQueueLoadingScreen and begins sending a lot of data required for game initialization. The specifics are under investigation.
