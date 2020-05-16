using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using VStancer.Client.UI;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json;
using VStancer.Client.Preset;
using System.Linq;

namespace VStancer.Client.Scripts
{
    public class MainScript : BaseScript
    {
        private readonly MainMenu Menu;

        private long _lastTime;
        private int _playerVehicleHandle;
        private int _playerPedHandle;
        private Vector3 _playerPedCoords;
        private List<int> _worldVehiclesHandles;
        private float _maxDistanceSquared;

        internal int PlayerVehicleHandle
        {
            get => _playerVehicleHandle;
            private set
            {
                if (Equals(_playerVehicleHandle, value))
                    return;

                _playerVehicleHandle = value;
                PlayerVehicleHandleChanged?.Invoke(this, value);
            }
        }

        internal int PlayerPedHandle
        {
            get => _playerPedHandle;
            private set
            {
                if (Equals(_playerPedHandle, value))
                    return;

                _playerPedHandle = value;
                PlayerPedHandleChanged?.Invoke(this, value);
            }
        }

        internal event EventHandler<int> PlayerVehicleHandleChanged;
        internal event EventHandler<int> PlayerPedHandleChanged;

        internal event EventHandler ToggleMenuVisibility;

        internal VStancerConfig Config { get; private set; }
        internal WheelScript WheelScript { get; private set; }
        internal WheelModScript WheelModScript { get; private set; }
        internal ClientPresetsScript ClientPresetsScript { get; private set; }

        public MainScript()
        {
            if (GetCurrentResourceName() != Globals.ResourceName)
            {
                Debug.WriteLine($"{nameof(MainScript)}: Invalid resource name, be sure the resource name is {Globals.ResourceName}");
                return;
            }

            _lastTime = GetGameTimer();
            _playerVehicleHandle = -1;
            _playerPedHandle = -1;
            _playerPedCoords = Vector3.Zero;
            _worldVehiclesHandles = new List<int>();
            _maxDistanceSquared = 10;

            Config = LoadConfig();
            _maxDistanceSquared = (float)Math.Sqrt(Config.ScriptRange);
            WheelScript = new WheelScript(this);
            RegisterScript(WheelScript);

            if (Config.EnableWheelMod)
            {
                WheelModScript = new WheelModScript(this);
                RegisterScript(WheelModScript);
            }

            if (Config.EnableClientPresets)
            {
                ClientPresetsScript = new ClientPresetsScript(this);
            }

            if (!Config.DisableMenu)
                Menu = new MainMenu(this);

            Tick += GetPlayerAndVehicleTask;
            Tick += TimedTask;
            Tick += HideUITask;

            RegisterCommands();

            Exports.Add("SetWheelPreset", new Func<int, float, float, float, float, bool>(SetWheelPreset));
            Exports.Add("GetWheelPreset", new Func<int, float[]>(GetWheelPreset));
            Exports.Add("ResetWheelPreset", new Func<int, bool>(ResetWheelPreset));

            Exports.Add("SetFrontCamber", new Func<int, float, bool>(SetFrontCamber));
            Exports.Add("SetRearCamber", new Func<int, float, bool>(SetRearCamber));
            Exports.Add("SetFrontTrackWidth", new Func<int, float, bool>(SetFrontTrackWidth));
            Exports.Add("SetRearTrackWidth", new Func<int, float, bool>(SetRearTrackWidth));

            Exports.Add("GetFrontCamber", new Func<int, float[]>(GetFrontCamber));
            Exports.Add("GetRearCamber", new Func<int, float[]>(GetRearCamber));
            Exports.Add("GetFrontTrackWidth", new Func<int, float[]>(GetFrontTrackWidth));
            Exports.Add("GetRearTrackWidth", new Func<int, float[]>(GetRearTrackWidth));

            Exports.Add("SaveClientPreset", new Func<string, int, bool>(SaveClientPreset));
            Exports.Add("LoadClientPreset", new Func<string, int, bool>(LoadClientPreset));
            Exports.Add("DeleteClientPreset", new Func<string, bool>(DeleteClientPreset));
            Exports.Add("GetClientPresetList", new Func<string[]>(GetClientPresetList));
        }

        private async Task HideUITask()
        {
            if (Menu != null)
                Menu.HideMenu = _playerVehicleHandle == -1;

            await Task.FromResult(0);
        }

