using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Text;
using NativeUI;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace Vstancer.Client
{
    public class Vstancer : BaseScript
    {
        private static string ResourceName;
        private static readonly string ScriptName = "VStancer";

        #region CONFIG_FIEDS
        private static float editingFactor = 0.01f;
        private static float maxSyncDistance = 150.0f;
        private static float FrontMaxOffset = 0.25f;
        private static float FrontMaxCamber = 0.20f;
        private static float RearMaxOffset = 0.25f;
        private static float RearMaxCamber = 0.20f;
        private static long timer = 1000;
        private static bool debug = false;
        private static int toggleMenu = 167;
        private static float screenPosX = 1.0f;
        private static float screenPosY = 0.0f;
        #endregion

        #region DECORATORS_NAMES
        private static readonly string decor_off_f = "vstancer_off_f";
        private static readonly string decor_rot_f = "vstancer_rot_f";
        private static readonly string decor_off_f_def = "vstancer_off_f_def";
        private static readonly string decor_rot_f_def = "vstancer_rot_f_def";

        private static readonly string decor_off_r = "vstancer_off_r";
        private static readonly string decor_rot_r = "vstancer_rot_r";
        private static readonly string decor_off_r_def = "vstancer_off_r_def";
        private static readonly string decor_rot_r_def = "vstancer_rot_r_def";
        #endregion

        #region FIELDS
        private long currentTime;
        private long lastTime;
        private int playerPed;
        private int currentVehicle;
        private VstancerPreset currentPreset;
        private IEnumerable<int> vehicles;
        #endregion

        #region GUI_FIELDS
        private MenuPool _menuPool;
        private UIMenu EditorMenu;
        private UIMenuDynamicListItem frontOffsetGUI;
        private UIMenuDynamicListItem rearOffsetGUI;
        private UIMenuDynamicListItem frontRotationGUI;
        private UIMenuDynamicListItem rearRotationGUI;
        #endregion

        #region GUI_METHODS
        private UIMenuItem AddMenuReset(UIMenu menu)
        {
            var newitem = new UIMenuItem("Reset", "Restores the default values");
            menu.AddItem(newitem);

            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    currentPreset.Reset();
                    RefreshVehicleUsingPreset(currentVehicle, currentPreset); // Force one single refresh to update rendering at correct position after reset
                    RemoveDecorators(currentVehicle);

                    InitialiseMenu();
                    EditorMenu.Visible = true;
                }
            };

            return newitem;
        }

        private UIMenuDynamicListItem AddDynamicFloatList(UIMenu menu, string name, float defaultValue, float value, float maxEditing)
        {
            var newitem = new UIMenuDynamicListItem(name, value.ToString("F3"), (sender, direction) =>
            {
                var newvalue = value;
                float min = defaultValue - maxEditing;
                float max = defaultValue + maxEditing;

                //newvalue = (float)Math.Round(newvalue, 3);
                //min = (float)Math.Round(min, 3);
                //max = (float)Math.Round(max, 3);

                if (direction == UIMenuDynamicListItem.ChangeDirection.Left)
                    newvalue -= editingFactor;
                else if (direction == UIMenuDynamicListItem.ChangeDirection.Right)
                    newvalue += editingFactor;
                else return value.ToString("F3");

                if (newvalue < min)
                    CitizenFX.Core.UI.Screen.ShowNotification($"~o~Warning~w~: Min ~b~{name}~w~ value allowed is {min} for this vehicle");
                else if (newvalue > max)
                    CitizenFX.Core.UI.Screen.ShowNotification($"~o~Warning~w~: Max ~b~{name}~w~ value allowed is {max} for this vehicle");
                else
                {
                    value = newvalue;
                    if (sender == frontRotationGUI) currentPreset.SetRotationFront(value);
                    else if (sender == rearRotationGUI) currentPreset.SetRotationRear(value);
                    else if (sender == frontOffsetGUI) currentPreset.SetOffsetFront(-value);
                    else if (sender == rearOffsetGUI) currentPreset.SetOffsetRear(-value);

                    // Force one single refresh to update rendering at correct position after reset
                    if (value == defaultValue)
                        RefreshVehicleUsingPreset(currentVehicle, currentPreset);

                    if (debug)
                        Debug.WriteLine($"{ScriptName}: Edited {sender.Text} => value:{value}");
                }
                return value.ToString("F3");
            });
            menu.AddItem(newitem);
            return newitem;
        }

        private async void InitialiseMenu()
        {
            if(EditorMenu == null)
            {
                EditorMenu = new UIMenu(ScriptName, "Edit Track Width and Camber", new PointF(screenPosX * Screen.Width, screenPosY * Screen.Height));
                {
                    EditorMenu.MouseEdgeEnabled = false;
                    EditorMenu.ControlDisablingEnabled = false;
                    EditorMenu.MouseControlsEnabled = false;
                }
            }else EditorMenu.Clear();

            frontOffsetGUI = AddDynamicFloatList(EditorMenu, "Front Track Width", -currentPreset.DefaultOffsetX[0], -currentPreset.OffsetX[0], FrontMaxOffset);
            rearOffsetGUI = AddDynamicFloatList(EditorMenu, "Rear Track Width", -currentPreset.DefaultOffsetX[currentPreset.FrontWheelsCount], -currentPreset.OffsetX[currentPreset.FrontWheelsCount], RearMaxOffset);
            frontRotationGUI = AddDynamicFloatList(EditorMenu, "Front Camber", currentPreset.DefaultRotationY[0], currentPreset.RotationY[0], FrontMaxCamber);
            rearRotationGUI = AddDynamicFloatList(EditorMenu, "Rear Camber", currentPreset.DefaultRotationY[currentPreset.FrontWheelsCount], currentPreset.RotationY[currentPreset.FrontWheelsCount], RearMaxCamber);
            AddMenuReset(EditorMenu);

            if(_menuPool == null)
            {
                _menuPool = new MenuPool();
                {
                    _menuPool.ResetCursorOnOpen = true;
                }

                _menuPool.Add(EditorMenu);
            }
            _menuPool.RefreshIndex();

            await Delay(0);
        }
        #endregion

        public Vstancer()
        {
            ResourceName = GetCurrentResourceName();
            Debug.WriteLine($"{ScriptName}: Script by Neos7");

            RegisterDecorators();
            LoadConfig();

            currentTime = GetGameTimer();
            lastTime = currentTime;
            currentVehicle = -1;
            currentPreset = null;
            vehicles = Enumerable.Empty<int>();

            RegisterCommand("vstancer_distance", new Action<int, dynamic>((source, args) =>
            {
                if (args.Count < 1)
                {
                    Debug.WriteLine($"{ScriptName}: Missing float argument");
                    return;
                }

                if (float.TryParse(args[0], out float value))
                {
                    maxSyncDistance = value;
                    Debug.WriteLine($"{ScriptName}: Received new {nameof(maxSyncDistance)} value {value}");
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
            
            // In the constructor of a class that inherits BaseScript.
            EventHandlers.Add("VSTANCE:MenuControl", new Action (EventMenuControl));

            Tick += HandleMenu;
            Tick += VstancerTask;
        }
        
        // Create a function to handle the event somewhere else in your code.
        private void EventMenuControl()
        {
            // Code that gets executed once the event is triggered goes here.
            EditorMenu.Visible = !EditorMenu.Visible;
        }

        /// <summary>
        /// The GUI task of the script
        /// </summary>
        /// <returns></returns>
        private async Task HandleMenu()
        {
            if(_menuPool != null)
            {
                _menuPool.ProcessMenus();

                if (_menuPool.IsAnyMenuOpen())
                    DisableControls();

                if (currentVehicle != -1 && currentPreset != null)
                {
                    if (IsControlJustPressed(1, toggleMenu) || IsDisabledControlJustPressed(1, toggleMenu))
                        EditorMenu.Visible = !EditorMenu.Visible;
                }
                else
                {
                    if (_menuPool.IsAnyMenuOpen())
                        _menuPool.CloseAllMenus();
                }
            }
            await Task.FromResult(0);
        }

        /// <summary>
        /// Disable controls for controller to use the script with the controller
        /// </summary>
        private async void DisableControls()
        {
            DisableControlAction(1, 85, true); // INPUT_VEH_RADIO_WHEEL = DPAD - LEFT
            DisableControlAction(1, 74, true); // INPUT_VEH_HEADLIGHT = DPAD - RIGHT
            DisableControlAction(1, 48, true); // INPUT_HUD_SPECIAL = DPAD - DOWN
            DisableControlAction(1, 27, true); // INPUT_PHONE = DPAD - UP
            DisableControlAction(1, 80, true); // INPUT_VEH_CIN_CAM = B
            DisableControlAction(1, 73, true); // INPUT_VEH_DUCK = A

            await Delay(0);
        }

        /// <summary>
        /// The main task of the script
        /// </summary>
        /// <returns></returns>
        private async Task VstancerTask()
        {
            currentTime = (GetGameTimer() - lastTime);

            playerPed = PlayerPedId();
            
            if (IsPedInAnyVehicle(playerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(playerPed, false);

                if (IsThisModelACar((uint)GetEntityModel(vehicle)) && GetPedInVehicleSeat(vehicle, -1) == playerPed && !IsEntityDead(vehicle))
                {
                    // Update current vehicle and get its preset
                    if (vehicle != currentVehicle)
                    {
                        currentPreset = CreatePreset(vehicle);
                        currentVehicle = vehicle;
                        InitialiseMenu();
                    }
                }
                else
                {
                    // If current vehicle isn't a car or player isn't driving current vehicle or vehicle is dead
                    currentPreset = null;
                    currentVehicle = -1;
                }
            }
            else
            {
                // If player isn't in any vehicle
                currentPreset = null;
                currentVehicle = -1;
            }

            // Check if current vehicle needs to be refreshed
            if (currentVehicle != -1 && currentPreset != null)
            {
                if (currentPreset.IsEdited)
                    RefreshVehicleUsingPreset(currentVehicle, currentPreset);
            }

            // Check if decorators needs to be updated
            if (currentTime > timer)
            {
                if (currentVehicle != -1 && currentPreset != null)
                    UpdateVehicleDecorators(currentVehicle, currentPreset);

                vehicles = new VehicleList();

                lastTime = GetGameTimer();
            }

            // Refreshes the iterated vehicles
            RefreshVehicles(vehicles.Except(new List<int> { currentVehicle }));

            await Delay(0);
        }

        /// <summary>
        /// Registers the decorators for this script
        /// </summary>
        private async void RegisterDecorators()
        {
            DecorRegister(decor_off_f, 1);
            DecorRegister(decor_rot_f, 1);
            DecorRegister(decor_off_f_def, 1);
            DecorRegister(decor_rot_f_def, 1);

            DecorRegister(decor_off_r, 1);
            DecorRegister(decor_rot_r, 1);
            DecorRegister(decor_off_r_def, 1);
            DecorRegister(decor_rot_r_def, 1);

            await Delay(0);
        }

        /// <summary>
        /// Removes the decorators from the <paramref name="vehicle"/>
        /// </summary>
        /// <param name="vehicle"></param>
        private async void RemoveDecorators(int vehicle)
        {
            if (DecorExistOn(vehicle, decor_off_f))
                DecorRemove(vehicle, decor_off_f);

            if (DecorExistOn(vehicle, decor_rot_f))
                DecorRemove(vehicle, decor_rot_f);

            if (DecorExistOn(vehicle, decor_off_f_def))
                DecorRemove(vehicle, decor_off_f_def);

            if (DecorExistOn(vehicle, decor_rot_f_def))
                DecorRemove(vehicle, decor_rot_f_def);

            if (DecorExistOn(vehicle, decor_off_r))
                DecorRemove(vehicle, decor_off_r);

            if (DecorExistOn(vehicle, decor_rot_r))
                DecorRemove(vehicle, decor_rot_r);

            if (DecorExistOn(vehicle, decor_off_r_def))
                DecorRemove(vehicle, decor_off_r_def);

            if (DecorExistOn(vehicle, decor_rot_r_def))
                DecorRemove(vehicle, decor_rot_r_def);

            await Delay(0);
        }

        /// <summary>
        /// It checks if the <paramref name="vehicle"/> has a decorator named <paramref name="name"/> and updates its value with <paramref name="currentValue"/>, otherwise if <paramref name="currentValue"/> isn't equal to <paramref name="defaultValue"/> it adds the decorator <paramref name="name"/>
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="name"></param>
        /// <param name="currentValue"></param>
        /// <param name="defaultValue"></param>
        private async void UpdateFloatDecorator(int vehicle, string name, float currentValue, float defaultValue)
        {
            // Decorator exists but needs to be updated
            if (DecorExistOn(vehicle, name))
            {
                float decorValue = DecorGetFloat(vehicle, name);
                if (Math.Abs(currentValue - decorValue) > 0.001f)
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (debug)
                        Debug.WriteLine($"{ScriptName}: Updated decorator {name} from {decorValue} to {currentValue} on vehicle {vehicle}");
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (Math.Abs(currentValue - defaultValue) > 0.001f)
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (debug)
                        Debug.WriteLine($"{ScriptName}: Added decorator {name} with value {currentValue} to vehicle {vehicle}");
                }
            }
            await Delay(0);
        }

        /// <summary>
        /// Updates the decorators on the <paramref name="vehicle"/> with updated values from the <paramref name="preset"/>
        /// </summary>
        /// <param name="vehicle"></param>
        private async void UpdateVehicleDecorators(int vehicle, VstancerPreset preset)
        {
            float[] DefaultOffsetX = preset.DefaultOffsetX;
            float[] DefaultRotationY = preset.DefaultRotationY;
            float[] OffsetX = preset.OffsetX;
            float[] RotationY = preset.RotationY;
            int frontCount = preset.FrontWheelsCount;

            UpdateFloatDecorator(vehicle, decor_off_f_def, DefaultOffsetX[0], OffsetX[0]);
            UpdateFloatDecorator(vehicle, decor_rot_f_def, DefaultRotationY[0], RotationY[0]);
            UpdateFloatDecorator(vehicle, decor_off_r_def, DefaultOffsetX[frontCount], OffsetX[frontCount]);
            UpdateFloatDecorator(vehicle, decor_rot_r_def, DefaultRotationY[frontCount], RotationY[frontCount]);

            UpdateFloatDecorator(vehicle, decor_off_f, OffsetX[0], DefaultOffsetX[0]);
            UpdateFloatDecorator(vehicle, decor_rot_f, RotationY[0], DefaultRotationY[0]);
            UpdateFloatDecorator(vehicle, decor_off_r, OffsetX[frontCount], DefaultOffsetX[frontCount]);
            UpdateFloatDecorator(vehicle, decor_rot_r, RotationY[frontCount], DefaultRotationY[frontCount]);

            await Delay(0);
        }

        /// <summary>
        /// Creates a preset for the <paramref name="vehicle"/> to edit it locally
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        private VstancerPreset CreatePreset(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = wheelsCount / 2;
            if (frontCount % 2 != 0)
                frontCount -= 1;

            // Get default values first
            float off_f_def = DecorExistOn(vehicle, decor_off_f_def) ? DecorGetFloat(vehicle, decor_off_f_def) : GetVehicleWheelXOffset(vehicle, 0);
            float rot_f_def = DecorExistOn(vehicle, decor_rot_f_def) ? DecorGetFloat(vehicle, decor_rot_f_def) : GetVehicleWheelXrot(vehicle, 0);
            float off_r_def = DecorExistOn(vehicle, decor_off_r_def) ? DecorGetFloat(vehicle, decor_off_r_def) : GetVehicleWheelXOffset(vehicle, frontCount);
            float rot_r_def = DecorExistOn(vehicle, decor_rot_r_def) ? DecorGetFloat(vehicle, decor_rot_r_def) : GetVehicleWheelXrot(vehicle, frontCount);

            float off_f = DecorExistOn(vehicle, decor_off_f) ? DecorGetFloat(vehicle, decor_off_f) : off_f_def;
            float rot_f = DecorExistOn(vehicle, decor_rot_f) ? DecorGetFloat(vehicle, decor_rot_f) : rot_f_def;
            float off_r = DecorExistOn(vehicle, decor_off_r) ? DecorGetFloat(vehicle, decor_off_r) : off_r_def;
            float rot_r = DecorExistOn(vehicle, decor_rot_r) ? DecorGetFloat(vehicle, decor_rot_r) : rot_r_def;

            VstancerPreset preset = new VstancerPreset(wheelsCount, rot_f, rot_r, off_f, off_r, rot_f_def, rot_r_def, off_f_def, off_r_def);

            return preset;
        }

        /// <summary>
        /// Refreshes the <paramref name="vehicle"/> with values from the <paramref name="preset"/>
        /// </summary>
        private async void RefreshVehicleUsingPreset(int vehicle, VstancerPreset preset)
        {
            if (DoesEntityExist(vehicle))
            {
                int wheelsCount = preset.WheelsCount;
                for (int index = 0; index < wheelsCount; index++)
                {
                    SetVehicleWheelXOffset(vehicle, index, preset.OffsetX[index]);
                    SetVehicleWheelXrot(vehicle, index, preset.RotationY[index]);
                }
            }
            await Delay(0);
        }

        /// <summary>
        /// Refreshes all the vehicles
        /// </summary>
        private async void RefreshVehicles(IEnumerable<int> vehiclesList)
        {
            Vector3 currentCoords = GetEntityCoords(playerPed, true);

            foreach (int entity in vehiclesList)
            {
                if (DoesEntityExist(entity))
                {
                    Vector3 coords = GetEntityCoords(entity, true);

                    if (Vector3.Distance(currentCoords, coords) <= maxSyncDistance)
                        RefreshVehicleUsingDecorators(entity);
                }
            }
            await Delay(0);
        }

        /// <summary>
        /// Refreshes the <paramref name="vehicle"/> with values from its decorators (if exist)
        /// </summary>
        /// <param name="vehicle"></param>
        private async void RefreshVehicleUsingDecorators(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = wheelsCount / 2;

            if (frontCount % 2 != 0)
                frontCount -= 1;

            if (DecorExistOn(vehicle, decor_off_f))
            {
                float value = DecorGetFloat(vehicle, decor_off_f);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }

            }

            if (DecorExistOn(vehicle, decor_rot_f))
            {
                float value = DecorGetFloat(vehicle, decor_rot_f);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXrot(vehicle, index, value);
                    else
                        SetVehicleWheelXrot(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, decor_off_r))
            {
                float value = DecorGetFloat(vehicle, decor_off_r);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, decor_rot_r))
            {
                float value = DecorGetFloat(vehicle, decor_rot_r);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXrot(vehicle, index, value);
                    else
                        SetVehicleWheelXrot(vehicle, index, -value);
                }
            }

            await Delay(0);
        }

        /// <summary>
        /// Prints the values of the decorators used on the <paramref name="vehicle"/>
        /// </summary>
        private async void PrintDecoratorsInfo(int vehicle)
        {
            if (DoesEntityExist(vehicle))
            {
                int wheelsCount = GetVehicleNumberOfWheels(vehicle);
                int netID = NetworkGetNetworkIdFromEntity(vehicle);
                StringBuilder s = new StringBuilder();
                s.AppendLine($"{ScriptName}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

                if (DecorExistOn(vehicle, decor_off_f))
                {
                    float value = DecorGetFloat(vehicle, decor_off_f);
                    s.AppendLine($"{decor_off_f}: {value}");
                }

                if (DecorExistOn(vehicle, decor_rot_f))
                {
                    float value = DecorGetFloat(vehicle, decor_rot_f);
                    s.AppendLine($"{decor_rot_f}: {value}");
                }

                if (DecorExistOn(vehicle, decor_off_r))
                {
                    float value = DecorGetFloat(vehicle, decor_off_r);
                    s.AppendLine($"{decor_off_r}: {value}");
                }

                if (DecorExistOn(vehicle, decor_rot_r))
                {
                    float value = DecorGetFloat(vehicle, decor_rot_r);
                    s.AppendLine($"{decor_rot_r}: {value}");
                }
                Debug.WriteLine(s.ToString());
            }
            else Debug.WriteLine($"{ScriptName}: Can't find vehicle with handle {vehicle}");

            await Delay(0);
        }

        /// <summary>
        /// Prints the list of vehicles using any vstancer decorator.
        /// </summary>
        private async void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => HasDecorators(entity));

            Debug.WriteLine($"{ScriptName}: Vehicles with decorators: {entities.Count()}");

            foreach (var item in entities)
                Debug.WriteLine($"Vehicle: {item}");

            await Delay(0);
        }

        /// <summary>
        /// Returns true if the <paramref name="entity"/> has any vstancer decorator
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool HasDecorators(int entity)
        {
            return (    
                DecorExistOn(entity, decor_off_f) ||
                DecorExistOn(entity, decor_rot_f) ||    
                DecorExistOn(entity, decor_off_r) ||
                DecorExistOn(entity, decor_rot_r) ||
                DecorExistOn(entity, decor_off_f_def) ||
                DecorExistOn(entity, decor_rot_f_def) ||
                DecorExistOn(entity, decor_off_r_def) ||
                DecorExistOn(entity, decor_rot_r_def)
                );
        }

        private void LoadConfig(string filename = "config.ini")
        {
            string strings = null;
            try
            {
                strings = LoadResourceFile(ResourceName, filename);

                Debug.WriteLine($"{ScriptName}: Loaded settings from {filename}");
            }
            catch(Exception e)
            {
                Debug.WriteLine($"{ScriptName}: Impossible to load {filename}");
                Debug.WriteLine(e.StackTrace);
            }
            finally
            {
                Config config = new Config(strings);

                toggleMenu = config.GetIntValue("toggleMenu", toggleMenu);
                editingFactor = config.GetFloatValue("editingFactor", editingFactor);
                maxSyncDistance = config.GetFloatValue("maxSyncDistance", maxSyncDistance);
                FrontMaxOffset = config.GetFloatValue("FrontMaxOffset", FrontMaxOffset);
                FrontMaxCamber = config.GetFloatValue("FrontMaxCamber", FrontMaxCamber);
                RearMaxOffset = config.GetFloatValue("RearMaxOffset", RearMaxOffset);
                RearMaxCamber = config.GetFloatValue("RearMaxCamber", RearMaxCamber);
                timer = config.GetLongValue("timer", timer);
                debug = config.GetBoolValue("debug", debug);
                screenPosX = config.GetFloatValue("screenPosX", screenPosX);
                screenPosY = config.GetFloatValue("screenPosY", screenPosY);

                Debug.WriteLine($"{ScriptName}: Settings {nameof(FrontMaxOffset)}={FrontMaxOffset} {nameof(FrontMaxCamber)}={FrontMaxCamber} {nameof(RearMaxOffset)}={RearMaxOffset} {nameof(RearMaxCamber)}={RearMaxCamber} {nameof(timer)}={timer} {nameof(debug)}={debug} {nameof(maxSyncDistance)}={maxSyncDistance}");
            }
        }
    }
}
