using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json;

namespace VStancer.Client
{
    public class VStancerEditor : BaseScript
    {
        /// <summary>
        /// The script which renders the menu
        /// </summary>
        private readonly VStancerMenu _vstancerMenu;

        /// <summary>
        /// The handle of the current vehicle
        /// </summary>
        private int _playerVehicleHandle;

        /// <summary>
        /// Indicates the last game time the timed tasks have been executed
        /// </summary>
        private long _lastTime;

        /// <summary>
        /// The handle of the current player ped
        /// </summary>
        private int _playerPedHandle;

        /// <summary>
        /// The list of all the vehicles' handles around the client's position 
        /// </summary>
        private IEnumerable<int> _worldVehiclesHandles;

        /// <summary>
        /// The delta among which two float are considered equals
        /// </summary>
        private const float Epsilon = 0.001f;

        public const string FrontTrackWidthID = "vstancer_trackwidth_f";
        public const string RearTrackWidthID = "vstancer_trackwidth_r";
        public const string FrontCamberID = "vstancer_camber_f";
        public const string RearCamberID = "vstancer_camber_r";

        public const string DefaultFrontTrackWidthID = "vstancer_trackwidth_f_def";
        public const string DefaultRearTrackWidthID = "vstancer_trackwidth_r_def";
        public const string DefaultFrontCamberID = "vstancer_camber_f_def";
        public const string DefaultRearCamberID = "vstancer_camber_r_def";

        public const string ResetID = "vstancer_reset";
        public const string ExtraResetID = "vstancer_extra_reset";

        public const string ExtraWidthID = "vstancer_extra_width";
        public const string ExtraSizeID = "vstancer_extra_size";
        public const string DefaultExtraWidthID = "vstancer_extra_width_def";
        public const string DefaultExtraSizeID = "vstancer_extra_size_def";

        public const string FrontExtraTireColliderWidthID = "vstancer_extra_tirecollider_width_f";
        public const string FrontExtraTireColliderSizeID = "vstancer_extra_tirecollider_size_f";
        public const string FrontExtraRimColliderSizeID = "vstancer_extra_rimcollider_size_f";

        public const string RearExtraTireColliderWidthID = "vstancer_extra_tirecollider_width_r";
        public const string RearExtraTireColliderSizeID = "vstancer_extra_tirecollider_size_r";
        public const string RearExtraRimColliderSizeID = "vstancer_extra_rimcollider_size_r";

        public const string DefaultFrontExtraTireColliderWidthID = "vstancer_extra_tirecollider_width_f_def";
        public const string DefaultFrontExtraTireColliderSizeID = "vstancer_extra_tirecollider_size_f_def";
        public const string DefaultFrontExtraRimColliderSizeZID = "vstancer_extra_rimcollider_size_f_def";

        public const string DefaultRearExtraTireColliderWidthID = "vstancer_extra_tirecollider_width_r_def";
        public const string DefaultRearExtraTireColliderSizeID = "vstancer_extra_tirecollider_size_r_def";
        public const string DefaultRearExtraRimColliderSizeID = "vstancer_extra_rimcollider_size_r_def";

        /// <summary>
        /// Returns wheter <see cref="_playerVehicleHandle"/> and <see cref="CurrentPreset"/> are valid
        /// </summary>
        public bool CurrentPresetIsValid => _playerVehicleHandle != -1 && CurrentPreset != null;
        public bool CurrentExtraIsValid => _playerVehicleHandle != -1 && CurrentExtra != null && VehicleWheelMod != -1;
        public int VehicleWheelMod { get; set; }

        /// <summary>
        /// The preset associated to the player's vehicle
        /// </summary>
        public VStancerPreset CurrentPreset { get; private set; }
        public VStancerExtra CurrentExtra { get; private set; }

        /// <summary>
        /// The configuration of the script
        /// </summary>
        public VStancerConfig Config { get; private set; }

        /// <summary>
        /// The service which manages the local presets
        /// </summary>
        public IPresetManager<string, VStancerPreset> LocalPresetsManager { get; private set; }

        /// <summary>
        /// Invoked when <see cref="CurrentPreset"/> is changed
        /// </summary>
        public event EventHandler NewPresetCreated;
        public event EventHandler ExtraChanged;

        /// <summary>
        /// Triggered when the client wants to manually toggle the menu visibility
        /// using the optional command/event
        /// </summary>
        public event EventHandler ToggleMenuVisibility;

