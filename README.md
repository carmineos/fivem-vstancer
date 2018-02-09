An attempt to use the features from ikt's VStancer as resource for FiveM servers to synchronize the edits among players.
It is built using FiveM API and FiveM port of NativeUI

The script allows to edit the camber and the track width of the wheels.

You can edit locally a vehicle or synch the changes with the others players in the server.

In order to work you MUST be able to get the netID of the vehicles' entities you want to edit, this means that you either spawn a vehicle with this script or embedd this code in your favourite vehicle spawner. (Unless someone shows me how to get the netID of entities spawned by another script, because at the moment I always get a netID = 0, entities seem to not be networked)

Usage:
- F7 Spawn a vehicle by model name
- F6 Open the menu to edit the wheels

When you have finished click on "Sync Preset" to allow all the players in the server to see your changes.
Click on "Reset Preset" to restore the default values of the wheels.

- VStancer by ikt: https://github.com/E66666666/GTAVStancer
- FiveM by CitizenFX: https://github.com/citizenfx/fivem
