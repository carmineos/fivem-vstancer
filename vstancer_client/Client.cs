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
        // Config fields
        private static float editingFactor;
        private static float maxSyncDistance;
        private static float maxOffset;
        private static float maxCamber;
        private static long timer;
        private static bool debug;
        private static int toggleMenu;

        private static long lastTime;
        private static bool initialised = false;
        private static Dictionary<int, vstancerPreset> synchedPresets = new Dictionary<int, vstancerPreset>();

        private int playerPed;

        private int currentVehicle;
        private vstancerPreset currentPreset;
        private int currentVehicleNetID;

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

            //playerID = GetPlayerServerId(PlayerId());

            currentVehicle = 0;
            currentPreset = new vstancerPreset(4, new float[4] { 0, 0, 0, 0 }, new float[4] { 0, 0, 0, 0 });
            InitialiseMenu();

            EventHandlers.Add("vstancer:addPreset", new Action<int, int, float, float, float, float, float, float, float, float>(SavePreset));
            EventHandlers.Add("vstancer:removePreset", new Action<int>(RemovePreset));
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

            // On first tick notify the server that the client is ready to receive info
            if (!initialised)
            {
                initialised = true;
                TriggerServerEvent("vstancer:clientReady");
            }
            
            // Check if the server has to be notified about the status of the current preset
            if ((GetGameTimer() - lastTime) > timer)
            {
                // If current preset hasn't default values
                if (currentPreset.HasBeenEdited)
                {
                    bool isSynched = synchedPresets.ContainsKey(currentVehicleNetID);
                    if (!isSynched || (isSynched && !synchedPresets[currentVehicleNetID].Equals(currentPreset)))
                        NotifyServerAdd(currentVehicleNetID, currentPreset);

                    /** DEBUG
                    #region DEBUG
                    if (!isSynched)
                    {
                        NotifyServerAdd(CurrentVehicleNetID, currentPreset);
                        Debug.WriteLine("Is not synched");
                    }
                    else
                    {
                        if (!synchedPresets[CurrentVehicleNetID].Equals(currentPreset))
                        {
                            Debug.WriteLine("Synched but not equal");
                            Debug.WriteLine("synched: {0}", synchedPresets[CurrentVehicleNetID].ToString());
                            Debug.WriteLine("current: {0}", currentPreset.ToString());
                            NotifyServerAdd(CurrentVehicleNetID, currentPreset);
                        }

                    }
                    #endregion
                    */
                }
                else // Probably the preset has been reset
                    NotifyServerRemove(currentVehicleNetID);

                lastTime = GetGameTimer();
            }

            //CURRENT VEHICLE/PRESET HANDLER
            if (IsPedInAnyVehicle(playerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(playerPed, false);

                if (IsThisModelACar((uint)GetEntityModel(vehicle)) && GetPedInVehicleSeat(vehicle, -1) == playerPed && !IsEntityDead(vehicle))
                {
                    int netID = NetworkGetNetworkIdFromEntity(vehicle);

                    if (vehicle != currentVehicle)
                    {
                        if (synchedPresets.ContainsKey(netID))
                            currentPreset = synchedPresets[netID];
                        else
                            currentPreset = CreatePresetFromVehicle(vehicle);

                        currentVehicleNetID = netID;
                        currentVehicle = vehicle;
                        InitialiseMenu();
                    }

                    if (IsControlJustPressed(1, toggleMenu) || IsDisabledControlJustPressed(1, toggleMenu)) // TOGGLE MENU VISIBLE
                        wheelsEditorMenu.Visible = !wheelsEditorMenu.Visible;
                }
                else
                {
                    //If current vehicle isn't a car or player isn't driving current vehicle or vehicle is dead

                }
            }
            else
            {
                //CLOSE MENU IF NOT IN VEHICLE
                if (wheelsEditorMenu.Visible)
                    wheelsEditorMenu.Visible = false;
            }


            // Current preset is always refreshed
            RefreshLocalPreset();

            // Refresh entities of all the local netIDs synched with the server dictionary
            IEnumerable<int> refresh = synchedPresets.Keys.Where(key => key != currentVehicleNetID);
            foreach (int ID in refresh)
            {
                if (NetworkDoesNetworkIdExist(ID))
                    UpdateEntityByNetID(ID);
                else // If any ID doesn't exist then notify the server to remove it
                    NotifyServerRemove(ID);
            }

            await Task.FromResult(0);
        }

        /// <summary>
        /// Removes the <paramref name="ID"/> from the local dictionary
        /// </summary>
        /// <param name="ID"></param>
        public async void RemovePreset(int ID)
        {
            if (synchedPresets.ContainsKey(ID))
            {
                // If the netID exists, it's probably a reset, and if no player is in the vehicle then it requires to be forced
                if (NetworkDoesNetworkIdExist(ID))
                {
                    synchedPresets[ID].ResetDefault(); //FORCED
                    UpdateEntityByNetID(ID); //FORCED
                }

                bool removed = synchedPresets.Remove(ID);
                if (removed)
                {
                    if (debug)
                        Debug.WriteLine("VSTANCER: Removed preset for netID={0}", ID);
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

        /// <summary>
        /// Triggers the event to tell the server to remove a netID from the dictionary
        /// </summary>
        /// <param name="netID">The netID the server has to remove</param>
        public async void NotifyServerRemove(int netID)
        {
            if (synchedPresets.ContainsKey(netID))
            {
                TriggerServerEvent("vstancer:clientUnsync", netID);
            }
            await Task.FromResult(0);
        }

        /// <summary>
        /// Triggers the event to tell the server to add a netID in the dictionary
        /// </summary>
        /// <param name="netID">The netID the server has to add</param>
        /// <param name="preset">The preset linked to the <paramref name="netID"/></param>
        public async void NotifyServerAdd(int netID , vstancerPreset preset)
        {
            int frontCount = currentPreset.frontCount;
            TriggerServerEvent("vstancer:clientSync",
                netID,
            preset.wheelsCount,
            preset.currentWheelsRot[0],
            preset.currentWheelsRot[frontCount],
            preset.currentWheelsOffset[0],
            preset.currentWheelsOffset[frontCount],
            preset.defaultWheelsRot[0],
            preset.defaultWheelsRot[frontCount],
            preset.defaultWheelsOffset[0],
            preset.defaultWheelsOffset[frontCount]
            );

            if (debug)
                Debug.WriteLine("VSTANCER: Sent preset to the server netID={0} Entity={1} EntityFromNetID={2}", currentVehicleNetID, currentVehicle, NetworkGetEntityFromNetworkId(currentVehicleNetID));

            await Task.FromResult(0);
        }

        /// <summary>
        /// Creates a preset from values reived from the server and saves it in the local dictionary
        /// </summary>
        /// <param name="ID">The netID linked to the preset</param>
        public static async void SavePreset(int ID, int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            vstancerPreset preset = new vstancerPreset(count, currentRotFront, currentRotRear, currentOffFront, currentOffRear, defRotFront, defRotRear, defOffFront, defOffRear);
            synchedPresets[ID] = preset;

            if (debug)
                Debug.WriteLine("VSTANCER: Received preset for netID={0} EntityFromNetID={1}", ID, NetworkGetEntityFromNetworkId(ID));

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

        public async void UpdateEntityByNetID(int ID)
        {
            int entity = NetworkGetEntityFromNetworkId(ID);

            if (DoesEntityExist(entity))
            {
                Vector3 currentCoords = GetEntityCoords(playerPed, true);
                Vector3 entityCoords = GetEntityCoords(entity, true);

                if (Vector3.Distance(currentCoords, entityCoords) <= maxSyncDistance)
                {
                    vstancerPreset vehPreset = synchedPresets[ID];
                    for (int index = 0; index < vehPreset.wheelsCount; index++)
                    {
                        SetVehicleWheelXOffset(entity, index, vehPreset.currentWheelsOffset[index]);
                        SetVehicleWheelXrot(entity, index, vehPreset.currentWheelsRot[index]);
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

                Debug.WriteLine("Preset: netID={0} EntityFromNetID={1}", ID, NetworkGetEntityFromNetworkId(ID));
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
