**INTRODUCTION**<br />
An attempt to use the features from ikt's VStancer as resource for FiveM servers to synchronize the edits among players. It is built using FiveM API and FiveM port of NativeUI.<br />

The script allows to edit the camber and the track width of the wheels.<br />

When a client edits something, it will be automatically synchronized.<br />
If a client resets the settings it will stop from being synchronized until he changes something again.<br />

This version of the script tries to achieve the result using decorators.<br />
The default key to open the menu is F6<br />

**CLIENT COMMANDS**<br />
`vstancer_preset`
#Prints the preset of the current vehicle

`vstancer_decorators`
#Prints the info about decorators on the current vehicle

`vstancer_decorators_on 'int'` 
#Prints the info about decorators on the 'int' vehicle

`vstancer_print`
#Prints the list of all the vehicles with any decorator of this script

`vstancer_distance 'float'`
#Sets the 'float' as the maximum distance used to refresh wheels of the vehicles with decorators

`vstancer_debug 'bool'`
#Enables or disables the logs to be printed in the console

**CONFIG.INI**<br />
`toggleMenu=167`
#The Control to toggle the Menu, default is 167 which is F6

`editingFactor=0.01`
#The amount each value changes when you increase or decrease it

`maxOffset=0.30`
#The max value you can increase or decrease the default Track Width

`maxCamber=0.25`
#The max value you can increase or decrease the default Camber

`maxSyncDistance=150.0`
#The max distance within which each client refreshes others clients' vehicles

`timer=1000`
#The value in milliseconds used by each client to check if its preset requires to be synched again

`debug=false`
#Enables the debug mode, which prints some logs in the console

`title=Wheels Editor`
#Title of the NativeUI Menu

`description=~b~Track Width & Camber`
#Description of the NativeUI Menu

[Source](https://github.com/neos7/fivem_vstancer)<br />
[Download](https://github.com/neos7/fivem_vstancer/releases)<br />
I am open to any kind of feedback. Report suggestions and bugs you find.<br />


**BUILD**<br />
Open the `postbuild.bat`and edit the path of the resource folder. The post build event will copy the script, the `config.ini` and the `__resource.lua` to such path.



**CREDITS**<br />
* VStancer by ikt: https://github.com/E66666666/GTAVStancer
* FiveM by CitizenFX: https://github.com/citizenfx/fivem
* NativeUI by GUAD: https://github.com/Guad/NativeUI
* GTADrifting members: https://gtad.club/
* All the testers