        internal List<int> GetCloseVehicleHandles()
        {
            List<int> closeVehicles = new List<int>();

            foreach (int handle in _worldVehiclesHandles)
            {
                if (!DoesEntityExist(handle))
                    continue;

                Vector3 coords = GetEntityCoords(handle, true);

                if (Vector3.DistanceSquared(_playerPedCoords, coords) <= _maxDistanceSquared)
                    closeVehicles.Add(handle);
            }

            return closeVehicles;
        }

        private async Task TimedTask()
        {
            long currentTime = GetGameTimer() - _lastTime;

            if (currentTime > Config.Timer)
            {
                _playerPedCoords = GetEntityCoords(_playerPedHandle, true);

                _worldVehiclesHandles = VStancerUtilities.GetWorldVehicles();

                _lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        private async Task GetPlayerAndVehicleTask()
        {
            await Task.FromResult(0);

            _playerPedHandle = PlayerPedId();

            if (!IsPedInAnyVehicle(_playerPedHandle, false))
            {
                PlayerVehicleHandle = -1;
                return;
            }

            int vehicle = GetVehiclePedIsIn(_playerPedHandle, false);

            // If this model isn't a car, or player isn't the driver, or vehicle is not driveable
            if (!IsThisModelACar((uint)GetEntityModel(vehicle)) || GetPedInVehicleSeat(vehicle, -1) != _playerPedHandle || !IsVehicleDriveable(vehicle, false))
            {
                PlayerVehicleHandle = -1;
                return;
            }

            PlayerVehicleHandle = vehicle;
        }

        private VStancerConfig LoadConfig(string filename = "config.json")
        {
            VStancerConfig config;

            try
            {
                string strings = LoadResourceFile(Globals.ResourceName, filename);
                config = JsonConvert.DeserializeObject<VStancerConfig>(strings);

                Debug.WriteLine($"{nameof(MainScript)}: Loaded config from {filename}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{nameof(MainScript)}: Impossible to load {filename}", e.Message);
                Debug.WriteLine(e.StackTrace);

                config = new VStancerConfig();
            }

            return config;
        }

        public async Task<string> GetOnScreenString(string title, string defaultText)
        {
            DisplayOnscreenKeyboard(1, title, "", defaultText, "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await Delay(100);

            return GetOnscreenKeyboardResult();
        }

        private void RegisterCommands()
        {
            RegisterCommand($"{Globals.CommandPrefix}decorators", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    WheelScript.PrintDecoratorsInfo(_playerVehicleHandle);
                    WheelModScript.PrintDecoratorsInfo(_playerVehicleHandle);
                }
                else
                {
                    if (int.TryParse(args[0], out int value))
                    {
                        WheelScript.PrintDecoratorsInfo(value);
                        WheelModScript.PrintDecoratorsInfo(value);
                    }
                    else Debug.WriteLine($"{nameof(MainScript)}: Error parsing entity handle {args[0]} as int");
                }
            }), false);

            RegisterCommand($"{Globals.CommandPrefix}range", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"{nameof(MainScript)}: Missing float argument");
                    return;
                }

