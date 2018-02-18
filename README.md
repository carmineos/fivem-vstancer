An attempt to use the features from ikt's VStancer as resource for FiveM servers to synchronize the edits among players. It is built using FiveM API and FiveM port of NativeUI.<br />

The script allows to edit the camber and the track width of the wheels.<br />

You can edit locally a vehicle or synch the changes with the others players in the server.<br />

When you have finished click on "Sync Preset" to allow all the players in the server to see your changes. Click on "Reset Preset" to restore locally the default values of the wheels.<br />

This version of the script tries to achieve the result without using netID of the entities but synching only the vehicle the playerPed is using.<br />
* Only allows to sync the current vehicle the playerPed is using
* Changes automatically synched if required
* Unsync the preset if playerDropped
* Only refreshes players' vehicles which are close to you (by default distance is 150)

F6 Opens the menu to edit the wheels<br />

**CLIENT COMMANDS**<br />
`vstancer_print`
Prints the list of all the synched players

`vstancer_distance 'float'`
Sets the 'float' as the maximum distance used to synch others players' vehicles

`vstancer_debug 'bool'`
Enables or disables the logs to be printed in the console

**SERVER COMMANDS**<br />
`vstancer_print`
Prints the list of all the synched players

`vstancer_debug 'bool'`
Enables or disables the logs to be printed in the console

`vstancer_maxOffset 'float'`
Sets the 'float' as the min/max offset for the default track width value

`vstancer_maxCamber 'float'`
Sets the 'float' as the min/max offset for the default camber value

`vstancer_timer 'long'`
Sets the 'long' as timer to wait for checking if each client needs to syncs again its preset
<br />

**CONFIG.INI**<br />
`toggleMenu=167`
The Control to toggle the Menu, default is 167 which is F6

`editingFactor=0.01`
The amount each value changes when you increase or decrease it

`maxOffset=0.30`
The max value you can increase or decrease the default Track Width

`maxCamber=0.25`
The max value you can increase or decrease the default Camber

`maxSyncDistance=150.0`
The max distance within which each client refreshes others clients' vehicles

`timer=1000`
The value in milliseconds used by each client to check if its preset requires to be synched again

`debug=false`
Enables the debug mode, which prints some logs in the console

[Source](https://github.com/neos7/FiveM_vstancer)<br />
[Download](https://github.com/neos7/FiveM_vstancer/releases/download/v1.0/vstancer.rar)<br />
I am open to any kind of feedback. Report suggestions and bugs you find.<br />


**CREDITS**<br />
* VStancer by ikt: https://github.com/E66666666/GTAVStancer
* FiveM by CitizenFX: https://github.com/citizenfx/fivem
* NativeUI by GUAD: https://github.com/Guad/NativeUI

VIDEO:<br />
https://streamable.com/hzjyj
