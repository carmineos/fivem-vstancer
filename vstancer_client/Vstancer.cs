using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Text;
using NativeUI;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;
using static NativeUI.UIMenuDynamicListItem;

namespace vstancer_client
{
    public class Vstancer : BaseScript
    {
        #region CONFIG_FIEDS
        private static float editingFactor;
        private static float maxSyncDistance;
        private static float maxOffset;
        private static float maxCamber;
        private static long timer;
        private static bool debug;
        private static int toggleMenu;
        private static float screenPosX;
        private static float screenPosY;
        private static string title;
        private static string description;
        private static string ResourceName;
        #endregion

        #region DECORATORS_NAMES
        private static string decor_off_f = "vstancer_off_f";
        private static string decor_rot_f = "vstancer_rot_f";
        private static string decor_off_f_def = "vstancer_off_f_def";
        private static string decor_rot_f_def = "vstancer_rot_f_def";

        private static string decor_off_r = "vstancer_off_r";
        private static string decor_rot_r = "vstancer_rot_r";
        private static string decor_off_r_def = "vstancer_off_r_def";
        private static string decor_rot_r_def = "vstancer_rot_r_def";
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
            // Avoid to detect the preset as not edited when you edit it and then edit it back to stock without pressing reset button (allowing it to refresh to stock visual)
            //value = (float)Math.Round(value, 3);

            var newitem = new UIMenuDynamicListItem(name, value.ToString("F3"), (sender, direction) =>
            {
                float min = defaultValue - maxEditing;
                float max = defaultValue + maxEditing;

                if (direction == ChangeDirection.Left)
                {
                    var newvalue = value - editingFactor;
                    if (newvalue < min)
                        CitizenFX.Core.UI.Screen.ShowNotification($"Min value allowed is {min}");
                    else
                    {
                        value = newvalue;
                        if (sender == frontRotationGUI) currentPreset.SetRotationFront(value);
                        else if (sender == rearRotationGUI) currentPreset.SetRotationRear(value);
                        else if (sender == frontOffsetGUI) currentPreset.SetOffsetFront(value);
                        else if (sender == rearOffsetGUI) currentPreset.SetOffsetRear(value);

                        // Force one single refresh to update rendering at correct position after reset
                        if (value == defaultValue)
                            RefreshVehicleUsingPreset(currentVehicle, currentPreset);

                        if (debug)
                            Debug.WriteLine($"Edited {sender.Text} => value:{value}");
                    }
                }
                else if (direction == ChangeDirection.Right)
                {
                    var newvalue = value + editingFactor;
                    if (newvalue > max)
                        CitizenFX.Core.UI.Screen.ShowNotification($"Max value allowed is {max}");
                    else
                    {
                        value = newvalue;
                        if (sender == frontRotationGUI) currentPreset.SetRotationFront(value);
                        else if (sender == rearRotationGUI) currentPreset.SetRotationRear(value);
                        else if (sender == frontOffsetGUI) currentPreset.SetOffsetFront(value);
                        else if (sender == rearOffsetGUI) currentPreset.SetOffsetRear(value);

                        // Force one single refresh to update rendering at correct position after reset
                        if (value == defaultValue)
                            RefreshVehicleUsingPreset(currentVehicle, currentPreset);

                        if (debug)
                            Debug.WriteLine($"Edited {sender.Text} => value:{value}");
                    }
                }
                return value.ToString("F3");
            });
            menu.AddItem(newitem);
            return newitem;
        }

        private void InitialiseMenu()
        {
            _menuPool = new MenuPool();
            EditorMenu = new UIMenu(title, description, new PointF(screenPosX*Screen.Width, screenPosY*Screen.Height));

            frontOffsetGUI = AddDynamicFloatList(EditorMenu, "Front Track Width", -currentPreset.DefaultOffsetX[0], -currentPreset.OffsetX[0], maxOffset);
            rearOffsetGUI = AddDynamicFloatList(EditorMenu, "Rear Track Width", -currentPreset.DefaultOffsetX[currentPreset.frontCount], -currentPreset.OffsetX[currentPreset.frontCount], maxOffset);
            frontRotationGUI = AddDynamicFloatList(EditorMenu, "Front Camber", currentPreset.DefaultRotationY[0], currentPreset.RotationY[0], maxCamber);
            rearRotationGUI = AddDynamicFloatList(EditorMenu, "Rear Camber", currentPreset.DefaultRotationY[currentPreset.frontCount], currentPreset.RotationY[currentPreset.frontCount], maxCamber);
            AddMenuReset(EditorMenu);

            EditorMenu.MouseEdgeEnabled = false;
            EditorMenu.ControlDisablingEnabled = false;
            EditorMenu.MouseControlsEnabled = false;

            _menuPool.ResetCursorOnOpen = true;
            _menuPool.Add(EditorMenu);
            _menuPool.RefreshIndex();
        }

