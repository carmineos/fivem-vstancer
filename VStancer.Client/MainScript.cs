using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using Newtonsoft.Json;
using VStancer.Client.UI;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client
{
    public class MainScript : BaseScript
    {
        private readonly MainMenu _mainMenu;

        private long _lastTime;
        private int _playerVehicleHandle;
        private int _playerPedHandle;
        private Vector3 _playerPedCoords;
        private List<int> _worldVehiclesHandles;

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
        internal VStancerDataManager VStancerDataManager { get; private set; }
        internal VStancerExtraManager VStancerExtraManager { get; private set; }
        internal LocalPresetsManager LocalPresetsManager { get; private set; }

        public MainScript()
        {
            if (GetCurrentResourceName() != Globals.ResourceName)
            {
                Debug.WriteLine($"{Globals.ScriptName}: Invalid resource name, be sure the resource name is {Globals.ResourceName}");
                return;
            }

            _lastTime = GetGameTimer();
            _playerVehicleHandle = -1;
            _playerPedHandle = -1;
            _playerPedCoords = Vector3.Zero;
            _worldVehiclesHandles = new List<int>();

            Config = LoadConfig();
            VStancerDataManager = new VStancerDataManager(this);
            VStancerExtraManager = new VStancerExtraManager(this);
            LocalPresetsManager = new LocalPresetsManager(this);

            _mainMenu = new MainMenu(this);

            Tick += GetPlayerAndVehicleTask;
            Tick += TimedTask;
            Tick += HideUITask;

            RegisterScript(VStancerDataManager);
            RegisterScript(VStancerExtraManager);

            RegisterCommands();
        }

        private async Task HideUITask()
        {
            if (_mainMenu != null)
                _mainMenu.HideMenu = _playerVehicleHandle == -1;

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

                if (Vector3.Distance(_playerPedCoords, coords) <= Config.ScriptRange)
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

            // Update current vehicle and get its preset
            if (vehicle != _playerVehicleHandle)
                PlayerVehicleHandle = vehicle;
        }

        private VStancerConfig LoadConfig(string filename = "config.json")
        {
            VStancerConfig config;

            try
            {
                string strings = LoadResourceFile(Globals.ResourceName, filename);
                config = JsonConvert.DeserializeObject<VStancerConfig>(strings);

                Debug.WriteLine($"{Globals.ScriptName}: Loaded config from {filename}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{Globals.ScriptName}: Impossible to load {filename}", e.Message);
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
            RegisterCommand("vstancer_decorators", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                    VStancerDataManager.PrintDecoratorsInfo(_playerVehicleHandle);
                else
                {
                    if (int.TryParse(args[0], out int value))
                        VStancerDataManager.PrintDecoratorsInfo(value);
                    else Debug.WriteLine($"{Globals.ScriptName}: Error parsing entity handle {args[0]} as int");
                }
            }), false);
            /**
            RegisterCommand("vstancer_range", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"{Globals.ScriptName}: Missing float argument");
                    return;
                }

                if (float.TryParse(args[0], out float value))
                {
                    Config.ScriptRange = value;
                    Debug.WriteLine($"{Globals.ScriptName}: {nameof(Config.ScriptRange)} updated to {value}");
                }
                else Debug.WriteLine($"{Globals.ScriptName}: Error parsing {args[0]} as float");

            }), false);

            RegisterCommand("vstancer_debug", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"{Globals.ScriptName}: Missing bool argument");
                    return;
                }

                if (bool.TryParse(args[0], out bool value))
                {
                    Config.Debug = value;
                    Debug.WriteLine($"{Globals.ScriptName}: {nameof(Config.Debug)} updated to {value}");
                }
                else Debug.WriteLine($"{Globals.ScriptName}: Error parsing {args[0]} as bool");

            }), false);

            RegisterCommand("vstancer_preset", new Action<int, dynamic>((source, args) =>
            {
                if (CurrentPreset != null)
                    Debug.WriteLine(CurrentPreset.ToString());
                else
                    Debug.WriteLine($"{Globals.ScriptName}: {nameof(CurrentPreset)} is null");

                if (CurrentExtra != null)
                    Debug.WriteLine(CurrentExtra.ToString());
                else
                    Debug.WriteLine($"{Globals.ScriptName}: {nameof(CurrentExtra)} is null");
            }), false);

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(_worldVehiclesHandles);
                if (Config.Extra.EnableExtra)
                    PrintVehiclesWithExtraDecorators(_worldVehiclesHandles);
            }), false);

            if (Config.ExposeCommand)
                RegisterCommand("vstancer", new Action<int, dynamic>((source, args) => { ToggleMenuVisibility?.Invoke(this, EventArgs.Empty); }), false);

            if (Config.ExposeEvent)
                EventHandlers.Add("vstancer:toggleMenu", new Action(() => { ToggleMenuVisibility?.Invoke(this, EventArgs.Empty); }));


            Exports.Add("SetVstancerPreset", new Action<int, float, float, float, float, object, object, object, object>(SetVstancerPreset));
            Exports.Add("GetVstancerPreset", new Func<int, float[]>(GetVstancerPreset));
            */
        }
    }
}
