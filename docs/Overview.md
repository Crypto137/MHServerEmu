# Overview

Marvel Heroes uses [Protocol Buffers](https://protobuf.dev/) for network message serialization. It is possible to extract protobuf schemas (.proto) from the main client executable using [protod](https://github.com/dennwc/protod) and then use them with tools such as protogen included with [protobuf-csharp-port](https://github.com/jskeet/protobuf-csharp-port).

Protobuf payloads follow the following structure:

```
byte MessageId          // same as index in its .proto schema
byte MessageSize        // in bytes
byte[] EncodedMessage
```

When a player tries to log in, the client first requests an AuthTicket protobuf message from the auth server specified in SiteConfig.xml over https. This message contains session info (id, token, AES-256 encryption key), as well as the frontend server address and port that the client proceeds to connect to.

The frontend server routes messages to various services using what is called mux. Communication with the frontend server requires a 6 byte header with the following structure:

```
ushort MuxId        // used to determine where to route the message on the backend
UInt24 BodySize     // in bytes
byte Command
```

There are a total of 5 mux commands:

```
enum MuxCommand
{
    Connect = 0x01,
    Accept = 0x02,      // Expected response to Connect and Insert
    Disconnect = 0x03,
    Insert = 0x04,      // Purpose unclear, works similar to connect
    Message = 0x05      // Requires a protobuf payload as body
}
```

After establishing a mux connection with the frontend server, the client sends a ClientCredentials message that contains a session id and a session token encrypted using the AES-256 key provided in the AuthTicket message along with a specified random IV.

The frontend server can respond with either a LoginQueueStatus to display a login queue screen in the client, or a SessionEncryptionChanged to proceed with authorization. It's currently unclear how SessionEncryptionChanged functions.

The client responds to the SessionEncryptionChanged message with two messages in a row: InitialClientHandshake with PLAYERMGR_SERVER_FRONTEND followed by  NetMessageReadyForGameJoin that is most likely supposed to be routed to the server specified in the handshake.

What is supposed to happen after that is currently unclear:

- PLAYERMGR_SERVER_FRONTEND can initiate time synchronization with a NetMessageReadyForTimeSync. The client will then send a NetMessageSyncTimeRequest that can be responded with NetMessageSyncTimeReply. The client will then periodically send NetMessagePing.

- Sending a NetMessageReadyAndLoggedIn will cause the client to start a new mux connection on a different muxId and use it to send another InitialClientHandshake message, but this one is addressed to GROUPING_MANAGER_FRONTEND. The client doesn't respond to any GroupingManager messages.

- It's is possible in some cases to make the client display either a hero selection screen or a loading screen using NetMessageSelectStartingAvatarForNewPlayer or NetMessageQueueLoadingScreen respectively. However, these messages appear to require a timestamp of some type, and it's currently unclear how it fits in the protobuf payload structure.
