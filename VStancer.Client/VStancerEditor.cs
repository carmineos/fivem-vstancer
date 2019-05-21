using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace Vstancer.Client
{
    public class VStancerEditor : BaseScript
    {
        #region Fields

        /// <summary>
        /// The script which renders the menu
        /// </summary>
        private VStancerMenu vstancerMenu;

        /// <summary>
        /// The expected resource name
        /// </summary>
        public const string ResourceName = "vstancer";

        /// <summary>
        /// The name of the script
        /// </summary>
        public const string ScriptName = "VStancer";

        /// <summary>
        /// The current vstancer preset
        /// </summary>
        public VStancerPreset currentPreset;

        /// <summary>
        /// The handle of the current vehicle
        /// </summary>
        private int currentVehicle;

        /// <summary>
        /// Indicates the last game time the timed tasks have been executed
        /// </summary>
        private long lastTime;

        /// <summary>
        /// The handle of the current player ped
        /// </summary>
        private int playerPed;

        /// <summary>
        /// The list of all the vehicles' handles around the client's position 
        /// </summary>
        private IEnumerable<int> vehicles;

        #endregion

        #region Config Fields

        public int toggleMenu = 167;
        public float ScriptRange = 150.0f;
        public float FloatStep = 0.01f;
        public float frontMaxOffset = 0.25f;
        public float frontMaxCamber = 0.20f;
        public float rearMaxOffset = 0.25f;
        public float rearMaxCamber = 0.20f;

        private float FloatPrecision = 0.001f;
        private long timer = 1000;
        private bool debug = false;
        private bool exposeCommand = false;
        private bool exposeEvent = false;

        #endregion

        #region Decorator Names

        public const string FrontOffsetID = "vstancer_off_f";
        public const string FrontRotationID = "vstancer_rot_f";
        public const string RearOffsetID = "vstancer_off_r";
        public const string RearRotationID = "vstancer_rot_r";

        public const string DefaultFrontOffsetID = "vstancer_off_f_def";
        public const string DefaultFrontRotationID = "vstancer_rot_f_def";
        public const string DefaultRearOffsetID = "vstancer_off_r_def";
        public const string DefaultRearRotationID = "vstancer_rot_r_def";

        public const string ResetID = "vstancer_reset";

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns wheter <see cref="currentVehicle"/> and <see cref="currentPreset"/> are valid
        /// </summary>
        public bool CurrentPresetIsValid => currentVehicle != -1 && currentPreset != null;

        #endregion

        #region Public Events

        /// <summary>
        /// Triggered when <see cref="currentPreset"/> is changed
        /// </summary>
        public event EventHandler PresetChanged;

        /// <summary>
        /// Triggered when the client wants to manually toggle the menu visibility
        /// using the optional command/event
        /// </summary>
        public event EventHandler ToggleMenuVisibility;

        #endregion

        #region GUI Event Handlers

        /// <summary>
        /// Invoked when the reset button is pressed in the UI
        /// </summary>
        private async void OnMenuResetPresetButtonPressed()
        {
            if (!CurrentPresetIsValid)
                return;

            currentPreset.Reset();
            RemoveDecorators(currentVehicle);

            // Force one single refresh to update rendering at correct position after reset
            // This is required because otherwise the vehicle won't update immediately
            RefreshVehicleUsingPreset(currentVehicle, currentPreset);

            await Delay(200);
            PresetChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Invoked when a value is changed in the UI
        /// </summary>
        /// <param name="id">The id of the property</param>
        /// <param name="value">The value of the property</param>
        private void OnMenuPresetValueChanged(string id, string newValue)
        {
            if (!CurrentPresetIsValid)
                return;

            if(!float.TryParse(newValue, out float value))
                return;

            float defaultValue = value;

            if (id == FrontRotationID)
            {
                currentPreset.SetRotationFront(value);
                defaultValue = currentPreset.DefaultRotationY[0];
            }
            else if (id == RearRotationID)
            {
                currentPreset.SetRotationRear(value);
                defaultValue = currentPreset.DefaultRotationY[currentPreset.FrontWheelsCount];
            }
            else if (id == FrontOffsetID)
            {
                currentPreset.SetOffsetFront(-value);
                defaultValue = currentPreset.DefaultOffsetX[0];
            }
            else if (id == RearOffsetID)
            {
                currentPreset.SetOffsetRear(-value);
                defaultValue = currentPreset.DefaultOffsetX[currentPreset.FrontWheelsCount];
            }

            // Force one single refresh to update rendering at correct position after reset
            if (value == defaultValue)
                RefreshVehicleUsingPreset(currentVehicle, currentPreset);
        }

        #endregion

        #region Constructor

        public VStancerEditor()
        {
            // If the resource name is not the expected one ...
            if (GetCurrentResourceName() != ResourceName)
            {
                CitizenFX.Core.Debug.WriteLine($"{ScriptName}: Invalid resource name, be sure the resource name is {ResourceName}");
                return;
            }

            lastTime = GetGameTimer();
            currentVehicle = -1;
            currentPreset = null;
            vehicles = Enumerable.Empty<int>();

            RegisterDecorators();
            LoadConfig();

            #region Register Commands

            RegisterCommand("vstancer_range", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"{ScriptName}: Missing float argument");
                    return;
                }

                if (float.TryParse(args[0], out float value))
                {
                    ScriptRange = value;
                    Debug.WriteLine($"{ScriptName}: Received new {nameof(ScriptRange)} value {value}");
                }
                else Debug.WriteLine($"{ScriptName}: Error parsing {args[0]} as float");

            }), false);

            RegisterCommand("vstancer_debug", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"{ScriptName}: Missing bool argument");
                    return;
                }

                if (bool.TryParse(args[0], out bool value))
                {
                    debug = value;
                    Debug.WriteLine($"{ScriptName}: Received new {nameof(debug)} value {value}");
                }
                else Debug.WriteLine($"{ScriptName}: Error parsing {args[0]} as bool");

            }), false);

            RegisterCommand("vstancer_decorators", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                    PrintDecoratorsInfo(currentVehicle);
                else
                {
                    if (int.TryParse(args[0], out int value))
                        PrintDecoratorsInfo(value);
                    else Debug.WriteLine($"{ScriptName}: Error parsing entity handle {args[0]} as int");
                }
            }), false);

            RegisterCommand("vstancer_preset", new Action<int, dynamic>((source, args) =>
            {
                if (currentPreset != null)
                    Debug.WriteLine(currentPreset.ToString());
                else
                    Debug.WriteLine($"{ScriptName}: Current preset doesn't exist");
            }), false);

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(vehicles);
            }), false);

            #endregion

            
            if (exposeCommand)
            {
                RegisterCommand("vstancer", new Action<int, dynamic>((source, args) =>
                {
                    ToggleMenuVisibility?.Invoke(this, EventArgs.Empty);
                }), false);
            }

            if (exposeEvent)
            {
                EventHandlers.Add("vstancer:toggleMenu", new Action(() =>
                {
                    ToggleMenuVisibility?.Invoke(this, EventArgs.Empty);
                }));
            }


            Exports.Add("SetVstancerPreset", new Action<int, float, float, float, float, object, object, object, object>(SetVstancerPreset));
            Exports.Add("GetVstancerPreset", new Func<int, float[]>(GetVstancerPreset));

            // Create a script for the menu ...
            vstancerMenu = new VStancerMenu(this);

            if (vstancerMenu != null)
                // Actually only required to have its Tick event triggered
                // TODO: Workaround this and avoid to register the script
                RegisterScript(vstancerMenu);

            vstancerMenu.MenuResetPresetButtonPressed += (sender,args) => OnMenuResetPresetButtonPressed();
            vstancerMenu.MenuPresetValueChanged += OnMenuPresetValueChanged;

            Tick += GetCurrentVehicle;
            Tick += UpdateCurrentVehicle;
            Tick += UpdateWorldVehicles;
            Tick += UpdateCurrentVehicleDecorators;
        }

        #endregion

        #region Tasks

        /// <summary>
        /// Updates the <see cref="currentVehicle"/> and the <see cref="currentPreset"/>
        /// </summary>
        /// <returns></returns>
        private async Task GetCurrentVehicle()
        {
            playerPed = PlayerPedId();

            if (IsPedInAnyVehicle(playerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(playerPed, false);

                if (IsThisModelACar((uint)GetEntityModel(vehicle)) && GetPedInVehicleSeat(vehicle, -1) == playerPed && IsVehicleDriveable(vehicle, false))
                {
                    // Update current vehicle and get its preset
                    if (vehicle != currentVehicle)
                    {
                        currentPreset = CreatePreset(vehicle);
                        currentVehicle = vehicle;
                        PresetChanged?.Invoke(this, EventArgs.Empty);
                        Tick += UpdateCurrentVehicle;
                    }
                }
                else
                {
                    if(CurrentPresetIsValid)
                    {
                        // If current vehicle isn't a car or player isn't driving current vehicle or vehicle is dead
                        currentPreset = null;
                        currentVehicle = -1;
                        Tick -= UpdateCurrentVehicle;
                    }
                }
            }
            else
            {
                if (CurrentPresetIsValid)
                {
                    // If player isn't in any vehicle
                    currentPreset = null;
                    currentVehicle = -1;
                    Tick -= UpdateCurrentVehicle;
                }
            }

            await Task.FromResult(0);
        }

        /// <summary>
        /// The task that updates the current vehicle
        /// </summary>
        /// <returns></returns>
        private async Task UpdateCurrentVehicle()
        {
            // Check if current vehicle needs to be refreshed
            if (CurrentPresetIsValid && currentPreset.IsEdited)
                    RefreshVehicleUsingPreset(currentVehicle, currentPreset);

            await Task.FromResult(0);
        }

        /// <summary>
        /// The task that updates the vehicles of the world
        /// </summary>
        /// <returns></returns>
        private async Task UpdateWorldVehicles()
        {
            // Refreshes the iterated vehicles
            var vehiclesList = vehicles.Except(new List<int> { currentVehicle });
            Vector3 currentCoords = GetEntityCoords(playerPed, true);

            foreach (int entity in vehiclesList)
            {
                if (DoesEntityExist(entity))
                {
                    Vector3 coords = GetEntityCoords(entity, true);

                    if (Vector3.Distance(currentCoords, coords) <= ScriptRange)
                        RefreshVehicleUsingDecorators(entity);
                }
            }

            await Task.FromResult(0);
        }

        /// <summary>
        /// The task that updates the script decorators attached on the current vehicle
        /// </summary>
        /// <returns></returns>
        private async Task UpdateCurrentVehicleDecorators()
        {
            var currentTime = (GetGameTimer() - lastTime);

            // Check if decorators needs to be updated
            if (currentTime > timer)
            {
                if (CurrentPresetIsValid)
                    UpdateVehicleDecorators(currentVehicle, currentPreset);

                // Also update world vehicles list
                vehicles = new VehicleEnumerable();

                lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Disable controls for controller to use the script with the controller
        /// </summary>
        private void DisableControls()
        {
            DisableControlAction(1, 85, true); // INPUT_VEH_RADIO_WHEEL = DPAD - LEFT
            DisableControlAction(1, 74, true); // INPUT_VEH_HEADLIGHT = DPAD - RIGHT
            DisableControlAction(1, 48, true); // INPUT_HUD_SPECIAL = DPAD - DOWN
            DisableControlAction(1, 27, true); // INPUT_PHONE = DPAD - UP
            DisableControlAction(1, 80, true); // INPUT_VEH_CIN_CAM = B
            DisableControlAction(1, 73, true); // INPUT_VEH_DUCK = A
        }

        /// <summary>
        /// Registers the decorators for this script
        /// </summary>
        private void RegisterDecorators()
        {
            DecorRegister(FrontOffsetID, 1);
            DecorRegister(FrontRotationID, 1);
            DecorRegister(DefaultFrontOffsetID, 1);
            DecorRegister(DefaultFrontRotationID, 1);

            DecorRegister(RearOffsetID, 1);
            DecorRegister(RearRotationID, 1);
            DecorRegister(DefaultRearOffsetID, 1);
            DecorRegister(DefaultRearRotationID, 1);
        }

        /// <summary>
        /// Removes the decorators from the <paramref name="vehicle"/>
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        private void RemoveDecorators(int vehicle)
        {
            if (DecorExistOn(vehicle, FrontOffsetID))
                DecorRemove(vehicle, FrontOffsetID);

            if (DecorExistOn(vehicle, FrontRotationID))
                DecorRemove(vehicle, FrontRotationID);

            if (DecorExistOn(vehicle, DefaultFrontOffsetID))
                DecorRemove(vehicle, DefaultFrontOffsetID);

            if (DecorExistOn(vehicle, DefaultFrontRotationID))
                DecorRemove(vehicle, DefaultFrontRotationID);

            if (DecorExistOn(vehicle, RearOffsetID))
                DecorRemove(vehicle, RearOffsetID);

            if (DecorExistOn(vehicle, RearRotationID))
                DecorRemove(vehicle, RearRotationID);

            if (DecorExistOn(vehicle, DefaultRearOffsetID))
                DecorRemove(vehicle, DefaultRearOffsetID);

            if (DecorExistOn(vehicle, DefaultRearRotationID))
                DecorRemove(vehicle, DefaultRearRotationID);
        }

        /// <summary>
        /// Returns the preset as an array of floats containing in order: 
        /// frontOffset, frontRotation, rearOffset, rearRotation, defaultFrontOffset, defaultFrontRotation, defaultRearOffset, defaultRearRotation
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        /// <returns>The float array</returns>
        public float[] GetVstancerPreset(int vehicle)
        {
            VStancerPreset preset = (vehicle == currentVehicle && CurrentPresetIsValid) ? currentPreset : CreatePreset(vehicle);
            return preset.ToArray();
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
            if (debug)
                Debug.WriteLine($"{ScriptName}: SetVstancerPreset parameters {frontOffset} {frontRotation} {rearOffset} {rearRotation} {defaultFrontOffset} {defaultFrontRotation} {defaultRearOffset} {defaultRearRotation}");

            if (!DoesEntityExist(vehicle))
                return;

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerPreset.CalculateFrontWheelsCount(wheelsCount);

            float off_f_def, rot_f_def, off_r_def, rot_r_def;

            if (defaultFrontOffset != null && defaultFrontOffset is float)
                off_f_def = (float)defaultFrontOffset;
            else
                off_f_def = DecorExistOn(vehicle, DefaultFrontOffsetID) ? DecorGetFloat(vehicle, DefaultFrontOffsetID) : GetVehicleWheelXOffset(vehicle, 0);

            if (defaultFrontRotation != null && defaultFrontRotation is float)
                rot_f_def = (float)defaultFrontRotation;
            else
                rot_f_def = DecorExistOn(vehicle, DefaultFrontRotationID) ? DecorGetFloat(vehicle, DefaultFrontRotationID) : GetVehicleWheelYRotation(vehicle, 0);

            if (defaultRearOffset != null && defaultRearOffset is float)
                off_r_def = (float)defaultRearOffset;
            else
                off_r_def = DecorExistOn(vehicle, DefaultRearOffsetID) ? DecorGetFloat(vehicle, DefaultRearOffsetID) : GetVehicleWheelXOffset(vehicle, frontCount);

            if (defaultRearRotation != null && defaultRearRotation is float)
                rot_r_def = (float)defaultRearRotation;
            else
                rot_r_def = DecorExistOn(vehicle, DefaultRearRotationID) ? DecorGetFloat(vehicle, DefaultRearRotationID) : GetVehicleWheelYRotation(vehicle, frontCount);

            if (vehicle == currentVehicle)
            {
                currentPreset = new VStancerPreset(wheelsCount, frontOffset, frontRotation, rearOffset, rearRotation, off_f_def, rot_f_def, off_r_def, rot_r_def);
                PresetChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                UpdateFloatDecorator(vehicle, DefaultFrontOffsetID, off_f_def, frontOffset);
                UpdateFloatDecorator(vehicle, DefaultFrontRotationID, rot_f_def, frontRotation);
                UpdateFloatDecorator(vehicle, DefaultRearOffsetID, off_r_def, rearOffset);
                UpdateFloatDecorator(vehicle, DefaultRearRotationID, rot_r_def, rearRotation);

                UpdateFloatDecorator(vehicle, FrontOffsetID, frontOffset, off_f_def);
                UpdateFloatDecorator(vehicle, FrontRotationID, frontRotation, rot_f_def);
                UpdateFloatDecorator(vehicle, RearOffsetID, rearOffset, off_r_def);
                UpdateFloatDecorator(vehicle, RearRotationID, rearRotation, rot_r_def);
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
                if (Math.Abs(currentValue - decorValue) > FloatPrecision)
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (debug)
                        Debug.WriteLine($"{ScriptName}: Updated decorator {name} from {decorValue} to {currentValue} on vehicle {vehicle}");
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (Math.Abs(currentValue - defaultValue) > FloatPrecision)
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (debug)
                        Debug.WriteLine($"{ScriptName}: Added decorator {name} with value {currentValue} to vehicle {vehicle}");
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
            float[] DefaultOffsetX = preset.DefaultOffsetX;
            float[] DefaultRotationY = preset.DefaultRotationY;
            float[] OffsetX = preset.OffsetX;
            float[] RotationY = preset.RotationY;
            int frontCount = preset.FrontWheelsCount;

            UpdateFloatDecorator(vehicle, DefaultFrontOffsetID, DefaultOffsetX[0], OffsetX[0]);
            UpdateFloatDecorator(vehicle, DefaultFrontRotationID, DefaultRotationY[0], RotationY[0]);
            UpdateFloatDecorator(vehicle, DefaultRearOffsetID, DefaultOffsetX[frontCount], OffsetX[frontCount]);
            UpdateFloatDecorator(vehicle, DefaultRearRotationID, DefaultRotationY[frontCount], RotationY[frontCount]);

            UpdateFloatDecorator(vehicle, FrontOffsetID, OffsetX[0], DefaultOffsetX[0]);
            UpdateFloatDecorator(vehicle, FrontRotationID, RotationY[0], DefaultRotationY[0]);
            UpdateFloatDecorator(vehicle, RearOffsetID, OffsetX[frontCount], DefaultOffsetX[frontCount]);
            UpdateFloatDecorator(vehicle, RearRotationID, RotationY[frontCount], DefaultRotationY[frontCount]);
        }

        /// <summary>
        /// Creates a preset for the <paramref name="vehicle"/> to edit it locally
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        /// <returns></returns>
        private VStancerPreset CreatePreset(int vehicle)
        {
            if (IsVehicleDamaged(vehicle))
                Screen.ShowNotification($"~o~Warning~w~: You are creating a vstancer preset for a damaged vehicle, default position and rotation of the wheels might be wrong");

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerPreset.CalculateFrontWheelsCount(wheelsCount);

            // Get default values first
            float off_f_def = DecorExistOn(vehicle, DefaultFrontOffsetID) ? DecorGetFloat(vehicle, DefaultFrontOffsetID) : GetVehicleWheelXOffset(vehicle, 0);
            float rot_f_def = DecorExistOn(vehicle, DefaultFrontRotationID) ? DecorGetFloat(vehicle, DefaultFrontRotationID) : GetVehicleWheelYRotation(vehicle, 0);
            float off_r_def = DecorExistOn(vehicle, DefaultRearOffsetID) ? DecorGetFloat(vehicle, DefaultRearOffsetID) : GetVehicleWheelXOffset(vehicle, frontCount);
            float rot_r_def = DecorExistOn(vehicle, DefaultRearRotationID) ? DecorGetFloat(vehicle, DefaultRearRotationID) : GetVehicleWheelYRotation(vehicle, frontCount);

            float off_f = DecorExistOn(vehicle, FrontOffsetID) ? DecorGetFloat(vehicle, FrontOffsetID) : off_f_def;
            float rot_f = DecorExistOn(vehicle, FrontRotationID) ? DecorGetFloat(vehicle, FrontRotationID) : rot_f_def;
            float off_r = DecorExistOn(vehicle, RearOffsetID) ? DecorGetFloat(vehicle, RearOffsetID) : off_r_def;
            float rot_r = DecorExistOn(vehicle, RearRotationID) ? DecorGetFloat(vehicle, RearRotationID) : rot_r_def;

            return new VStancerPreset(wheelsCount, off_f, rot_f, off_r, rot_r, off_f_def, rot_f_def, off_r_def, rot_r_def);
        }

        /// <summary>
        /// Refreshes the <paramref name="vehicle"/> with values from the <paramref name="preset"/>
        /// </summary>
        private void RefreshVehicleUsingPreset(int vehicle, VStancerPreset preset)
        {
            if (!DoesEntityExist(vehicle) || preset == null)
                return;

            int wheelsCount = preset.WheelsCount;
            for (int index = 0; index < wheelsCount; index++)
            {
                SetVehicleWheelXOffset(vehicle, index, preset.OffsetX[index]);
                SetVehicleWheelYRotation(vehicle, index, preset.RotationY[index]);
            }
        }

        /// <summary>
        /// Refreshes the <paramref name="vehicle"/> with values from its decorators (if exist)
        /// </summary>
        /// <param name="vehicle">The handle of the entity</param>
        private void RefreshVehicleUsingDecorators(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerPreset.CalculateFrontWheelsCount(wheelsCount);

            if (DecorExistOn(vehicle, FrontOffsetID))
            {
                float value = DecorGetFloat(vehicle, FrontOffsetID);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, FrontRotationID))
            {
                float value = DecorGetFloat(vehicle, FrontRotationID);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelYRotation(vehicle, index, value);
                    else
                        SetVehicleWheelYRotation(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, RearOffsetID))
            {
                float value = DecorGetFloat(vehicle, RearOffsetID);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, RearRotationID))
            {
                float value = DecorGetFloat(vehicle, RearRotationID);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelYRotation(vehicle, index, value);
                    else
                        SetVehicleWheelYRotation(vehicle, index, -value);
                }
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
                Debug.WriteLine($"{ScriptName}: Can't find vehicle with handle {vehicle}");
                return;
            }

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int netID = NetworkGetNetworkIdFromEntity(vehicle);
            StringBuilder s = new StringBuilder();
            s.AppendLine($"{ScriptName}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

            if (DecorExistOn(vehicle, FrontOffsetID))
            {
                float value = DecorGetFloat(vehicle, FrontOffsetID);
                s.AppendLine($"{FrontOffsetID}: {value}");
            }

            if (DecorExistOn(vehicle, FrontRotationID))
            {
                float value = DecorGetFloat(vehicle, FrontRotationID);
                s.AppendLine($"{FrontRotationID}: {value}");
            }

            if (DecorExistOn(vehicle, RearOffsetID))
            {
                float value = DecorGetFloat(vehicle, RearOffsetID);
                s.AppendLine($"{RearOffsetID}: {value}");
            }

            if (DecorExistOn(vehicle, RearRotationID))
            {
                float value = DecorGetFloat(vehicle, RearRotationID);
                s.AppendLine($"{RearRotationID}: {value}");
            }

            Debug.WriteLine(s.ToString());
        }

        /// <summary>
        /// Prints the list of vehicles using any vstancer decorator.
        /// </summary>
        /// <param name="vehiclesList">The list of the vehicles' handles</param>
        private void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => HasDecorators(entity));

            Debug.WriteLine($"{ScriptName}: Vehicles with decorators: {entities.Count()}");

            foreach (var item in entities)
                Debug.WriteLine($"Vehicle: {item}");
        }

        /// <summary>
        /// Returns true if the <paramref name="entity"/> has any vstancer decorator
        /// </summary>
        /// <param name="entity">The handle of the entity</param>
        /// <returns></returns>
        private bool HasDecorators(int entity)
        {
            return (
                DecorExistOn(entity, FrontOffsetID) ||
                DecorExistOn(entity, FrontRotationID) ||
                DecorExistOn(entity, RearOffsetID) ||
                DecorExistOn(entity, RearRotationID) ||
                DecorExistOn(entity, DefaultFrontOffsetID) ||
                DecorExistOn(entity, DefaultFrontRotationID) ||
                DecorExistOn(entity, DefaultRearOffsetID) ||
                DecorExistOn(entity, DefaultRearRotationID)
                );
        }

        /// <summary>
        /// Loads the config file containing all the customizable properties
        /// </summary>
        /// <param name="filename">The name of the file</param>
        private void LoadConfig(string filename = "config.ini")
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile(ResourceName, filename);

                Debug.WriteLine($"{ScriptName}: Loaded settings from {filename}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{ScriptName}: Impossible to load {filename}");
                Debug.WriteLine(e.StackTrace);
            }
            finally
            {
                Config config = new Config(strings);

                toggleMenu = config.GetIntValue("toggleMenu", toggleMenu);
                FloatStep = config.GetFloatValue("FloatStep", FloatStep);
                ScriptRange = config.GetFloatValue("ScriptRange", ScriptRange);
                frontMaxOffset = config.GetFloatValue("frontMaxOffset", frontMaxOffset);
                frontMaxCamber = config.GetFloatValue("frontMaxCamber", frontMaxCamber);
                rearMaxOffset = config.GetFloatValue("rearMaxOffset", rearMaxOffset);
                rearMaxCamber = config.GetFloatValue("rearMaxCamber", rearMaxCamber);
                timer = config.GetLongValue("timer", timer);
                debug = config.GetBoolValue("debug", debug);
                exposeCommand = config.GetBoolValue("exposeCommand", exposeCommand);
                exposeEvent = config.GetBoolValue("exposeEvent", exposeEvent);

                Debug.WriteLine($"{ScriptName}: Settings {nameof(frontMaxOffset)}={frontMaxOffset} {nameof(frontMaxCamber)}={frontMaxCamber} {nameof(rearMaxOffset)}={rearMaxOffset} {nameof(rearMaxCamber)}={rearMaxCamber} {nameof(timer)}={timer} {nameof(debug)}={debug} {nameof(ScriptRange)}={ScriptRange}");
            }
        }

        #endregion
    }
}
