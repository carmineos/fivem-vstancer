An attempt to use the features from ikt's VStancer as resource for FiveM servers to synchronize the edits among players. It is built using FiveM API and FiveM port of NativeUI.<br />

The script allows to edit the camber and the track width of the wheels.<br />

You can edit locally a vehicle or synch the changes with the others players in the server.<br />

When you have finished click on "Sync Preset" to allow all the players in the server to see your changes. Click on "Reset Preset" to restore locally the default values of the wheels.<br />

This version of the script tries to achieve the result without using netID of the entities but synching only the vehicle the playerPed is using.<br />
* Only allows to sync the current vehicle the playerPed is using
* Unsync the preset if playerDropped
* Antispam system to prevent players to spam synched presets (by default max 1 syncs in 30 seconds)
* Only refreshes players' vehicles which are close to you (by default distance is 150)
* Can manually unsync your preset

F6 Opens the menu to edit the wheels<br />

**CLIENT COMMANDS**<br />
> vstancer_print

Prints the list of all the synched players

>vstancer_distance 'float'

Sets the 'float' as the maximum distance used to synch others players' vehicles

**SERVER COMMANDS**<br />
> vstancer_print

Prints the list of all the synched players

>vstancer_maxEditing 'float'

Sets the 'float' as the min/max offset for the default value of each property (for all the players)

>vstancer_maxSyncCount 'int'

Sets the 'int' as max number of synchs for antispam (for all the players)

>vstancer_cooldown 'int'

Sets the 'int' as cooldown value (in seconds) used as timer for antispam (for all the players)
<br />
[Source](https://github.com/neos7/FiveM_vstancer)<br />
[Download](https://github.com/neos7/FiveM_vstancer/releases/download/v1.0/vstancer.rar)<br />
I am open to any kind of feedback. Report suggestions and bugs you find.<br />


**CREDITS**<br />
* VStancer by ikt: https://github.com/E66666666/GTAVStancer
* FiveM by CitizenFX: https://github.com/citizenfx/fivem
* NativeUI by GUAD: https://github.com/Guad/NativeUI

VIDEO:<br />
https://streamable.com/hzjyj
