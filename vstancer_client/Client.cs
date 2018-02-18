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
        #region CONFIG
        private static float editingFactor;
        private static float maxSyncDistance;
        private static float maxOffset;
        private static float maxCamber;
        private static long timer;
        private static bool debug;
        private static int toggleMenu;
        #endregion

        private static long lastTime;
        private static bool initialised = false;
        private static Dictionary<int, vstancerPreset> synchedPresets = new Dictionary<int, vstancerPreset>();

        private static int playerID;
        private int playerPed;

        private int currentVehicle;
        private vstancerPreset currentPreset;

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

        /*public UIMenuListItem AddEditingFactorValues(UIMenu menu)
        {
            var values = new List<dynamic>() { 0.001f, 0.01f, 0.1f };
            var newitem = new UIMenuListItem("Editing Factor", values, 1);
            menu.AddItem(newitem);
            menu.OnListChange += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    editingFactor = values[index];
                    InitialiseMenu();
                    wheelsEditorMenu.Visible = true;
                }

            };
            return newitem;
        }*/

        public void AddMenuReset(UIMenu menu)
        {
            var newitem = new UIMenuItem("Reset", "Restores locally the default values.");
            menu.AddItem(newitem);

            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    currentPreset.ResetDefault();
                    //RefreshLocalPreset();

                    InitialiseMenu();
                    wheelsEditorMenu.Visible = true;
                    
                    CitizenFX.Core.UI.Screen.ShowNotification("Default values restored");
                }
            };
        }

        public void InitialiseMenu()
        {
            _menuPool = new MenuPool();
            wheelsEditorMenu = new UIMenu("Wheels Editor", "~b~Track Width & Camber", new PointF(Screen.Width, 0));
            _menuPool.Add(wheelsEditorMenu);
            //editingFactorGUI = AddEditingFactorValues(wheelsEditorMenu);
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

            playerID = GetPlayerServerId(PlayerId());

            currentVehicle = 0;
            currentPreset = new vstancerPreset(4, new float[4] { 0, 0, 0, 0 }, new float[4] { 0, 0, 0, 0 });
            InitialiseMenu();

            EventHandlers.Add("vstancer:addPreset", new Action<int, int, float, float, float, float, float, float, float, float>(SaveSynchedPreset));
            EventHandlers.Add("vstancer:removePreset", new Action<int>(RemoveSynchedPreset));
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

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintDictionary();
            }), false);
            RegisterCommand("vstancer_distance", new Action<int, dynamic>((source, args) =>
            {
                maxSyncDistance = float.Parse(args[0]);
                Debug.WriteLine("VSTANCER: Received new maxSyncDistance value {0}", maxSyncDistance.ToString());
            }), false);
            RegisterCommand("vstancer_debug", new Action<int, dynamic>((source, args) =>
            {
                debug = bool.Parse(args[0]);
                Debug.WriteLine("VSTANCER: Received new debug value {0}", debug.ToString());
            }), false);

            Tick += OnTick;
        }

        public async Task OnTick()
        {
            _menuPool.ProcessMenus();

            playerPed = GetPlayerPed(-1);

            //FIRST TICK
            if (!initialised)
            {
                initialised = true;
                TriggerServerEvent("vstancer:clientReady");
            }
            
            //RESYNC EACH TIMER
            if ((GetGameTimer() - lastTime) > timer)
            {
                if (currentPreset.HasBeenEdited)
                {
                    bool isSynched = synchedPresets.ContainsKey(playerID);
                    if (!isSynched || (isSynched && !synchedPresets[playerID].Equals(currentPreset)))
                        Synch();
                }
                else
                    StopSync();

                lastTime = GetGameTimer();
            }

            //CURRENT VEHICLE/PRESET HANDLER
            if (IsPedInAnyVehicle(playerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(playerPed, false);

                if (IsThisModelACar((uint)GetEntityModel(vehicle)) && GetPedInVehicleSeat(vehicle, -1) == playerPed && !IsEntityDead(vehicle))
                {
                    if (vehicle != currentVehicle)
                    {
                        if (currentPreset.HasBeenEdited)
                        {
                            currentPreset.ResetDefault();
                            RefreshLocalPreset(); //FORCED
                            Synch(); //FORCED
                        }

                        currentPreset = CreatePresetFromVehicle(vehicle);
                        currentVehicle = vehicle;
                        InitialiseMenu();
                    }

                    if (IsControlJustPressed(1, toggleMenu) || IsDisabledControlJustPressed(1, toggleMenu)) // TOGGLE MENU VISIBLE
                        wheelsEditorMenu.Visible = !wheelsEditorMenu.Visible;
                }
                else
                {
                    //TEMP FIX TO PASSENGER SPAMMING IF HAD AN EDITED VEHICLE
                    if (currentPreset.HasBeenEdited)
                    {
                        currentPreset.ResetDefault();
                        RefreshLocalPreset(); //FORCED
                        Synch(); //FORCED
                    }
                }
            }
            else
            {
                //CLOSE MENU IF NOT IN VEHICLE
                if (wheelsEditorMenu.Visible)
                    wheelsEditorMenu.Visible = false;

                /*//RESET IF PED EXITS FROM VEHICLE
                if (currentPreset.HasBeenEdited)
                {
                    currentPreset.ResetDefault();
                    RefreshCurrentPreset();
                }*/
            }

            RefreshLocalPreset();
            foreach (int ID in synchedPresets.Keys.Where(key => key != playerID))
                RefreshSynchedPreset(ID);

            await Task.FromResult(0);
        }

        public async void RemoveSynchedPreset(int ID)
        {
            if (synchedPresets.ContainsKey(ID))
            {
                synchedPresets[ID].ResetDefault(); //FORCED
                RefreshSynchedPreset(ID); //FORCED

                bool removed = synchedPresets.Remove(ID);
                if (removed)
                {
                    if (debug)
                        Debug.WriteLine("VSTANCER: Removed preset for Player ID={0}", ID);
                }    
            }
            await Task.FromResult(0);
        }

        public vstancerPreset CreatePresetFromVehicle(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            float[] defaultWheelsRot = new float[wheelsCount];
            float[] defaultWheelsOffset = new float[wheelsCount];

            for (int index = 0; index < wheelsCount; index++)
            {
                defaultWheelsRot[index] = GetVehicleWheelXrot(vehicle, index);
                defaultWheelsOffset[index] = GetVehicleWheelXOffset(vehicle, index);
            }
            return (new vstancerPreset(wheelsCount, defaultWheelsRot, defaultWheelsOffset));
        }

        public async void StopSync()
        {
            if (synchedPresets.ContainsKey(playerID))
            {
                TriggerServerEvent("vstancer:clientUnsync");
            }
            await Task.FromResult(0);
        }

        public async void Synch()
        {
            int frontCount = currentPreset.frontCount;
            TriggerServerEvent("vstancer:clientSync",
            currentPreset.wheelsCount,
            currentPreset.currentWheelsRot[0],
            currentPreset.currentWheelsRot[frontCount],
            currentPreset.currentWheelsOffset[0],
            currentPreset.currentWheelsOffset[frontCount],
            currentPreset.defaultWheelsRot[0],
            currentPreset.defaultWheelsRot[frontCount],
            currentPreset.defaultWheelsOffset[0],
            currentPreset.defaultWheelsOffset[frontCount]
            );

            if (debug)
                Debug.WriteLine("VSTANCER: Sent preset to the server ID={0}", playerID);

            await Task.FromResult(0);
        }

        public static async void SaveSynchedPreset(int ID, int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            vstancerPreset preset = new vstancerPreset(count, currentRotFront, currentRotRear, currentOffFront, currentOffRear, defRotFront, defRotRear, defOffFront, defOffRear);
            synchedPresets[ID] = preset;

            if (debug)
                Debug.WriteLine("VSTANCER: Received preset for Player ID={0}", ID);

            await Task.FromResult(0);
        }

        public async void RefreshLocalPreset()
        {
            if (DoesEntityExist(currentVehicle))
            {
                for (int index = 0; index < currentPreset.wheelsCount; index++)
                {
                    SetVehicleWheelXOffset(currentVehicle, index, currentPreset.currentWheelsOffset[index]);
                    SetVehicleWheelXrot(currentVehicle, index, currentPreset.currentWheelsRot[index]);
                }
            }
            await Task.FromResult(0);
        }

        public async void RefreshSynchedPreset(int ID)
        {
            int player = GetPlayerFromServerId(ID);
            int ped = GetPlayerPed(player);

            Vector3 currentCoords = GetEntityCoords(playerPed, true);
            Vector3 coords = GetEntityCoords(ped, true);

            if (Vector3.Distance(currentCoords, coords) <= maxSyncDistance)
            {
                int vehicle = GetVehiclePedIsIn(ped, false);
                if (DoesEntityExist(vehicle) && GetPedInVehicleSeat(vehicle, -1) == ped)
                {
                    vstancerPreset vehPreset = synchedPresets[ID];
                    for (int index = 0; index < vehPreset.wheelsCount; index++)
                    {
                        SetVehicleWheelXOffset(vehicle, index, vehPreset.currentWheelsOffset[index]);
                        SetVehicleWheelXrot(vehicle, index, vehPreset.currentWheelsRot[index]);
                    }
                }
            }
            await Task.FromResult(0);
        }

        public static async void PrintDictionary()
        {
            Debug.WriteLine("VSTANCER: Synched Presets Count={0}", synchedPresets.Count.ToString());
            Debug.WriteLine("VSTANCER: Settings maxOffset={0} maxCamber={1} timer={2} debug={3} maxSyncDistance={4}", maxOffset, maxCamber, timer, debug, maxSyncDistance);
            foreach (int ID in synchedPresets.Keys)
            {
                int player = GetPlayerFromServerId(ID);
                string name = GetPlayerName(player);

                Debug.WriteLine("Player: {0}({1})", name, ID);
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
            catch (Exception e)
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