        public Vstancer()
        {
            ResourceName = GetCurrentResourceName();
            Debug.WriteLine("VSTANCER: Script by Neos7");

            RegisterDecorators();

            LoadConfig();

            currentTime = GetGameTimer();
            lastTime = GetGameTimer();

            currentVehicle = -1;
            currentPreset = new VstancerPreset();
            vehicles = Enumerable.Empty<int>();

            InitialiseMenu();

            RegisterCommand("vstancer_distance", new Action<int, dynamic>((source, args) =>
            {
                bool result = float.TryParse(args[0], out float value);
                if (result)
                {
                    maxSyncDistance = value;
                    Debug.WriteLine("VSTANCER: Received new maxSyncDistance value {0}", value);
                }
                else Debug.WriteLine("VSTANCER: Can't parse {0}", value);

            }), false);

            RegisterCommand("vstancer_debug", new Action<int, dynamic>((source, args) =>
            {
                bool result = bool.TryParse(args[0], out bool value);
                if (result)
                {
                    debug = value;
                    Debug.WriteLine("VSTANCER: Received new debug value {0}", value);
                }
                else Debug.WriteLine("VSTANCER: Can't parse {0}", value);

            }), false);

            RegisterCommand("vstancer_decorators", new Action<int, dynamic>((source, args) =>
            {
                PrintDecoratorsInfo(currentVehicle);
            }), false);

            RegisterCommand("vstancer_preset", new Action<int, dynamic>((source, args) =>
            {
                if (currentPreset != null)
                    Debug.WriteLine(currentPreset.ToString());
                else
                    Debug.WriteLine("Current preset doesn't exist");
            }), false);

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators(vehicles);
            }), false);

            Tick += HandleMenu;
            Tick += VstancerTask;
        }

        private async Task HandleMenu()
        {
            _menuPool.ProcessMenus();

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
            await Task.FromResult(0);
        }

        private async Task VstancerTask()
        {
            currentTime = (GetGameTimer() - lastTime);

            playerPed = PlayerPedId();
            //CURRENT VEHICLE/PRESET HANDLER
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

        public async void UpdateFloatDecorator(int vehicle, string name, float currentValue, float defaultValue)
        {
            // Decorator exists but needs to be updated
            if (DecorExistOn(vehicle, name))
            {
                float decorValue = DecorGetFloat(vehicle, name);
                if (Math.Abs(currentValue - decorValue) > 0.001f)
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (debug)
                        Debug.WriteLine($"Updated decorator {name} updated from {decorValue} to {currentValue} for vehicle {vehicle}");
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (Math.Abs(currentValue - defaultValue) > 0.001f)
                {
                    DecorSetFloat(vehicle, name, currentValue);
                    if (debug)
                        Debug.WriteLine($"Added decorator {name} with value {currentValue} to vehicle {vehicle}");
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
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = wheelsCount / 2;

            if (frontCount % 2 != 0)
                frontCount -= 1;

            UpdateFloatDecorator(vehicle, decor_off_f_def, preset.DefaultOffsetX[0], preset.OffsetX[0]);
            UpdateFloatDecorator(vehicle, decor_rot_f_def, preset.DefaultRotationY[0], preset.RotationY[0]);

            UpdateFloatDecorator(vehicle, decor_off_r_def, preset.DefaultOffsetX[frontCount], preset.OffsetX[frontCount]);
            UpdateFloatDecorator(vehicle, decor_rot_r_def, preset.DefaultRotationY[frontCount], preset.RotationY[frontCount]);

            UpdateFloatDecorator(vehicle, decor_off_f, preset.OffsetX[0], preset.DefaultOffsetX[0]);
            UpdateFloatDecorator(vehicle, decor_rot_f, preset.RotationY[0], preset.DefaultRotationY[0]);
            UpdateFloatDecorator(vehicle, decor_off_r, preset.OffsetX[frontCount], preset.DefaultOffsetX[frontCount]);
            UpdateFloatDecorator(vehicle, decor_rot_r, preset.RotationY[frontCount], preset.DefaultRotationY[frontCount]);

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

            float currentRotationFront, currentRotationRear, currentOffsetFront, currentOffsetRear, defaultRotationFront, defaultRotationRear, defaultOffsetFront, defaultOffsetRear;

            if (DecorExistOn(vehicle, decor_off_f_def))
                defaultOffsetFront = DecorGetFloat(vehicle, decor_off_f_def);
            else defaultOffsetFront = GetVehicleWheelXOffset(vehicle, 0);

            if (DecorExistOn(vehicle, decor_rot_f_def))
                defaultRotationFront = DecorGetFloat(vehicle, decor_rot_f_def);
            else defaultRotationFront = GetVehicleWheelXrot(vehicle, 0);  // native should be called GetVehicleWheelYrot

            if (DecorExistOn(vehicle, decor_off_f))
                currentOffsetFront = DecorGetFloat(vehicle, decor_off_f);
            else currentOffsetFront = defaultOffsetFront;

            if (DecorExistOn(vehicle, decor_rot_f))
                currentRotationFront = DecorGetFloat(vehicle, decor_rot_f);
            else currentRotationFront = defaultRotationFront;

            if (DecorExistOn(vehicle, decor_off_r_def))
                defaultOffsetRear = DecorGetFloat(vehicle, decor_off_r_def);
            else defaultOffsetRear = GetVehicleWheelXOffset(vehicle, frontCount);

            if (DecorExistOn(vehicle, decor_rot_r_def))
                defaultRotationRear = DecorGetFloat(vehicle, decor_rot_r_def);
            else defaultRotationRear = GetVehicleWheelXrot(vehicle, frontCount); // native should be called GetVehicleWheelYrot

            if (DecorExistOn(vehicle, decor_off_r))
                currentOffsetRear = DecorGetFloat(vehicle, decor_off_r);
            else currentOffsetRear = defaultOffsetRear;

            if (DecorExistOn(vehicle, decor_rot_r))
                currentRotationRear = DecorGetFloat(vehicle, decor_rot_r);
            else currentRotationRear = defaultRotationRear;

            VstancerPreset preset = new VstancerPreset(wheelsCount, currentRotationFront, currentRotationRear, currentOffsetFront, currentOffsetRear, defaultRotationFront, defaultRotationRear, defaultOffsetFront, defaultOffsetRear);

            return preset;
        }

        /// <summary>
        /// Refreshes the <paramref name="vehicle"/> with values from the <paramref name="preset"/>
        /// </summary>
        private async void RefreshVehicleUsingPreset(int vehicle, VstancerPreset preset)
        {
            if (DoesEntityExist(vehicle))
            {
                for (int index = 0; index < preset.wheelsCount; index++)
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
                //if (entity != currentVehicle)
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
                s.AppendLine($"VSTANCER: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

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
            else Debug.WriteLine("VSTANCER: Current vehicle doesn't exist");

            await Delay(0);
        }

        /// <summary>
        /// Prints the list of vehicles using any vstancer decorator.
        /// </summary>
        private async void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => HasDecorators(entity));

            Debug.WriteLine($"VSTANCER: Vehicles with decorators: {entities.Count()}");

            foreach (var item in entities)
            {
                //PrintDecoratorsInfo(item);
                Debug.WriteLine($"Vehicle: {item}");
            }

            await Delay(0);
        }

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
                ) == true;
        }

        protected void LoadConfig()
        {
            string strings = null;
            Config config = new Config();
            try
            {
                strings = LoadResourceFile(ResourceName, "config.ini");
                config.ParseConfigFile(strings);
                Debug.WriteLine("VSTANCER: Loaded settings from config.ini");
            }
            catch(Exception e)
            {
                Debug.WriteLine("VSTANCER: Impossible to load config.ini");
                Debug.WriteLine(e.StackTrace);
            }
            finally
            {
                toggleMenu = config.toggleMenu;
                editingFactor = config.editingFactor;
                maxSyncDistance = config.maxSyncDistance;
                maxOffset = config.maxOffset;
                maxCamber = config.maxCamber;
                timer = config.timer;
                debug = config.debug;
                screenPosX = config.screenPosX;
                screenPosY = config.screenPosY;
                title = config.title;
                description = config.description;

                Debug.WriteLine("VSTANCER: Settings maxOffset={0} maxCamber={1} timer={2} debug={3} maxSyncDistance={4} position={5}-{6}", maxOffset, maxCamber, timer, debug, maxSyncDistance, screenPosX, screenPosY);
            }
        }
    }


    public class VehicleList : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            int entity = -1;
            int handle = FindFirstVehicle(ref entity);

            if (handle != -1)
            {
                do
                {
                    yield return entity;
                }
                while (FindNextVehicle(handle, ref entity));

                EndFindVehicle(handle);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