                if (float.TryParse(args[0], out float value))
                {
                    Config.ScriptRange = value;
                    _maxDistanceSquared = (float)Math.Sqrt(value);
                    Debug.WriteLine($"{nameof(MainScript)}: {nameof(Config.ScriptRange)} updated to {value}");
                }
                else Debug.WriteLine($"{nameof(MainScript)}: Error parsing {args[0]} as float");

            }), false);

            RegisterCommand($"{Globals.CommandPrefix}debug", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"{nameof(MainScript)}: Missing bool argument");
                    return;
                }

                if (bool.TryParse(args[0], out bool value))
                {
                    Config.Debug = value;
                    Debug.WriteLine($"{nameof(MainScript)}: {nameof(Config.Debug)} updated to {value}");
                }
                else Debug.WriteLine($"{nameof(MainScript)}: Error parsing {args[0]} as bool");

            }), false);

            RegisterCommand($"{Globals.CommandPrefix}preset", new Action<int, dynamic>((source, args) =>
            {
                if (WheelScript?.WheelData != null)
                    Debug.WriteLine(WheelScript.WheelData.ToString());
                else
                    Debug.WriteLine($"{nameof(MainScript)}: {nameof(WheelScript.WheelData)} is null");

                if (WheelModScript?.WheelModData != null)
                    Debug.WriteLine(WheelModScript.WheelModData.ToString());
                else
                    Debug.WriteLine($"{nameof(MainScript)}: {nameof(WheelModScript.WheelModData)} is null");
            }), false);

            RegisterCommand($"{Globals.CommandPrefix}print", new Action<int, dynamic>((source, args) =>
            {
                if (WheelScript != null)
                    WheelScript.PrintVehiclesWithDecorators(_worldVehiclesHandles);
                if (WheelModScript != null)
                    WheelModScript.PrintVehiclesWithDecorators(_worldVehiclesHandles);
            }), false);

            if(!Config.DisableMenu)
            {
                if (Config.ExposeCommand)
                    RegisterCommand("vstancer", new Action<int, dynamic>((source, args) => { ToggleMenuVisibility?.Invoke(this, EventArgs.Empty); }), false);

                if (Config.ExposeEvent)
                    EventHandlers.Add("vstancer:toggleMenu", new Action(() => { ToggleMenuVisibility?.Invoke(this, EventArgs.Empty); }));
            }
        }

        public float[] GetWheelPreset(int vehicle)
        {
            if (WheelScript == null)
                return new float[] { };

            if (WheelScript.API_GetWheelPreset(vehicle, out WheelPreset preset))
                return preset.ToArray();

            return new float[] { };
        }

        public bool SetWheelPreset(int vehicle, float frontTrackWidth, float frontCamber, float rearTrackWidth, float rearCamber)
        {
            if (WheelScript == null)
                return false;

            WheelPreset preset = new WheelPreset(frontTrackWidth, frontCamber, rearTrackWidth, rearCamber);
            return WheelScript.API_SetWheelPreset(vehicle, preset);
        }
        
        public bool ResetWheelPreset(int vehicle)
        {
            if (WheelScript == null)
                return false;

            return WheelScript.API_ResetWheelPreset(vehicle);
        }

        public bool SetFrontCamber(int vehicle, float value)
        {
            if (WheelScript == null)
                return false;

            return WheelScript.API_SetFrontCamber(vehicle, value);
        }

        public bool SetRearCamber(int vehicle, float value)
        {
            if (WheelScript == null)
                return false;

            return WheelScript.API_SetRearCamber(vehicle, value);
        }

        public bool SetFrontTrackWidth(int vehicle, float value)
        {
            if (WheelScript == null)
                return false;

            return WheelScript.API_SetFrontTrackWidth(vehicle, value);
        }

        public bool SetRearTrackWidth(int vehicle, float value)
        {
            if (WheelScript == null)
                return false;

            return WheelScript.API_SetRearTrackWidth(vehicle, value);
        }

        public float[] GetFrontCamber(int vehicle)
        {
            if (WheelScript == null)
                return new float[] { };

            if (WheelScript.API_GetFrontCamber(vehicle, out float value))
                return new float[] { value };

            return new float[] { };
        }

        public float[] GetRearCamber(int vehicle)
        {
            if (WheelScript == null)
                return new float[] { };

            if (WheelScript.API_GetRearCamber(vehicle, out float value))
                return new float[] { value };

            return new float[] { };
        }

        public float[] GetFrontTrackWidth(int vehicle)
        {
            if (WheelScript == null)
                return new float[] { };

            if (WheelScript.API_GetFrontTrackWidth(vehicle, out float value))
                return new float[] { value };

            return new float[] { };
        }

        public float[] GetRearTrackWidth(int vehicle)
        {
            if (WheelScript == null)
                return new float[] { };

            if (WheelScript.API_GetRearTrackWidth(vehicle, out float value))
                return new float[] { value };

            return new float[] { };
        }

        public bool SaveClientPreset(string id, int vehicle)
        {
            if (ClientPresetsScript == null)
                return false;

            return ClientPresetsScript.API_SavePreset(id, vehicle);
        }

        public bool LoadClientPreset(string id, int vehicle)
        {
            if (ClientPresetsScript == null)
                return false;

            return ClientPresetsScript.API_LoadPreset(id, vehicle);
        }

        public bool DeleteClientPreset(string id)
        {
            if (ClientPresetsScript == null)
                return false;

            return ClientPresetsScript.API_DeletePreset(id);
        }

        public string[] GetClientPresetList()
        {
            if (ClientPresetsScript == null)
                return new string[] { };

            return ClientPresetsScript.API_GetClientPresetList().ToArray();
        }
    }
}
