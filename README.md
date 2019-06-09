# ServerMonitor

Monitor service consists of 2 separate apps:

- MonitorService - background windows service capable of providing information about currently running processes of the host
- WebUi - simple web UI for MonitorService

## Configuration

Both apps can be configured by editing `appSettings` section of .config files.
### Supported options for `MonitorService`:

- `WsUrl`: url which will be used to broadcast information via WebSockets. Must start with `http` or `https` protocol, and end with `/`
- `CpuUsageNotificationPercent`: if CPU utilization percentage exceeds this limit, notification is sent to connected clients
- `RamUsageNotificationPercent`: if RAM utilization percentage exceeds this limit, notification is sent to connected clients
- `RamUsageNotificationBytes`: if more RAM bytes used, notification is sent to connected clients
- `UpdateFrequency`: information gathering periodicity in milliseconds

**Note:** set notification limit to `0` to disable notifications

**Note:** information gathering is quite processor heavy, so setting `UpdateFrequency` below `4000` is not recommended.

### Supported options for `WebUi`:

- `WsUrl`: url which will be used to receive broadcasted information via WebSockets. Must start with `ws` or `wss` protocol

## Installation

- run `MonitorService.exe --install` to install service and `MonitorService.exe --uninstall` to uninstall. If it didn't start immediately after installation, start it manually via MMC
- deploy WebUi as IIS website

## Creating your own client

The only API provided is `MonitorService` broadcasting endpoint. Just implement simple WebSocket client and connect it to correct url.

Server doesn't respond to incoming messages and only sends information about currently running processes serialized to JSON in UTF-8 encoding.

Data structure:
```
{
    "UpdateTime" : "DateTime string in ISO 8601 format",
    "CpuUsage" : "CPU utilisation as fraction of 1, double",
    "RamUsageBytes" : "Amount of allocated memory, long",
    "Processes" : [
        {
            "Name" : "string",
            "ProcessId" : "int",
            "CpuUsage" : "double",
            "RamUsageBytes" : "long",
            "ThreadsCount" : "int",
            "StartTime" : "DateTime string in ISO 8601 format"
        }
    ],
    "Notifications" : [
        {
            "Type": "enum; 1 - CPU usage exceeded limits, 2 - RAM usage exceeded limits",
            "Message": "string"
        }
    ]
}
```

If you're going to create client using JavaScript, you can use `MonitorServiceClient` from `WebUi/Scripts/service-client.js`

Use it as follows:

```javascript
// create new instance
let service = new MonitorServiceClient(serviceUrl);

// start receiving data
service.openConnection(
    function(data) {/* on data received */},
    function(event) {/* on disconnect */}
);
```