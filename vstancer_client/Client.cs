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
        #region CONFIG FIEDS
        private static float editingFactor;
        private static float maxSyncDistance;
        private static float maxOffset;
        private static float maxCamber;
        private static long timer;
        private static bool debug;
        private static int toggleMenu;
        #endregion

        #region DECORATORS NAMES
        private string decorOffsetPrefix = "vstancer_offset_";
        private string decorRotationPrefix = "vstancer_rotation_";
        private string decorDefaultOffsetPrefix = "vstancer_offset_default_";
        private string decorDefaultRotationPrefix = "vstancer_rotation_default_";
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
        //private UIMenuListItem editingFactorGUI;
        private UIMenuListItem frontOffsetGUI;
        private UIMenuListItem rearOffsetGUI;
        private UIMenuListItem frontRotationGUI;
        private UIMenuListItem rearRotationGUI;

        public UIMenuListItem AddMenuListValues(UIMenu menu, string name, int property, float defaultValue)
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
                countValues = (int)(maxOffset / editingFactor);
            }
                

            //POSITIVE VALUES
            for (int i = 0; i <= countValues; i++)
                values.Add((float)Math.Round(-defaultValue + (i * editingFactor), 3));
            //NEGATIVE VALUES
            for (int i = countValues; i >= 1; i--)
                values.Add((float)Math.Round(-defaultValue + (-i * editingFactor), 3));

            var newitem = new UIMenuListItem(name, values, 0);
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
                    RemoveDecors();

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

            frontOffsetGUI = AddMenuListValues(wheelsEditorMenu, "Front Track Width", 0, currentPreset.currentWheelsOffset[0]);
            frontRotationGUI = AddMenuListValues(wheelsEditorMenu, "Front Camber", 2, currentPreset.currentWheelsRot[0]);
            rearOffsetGUI = AddMenuListValues(wheelsEditorMenu, "Rear Track Width", 1, currentPreset.currentWheelsOffset[currentPreset.frontCount]);
            rearRotationGUI = AddMenuListValues(wheelsEditorMenu, "Rear Camber", 3, currentPreset.currentWheelsRot[currentPreset.frontCount]);

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

            lastTime = GetGameTimer();

            currentVehicle = 0;
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
            RegisterCommand("vstancer_debug", new Action<int, dynamic>((source, args) =>
            {
                debug = bool.Parse(args[0]);
                Debug.WriteLine("VSTANCER: Received new debug value {0}", debug.ToString());
            }), false);
            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintVstancerDecors();
            }), false);
            RegisterCommand("vstancer_list", new Action<int, dynamic>((source, args) =>
            {
                PrintListVehiclesWithDecors();
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
            RefreshLocalPreset();

            // Check decorators needs to be updated
            if ((GetGameTimer() - lastTime) > timer)
            {
                if (currentVehicle != -1 && currentPreset != null)
                    UpdateDecorsOnCurrentVehicle();
                lastTime = GetGameTimer();
            }

            // Iterates all the vehicles and refreshes them
            IterateVehicles();

            await Task.FromResult(0);
        }

        public async void RemoveDecors()
        {
            int vehicle = currentVehicle;
            int wheelsCount = GetVehicleNumberOfWheels(currentVehicle);
            string decorName;

            for (int index = 0; index < wheelsCount; index++)
            {
                decorName = decorDefaultOffsetPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                    DecorRemove(vehicle, decorName);

                decorName = decorDefaultRotationPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                    DecorRemove(vehicle, decorName);

                decorName = decorOffsetPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                    DecorRemove(vehicle, decorName);

                decorName = decorRotationPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                    DecorRemove(vehicle, decorName);
            }

            await Task.FromResult(0);
        }

        public async void UpdateDecorsOnCurrentVehicle()
        {
            int vehicle = currentVehicle;
            int wheelsCount = GetVehicleNumberOfWheels(currentVehicle);

            string decorName;

            for (int index = 0; index < wheelsCount; index++)
            {
                decorName = decorDefaultOffsetPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                {
                    float value = DecorGetFloat(vehicle, decorName);
                    if (value != currentPreset.defaultWheelsOffset[index])
                        DecorSetFloat(vehicle, decorName, currentPreset.defaultWheelsOffset[index]);
                }else
                {
                    if(currentPreset.defaultWheelsOffset[index] != currentPreset.currentWheelsOffset[index])
                    {
                        DecorRegister(decorName, 1);
                        DecorSetFloat(vehicle, decorName, currentPreset.defaultWheelsOffset[index]);
                    }
                }

                decorName = decorDefaultRotationPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                {
                    float value = DecorGetFloat(vehicle, decorName);
                    if (value != currentPreset.defaultWheelsRot[index])
                        DecorSetFloat(vehicle, decorName, currentPreset.defaultWheelsRot[index]);
                }
                else
                {
                    if (currentPreset.defaultWheelsRot[index] != currentPreset.currentWheelsRot[index])
                    {
                        DecorRegister(decorName, 1);
                        DecorSetFloat(vehicle, decorName, currentPreset.defaultWheelsRot[index]);
                    }
                }

                decorName = decorOffsetPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                {
                    float value = DecorGetFloat(vehicle, decorName);
                    if (value != currentPreset.currentWheelsOffset[index])
                        DecorSetFloat(vehicle, decorName, currentPreset.currentWheelsOffset[index]);
                }
                else
                {
                    if (currentPreset.defaultWheelsOffset[index] != currentPreset.currentWheelsOffset[index])
                    {
                        DecorRegister(decorName, 1);
                        DecorSetFloat(vehicle, decorName, currentPreset.currentWheelsOffset[index]);
                    }
                }

                decorName = decorRotationPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                {
                    float value = DecorGetFloat(vehicle, decorName);
                    if (value != currentPreset.currentWheelsRot[index])
                        DecorSetFloat(vehicle, decorName, currentPreset.currentWheelsRot[index]);
                }
                else
                {
                    if (currentPreset.defaultWheelsOffset[index] != currentPreset.currentWheelsOffset[index])
                    {
                        DecorRegister(decorName, 1);
                        DecorSetFloat(vehicle, decorName, currentPreset.currentWheelsRot[index]);
                    }
                }

            }

            await Task.FromResult(0);
        }

        public vstancerPreset CreatePreset(int vehicle)
        {
            string decorName;

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            float[] defaultWheelsRot = new float[wheelsCount];
            float[] defaultWheelsOffset = new float[wheelsCount];

            for (int index = 0; index < wheelsCount; index++)
            {
                decorName = decorDefaultOffsetPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                    defaultWheelsOffset[index] = DecorGetFloat(vehicle, decorName);
                else
                    defaultWheelsOffset[index] = GetVehicleWheelXOffset(vehicle, index);

                decorName = decorDefaultRotationPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                    defaultWheelsRot[index] = DecorGetFloat(vehicle, decorName);
                else
                    defaultWheelsRot[index] = GetVehicleWheelXrot(vehicle, index);
            }

            vstancerPreset preset = new vstancerPreset(wheelsCount, defaultWheelsRot, defaultWheelsOffset);

            for (int index = 0; index < wheelsCount; index++)
            {
                decorName = decorOffsetPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                    preset.currentWheelsOffset[index] = DecorGetFloat(vehicle, decorName);

                decorName = decorRotationPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorName))
                    preset.currentWheelsRot[index] = DecorGetFloat(vehicle, decorName);
            }

            return preset;
        }

        public async void RefreshLocalPreset()
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

        public async void IterateVehicles()
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
                            RefreshVehicleWithDecor(entity);
                    }
                }
                EndFindVehicle(handle);
            }
            await Task.FromResult(0);
        }

        public async void RefreshVehicleWithDecor(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            for (int index = 0; index < wheelsCount; index++)
            {
                string decorOffsetName = decorOffsetPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorOffsetName))
                {
                    float value = DecorGetFloat(vehicle, decorOffsetName);
                    SetVehicleWheelXOffset(vehicle, index, value);
                }

                string decorRotationName = decorRotationPrefix + index.ToString();
                if (DecorExistOn(vehicle, decorRotationName))
                {
                    float value = DecorGetFloat(vehicle, decorRotationName);
                    SetVehicleWheelXrot(vehicle, index, value);
                }
            }
            await Task.FromResult(0);
        }

        public async void PrintVstancerDecors()
        {
            int vehicle = currentVehicle;

            if (DoesEntityExist(vehicle))
            {
                int wheelsCount = GetVehicleNumberOfWheels(vehicle);
                int netID = NetworkGetNetworkIdFromEntity(vehicle);
                Debug.WriteLine($"Vehicle: {vehicle}, wheelsCount: {wheelsCount}, netID: {netID}");
                for (int index = 0; index < wheelsCount; index++)
                {
                    string decorOffsetName = decorOffsetPrefix + index.ToString();
                    if (DecorExistOn(vehicle, decorOffsetName))
                    {
                        float value = DecorGetFloat(vehicle, decorOffsetName);
                        Debug.WriteLine($"{decorOffsetName}: {value}");
                    }

                    string decorRotationName = decorRotationPrefix + index.ToString();
                    if (DecorExistOn(vehicle, decorRotationName))
                    {
                        float value = DecorGetFloat(vehicle, decorRotationName);
                        Debug.WriteLine($"{decorRotationName}: {value}");
                    }
                }
            }else Debug.WriteLine("VSTANCER: Current vehicle doesn't exist");

            await Task.FromResult(0);
        }

        public async void PrintListVehiclesWithDecors()
        {
            List<int> list = new List<int>();
            int entity = -1;
            int handle = FindFirstVehicle(ref entity);

            if (handle != -1)
            {
                while (FindNextVehicle(handle, ref entity))
                {
                    if (entity != currentVehicle)
                    {
                        
                        int wheelsCount = GetVehicleNumberOfWheels(entity);
                        for (int index = 0; index < wheelsCount; index++)
                        {
                            if (
                                DecorExistOn(entity, decorDefaultOffsetPrefix + index.ToString()) ||
                                DecorExistOn(entity, decorDefaultRotationPrefix + index.ToString()) ||
                                DecorExistOn(entity, decorOffsetPrefix + index.ToString()) ||
                                DecorExistOn(entity, decorRotationPrefix + index.ToString())
                                )
                                list.Add(entity);
                        }
                    }
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
