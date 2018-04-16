using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NativeUI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using vstancer_shared;
using System.Drawing;
using CitizenFX.Core.UI;

namespace vstancer_client
{
    public class Client : BaseScript
    {
        #region CONFIG_FIEDS
        private static float editingFactor;
        private static float maxSyncDistance;
        private static float maxOffset;
        private static float maxCamber;
        private static long timer;
        private static bool debug;
        private static int toggleMenu;
        #endregion

        #region DECORATORS_NAMES
        private static string decorOffsetFront = "vstancer_offset_front";
        private static string decorRotationFront = "vstancer_rotation_front";
        private static string decorOffsetDefaultFront = "vstancer_offset_default_front";
        private static string decorRotationDefaultFront = "vstancer_rotation_default_front";

        private static string decorOffsetRear = "vstancer_offset_rear";
        private static string decorRotationRear = "vstancer_rotation_rear";
        private static string decorOffsetDefaultRear = "vstancer_offset_default_rear";
        private static string decorRotationDefaultRear = "vstancer_rotation_default_rear";
        #endregion

        #region FIELDS
        private static long lastTime;
        private int playerPed;
        private int currentVehicle;
        private vstancerPreset currentPreset;
        #endregion

        #region GUI
        private MenuPool _menuPool;
        private UIMenu wheelsEditorMenu;
        private UIMenuListItem frontOffsetGUI;
        private UIMenuListItem rearOffsetGUI;
        private UIMenuListItem frontRotationGUI;
        private UIMenuListItem rearRotationGUI;

