# Web API

MHServerEmu can provide web API functionality as part of its web frontend. It is enabled by default, and the server listens for requests on `http://localhost:8080/`. This can be customized in the `WebFrontend` section of `Config.ini`.

The web API uses JSON for serialization. Requests return JSON as output and expect JSON as input in the request's body when needed.

Some endpoints are restricted and require an API key to access.

- API keys can be generated using the `!webapi generatekey` command.

- API keys are saved to `Data/Web/ApiKeys.json`. Keys can be reloaded from this file at runtime using the `!webapi reloadkeys` command.

- API keys are not logged, the only way to access generated keys is by reading them from the `ApiKeys.json` file.

- Each API key has a specific access type assigned to it (e.g. `AccountManagement`), which limits its functionality to a specific domain.

- API keys need to be provided when making requests to restricted endpoints by adding them as an `Authorization` HTTP request header with the `Bearer` scheme (e.g. `Authorization: Bearer FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF`).

Below is reference information on available endpoints.

## Account Management

### /AccountManagement/Create

#### POST

Requests the server to create an account with the provided data.

- Access: `None`

- Input Fields:
  
  - `string Email`
  
  - `string PlayerName`
  
  - `string Password`

- Output Fields:
  
  - `int Result`

### /AccountManagement/SetPlayerName

#### POST

Requests the server to change the player name of an existing account.

- Access: `AccountManagement`

- Input Fields:
  
  - `string Email`
  
  - `string PlayerName`

- Output Fields:
  
  - `int Result`

### /AccountManagement/SetPassword

#### POST

Requests the server to change the password of an existing account.

- Access: `AccountManagement`

- Input Fields:
  
  - `string Email`
  
  - `string Password`

- Output Fields:
  
  - `int Result`

### /AccountManagement/SetUserLevel

#### POST

Requests the server to change the user level of an existing account.

- Access: `AccountManagement`

- Input Fields:
  
  - `string Email`
  
  - `byte UserLevel`

- Output Fields:
  
  - `int Result`

### /AccountManagement/SetFlag

#### POST

Requests the server to set a flag on an existing account.

- Access: `AccountManagement`

- Input Fields:
  
  - `string Email`
  
  - `int Flags`

- Output Fields:
  
  - `int Result`

Account Flag Reference:

```csharp
enum AccountFlags
{
    None                                = 0,
    IsBanned                            = 1 << 0,
    IsArchived                          = 1 << 1,
    IsPasswordExpired                   = 1 << 2,
    DEPRECATEDLinuxCompatibilityMode    = 1 << 3,
    IsWhitelisted                       = 1 << 4,
}
```

### /AccountManagement/ClearFlag

#### POST

Requests the server to clear a flag on an existing account.

- Access: `AccountManagement`

- Input Fields:
  
  - `string Email`
  
  - `int Flags`

- Output Fields:
  
  - `int Result`

See `/AccountManagement/SetFlag` above for flag reference.

## Server Status

### /ServerStatus

#### GET

Retrieves server status information.

- Access: `None`

- Output:
  
  - `Dictionary<string, long>`

## Region Report

### /RegionReport

#### GET

Retrieves server region information.

- Access: `None`

- Output Fields:
  
  - `List<RegionReport.Entry> Regions`
    
    - `string GameId`
    
    - `string RegionId`
    
    - `string Name`
    
    - `string DifficultyTier`
    
    - `string Uptime`

## Metrics

### /Metrics/Performance

#### GET

Retrieves server performance metrics.

- Access: `None`

- Output Fields:
  
  - `string Id`
  
  - `MemoryMetrics.Report Memory`
    
    - `long GCIndex`
    
    - `long GCCountGen0`
    
    - `long GCCountGen1`
    
    - `long TotalCommittedBytes`
    
    - `long HeapSizeBytes`
    
    - `double PauseTimePercentage`
    
    - `MetricTracker.ReportEntry PauseDuration`
  
  - `Dictionary<ulong, GamePerformanceMetrics.Report> Games`
    
    - `MetricTracker.ReportEntry UpdateTime`
    
    - `MetricTracker.ReportEntry FrameTime`
    
    - `MetricTracker.ReportEntry ScheduledEventsPerUpdate`
    
    - `MetricTracker.ReportEntry EntityCount`
    
    - `MetricTracker.ReportEntry PlayerCount`

- `MetricTracker.ReportEntry` fields:
  
  - `float Average`
  
  - `float Median`
  
  - `float Last`
  
  - `float Min`
  
  - `float Max`
