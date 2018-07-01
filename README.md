## VSTANCER
An attempt to use the features from ikt's VStancer as resource for FiveM servers to synchronize the edited vehicles with all the players. It is built using FiveM API and FiveM port of NativeUI.

When a client edits a vehicle, it will be automatically synchronized with all the players.
If a vehicle is reset to the default values it will stop from being synchronized.

This version of the script tries to achieve the result using decorators.
The default key to open the menu is F6

#### FEATURES
* Edit X Position of the wheels' bones (Track Width)
* Edit Y Rotation of the wheels' bones (Camber)

#### CLIENT COMMANDS
`vstancer_preset`
Prints the preset of the current vehicle

`vstancer_decorators`
Prints the info about decorators on the current vehicle

`vstancer_decorators 'int'` 
Prints the info about decorators on the vehicle with local 'int' handle

`vstancer_print`
Prints the list of all the vehicles with any decorator of this script

`vstancer_distance 'float'`
Sets the 'float' as the maximum distance used to refresh wheels of the vehicles with decorators

`vstancer_debug 'bool'`
Enables or disables the logs to be printed in the console

#### CONFIG
`toggleMenu=167`
The Control to toggle the Menu, default is 167 which is F6

`editingFactor=0.01`
The step used to increase and decrease a value

`maxOffset=0.25`
The max value you can increase or decrease the default Track Width

`maxCamber=0.20`
The max value you can increase or decrease the default Camber

`maxSyncDistance=150.0`
The max distance within which each client refreshes others clients' vehicles

`timer=1000`
The value in milliseconds used by each client to check if its preset requires to be synched again

`debug=false`
Enables the debug mode, which prints some logs in the console

[Source](https://github.com/neos7/fivem-vstancer)
[Download](https://github.com/neos7/fivem-vstancer/releases)
I am open to any kind of feedback. Report suggestions and bugs you find.

#### BUILD
Open the `postbuild.bat`and edit the path of the resource folder. The post build event will copy the script, the `config.ini` and the `__resource.lua` to such path. Also don't forget to include a copy of a built [NativeUI](https://github.com/citizenfx/NativeUI) script ported to FiveM.

#### CREDITS
* VStancer by ikt: https://github.com/E66666666/GTAVStancer
* FiveM by CitizenFX: https://github.com/citizenfx/fivem
* NativeUI by Guad: https://github.com/Guad/NativeUI
* GTADrifting members: https://gtad.club/
* All the testers