        public UIMenuListItem AddMenuListValues(UIMenu menu, string name, int property, float defaultValue, float currentValue)
        {
            int countValues;
            var values = new List<dynamic>();

            if (property == 2 || property == 3)
            {
                defaultValue = -defaultValue; 
                countValues = (int)(maxCamber / editingFactor);
            }
            else
            {
                currentValue = -currentValue;
                countValues = (int)(maxOffset / editingFactor);
            }

            //POSITIVE VALUES
            for (int i = 0; i <= countValues; i++)
                values.Add((float)Math.Round(-defaultValue + (i * editingFactor), 3));
            //NEGATIVE VALUES
            for (int i = countValues; i >= 1; i--)
                values.Add((float)Math.Round(-defaultValue + (-i * editingFactor), 3));

            var currentIndex = values.IndexOf((float)Math.Round(currentValue, 3)); // Index calculated at runtime in case of script restart
            //Debug.WriteLine($"current:{currentValue}, default:{defaultValue}, index:{currentIndex}");

            var newitem = new UIMenuListItem(name, values, currentIndex);
            menu.AddItem(newitem);
            menu.OnListChange += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    switch (property)
                    {
                        case 0:
                            currentPreset.SetFrontOffset(values[index]);
                            break;
                        case 1:
                            currentPreset.SetRearOffset(values[index]);
                            break;
                        case 2:
                            currentPreset.SetFrontRotation(values[index]);
                            break;
                        case 3:
                            currentPreset.SetRearRotation(values[index]);
                            break;
                    }
                }

            };
            return newitem;
        }

        public void AddMenuReset(UIMenu menu)
        {
            var newitem = new UIMenuItem("Reset", "Restores locally the default values.");
            menu.AddItem(newitem);

            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    currentPreset.ResetDefault();
                    RemoveDecorators(currentVehicle);

                    InitialiseMenu();
                    wheelsEditorMenu.Visible = true;
                }
            };
        }

        public void InitialiseMenu()
        {
            _menuPool = new MenuPool();
            wheelsEditorMenu = new UIMenu("Wheels Editor", "~b~Track Width & Camber", new PointF(Screen.Width, 0));
            _menuPool.Add(wheelsEditorMenu);

            frontOffsetGUI = AddMenuListValues(wheelsEditorMenu, "Front Track Width", 0, currentPreset.defaultWheelsOffset[0], currentPreset.currentWheelsOffset[0]);
            frontRotationGUI = AddMenuListValues(wheelsEditorMenu, "Front Camber", 2, currentPreset.defaultWheelsRot[0], currentPreset.currentWheelsRot[0]);
            rearOffsetGUI = AddMenuListValues(wheelsEditorMenu, "Rear Track Width", 1, currentPreset.defaultWheelsOffset[currentPreset.frontCount], currentPreset.currentWheelsOffset[currentPreset.frontCount]);
            rearRotationGUI = AddMenuListValues(wheelsEditorMenu, "Rear Camber", 3, currentPreset.defaultWheelsRot[currentPreset.frontCount], currentPreset.currentWheelsRot[currentPreset.frontCount]);

            AddMenuReset(wheelsEditorMenu);
            wheelsEditorMenu.MouseEdgeEnabled = false;
            wheelsEditorMenu.ControlDisablingEnabled = false;
            wheelsEditorMenu.MouseControlsEnabled = false;
            _menuPool.RefreshIndex();
        }
        #endregion

        public Client()
        {
            LoadConfig();

            DecorRegister(decorOffsetFront, 1);
            DecorRegister(decorRotationFront, 1);
            DecorRegister(decorOffsetDefaultFront, 1);
            DecorRegister(decorRotationDefaultFront, 1);

            DecorRegister(decorOffsetRear, 1);
            DecorRegister(decorRotationRear, 1);
            DecorRegister(decorOffsetDefaultRear, 1);
            DecorRegister(decorRotationDefaultRear, 1);

            lastTime = GetGameTimer();

            currentVehicle = -1;
            currentPreset = new vstancerPreset();
            InitialiseMenu();

            EventHandlers.Add("vstancer:maxOffset", new Action<float>((new_maxOffset) =>
            {
                maxOffset = new_maxOffset;
                Debug.WriteLine("VSTANCER: Received new maxOffset value {0}", new_maxOffset.ToString());
            }));

            EventHandlers.Add("vstancer:maxCamber", new Action<float>((new_maxCamber) =>
            {
                maxCamber = new_maxCamber;
                Debug.WriteLine("VSTANCER: Received new maxCamber value {0}", new_maxCamber.ToString());
            }));

            EventHandlers.Add("vstancer:timer", new Action<long>((new_timer) =>
            {
                timer = new_timer;
                Debug.WriteLine("VSTANCER: Received new timer value {0}", new_timer.ToString());
            }));

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

            RegisterCommand("vstancer_info", new Action<int, dynamic>((source, args) =>
            {
                PrintDecoratorsInfo(currentVehicle);
            }), false);

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVehiclesWithDecorators();
            }), false);

            Tick += OnTick;
        }

        public async Task OnTick()
        {
            _menuPool.ProcessMenus();

            playerPed = GetPlayerPed(-1);

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

                    if (IsControlJustPressed(1, toggleMenu) || IsDisabledControlJustPressed(1, toggleMenu)) // TOGGLE MENU VISIBLE
                        wheelsEditorMenu.Visible = !wheelsEditorMenu.Visible;
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

                //Close menu if opened
                if (wheelsEditorMenu.Visible)
                    wheelsEditorMenu.Visible = false;
            }

            // Current preset is always refreshed
            RefreshCurrentVehicle();

            // Check decorators needs to be updated
            if ((GetGameTimer() - lastTime) > timer)
            {
                if (currentVehicle != -1 && currentPreset != null)
                    UpdateVehicleDecorators(currentVehicle, currentPreset);
                lastTime = GetGameTimer();
            }

            // Iterates all the vehicles and refreshes them
            IterateVehicles();

            await Task.FromResult(0);
        }

        /// <summary>
        /// Removes the decorators from the <paramref name="vehicle"/>
        /// </summary>
        /// <param name="vehicle"></param>
        private async void RemoveDecorators(int vehicle)
        {
            if (DecorExistOn(vehicle, decorOffsetFront))
                DecorRemove(vehicle, decorOffsetFront);

            if (DecorExistOn(vehicle, decorRotationFront))
                DecorRemove(vehicle, decorRotationFront);

            if (DecorExistOn(vehicle, decorOffsetDefaultFront))
                DecorRemove(vehicle, decorOffsetDefaultFront);

            if (DecorExistOn(vehicle, decorRotationDefaultFront))
                DecorRemove(vehicle, decorRotationDefaultFront);

            if (DecorExistOn(vehicle, decorOffsetRear))
                DecorRemove(vehicle, decorOffsetRear);

            if (DecorExistOn(vehicle, decorRotationRear))
                DecorRemove(vehicle, decorRotationRear);

            if (DecorExistOn(vehicle, decorOffsetDefaultRear))
                DecorRemove(vehicle, decorOffsetDefaultRear);

            if (DecorExistOn(vehicle, decorRotationDefaultRear))
                DecorRemove(vehicle, decorRotationDefaultRear);

            await Task.FromResult(0);
        }

        /// <summary>
        /// Updates the decorators on the <paramref name="vehicle"/> with updated values from the <paramref name="preset"/>
        /// </summary>
        /// <param name="vehicle"></param>
        private async void UpdateVehicleDecorators(int vehicle, vstancerPreset preset)
        {
            int wheelsCount = GetVehicleNumberOfWheels(currentVehicle);
            int frontCount = wheelsCount / 2;

            if (frontCount % 2 != 0)
                frontCount -= 1;

            if (DecorExistOn(vehicle, decorOffsetDefaultFront))
            {
                float value = DecorGetFloat(vehicle, decorOffsetDefaultFront);
                if (value != preset.defaultWheelsOffset[0])
                    DecorSetFloat(vehicle, decorOffsetDefaultFront, preset.defaultWheelsOffset[0]);
            }
            else
            {
                if (preset.defaultWheelsOffset[0] != preset.currentWheelsOffset[0])
                    DecorSetFloat(vehicle, decorOffsetDefaultFront, preset.defaultWheelsOffset[0]);
            }

            if (DecorExistOn(vehicle, decorRotationDefaultFront))
            {
                float value = DecorGetFloat(vehicle, decorRotationDefaultFront);
                if (value != preset.defaultWheelsRot[0])
                    DecorSetFloat(vehicle, decorRotationDefaultFront, preset.defaultWheelsRot[0]);
            }
            else
            {
                if (preset.defaultWheelsRot[0] != preset.currentWheelsRot[0])
                    DecorSetFloat(vehicle, decorRotationDefaultFront, preset.defaultWheelsRot[0]);
            }

            if (DecorExistOn(vehicle, decorOffsetDefaultRear))
            {
                float value = DecorGetFloat(vehicle, decorOffsetDefaultRear);
                if (value != preset.defaultWheelsOffset[frontCount])
                    DecorSetFloat(vehicle, decorOffsetDefaultRear, preset.defaultWheelsOffset[frontCount]);
            }
            else
            {
                if (preset.defaultWheelsOffset[frontCount] != preset.currentWheelsOffset[frontCount])
                    DecorSetFloat(vehicle, decorOffsetDefaultRear, preset.defaultWheelsOffset[frontCount]);
            }

            if (DecorExistOn(vehicle, decorRotationDefaultRear))
            {
                float value = DecorGetFloat(vehicle, decorRotationDefaultRear);
                if (value != preset.defaultWheelsRot[frontCount])
                    DecorSetFloat(vehicle, decorRotationDefaultRear, preset.defaultWheelsRot[frontCount]);
            }
            else
            {
                if (preset.defaultWheelsRot[frontCount] != preset.currentWheelsRot[frontCount])
                    DecorSetFloat(vehicle, decorRotationDefaultRear, preset.defaultWheelsRot[frontCount]);
            }

            if (DecorExistOn(vehicle, decorOffsetFront))
            {
                float value = DecorGetFloat(vehicle, decorOffsetFront);
                if (value != preset.currentWheelsOffset[0])
                    DecorSetFloat(vehicle, decorOffsetFront, preset.currentWheelsOffset[0]);
            }
            else
            {
                if (preset.defaultWheelsOffset[0] != preset.currentWheelsOffset[0])
                    DecorSetFloat(vehicle, decorOffsetFront, preset.currentWheelsOffset[0]);
            }

            if (DecorExistOn(vehicle, decorRotationFront))
            {
                float value = DecorGetFloat(vehicle, decorRotationFront);
                if (value != preset.currentWheelsRot[0])
                    DecorSetFloat(vehicle, decorRotationFront, preset.currentWheelsRot[0]);
            }
            else
            {
                if (preset.defaultWheelsRot[0] != preset.currentWheelsRot[0])
                    DecorSetFloat(vehicle, decorRotationFront, preset.currentWheelsRot[0]);
            }

            if (DecorExistOn(vehicle, decorOffsetRear))
            {
                float value = DecorGetFloat(vehicle, decorOffsetRear);
                if (value != preset.currentWheelsOffset[frontCount])
                    DecorSetFloat(vehicle, decorOffsetRear, preset.currentWheelsOffset[frontCount]);
            }
            else
            {
                if (preset.defaultWheelsOffset[frontCount] != preset.currentWheelsOffset[frontCount])
                    DecorSetFloat(vehicle, decorOffsetRear, preset.currentWheelsOffset[frontCount]);
            }

            if (DecorExistOn(vehicle, decorRotationRear))
            {
                float value = DecorGetFloat(vehicle, decorRotationRear);
                if (value != preset.currentWheelsRot[frontCount])
                    DecorSetFloat(vehicle, decorRotationRear, preset.currentWheelsRot[frontCount]);
            }
            else
            {
                if (preset.defaultWheelsRot[frontCount] != preset.currentWheelsRot[frontCount])
                    DecorSetFloat(vehicle, decorRotationRear, preset.currentWheelsRot[frontCount]);
            }

            await Task.FromResult(0);
        }

        /// <summary>
        /// Creates a preset for the <paramref name="vehicle"/> to edit it locally
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        private vstancerPreset CreatePreset(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = wheelsCount / 2;
            if (frontCount % 2 != 0)
                frontCount -= 1;

            float currentRotationFront, currentRotationRear, currentOffsetFront, currentOffsetRear, defaultRotationFront, defaultRotationRear, defaultOffsetFront, defaultOffsetRear;

            if (DecorExistOn(vehicle, decorOffsetDefaultFront))
                defaultOffsetFront = DecorGetFloat(vehicle, decorOffsetDefaultFront);
            else defaultOffsetFront = GetVehicleWheelXOffset(vehicle, 0);

            if (DecorExistOn(vehicle, decorRotationDefaultFront))
                defaultRotationFront = DecorGetFloat(vehicle, decorRotationDefaultFront);
            else defaultRotationFront = GetVehicleWheelXrot(vehicle, 0);

            if (DecorExistOn(vehicle, decorOffsetFront))
                currentOffsetFront = DecorGetFloat(vehicle, decorOffsetFront);
            else currentOffsetFront = defaultOffsetFront;

            if (DecorExistOn(vehicle, decorRotationFront))
                currentRotationFront = DecorGetFloat(vehicle, decorRotationFront);
            else currentRotationFront = defaultRotationFront;

            if (DecorExistOn(vehicle, decorOffsetDefaultRear))
                defaultOffsetRear = DecorGetFloat(vehicle, decorOffsetDefaultRear);
            else defaultOffsetRear = GetVehicleWheelXOffset(vehicle, frontCount);

            if (DecorExistOn(vehicle, decorRotationDefaultRear))
                defaultRotationRear = DecorGetFloat(vehicle, decorRotationDefaultRear);
            else defaultRotationRear = GetVehicleWheelXrot(vehicle, frontCount);

            if (DecorExistOn(vehicle, decorOffsetRear))
                currentOffsetRear = DecorGetFloat(vehicle, decorOffsetRear);
            else currentOffsetRear = defaultOffsetRear;

            if (DecorExistOn(vehicle, decorRotationRear))
                currentRotationRear = DecorGetFloat(vehicle, decorRotationRear);
            else currentRotationRear = defaultRotationRear;

            vstancerPreset preset = new vstancerPreset(wheelsCount, currentRotationFront, currentRotationRear, currentOffsetFront, currentOffsetRear, defaultRotationFront, defaultRotationRear, defaultOffsetFront, defaultOffsetRear);

            return preset;
        }

        /// <summary>
        /// Refreshes the current vehicle with values from the current preset
        /// </summary>
        private async void RefreshCurrentVehicle()
        {
            if (currentVehicle != -1 && currentPreset != null)
            {
                if (DoesEntityExist(currentVehicle))
                {
                    for (int index = 0; index < currentPreset.wheelsCount; index++)
                    {
                        SetVehicleWheelXOffset(currentVehicle, index, currentPreset.currentWheelsOffset[index]);
                        SetVehicleWheelXrot(currentVehicle, index, currentPreset.currentWheelsRot[index]);
                    }
                }
            }
            await Task.FromResult(0);
        }

        /// <summary>
        /// Iterates all the vehicle entities
        /// </summary>
        private async void IterateVehicles()
        {
            int entity = -1;
            int handle = FindFirstVehicle(ref entity);

            if (handle != -1)
            {
                while(FindNextVehicle(handle,ref entity))
                {
                    if(entity != currentVehicle)
                    {
                        Vector3 currentCoords = GetEntityCoords(playerPed, true);
                        Vector3 coords = GetEntityCoords(entity, true);

                        if (Vector3.Distance(currentCoords, coords) <= maxSyncDistance)
                            RefreshVehicle(entity);
                    }
                }
                EndFindVehicle(handle);
            }
            await Task.FromResult(0);
        }

        /// <summary>
        /// Refreshes the <paramref name="vehicle"/> with values from its decorators (if exist)
        /// </summary>
        /// <param name="vehicle"></param>
        private async void RefreshVehicle(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = wheelsCount / 2;

            if (frontCount % 2 != 0)
                frontCount -= 1;

            if (DecorExistOn(vehicle, decorOffsetFront))
            {
                float value = DecorGetFloat(vehicle, decorOffsetFront);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }
                    
            }

            if (DecorExistOn(vehicle, decorRotationFront))
            {
                float value = DecorGetFloat(vehicle, decorRotationFront);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXrot(vehicle, index, value);
                    else
                        SetVehicleWheelXrot(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, decorOffsetRear))
            {
                float value = DecorGetFloat(vehicle, decorOffsetRear);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, decorRotationRear))
            {
                float value = DecorGetFloat(vehicle, decorRotationRear);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXrot(vehicle, index, value);
                    else
                        SetVehicleWheelXrot(vehicle, index, -value);
                }
            }

            await Task.FromResult(0);
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
                Debug.WriteLine($"VSTANCER: Vehicle: {vehicle}, wheelsCount: {wheelsCount}, netID: {netID}");

                if (DecorExistOn(vehicle, decorOffsetFront))
                {
                    float value = DecorGetFloat(vehicle, decorOffsetFront);
                    Debug.WriteLine($"{decorOffsetFront}: {value}");
                }

                if (DecorExistOn(vehicle, decorRotationFront))
                {
                    float value = DecorGetFloat(vehicle, decorRotationFront);
                    Debug.WriteLine($"{decorRotationFront}: {value}");
                }

                if (DecorExistOn(vehicle, decorOffsetRear))
                {
                    float value = DecorGetFloat(vehicle, decorOffsetRear);
                    Debug.WriteLine($"{decorOffsetRear}: {value}");
                }

                if (DecorExistOn(vehicle, decorRotationRear))
                {
                    float value = DecorGetFloat(vehicle, decorRotationRear);
                    Debug.WriteLine($"{decorRotationRear}: {value}");
                }
            }
            else Debug.WriteLine("VSTANCER: Current vehicle doesn't exist");

            await Task.FromResult(0);
        }

        /// <summary>
        /// Prints the list of vehicles using any vstancer decorator.
        /// </summary>
        private async void PrintVehiclesWithDecorators()
        {
            List<int> list = new List<int>();
            int entity = -1;
            int handle = FindFirstVehicle(ref entity);

            if (handle != -1)
            {
                while (FindNextVehicle(handle, ref entity))
                {
                        if (
                            DecorExistOn(entity, decorOffsetFront) ||
                            DecorExistOn(entity, decorRotationFront) ||
                            DecorExistOn(entity, decorOffsetDefaultFront) ||
                            DecorExistOn(entity, decorRotationDefaultFront) ||
                            DecorExistOn(entity, decorOffsetRear) ||
                            DecorExistOn(entity, decorRotationRear) ||
                            DecorExistOn(entity, decorOffsetDefaultRear) ||
                            DecorExistOn(entity, decorRotationDefaultRear)
                            )
                            list.Add(entity);
                }
                EndFindVehicle(handle);
            }
            IEnumerable<int> entities = list.Distinct();
            Debug.WriteLine($"VSTANCER: Vehicles with decorators: {entities.Count()}");
            foreach (var item in entities)
            {
                Debug.WriteLine($"Vehicle: {item}, NetID: {NetworkGetNetworkIdFromEntity(item)}");
            }

            await Task.FromResult(0);
        }

        protected void LoadConfig()
        {
            string strings = null;
            vstancerConfig config = new vstancerConfig();
            try
            {
                strings = LoadResourceFile("vstancer", "config.ini");
                Debug.WriteLine("VSTANCER: Loaded settings from config.ini");
                config.ParseConfigFile(strings);
            }
            catch
            {
                Debug.WriteLine("VSTANCER: Impossible to load config.ini");
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

                Debug.WriteLine("VSTANCER: Settings maxOffset={0} maxCamber={1} timer={2} debug={3} maxSyncDistance={4}", maxOffset, maxCamber, timer, debug, maxSyncDistance);
            }
        }
    }
}
