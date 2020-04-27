# VStancer
|Master|Development|
|:-:|:-:|
|[![Build status](https://ci.appveyor.com/api/projects/status/qialhqew9j0i9528/branch/master?svg=true)](https://ci.appveyor.com/project/carmineos/fivem-vstancer/branch/master) |[![Build status](https://ci.appveyor.com/api/projects/status/qialhqew9j0i9528/branch/development?svg=true)](https://ci.appveyor.com/project/carmineos/fivem-vstancer/branch/development)|

### Description
An attempt to use the features from ikt's VStancer as resource for FiveM servers to synchronize the edited vehicles with all the players. It is built using FiveM API and MenuAPI.

When a client edits a vehicle, it will be automatically synchronized with all the players.
If a vehicle is reset to the default values it will stop from being synchronized.
The synchronization is made using decorators.

The default key to open the menu is F6

### Features
* Edit X Position of the wheels' bones (Track Width)
* Edit Y Rotation of the wheels' bones (Camber)

### Limitations
When a preset is created for the first time, it will use the current wheels' state as default. So in case of damaged vehicles (e.g. deformed wheels), the default values might be incorrect. 
Workaround: If a vehicle is damaged, be sure to fix it before to enter it and create a preset. (e.g. reset preset, fix the vehicle, exit the vehicle and enter again) 

### Roadmap
Once FiveM exposes extra-natives to edit `SubHandlingData` fields at runtime, the script will allow to edit XYZ rotation using the native handling fields of `CCarHandlingData` such as `fToeFront`, `fToeRear`, `fCamberFront`, `fCamberRear`, `fCastor`. (This will also improve a lot performances as such values won't need to be set each tick)

### Client Commands
* `vstancer_preset`: Prints the preset of the current vehicle

* `vstancer_decorators`: Prints the info about decorators on the current vehicle

* `vstancer_decorators <int>`: Prints the info about decorators on the vehicle with the specified int as local handle

* `vstancer_print`: Prints the list of all the vehicles with any decorator of this script

* `vstancer_range <float>`: Sets the specified float as the maximum distance used to refresh wheels of the vehicles with decorators

* `vstancer_debug <bool>`: Enables or disables the logs to be printed in the console

* `vstancer`: Toggles the menu, this command has to be enabled in the config

### Config
* `ToggleMenuControl`:The Control to toggle the Menu, default is 167 which is F6 (check the [controls list](https://docs.fivem.net/game-references/controls/))

* `FloatStep`: The step used to increase and decrease a value

* `PositionX`: The max value you can increase or decrease the Track Width

* `RotationY`: The max value you can increase or decrease the Camber

* `ScriptRange`: The max distance within which each client refreshes others clients' vehicles

* `Timer`: The value in milliseconds used by each client to check if its preset requires to be synched again

* `Debug`: Enables the debug mode, which prints some logs in the console

* `ExposeCommand`: Enables the /vstancer command to toggle the menu

* `ExposeEvent`: Enable the "vstancer:toggleMenu" event to toggle the menu

### Exports

Remember that exports require the resource to be called “vstancer”

```csharp
private void SetVstancerPreset(int vehicle, float off_f, float rot_f, float off_r, float rot_r, object defaultFrontOffset = null, object defaultFrontRotation = null, object defaultRearOffset = null, object defaultRearRotation = null);
private float[] GetVstancerPreset(int vehicle);
```

**SET**

Note that when using the `SetVstancerPreset`, the default values are optional and the script will get them itself if you don't pass them.
This is an example of how to set a vstancer preset on a vehicle:

C#:
```csharp
Exports["vstancer"].SetVstancerPreset(vehicle,offset_f,rotation_f,offset_r,rotation_r);
```
Lua:
```lua
exports["vstancer"]:SetVstancerPreset(vehicle,offset_f,rotation_f,offset_r,rotation_r)
```

**GET**

When using the `GetVstancerPreset` the returned array will contain the following floats in order: off_f, rot_f, off_r, rot_r, off_f_def, rot_f_def, off_r_def, rot_r_def.
This is an example of how to get a vstancer preset (in case you want to store them):

C#:
```csharp
float[] preset = Exports["vstancer"].GetVstancerPreset(vehicle);
```
Lua:
```lua
local preset = exports["vstancer"]:GetVstancerPreset(vehicle);
```

[Source](https://github.com/carmineos/fivem-vstancer)
[Download](https://github.com/carmineos/fivem-vstancer/releases)
I am open to any kind of feedback. Report suggestions and bugs you find.

### Build
Open the `postbuild.bat` and edit the path of the resource folder. If in Debug configuration, the post build event will copy the following files to the specified path: the built assembly of the script, the `config.json`, the `fxmanifest.lua`.

### Requirements
The script uses [MenuAPI](https://github.com/TomGrobbe/MenuAPI) by Vespura to render the UI, ~~it uses FiveM built-in resource dependency, so the script will only work if MenuAPI resource is found and running~~ and comes already with a built assembly so that it's ready to use.


### Credits
* VStancer by ikt: https://github.com/E66666666/GTAVStancer
* FiveM by CitizenFX: https://github.com/citizenfx/fivem
* MenuAPI by Vespura: https://github.com/TomGrobbe/MenuAPI
* GTADrifting members: https://gtad.club/
* All the testers
