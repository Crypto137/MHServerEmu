# Overview

Marvel Heroes uses [Protocol Buffers](https://protobuf.dev/) for network message serialization. It is possible to extract protobuf schemas (.proto) from the main client executable using [protod](https://github.com/dennwc/protod) and then use them with tools such as protogen included with [protobuf-csharp-port](https://github.com/jskeet/protobuf-csharp-port).

Most protobuf payloads have the following structure:

```
byte MessageId          // same as index in its .proto schema
byte MessageSize        // in bytes
byte[] EncodedMessage
```

Some payloads appear to slightly deviate from this structure (e.g. dumped NetMessageInitialTimeSync has an extra 0x01 byte between id and size). The details of this are still being investigated.

When a player tries to log in, the client communicates with the auth server specified in SiteConfig.xml over https. The end result of this communication is an AuthTicket payload that contains session info (id, token, AES-256 encryption key), as well as the frontend server address and port that the client proceeds to connect to. It's possible to bypass the authorization process entirely by responding to the initial request with an AuthTicket payload straight away.

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

The frontend server can respond with either a LoginQueueStatus to display a login queue screen in the client, or a SessionEncryptionChanged to proceed with logging in. It's currently unclear how SessionEncryptionChanged functions.

The client responds to the SessionEncryptionChanged message with two messages in a row: InitialClientHandshake with PLAYERMGR_SERVER_FRONTEND followed by  NetMessageReadyForGameJoin that is most likely supposed to be routed to the server specified in the handshake.

After that roughly the following sequence happens:

- The server responds with NetMessageReadyAndLoggedIn.

- The client connects on mux id 2 and sends InitialClientHandshake for GROUPING_MANAGER_FRONTEND. It appears mux id 2 is used for communicating with GROUPING_MANAGER_FRONTEND.

- The server initiates time sync with NetMessageInitialTimeSync.

- After syncing time the client starts sending NetMessagePing periodically.

- The server queues a loading screen with NetMessageQueueLoadingScreen and begins sending a lot of data required for game initialization. The specifics are under investigation.