        public VStancerEditor()
        {
            // If the resource name is not the expected one ...
            if (GetCurrentResourceName() != Globals.ResourceName)
            {
                Debug.WriteLine($"{Globals.ScriptName}: Invalid resource name, be sure the resource name is {Globals.ResourceName}");
                return;
            }

            _lastTime = GetGameTimer();
            _playerVehicleHandle = -1;
            CurrentPreset = null;
            VehicleWheelMod = -1;
            _worldVehiclesHandles = Enumerable.Empty<int>();

            Config = LoadConfig();
            RegisterRequiredDecorators();

            if (Config.Extra.EnableExtra)
                RegisterExtraDecorators();

            LocalPresetsManager = new KvpPresetManager(Globals.KvpPrefix);

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

            RegisterCommand("vstancer_decorators", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                    PrintDecoratorsInfo(_playerVehicleHandle);
                else
                {
                    if (int.TryParse(args[0], out int value))
                        PrintDecoratorsInfo(value);
                    else Debug.WriteLine($"{Globals.ScriptName}: Error parsing entity handle {args[0]} as int");
                }
            }), false);

            RegisterCommand("vstancer_preset", new Action<int, dynamic>((source, args) =>
            {
                if (CurrentPreset != null)
                    Debug.WriteLine(CurrentPreset.ToString());
                else
                    Debug.WriteLine($"{Globals.ScriptName}: Current preset doesn't exist");
            }), false);

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(_worldVehiclesHandles);
            }), false);

            if (Config.ExposeCommand)
                RegisterCommand("vstancer", new Action<int, dynamic>((source, args) => { ToggleMenuVisibility?.Invoke(this, EventArgs.Empty); }), false);

            if (Config.ExposeEvent)
                EventHandlers.Add("vstancer:toggleMenu", new Action(() => { ToggleMenuVisibility?.Invoke(this, EventArgs.Empty); }));


            Exports.Add("SetVstancerPreset", new Action<int, float, float, float, float, object, object, object, object>(SetVstancerPreset));
            Exports.Add("GetVstancerPreset", new Func<int, float[]>(GetVstancerPreset));

            // Create a script for the menu ...
            _vstancerMenu = new VStancerMenu(this);

            _vstancerMenu.EditorMenuResetPreset += OnEditorMenuResetPresetInvoked;
            _vstancerMenu.EditorMenuPresetValueChanged += OnEditorMenuPresetValueChanged;
            _vstancerMenu.ExtraMenuPresetValueChanged += OnExtraMenuPresetValueChanged;
            _vstancerMenu.ExtraMenuResetPreset += OnExtraMenuResetPresetInvoked;

            _vstancerMenu.PersonalPresetsMenuApplyPreset += OnPersonalPresetsMenuApplyPresetInvoked;
            _vstancerMenu.PersonalPresetsMenuSavePreset += OnPersonalPresetsMenuSavePresetInvoked;
            _vstancerMenu.PersonalPresetsMenuDeletePreset += OnPersonalPresetsMenuDeletePresetInvoked;

            Tick += GetPlayerVehicleTask;
            Tick += UpdateWorldVehiclesTask;
            Tick += UpdatePlayerVehicleDecoratorsTask;
            Tick += HideUITask;
        }

        private async Task HideUITask()
        {
            if (_vstancerMenu != null)
                _vstancerMenu.HideUI = !CurrentPresetIsValid;

            await Task.FromResult(0);
        }

        /// <summary>
        /// Invalidates the preset
        /// </summary>
        private void InvalidatePreset()
        {
            if(CurrentPresetIsValid)
            {
                CurrentPreset.PropertyChanged -= OnPresetEdited;
                CurrentPreset = null;

                _playerVehicleHandle = -1;

                Tick -= UpdatePlayerVehicleTask;
            }
        }

        private void InvalidateExtra()
        {
            if (CurrentExtraIsValid)
            {
                CurrentExtra.PropertyChanged -= OnExtraPropertyEdited;
                CurrentExtra = null;
            }
            VehicleWheelMod = -1;
        }

        private void OnExtraPropertyEdited(string propertyName, float value)
        {
            bool result = false;
            switch(propertyName)
            {
                case nameof(VStancerExtra.WheelWidth):
                    result = SetVehicleWheelWidth(_playerVehicleHandle, value);
                    if (result)
                    {
                        UpdateFloatDecorator(_playerVehicleHandle, DefaultExtraWidthID, CurrentExtra.DefaultWheelWidth, value);
                        UpdateFloatDecorator(_playerVehicleHandle, ExtraWidthID, value, CurrentExtra.DefaultWheelWidth);
                    }
                    break;

                case nameof(VStancerExtra.WheelSize):
                    result = SetVehicleWheelSize(_playerVehicleHandle, value); 
                    if (result)
                    {
                        UpdateFloatDecorator(_playerVehicleHandle, DefaultExtraSizeID, CurrentExtra.DefaultWheelSize, value);
                        UpdateFloatDecorator(_playerVehicleHandle, ExtraSizeID, value, CurrentExtra.DefaultWheelSize);
                    }
                    break;

                case nameof(VStancerExtra.Reset):
                    RemoveExtraDecoratorsFromVehicle(_playerVehicleHandle);
                    break;

                default:
                    break;
            }

            if(Config.Debug)
                CitizenFX.Core.Debug.WriteLine($"Edited: {propertyName}, value: {value}, result: {result}");
        }

        /// <summary>
        /// Invoked when a value of <see cref="CurrentPreset"/> changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void OnPresetEdited(object sender, string editedProperty)
        {
            if (!CurrentPresetIsValid)
                return;

             // If false then this has been invoked by after a reset
            if (editedProperty == nameof(VStancerPreset.Reset))
                RemoveDecoratorsFromVehicle(_playerVehicleHandle);

            // Force one single refresh to update rendering at correct position after reset
            // This is required because otherwise the vehicle won't update immediately as
            UpdateVehicleUsingPreset(_playerVehicleHandle, CurrentPreset);
        }

        /// <summary>
        /// Updates the <see cref="_playerVehicleHandle"/> and the <see cref="CurrentPreset"/>
        /// </summary>
        /// <returns></returns>
        private async Task GetPlayerVehicleTask()
        {
            await Task.FromResult(0);

            _playerPedHandle = PlayerPedId();

            if (!IsPedInAnyVehicle(_playerPedHandle, false))
            {
                InvalidatePreset();
                InvalidateExtra();
                return;
            }

            int vehicle = GetVehiclePedIsIn(_playerPedHandle, false);

            // If this model isn't a car, or player isn't the driver, or vehicle is not driveable
            if (!IsThisModelACar((uint)GetEntityModel(vehicle)) || GetPedInVehicleSeat(vehicle, -1) != _playerPedHandle || !IsVehicleDriveable(vehicle, false))
            {
                InvalidatePreset();
                InvalidateExtra();
                return;
            }
            
            bool triggerEvent = false;
            
            // Update current vehicle and get its preset
            if (vehicle != _playerVehicleHandle)
            {
                InvalidatePreset();
                InvalidateExtra();

                CurrentPreset = CreatePresetFromHandle(vehicle);
                CurrentPreset.PropertyChanged += OnPresetEdited;

                _playerVehicleHandle = vehicle;
                Tick += UpdatePlayerVehicleTask;

                triggerEvent = true;
            }

            if (Config.Extra.EnableExtra)
            {
                int vehicleWheelMod = GetVehicleMod(_playerVehicleHandle, 23);
                
                // If wheel mod status is different from previous one
                if (vehicleWheelMod != VehicleWheelMod)
                {
                    InvalidateExtra();

                    if (Config.Debug)
                        CitizenFX.Core.Debug.WriteLine($"New vehicle mod of type 23: {vehicleWheelMod}");

                    // If new value value isn't "no mod installed"
                    if (vehicleWheelMod != -1)
                    {
                        // TODO: Find out why on first change it shows value 1

                        // Get new data from entity
                        CurrentExtra = GetVStancerExtraFromHandle(_playerVehicleHandle);
                        CurrentExtra.PropertyChanged += OnExtraPropertyEdited;
                    }

                    VehicleWheelMod = vehicleWheelMod;

                    // Notify UI to redraw extra menu
                    ExtraChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            if (triggerEvent)
                NewPresetCreated?.Invoke(this, EventArgs.Empty);
        }

        private VStancerExtra GetVStancerExtraFromHandle(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            float wheelWidth = DecorExistOn(vehicle, DefaultExtraWidthID) ? DecorGetFloat(vehicle, DefaultExtraWidthID) : GetVehicleWheelWidth(vehicle);
            float wheelSize = DecorExistOn(vehicle, DefaultExtraSizeID) ? DecorGetFloat(vehicle, DefaultExtraSizeID) : GetVehicleWheelSize(vehicle);
            float frontTireColliderScaleX_def = DecorExistOn(vehicle, DefaultFrontExtraTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultFrontExtraTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, 0);
            float frontTireColliderScaleYZ_def = DecorExistOn(vehicle, DefaultFrontExtraTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultFrontExtraTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, 0);
            float frontRimColliderScaleYZ_def = DecorExistOn(vehicle, DefaultFrontExtraRimColliderSizeZID) ? DecorGetFloat(vehicle, DefaultFrontExtraRimColliderSizeZID) : GetVehicleWheelRimColliderSize(vehicle, 0);

            float rearTireColliderScaleX_def = DecorExistOn(vehicle, DefaultRearExtraTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultRearExtraTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, frontCount);
            float rearTireColliderScaleYZ_def = DecorExistOn(vehicle, DefaultRearExtraTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearExtraTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, frontCount);
            float rearRimColliderScaleYZ_def = DecorExistOn(vehicle, DefaultRearExtraRimColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearExtraRimColliderSizeID) : GetVehicleWheelRimColliderSize(vehicle, frontCount);

            // Create the preset with the default values
            return new VStancerExtra(wheelsCount, wheelWidth, wheelSize,
                frontTireColliderScaleX_def, frontTireColliderScaleYZ_def, frontRimColliderScaleYZ_def,
                rearTireColliderScaleX_def, rearTireColliderScaleYZ_def, rearRimColliderScaleYZ_def)
            {
                // Assign the current values
                WheelWidth = DecorExistOn(vehicle, ExtraWidthID) ? DecorGetFloat(vehicle, ExtraWidthID) : wheelWidth,
                WheelSize = DecorExistOn(vehicle, ExtraSizeID) ? DecorGetFloat(vehicle, ExtraSizeID) : wheelSize,
                
                //FrontTireColliderScaleX = DecorExistOn(vehicle, FrontWheelModTireColliderScaleXID) ? DecorGetFloat(vehicle, FrontWheelModTireColliderScaleXID) : frontTireColliderScaleX_def,
                //FrontTireColliderScaleYZ = DecorExistOn(vehicle, FrontWheelModTireColliderScaleYZID) ? DecorGetFloat(vehicle, FrontWheelModTireColliderScaleYZID) : frontTireColliderScaleYZ_def,
                //FrontRimColliderScaleYZ = DecorExistOn(vehicle, FrontWheelModRimColliderScaleYZID) ? DecorGetFloat(vehicle, FrontWheelModRimColliderScaleYZID) : frontRimColliderScaleYZ_def,

                //RearTireColliderScaleX = DecorExistOn(vehicle, RearWheelModTireColliderScaleXID) ? DecorGetFloat(vehicle, RearWheelModTireColliderScaleXID) : rearTireColliderScaleX_def,
                //RearTireColliderScaleYZ = DecorExistOn(vehicle, RearWheelModTireColliderScaleYZID) ? DecorGetFloat(vehicle, RearWheelModTireColliderScaleYZID) : rearTireColliderScaleYZ_def,
                //RearRimColliderScaleYZ = DecorExistOn(vehicle, RearWheelModRimColliderScaleYZID) ? DecorGetFloat(vehicle, RearWheelModRimColliderScaleYZID) : rearRimColliderScaleYZ_def
            };
        }

        /// <summary>
        /// The task that updates the current vehicle
        /// </summary>
        /// <returns></returns>
        private async Task UpdatePlayerVehicleTask()
        {
            // Check if current vehicle needs to be refreshed
            if (CurrentPresetIsValid && CurrentPreset.IsEdited)
                UpdateVehicleUsingPreset(_playerVehicleHandle, CurrentPreset);

            await Task.FromResult(0);
        }

        /// <summary>
        /// The task that updates the vehicles of the world
        /// </summary>
        /// <returns></returns>
        private async Task UpdateWorldVehiclesTask()
        {
            // Refreshes the iterated vehicles
            IEnumerable<int> vehiclesList = _worldVehiclesHandles.Except(new List<int> { _playerVehicleHandle });
            Vector3 currentCoords = GetEntityCoords(_playerPedHandle, true);

            foreach (int entity in vehiclesList)
            {
                if (DoesEntityExist(entity))
                {
                    Vector3 coords = GetEntityCoords(entity, true);

                    if (Vector3.Distance(currentCoords, coords) <= Config.ScriptRange)
                        UpdateVehicleUsingDecorators(entity);
                }
            }

            await Task.FromResult(0);
        }

        /// <summary>
        /// The task that updates the script decorators attached on the current vehicle
        /// </summary>
        /// <returns></returns>
        private async Task UpdatePlayerVehicleDecoratorsTask()
        {
            long currentTime = (GetGameTimer() - _lastTime);

            // Check if decorators needs to be updated
            if (currentTime > Config.Timer)
            {
                if (CurrentPresetIsValid)
                    UpdateVehicleDecorators(_playerVehicleHandle, CurrentPreset);

                // Also update world vehicles list
                _worldVehiclesHandles = new VehicleEnumerable();

                _lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        /// <summary>
        /// Registers the decorators for this script
        /// </summary>
        private void RegisterRequiredDecorators()
        {
            DecorRegister(FrontTrackWidthID, 1);
            DecorRegister(FrontCamberID, 1);
            DecorRegister(RearTrackWidthID, 1);
            DecorRegister(RearCamberID, 1);

            DecorRegister(DefaultFrontTrackWidthID, 1);
            DecorRegister(DefaultFrontCamberID, 1);
            DecorRegister(DefaultRearTrackWidthID, 1);
            DecorRegister(DefaultRearCamberID, 1);
        }

        private void RegisterExtraDecorators()
        {
            DecorRegister(ExtraWidthID, 1);
            DecorRegister(ExtraSizeID, 1);
            DecorRegister(DefaultExtraWidthID, 1);
            DecorRegister(DefaultExtraSizeID, 1);
        }

        /// <summary>
        /// Removes the decorators from the <paramref name="vehicle"/>
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        private void RemoveDecoratorsFromVehicle(int vehicle)
        {
            if (DecorExistOn(vehicle, FrontTrackWidthID))
                DecorRemove(vehicle, FrontTrackWidthID);

            if (DecorExistOn(vehicle, FrontCamberID))
                DecorRemove(vehicle, FrontCamberID);

            if (DecorExistOn(vehicle, DefaultFrontTrackWidthID))
                DecorRemove(vehicle, DefaultFrontTrackWidthID);

            if (DecorExistOn(vehicle, DefaultFrontCamberID))
                DecorRemove(vehicle, DefaultFrontCamberID);

            if (DecorExistOn(vehicle, RearTrackWidthID))
                DecorRemove(vehicle, RearTrackWidthID);

            if (DecorExistOn(vehicle, RearCamberID))
                DecorRemove(vehicle, RearCamberID);

            if (DecorExistOn(vehicle, DefaultRearTrackWidthID))
                DecorRemove(vehicle, DefaultRearTrackWidthID);

            if (DecorExistOn(vehicle, DefaultRearCamberID))
                DecorRemove(vehicle, DefaultRearCamberID);
        }

        private void RemoveExtraDecoratorsFromVehicle(int vehicle)
        {
            if (DecorExistOn(vehicle, ExtraSizeID))
                DecorRemove(vehicle, ExtraSizeID);

            if (DecorExistOn(vehicle, ExtraWidthID))
                DecorRemove(vehicle, ExtraWidthID);

            if (DecorExistOn(vehicle, DefaultExtraSizeID))
                DecorRemove(vehicle, DefaultExtraSizeID);

            if (DecorExistOn(vehicle, DefaultExtraWidthID))
                DecorRemove(vehicle, DefaultExtraWidthID);
        }

        /// <summary>
        /// Returns the preset as an array of floats containing in order: 
        /// frontOffset, frontRotation, rearOffset, rearRotation, defaultFrontOffset, defaultFrontRotation, defaultRearOffset, defaultRearRotation
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        /// <returns>The float array</returns>
        public float[] GetVstancerPreset(int vehicle)
        {
            VStancerPreset preset = (vehicle == _playerVehicleHandle && CurrentPresetIsValid) ? CurrentPreset : CreatePresetFromHandle(vehicle);
            return preset?.ToArray();
        }

        /// <summary>
        /// Loads a Vstancer preset for the <paramref name="vehicle"/> with the specified values.
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        /// <param name="frontOffset">The front offset value</param>
        /// <param name="frontRotation">The front rotation value</param>
        /// <param name="rearOffset">The rear offset value</param>
        /// <param name="rearRotation">The rear rotation value</param>
        /// <param name="defaultFrontOffset">The default front offset value</param>
        /// <param name="defaultFrontRotation">The default front rotation value</param>
        /// <param name="defaultRearOffset">The default rear offset value</param>
        /// <param name="defaultRearRotation">The default rear rotation value</param>
        public void SetVstancerPreset(int vehicle, float frontOffset, float frontRotation, float rearOffset, float rearRotation, object defaultFrontOffset = null, object defaultFrontRotation = null, object defaultRearOffset = null, object defaultRearRotation = null)
        {
            if (Config.Debug)
                Debug.WriteLine($"{Globals.ScriptName}: SetVstancerPreset parameters {frontOffset} {frontRotation} {rearOffset} {rearRotation} {defaultFrontOffset} {defaultFrontRotation} {defaultRearOffset} {defaultRearRotation}");

            if (!DoesEntityExist(vehicle))
                return;

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            float off_f_def = defaultFrontOffset is float
                ? (float)defaultFrontOffset
                : DecorExistOn(vehicle, DefaultFrontTrackWidthID)
                ? DecorGetFloat(vehicle, DefaultFrontTrackWidthID)
                : GetVehicleWheelXOffset(vehicle, 0);

            float rot_f_def = defaultFrontRotation is float
                ? (float)defaultFrontRotation
                : DecorExistOn(vehicle, DefaultFrontCamberID)
                ? DecorGetFloat(vehicle, DefaultFrontCamberID)
                : GetVehicleWheelYRotation(vehicle, 0);

            float off_r_def = defaultRearOffset is float
                ? (float)defaultRearOffset
                : DecorExistOn(vehicle, DefaultRearTrackWidthID)
                ? DecorGetFloat(vehicle, DefaultRearTrackWidthID)
                : GetVehicleWheelXOffset(vehicle, frontCount);

            float rot_r_def = defaultRearRotation is float
                ? (float)defaultRearRotation
                : DecorExistOn(vehicle, DefaultRearCamberID)
                ? DecorGetFloat(vehicle, DefaultRearCamberID)
                : GetVehicleWheelYRotation(vehicle, frontCount);

            if (vehicle == _playerVehicleHandle)
            {
                CurrentPreset = new VStancerPreset(wheelsCount, off_f_def, rot_f_def, off_r_def, rot_r_def)
                {
                    FrontTrackWidth = frontOffset,
                    FrontCamber = frontRotation,
                    RearTrackWidth = rearOffset,
                    RearCamber = rearRotation
                };

                CurrentPreset.PropertyChanged += OnPresetEdited;
                NewPresetCreated?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, off_f_def, frontOffset);
                UpdateFloatDecorator(vehicle, DefaultFrontCamberID, rot_f_def, frontRotation);
                UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, off_r_def, rearOffset);
                UpdateFloatDecorator(vehicle, DefaultRearCamberID, rot_r_def, rearRotation);

                UpdateFloatDecorator(vehicle, FrontTrackWidthID, frontOffset, off_f_def);
                UpdateFloatDecorator(vehicle, FrontCamberID, frontRotation, rot_f_def);
                UpdateFloatDecorator(vehicle, RearTrackWidthID, rearOffset, off_r_def);
                UpdateFloatDecorator(vehicle, RearCamberID, rearRotation, rot_r_def);
            }
        }

        /// <summary>
        /// It checks if the <paramref name="vehicle"/> has a decorator named <paramref name="name"/> and updates its value with <paramref name="currentValue"/>, otherwise if <paramref name="currentValue"/> isn't equal to <paramref name="defaultValue"/> it adds the decorator <paramref name="name"/>
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="name"></param>
        /// <param name="currentValue"></param>
        /// <param name="defaultValue"></param>
        private void UpdateFloatDecorator(int vehicle, string name, float currentValue, float defaultValue)
        {
            // Decorator exists but needs to be updated
            if (DecorExistOn(vehicle, name))
            {
                float decorValue = DecorGetFloat(vehicle, name);
                if (!MathUtil.WithinEpsilon(currentValue, decorValue, Epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (Config.Debug)
                        Debug.WriteLine($"{Globals.ScriptName}: Updated decorator {name} from {decorValue} to {currentValue} on vehicle {vehicle}");
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (!MathUtil.WithinEpsilon(currentValue, defaultValue, Epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (Config.Debug)
                        Debug.WriteLine($"{Globals.ScriptName}: Added decorator {name} with value {currentValue} to vehicle {vehicle}");
                }
            }
        }

        /// <summary>
        /// Updates the decorators on the <paramref name="vehicle"/> with updated values from the <paramref name="preset"/>
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        /// <param name="preset">The preset for this vehicle</param>
        private void UpdateVehicleDecorators(int vehicle, VStancerPreset preset)
        {
            UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, preset.DefaultFrontTrackWidth, preset.FrontTrackWidth);
            UpdateFloatDecorator(vehicle, DefaultFrontCamberID, preset.DefaultFrontCamber, preset.FrontCamber);
            UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, preset.DefaultRearTrackWidth, preset.RearTrackWidth);
            UpdateFloatDecorator(vehicle, DefaultRearCamberID, preset.DefaultRearCamber, preset.RearCamber);

            UpdateFloatDecorator(vehicle, FrontTrackWidthID, preset.FrontTrackWidth, preset.DefaultFrontTrackWidth);
            UpdateFloatDecorator(vehicle, FrontCamberID, preset.FrontCamber, preset.DefaultFrontCamber);
            UpdateFloatDecorator(vehicle, RearTrackWidthID, preset.RearTrackWidth, preset.DefaultRearTrackWidth);
            UpdateFloatDecorator(vehicle, RearCamberID, preset.RearCamber, preset.DefaultRearCamber);
        }

        /// <summary>
        /// Creates a preset for the <paramref name="vehicle"/> to edit it locally
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        /// <returns></returns>
        private VStancerPreset CreatePresetFromHandle(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
                return null;

            if (Config.Debug && IsVehicleDamaged(vehicle))
                Screen.ShowNotification($"~o~Warning~w~: You are creating a vstancer preset for a damaged vehicle, default position and rotation of the wheels might be wrong");

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            // Get default values first
            float off_f_def = DecorExistOn(vehicle, DefaultFrontTrackWidthID) ? DecorGetFloat(vehicle, DefaultFrontTrackWidthID) : GetVehicleWheelXOffset(vehicle, 0);
            float rot_f_def = DecorExistOn(vehicle, DefaultFrontCamberID) ? DecorGetFloat(vehicle, DefaultFrontCamberID) : GetVehicleWheelYRotation(vehicle, 0);
            float off_r_def = DecorExistOn(vehicle, DefaultRearTrackWidthID) ? DecorGetFloat(vehicle, DefaultRearTrackWidthID) : GetVehicleWheelXOffset(vehicle, frontCount);
            float rot_r_def = DecorExistOn(vehicle, DefaultRearCamberID) ? DecorGetFloat(vehicle, DefaultRearCamberID) : GetVehicleWheelYRotation(vehicle, frontCount);

            float off_f = DecorExistOn(vehicle, FrontTrackWidthID) ? DecorGetFloat(vehicle, FrontTrackWidthID) : off_f_def;
            float rot_f = DecorExistOn(vehicle, FrontCamberID) ? DecorGetFloat(vehicle, FrontCamberID) : rot_f_def;
            float off_r = DecorExistOn(vehicle, RearTrackWidthID) ? DecorGetFloat(vehicle, RearTrackWidthID) : off_r_def;
            float rot_r = DecorExistOn(vehicle, RearCamberID) ? DecorGetFloat(vehicle, RearCamberID) : rot_r_def;

            return new VStancerPreset(wheelsCount, off_f_def, rot_f_def, off_r_def, rot_r_def)
            {
                FrontTrackWidth = off_f,
                FrontCamber = rot_f,
                RearTrackWidth = off_r,
                RearCamber = rot_r,
            };
        }

        /// <summary>
        /// Refreshes the <paramref name="vehicle"/> with values from the <paramref name="preset"/>
        /// </summary>
        private void UpdateVehicleUsingPreset(int vehicle, VStancerPreset preset)
        {
            if (!DoesEntityExist(vehicle) || preset == null)
                return;

            int wheelsCount = preset.WheelsCount;
            for (int index = 0; index < wheelsCount; index++)
            {
                // TODO: Avoid exposing preset nodes
                SetVehicleWheelXOffset(vehicle, index, preset.Nodes[index].PositionX);
                SetVehicleWheelYRotation(vehicle, index, preset.Nodes[index].RotationY);
            }
        }

        /// <summary>
        /// Refreshes the <paramref name="vehicle"/> with values from its decorators (if exist)
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        private void UpdateVehicleUsingDecorators(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            if (DecorExistOn(vehicle, FrontTrackWidthID))
            {
                float value = DecorGetFloat(vehicle, FrontTrackWidthID);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, FrontCamberID))
            {
                float value = DecorGetFloat(vehicle, FrontCamberID);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelYRotation(vehicle, index, value);
                    else
                        SetVehicleWheelYRotation(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, RearTrackWidthID))
            {
                float value = DecorGetFloat(vehicle, RearTrackWidthID);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, RearCamberID))
            {
                float value = DecorGetFloat(vehicle, RearCamberID);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelYRotation(vehicle, index, value);
                    else
                        SetVehicleWheelYRotation(vehicle, index, -value);
                }
            }

            if(Config.Extra.EnableExtra)
            {
                if (DecorExistOn(vehicle, ExtraSizeID))
                    SetVehicleWheelSize(vehicle, DecorGetFloat(vehicle, ExtraSizeID));

                if (DecorExistOn(vehicle, ExtraWidthID))
                    SetVehicleWheelWidth(vehicle, DecorGetFloat(vehicle, ExtraWidthID));
            }
        }

        /// <summary>
        /// Prints the values of the decorators used on the <paramref name="vehicle"/>
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        private void PrintDecoratorsInfo(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
            {
                Debug.WriteLine($"{Globals.ScriptName}: Can't find vehicle with handle {vehicle}");
                return;
            }

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int netID = NetworkGetNetworkIdFromEntity(vehicle);
            StringBuilder s = new StringBuilder();
            s.AppendLine($"{Globals.ScriptName}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

            if (DecorExistOn(vehicle, FrontTrackWidthID))
            {
                float value = DecorGetFloat(vehicle, FrontTrackWidthID);
                s.AppendLine($"{FrontTrackWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, FrontCamberID))
            {
                float value = DecorGetFloat(vehicle, FrontCamberID);
                s.AppendLine($"{FrontCamberID}: {value}");
            }

            if (DecorExistOn(vehicle, RearTrackWidthID))
            {
                float value = DecorGetFloat(vehicle, RearTrackWidthID);
                s.AppendLine($"{RearTrackWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, RearCamberID))
            {
                float value = DecorGetFloat(vehicle, RearCamberID);
                s.AppendLine($"{RearCamberID}: {value}");
            }

            Debug.WriteLine(s.ToString());
        }

        /// <summary>
        /// Prints the list of vehicles using any vstancer decorator.
        /// </summary>
        /// <param name="vehiclesList">The list of the vehicles' handles</param>
        private void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => EntityHasDecorators(entity));

            Debug.WriteLine($"{Globals.ScriptName}: Vehicles with decorators: {entities.Count()}");

            foreach (int item in entities)
                Debug.WriteLine($"Vehicle: {item}");
        }

        /// <summary>
        /// Returns true if the <paramref name="entity"/> has any vstancer decorator
        /// </summary>
        /// <param name="entity">The handle of the entity</param>
        /// <returns></returns>
        private bool EntityHasDecorators(int entity)
        {
            return (
                DecorExistOn(entity, FrontTrackWidthID) ||
                DecorExistOn(entity, FrontCamberID) ||
                DecorExistOn(entity, RearTrackWidthID) ||
                DecorExistOn(entity, RearCamberID) ||
                DecorExistOn(entity, DefaultFrontTrackWidthID) ||
                DecorExistOn(entity, DefaultFrontCamberID) ||
                DecorExistOn(entity, DefaultRearTrackWidthID) ||
                DecorExistOn(entity, DefaultRearCamberID)
                );
        }

        /// <summary>
        /// Loads the config file containing all the customizable properties
        /// </summary>
        /// <param name="filename">The name of the file</param>
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

        private async void OnPersonalPresetsMenuApplyPresetInvoked(object sender, string presetKey)
        {
            var loadedPreset = LocalPresetsManager.Load(presetKey);
            
            if (loadedPreset != null)
            {
                // TODO: Check if loadedPreset is within config limits

                // Assign new preset
                CurrentPreset.CopyFrom(loadedPreset);
                
                // Force refresh 
                UpdateVehicleUsingPreset(_playerVehicleHandle, CurrentPreset);
                
                Screen.ShowNotification($"Personal preset ~b~{presetKey}~w~ applied");
                
                await Delay(200);
                NewPresetCreated?.Invoke(this, EventArgs.Empty);
            }
            else
                Screen.ShowNotification($"~r~ERROR~w~ Personal preset ~b~{presetKey}~w~ corrupted");

            await Task.FromResult(0);
        }

        private void OnPersonalPresetsMenuSavePresetInvoked(object sender, string presetName)
        {
            if (LocalPresetsManager.Save(presetName, CurrentPreset))
            {
                Screen.ShowNotification($"Personal preset ~g~{presetName}~w~ saved");
            }
            else
                Screen.ShowNotification($"~r~ERROR~w~ The name {presetName} is invalid or already used.");
        }

        private void OnPersonalPresetsMenuDeletePresetInvoked(object sender, string presetKey)
        {
            if (LocalPresetsManager.Delete(presetKey))
            {
                Screen.ShowNotification($"Personal preset ~r~{presetKey}~w~ deleted");
            }
            else
                Screen.ShowNotification($"~r~ERROR~w~ No preset found with {presetKey} key.");
        }

        /// <summary>
        /// Invoked when the reset button is pressed in the UI
        /// </summary>
        private async void OnEditorMenuResetPresetInvoked(object sender, EventArgs eventArgs)
        {
            if (!CurrentPresetIsValid)
                return;

            CurrentPreset.Reset();

            await Delay(200);

            // Used to updated the UI
            // TODO: Maybe change this
            NewPresetCreated?.Invoke(this, EventArgs.Empty);
        }

        private async void OnExtraMenuResetPresetInvoked(object sender, EventArgs eventArgs)
        {
            if (!CurrentExtraIsValid)
                return;

            CurrentExtra.Reset();

            await Delay(200);

            ExtraChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Invoked when a value is changed in the UI
        /// </summary>
        /// <param name="id">The id of the property</param>
        /// <param name="value">The value of the property</param>
        private void OnEditorMenuPresetValueChanged(string id, string newValue)
        {
            if (!CurrentPresetIsValid)
                return;

            if (!float.TryParse(newValue, out float value))
                return;

            switch (id)
            {
                case FrontCamberID:
                    CurrentPreset.FrontCamber = value;
                    break;
                case RearCamberID:
                    CurrentPreset.RearCamber = value;
                    break;
                case FrontTrackWidthID:
                    CurrentPreset.FrontTrackWidth = -value;
                    break;
                case RearTrackWidthID:
                    CurrentPreset.RearTrackWidth = -value;
                    break;

                default:
                    break;
            }
        }

        private void OnExtraMenuPresetValueChanged(string id, string newValue)
        {
            if (!CurrentExtraIsValid)
                return;

            if (!float.TryParse(newValue, out float value))
                return;

            switch (id)
            {
                // Wheel size IDs
                case ExtraWidthID:
                    CurrentExtra.WheelWidth = value;
                    break;
                case ExtraSizeID:
                    CurrentExtra.WheelSize = value;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Get a string from the user using the on screen keyboard
        /// </summary>
        /// <param name="defaultText">The default value to display</param>
        /// <returns></returns>
        public async Task<string> GetOnScreenString(string title, string defaultText)
        {
            //var currentMenu = MenuController.GetCurrentMenu();
            //currentMenu.Visible = false;
            //MenuController.DisableMenuButtons = true;

            //DisableAllControlActions(1);

            DisplayOnscreenKeyboard(1, title, "", defaultText, "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await Delay(100);

            //EnableAllControlActions(1);

            //MenuController.DisableMenuButtons = false;
            //currentMenu.Visible = true;

            return GetOnscreenKeyboardResult();
        }
    }
}
