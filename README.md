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

### Glossary
* **Track Width**: It's the X offset of the vehicle's wheels bones in the entity local coords system. Because wheels model are rotated it means to have a positivie Track Width you have to assign a negative value.
* **Camber**: It's the Y rotation of the vehicle's wheels bones in the entity local coords system.
* **Wheel Mod**: It refers to a custom wheel you can apply on a vehicle from in-game tuning features. Since this term can create ambiguity with custom assets mods (wheel modifications), we will refers to these as "tuning wheels" and to game modifications as "wheel mods"

### Features of the script
* Edit Track Width of vehicles
* Edit Camber of vehicles
* Edit Tuning Wheel Size of vehicles (Requires a tuning wheel to be installed on the vehicle)
* Edit Tuning Wheel Width of vehicles (Requires a tuning wheel to be installed on the vehicle)
* Manage presets

### Note
When a preset is created for the first time, it will use the current wheels' state as default. So in case of damaged vehicles (e.g. deformed wheels), the default values might be incorrect. 
Workaround: If a vehicle is damaged, be sure to fix it before to enter it and create a preset. (e.g. reset preset, fix the vehicle, exit the vehicle and enter again) 

### Client Commands
* `vstancer_preset`: Prints the preset of the current vehicle
* `vstancer_decorators`: Prints the info about decorators on the current vehicle
* `vstancer_decorators <int>`: Prints the info about decorators on the vehicle with the specified int as local handle
* `vstancer_print`: Prints the list of all the vehicles with any decorator of this script
* `vstancer_range <float>`: Sets the specified float as the maximum distance used to refresh wheels of the vehicles with decorators
* `vstancer_debug <bool>`: Enables or disables the logs to be printed in the console
* `vstancer`: Toggles the menu, this command has to be enabled in the config

