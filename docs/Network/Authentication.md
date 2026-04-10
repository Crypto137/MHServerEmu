# Authentication

When a player tries to log in, the client sends an HTTP POST request over https to the `/AuthServer/Login/IndexPB` endpoint on the `AuthServerAddress` specified in [SiteConfig.xml](./../Web/SiteConfig.md). This request contains an encoded `LoginDataPB` message from `FrontendProtocol.proto` that has the same structure as [messages in data packets](./PacketStructure.md). `LoginDataPB` contains the following fields in version 1.52.0.1700:

```protobuf
message LoginDataPB {
    required string    emailAddress    = 1;
    required string    password    = 2;
    optional string    version    = 3;
    optional string    dbservername    = 4;
    optional string    token    = 5;
    optional bool    loginAsAnotherPlayer    = 6;
    optional bool    noPersistenceThisSession    = 7;
    optional string    loginAsPlayer    = 8;
    optional string    clientDownloader    = 9;
    optional string    machineId    = 10;
    optional string    machineIdDebugInfo    = 18;
    optional int32    timezoneutcbias    = 11;
    optional int32    platform    = 12;
    optional string    platformString    = 13;
    optional string    locale    = 14;
    optional string    twofactorcode    = 15;
    optional string    twofactorname    = 16;
    optional string    twofactortrustmachine    = 17;
    optional bool    streamingclient    = 19;
}
```

The auth server checks this data and indicates the result in the response via one of the following HTTP status codes:

```csharp
enum AuthStatusCode
{
    Success = 200,
    IncorrectUsernameOrPassword401 = 401,
    AccountBanned = 402,
    IncorrectUsernameOrPassword403 = 403,
    CouldNotReachAuthServer = 404,
    EmailNotVerified = 405,
    UnableToConnect406 = 406,
    NeedToAcceptLegal = 407,
    PatchRequired = 409,
    AccountArchived = 411,
    PasswordExpired = 412,
    UnableToConnect413 = 413,
    UnableToConnect414 = 414,
    UnableToConnect415 = 415,
    UnableToConnect416 = 416,
    AgeRestricted = 417,
    UnableToConnect418 = 418,
    InternalError500 = 500,
    TemporarilyUnavailable = 503
}
```

Responses with status codes 200 (success) and 407 (need to accept legal) contain an encoded `AuthTicket` message from `AuthMessages.proto`. This message can have the following fields:

```protobuf
message AuthTicket {
    optional bytes    sessionKey    = 1;
    optional bytes    sessionToken    = 2;
    required uint64    sessionId    = 3;
    optional string    errorMessage    = 4;
    optional string    frontendServer    = 5;
    optional string    frontendPort    = 6;
    optional string    platformTicket    = 7;
    optional bool    presalePurchase    = 8;
    optional string    tosurl    = 9;
    optional bool    success    = 10;
    optional bool    hasnews    = 12;
    optional string    newsurl    = 13;
    optional bool    hasPendingGift    = 14;
    optional string    pendingGiftUrl    = 15;
    optional bool    hasVerifiedEmail    = 16;
    optional bool    isAllowedToChat    = 17 [default = true];
    optional bool    isAgeRestrictionEnabled    = 18 [default = false];
    optional int64    ageRestrictionEndTimeUtc    = 19;
    optional int64    ageRestrictionWarningTimeUtc1    = 20;
    optional string    ageRestrictionType    = 21;
    optional int64    ageRestrictionWarningTimeUtc2    = 22;
    optional int64    ageRestrictionWarningTimeUtc3    = 23;
    optional string    countryCode    = 24;
    optional string    continentCode    = 25;
    optional bool    preReqCreateAccount    = 26;
    optional bool    preReqRenamePlayer    = 27;
    repeated AuthRequiredDoc    preReqAcceptDocs    = 28;
}

message AuthRequiredDoc {
    optional string    key    = 1;
    optional string    title    = 2;
    optional string    body    = 3;
}
```

For the authentication to proceed, the HTTP response should have status code 200, and the `AuthTicket` included in it should contain the following fields:

- `sessionId` - a 64-bit client connection id.

- `sessionKey` - an AES-256 encryption key.

- `sessionToken` - a 32-byte token used to authenticate the client on the frontend server.

- `frontendServer` - the frontend server address.

- `frontendPort` - the frontend server port.

- `success` - needs to be true.

The client also expects the `tosurl` field to be present with status code 407, which should contain a URL for the terms of service acception popup.

If the initial authentication succeded and the client has a valid `AuthTicket`, it proceeds to connect to the frontend server specified in it. The communication with the frontend server happens over a socket using the packet structure described [here](./PacketStructure.md).

The client establishes a new mux connection on channel `1` and sends a `ClientCredentials` message from `FrontendProtocol.proto` that has the following fields:

```protobuf
message ClientCredentials {
    required uint64    sessionid    = 1;
    required bytes    iv    = 2;
    required bytes    encryptedToken    = 3;
}
```

- `sessionid` - the same id that was provided to the client in the `AuthTicket`.

- `iv` - a random IV generated by the client.

- `encryptedToken` - the token from `AuthTicket` encrypted with the AES key from the same ticket and the IV generated by the client.

The frontend server verifies these credentials and responds with either a `LoginQueueStatus` message to display a login queue screen in the client, or a `SessionEncryptionChanged` message to proceed with logging in.

```protobuf
message LoginQueueStatus {
    required uint64    placeInLine    = 1;
    required uint64    numberOfPlayersInLine    = 2;
}

message SessionEncryptionChanged {
    required uint32    randomNumberIndex    = 1 [default = 1];
    required bytes    encryptedRandomNumber    = 2;
}
```

It is unclear what the purpose of `SessionEncryptionChanged` is, but setting `randomNumberIndex` to `0` causes the client to log a `[Login Manager] Successful auth` message. `encryptedRandomNumber` doesn't seem to have any role whatsoever, and it can be safely set to an empty ByteString.

After receiving the `SessionEncryptionChanged` message, the clients proceeds to establish connections with the Player Manager and the Grouping Manager using `InitialCliendHandshake` messages.

```protobuf
message InitialClientHandshake {
    required FrontendProtocolVersion    protocolVersion    = 1 [default = CURRENT_VERSION];
    required PubSubServerTypes    serverType    = 2;
}
```

On mux channel `1` the client sends a handshake for `PLAYERMGR_SERVER_FRONTEND`, and on mux channel `2` it sends a handshake for `GROUPING_MANAGER_FRONTEND`. The frontend server then routes all further messages on channel `1` to the Player Manager and messages on channel `2` to the Grouping Manager. It is important to receive both handshakes before proceeding with logging the player in, because early login can cause the client to not establish the Grouping Manager connection correctly and break chat.

The Player Manager uses `ClientToGameServer.proto` for client -> server communication and `GameServerToClient.proto` for server -> client. The Grouping Manager uses `ClientToGroupingManager.proto` for client -> server communication and `GroupingManager.proto` for server -> client communication, but most client -> server communication with the Grouping Manager actually gets routed through the Player Manager first.
