
## NetworkDiscoveryUnity

Network discovery for Unity3D.


## Features

- Simple. 1 script. 600 lines of code.

- Uses C#'s UDP sockets for broadcasting and sending responses.

- Independent of current networking framework.

- Single-threaded.

- Tested on: Linux, Windows, Android.

- Can lookup specific servers on the internet (outside of local network).

- Has a separate [GUI script](/NetworkDiscoveryHUD.cs) for easy testing.

- Has support for custom response data.

- By default, server responds with: current scene, game server port number, game signature.

- No impact on performance.


## Usage

Attach [NetworkDiscovery](/NetworkDiscovery.cs) script to any game object. Assign game server port number.

Now, you can use [NetworkDiscoveryHUD](/NetworkDiscoveryHUD.cs) script to test it (by attaching it to the same game object), or use the API directly:

```cs
// register listener
NetworkDiscovery.onReceivedServerResponse += (NetworkDiscovery.DiscoveryInfo info) =>
{
	// we received response from server
	// add it to list of servers, or connect immediately...
};

// send broadcast on LAN
// when server receives the packet, he will respond
NetworkDiscovery.SendBroadcast();

// on server side, you can register custom data for responding
NetworkDiscovery.RegisterResponseData("Game mode", "Deathmatch");
```

For more details on how to use it, check out NetworkDiscoveryHUD script.


## Inspector

![](https://i.imgur.com/R9ZU1G2.png)


## Example GUI

![](https://i.imgur.com/SXqKMbJ.png)


## Possible improvements

- Measure ping - requires that all socket operations are done in a separate thread, or using async methods

- Prevent detection of multiple localhost servers (by assigning GUID to each packet) ?

- Add "Refresh" button in GUI next to each server

- Catch the other exception which is thrown on windows - it's harmless, so it should not be logged

- Make sure packet-to-string conversion works with non-ascii characters