### Config
* `Debug`: Enables the debug mode, which prints some logs in the console
* `DisableMenu`: Allows to disable the menu in case you want to allow editing in your own menu using the provided API
* `ExposeCommand`: Enables the /vstancer command to toggle the menu
* `ExposeEvent`: Enable the "vstancer:toggleMenu" event to toggle the menu
* `ScriptRange`: The max distance within which each client refreshes edited vehicles
* `Timer`: The value in milliseconds used by each client to do some specific timed tasks
* `ToggleMenuControl`:The Control to toggle the Menu, default is 167 which is F6 (check the [controls list](https://docs.fivem.net/game-references/controls/))
* `FloatStep`: The step used to increase and decrease a value
* `EnableWheelMod`: Enables the script to edit wheel size and width of tuning wheels
* `EnableClientPresets`: Enables the script to manage clients' presets
* `WheelLimits`:
    * `FrontTrackWidth`: The max value you can increase or decrease the front Track Width from its default value
    * `RearTrackWidth`: The max value you can increase or decrease the rear Track Width from its default value
    * `FrontCamber`: The max value you can increase or decrease the front Camber from its default value
    * `RearCamber`: The max value you can increase or decrease the rear Camber from its default value
* `WheelModLimits`:
    * `WheelSize`: The max value you can increase or decrease the size of tuning wheels from its default value
    * `WheelWidth`: The max value you can increase or decrease the width of tuning wheels from its default value

### Exports
The script exposes some API to manage the main features from other scripts:

```csharp
bool SetWheelPreset(int vehicle, float frontTrackWidth, float frontCamber, float rearTrackWidth, float rearCamber);
float[] GetWheelPreset(int vehicle);
bool ResetWheelPreset(int vehicle);
float[] GetFrontCamber(int vehicle);
float[] GetRearCamber(int vehicle);
float[] GetFrontTrackWidth(int vehicle);
float[] GetRearTrackWidth(int vehicle);
bool SetFrontCamber(int vehicle, float value);
bool SetRearCamber(int vehicle, float value);
bool SetFrontTrackWidth(int vehicle, float value);
bool SetRearTrackWidth(int vehicle, float value);
bool SaveClientPreset(string presetName, int vehicle);
bool LoadClientPreset(string presetName, int vehicle);
bool DeleteClientPreset(string presetName);
string[] GetClientPresetList();
```

**NOTE**
Current API don't support editing of tuning wheel data (wheelSize and wheelWidth) yet.

#### Remember that API require the resource to be called exactly “vstancer”
**API Usage**

* **SetWheelPreset**
    * int vehicle: the handle of the vehicle entity
    * float frontTrackWidth: the value you want to assign as front track width 
    * float frontCamber: the value you want to assign as front camber
    * float rearTrackWidth: the value you want to assign as rear track width 
    * float rearCamber: the value you want to assign as rear camber
    * bool result: returns `true` if the action successfully executed otherwise `false`
    
    <details>
    <summary>Example</summary>

    C#:
    ```csharp
    bool result = Exports["vstancer"].SetWheelPreset(vehicle, frontTrackWidth, frontCamber, rearTrackWidth, rearCamber);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:SetWheelPreset(vehicle, frontTrackWidth, frontCamber, rearTrackWidth, rearCamber)
    ```

    </details>
* **GetWheelPreset**
    * int vehicle: the handle of the vehicle entity
    * float result: the array containing the oreset values in this order frontTrackWidth, frontCamber, rearTrackWidth, rearCamber.
    
    <details>
    <summary>Example</summary>
    
    C#:
    ```csharp
    float[] result = Exports["vstancer"].GetWheelPreset(vehicle);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:GetWheelPreset(vehicle);
    ```
    
    </details>
* **ResetWheelPreset**
    * int vehicle: the handle of the vehicle entity
    * bool result: returns `true` if the action successfully executed otherwise `false`

    <details>
    <summary>Example</summary>
    
    C#:
    ```csharp
    bool result = Exports["vstancer"].ResetWheelPreset(vehicle);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:ResetWheelPreset(vehicle);
    ```
    
    </details>
* **GetFrontCamber**
    * int vehicle: the handle of the vehicle entity
    * float[] frontCamber: an array which contains the value as first element if the request has success, otherwise is empty
    
    <details>
    <summary>Example</summary>
    
    C#:
    ```csharp
    float[] frontCamber = Exports["vstancer"].GetFrontCamber(vehicle);
    ```
    Lua:
    ```lua
    local frontCamber = exports["vstancer"]:GetFrontCamber(vehicle);
    ```
    
    </details>
* **GetRearCamber**
    * int vehicle: the handle of the vehicle entity
    * float[] rearCamber: an array which contains the value as first element if the request has success, otherwise is empty

    <details>
    <summary>Example</summary>
    
    C#:
    ```csharp
    float[] rearCamber = Exports["vstancer"].GetRearCamber(vehicle);
    ```
    Lua:
    ```lua
    local rearCamber = exports["vstancer"]:GetRearCamber(vehicle);
    ```
    
    </details>
* **GetFrontTrackWidth**
    * int vehicle: the handle of the vehicle entity
    * float[] frontTrackWidth: an array which contains the value as first element if the request has success, otherwise is empty
    
    <details>
    <summary>Example</summary>
    
    C#:
    ```csharp
    float[] frontTrackWidth = Exports["vstancer"].GetFrontTrackWidth(vehicle);
    ```
    Lua:
    ```lua
    local frontTrackWidth = exports["vstancer"]:GetFrontTrackWidth(vehicle);
    ```
    
    </details>
* **GetRearTrackWidth**
    * int vehicle: the handle of the vehicle entity
    * float[] rearTrackWidth: an array which contains the value as first element if the request has success, otherwise is empty 
    
    <details>
    <summary>Example</summary>
    
    C#:
    ```csharp
    float[] rearTrackWidth = Exports["vstancer"].GetRearTrackWidth(vehicle);
    ```
    Lua:
    ```lua
    local rearTrackWidth = exports["vstancer"]:GetRearTrackWidth(vehicle);
    ```
    
    </details>
* **SetFrontCamber**
    * int vehicle: the handle of the vehicle entity
    * float frontCamber: the value you want to assign as front camber
    * bool result: returns `true` if the action successfully executed otherwise `false`
    
    <details>
    <summary>Example</summary>

    C#:
    ```csharp
    bool result = Exports["vstancer"].SetFrontCamber(vehicle, frontCamber);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:SetFrontCamber(vehicle, frontCamber);
    ```

    </details>
* **SetRearCamber**
    * int vehicle: the handle of the vehicle entity
    * float rearCamber: the value you want to assign as rear camber
    * bool result: returns `true` if the action successfully executed otherwise `false`
    
    <details>
    <summary>Example</summary>

    C#:
    ```csharp
    bool result = Exports["vstancer"].SetRearCamber(vehicle, rearCamber);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:SetRearCamber(vehicle, rearCamber);
    ```

    </details>
* **SetFrontTrackWidth**
    * int vehicle: the handle of the vehicle entity
    * float frontTrackWidth: the value you want to assign as front track width
    * bool result: returns `true` if the action successfully executed otherwise `false`

    <details>
    <summary>Example</summary>

    C#:
    ```csharp
    bool result = Exports["vstancer"].SetFrontTrackWidth(vehicle, frontTrackWidth);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:SetFrontTrackWidth(vehicle, frontTrackWidth);
    ```

    </details>
* **SetRearTrackWidth**
    * int vehicle: the handle of the vehicle entity
    * float rearTrackWidth: the value you want to assign as rear track width
    * bool result: returns `true` if the action successfully executed otherwise `false`

    <details>
    <summary>Example</summary>

    C#:
    ```csharp
    bool result = Exports["vstancer"].SetRearTrackWidth(vehicle, rearTrackWidth);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:SetRearTrackWidth(vehicle, rearTrackWidth);
    ```

    </details>
* **SaveClientPreset**
    * string presetName: the name you want to use for the saved preset
    * int vehicle: the handle of the vehicle entity you want to save the preset from
    * bool result: returns `true` if the action successfully executed otherwise `false`

    <details>
    <summary>Example</summary>

    C#:
    ```csharp
    bool result = Exports["vstancer"].SaveClientPreset(presetName, vehicle);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:SaveClientPreset(presetName, vehicle);
    ```

    </details>
* **LoadClientPreset**
    * string presetName: the name of the preset you want to load
    * int vehicle: the handle of the vehicle entity you want to load the preset on
    * bool result: returns `true` if the action successfully executed otherwise `false`

    <details>
    <summary>Example</summary>

    C#:
    ```csharp
    bool result = Exports["vstancer"].LoadClientPreset(presetName, vehicle);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:LoadClientPreset(presetName, vehicle);
    ```

    </details>
* **DeleteClientPreset**
    * string presetName: the name of the preset you want to delete
    * bool result: returns `true` if the action successfully executed otherwise `false`
    
    <details>
    <summary>Example</summary>

    C#:
    ```csharp
    bool result = Exports["vstancer"].DeleteClientPreset(presetName);
    ```
    Lua:
    ```lua
    local result = exports["vstancer"]:DeleteClientPreset(presetName);
    ```

    </details>
* **GetClientPresetList**
    * string[] presetList: the list of all the presets saved locally
    
    <details>
    <summary>Example</summary>

    C#:
    ```csharp
    string[] presetList = Exports["vstancer"].GetClientPresetList();
    ```
    Lua:
    ```lua
    local presetList = exports["vstancer"]:GetClientPresetList();
    ```

    </details>

[Source](https://github.com/carmineos/fivem-vstancer)
[Download](https://github.com/carmineos/fivem-vstancer/releases)
I am open to any kind of feedback. Report suggestions and bugs you find.

### Build
Open the `postbuild.bat` and edit the path of the resource folder. If in Debug configuration, the post build event will copy the following files to the specified path: the built assembly of the script, the `config.json`, the `fxmanifest.lua`.

### Requirements
The script uses [MenuAPI](https://github.com/TomGrobbe/MenuAPI) by Vespura to render the UI, ~~it uses FiveM built-in resource dependency, so the script will only work if MenuAPI resource is found and running~~ and comes already with a built assembly so that it's ready to use.

### Installation
1. Download the zip file from the release page
2. Extract the content of the zip to the resources folder of your server (it should be a folder named `vstancer`)
3. Enable the resource in your server config (`start vstancer`)

### Todo
* Add API for wheel mod data
* Update local presets API to support wheel mod data
* Add limits check for API
* Add limits check for preset loading
* Workaround wheel mod data being reset after any tuning component is changed
* Clean duplicated code
* API shouldn't allow to edit vehicles other players are driving

### Roadmap
Once FiveM exposes extra-natives to edit `SubHandlingData` fields at runtime, the script will allow to edit XYZ rotation using the native handling fields of `CCarHandlingData` such as `fToeFront`, `fToeRear`, `fCamberFront`, `fCamberRear`, `fCastor`. (This will also improve a lot performances as such values won't need to be set each tick)

### Credits
* [VStancer by ikt](https://github.com/E66666666/GTAVStancer)
* [FiveM by CitizenFX](https://github.com/citizenfx/fivem)
* [MenuAPI by Vespura](https://github.com/TomGrobbe/MenuAPI)
* [GTADrifting members](https://gtad.club/)
* All the testers

### Support
If you would like to support my work, you can through:
* [Patreon](https://patreon.com/carmineos)