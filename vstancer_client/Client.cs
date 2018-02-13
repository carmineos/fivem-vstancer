using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private static float editingFactor = 0.01f;
        private static float maxSyncDistance = 150.0f;
        private static int syncCount = 0;

        private static float maxEditing = 0.30f;
        private static int maxSyncCount = 1;
        private static int cooldownSeconds = 30;

        private static bool initialised = false;
        private static Dictionary<int, vstancerPreset> synchedPresets = new Dictionary<int, vstancerPreset>();

        private static int playerID;
        private int playerPed;
        private Vector3 currentCoords;

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
            int countValues = (int)(maxEditing / editingFactor);
            var values = new List<dynamic>();

            if (property == 2 || property == 3)
                defaultValue = -defaultValue;

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

        public void AddMenuSync(UIMenu menu)
        {
            var newitem = new UIMenuItem("Sync Preset", "Syncs the changes with the server.");
            newitem.SetRightBadge(UIMenuItem.BadgeStyle.Lock);
            menu.AddItem(newitem);
            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    if (syncCount < maxSyncCount)
                    {
                        SynchPreset();
                        syncCount++;
                        CoolDown();
                        CitizenFX.Core.UI.Screen.ShowNotification("Vehicle synched");
                    }
                    else
                    {
                        CitizenFX.Core.UI.Screen.ShowNotification(String.Format("You've already synched {0} times in the last {1} seconds", syncCount, cooldownSeconds));
                    }
                    
                }
            };
        }

        public void AddMenuUnsync(UIMenu menu)
        {
            var newitem = new UIMenuItem("Unsync Preset", "Unsyncs the preset with the server.");
            newitem.SetRightBadge(UIMenuItem.BadgeStyle.Alert);
            menu.AddItem(newitem);
            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    if (synchedPresets.ContainsKey(playerID))
                    {
                        TriggerServerEvent("vstancer:clientUnsync");
                        CitizenFX.Core.UI.Screen.ShowNotification("Vehicle unsynched");
                    }
                    else
                    {
                        CitizenFX.Core.UI.Screen.ShowNotification("You are already unsynched");
                    }
                }
            };
        }

        public void AddMenuReset(UIMenu menu)
        {
            var newitem = new UIMenuItem("Reset Default", "Restores locally the default values.");
            //newitem.SetRightBadge(UIMenuItem.BadgeStyle.Tick);
            menu.AddItem(newitem);

            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    currentPreset.ResetDefault();
                    RefreshCurrentPreset();
                    InitialiseMenu();
                    wheelsEditorMenu.Visible = true;
                    
                    CitizenFX.Core.UI.Screen.ShowNotification("Preset resetted");
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
            AddMenuSync(wheelsEditorMenu);
            AddMenuUnsync(wheelsEditorMenu);
            wheelsEditorMenu.MouseEdgeEnabled = false;
            wheelsEditorMenu.ControlDisablingEnabled = false;
            wheelsEditorMenu.MouseControlsEnabled = false;
            _menuPool.RefreshIndex();
        }
        #endregion

        public Client()
        {
            playerID = GetPlayerServerId(PlayerId());

            currentVehicle = 0;
            currentPreset = new vstancerPreset(4, new float[4] { 0, 0, 0, 0 }, new float[4] { 0, 0, 0, 0 });
            InitialiseMenu();

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintDictionary();
            }),false);

            RegisterCommand("vstancer_distance", new Action<int, dynamic>((source, args) =>
            {
                float new_maxSyncDistance = float.Parse(args[0]);
                maxSyncDistance = new_maxSyncDistance;
                Debug.WriteLine("VSTANCER: Received new maxSyncDistance value {0}", new_maxSyncDistance.ToString());
            }), false);

            EventHandlers.Add("vstancer:addPreset", new Action<int, int, float, float, float, float, float, float, float, float>(SaveSynchedPreset));
            EventHandlers.Add("vstancer:removePreset", new Action<int>(RemoveSynchedPreset));

            EventHandlers.Add("vstancer:maxEditing", new Action<float>((new_maxEditing) =>
            {
                maxEditing = new_maxEditing;
                Debug.WriteLine("VSTANCER: Received new maxEditing value {0}", new_maxEditing.ToString());
            }));

            EventHandlers.Add("vstancer:maxSyncCount", new Action<int>((new_maxSyncCount) =>
            {
                maxSyncCount = new_maxSyncCount;
                Debug.WriteLine("VSTANCER: Received new maxSyncCount value {0}", new_maxSyncCount.ToString());
            }));

            EventHandlers.Add("vstancer:cooldown", new Action<int>((new_coolDownSeconds) =>
            {
                cooldownSeconds = new_coolDownSeconds;
                Debug.WriteLine("VSTANCER: Received new coolDownSeconds value {0}", new_coolDownSeconds.ToString());
            }));

            EventHandlers.Add("vstancer:settings", new Action<float,int,int>((new_maxEditing, new_maxSyncCount,new_coolDownSeconds) =>
            {
                maxEditing = new_maxEditing;
                maxSyncCount = new_maxSyncCount;
                cooldownSeconds = new_coolDownSeconds;
                Debug.WriteLine("VSTANCER: Received settings maxEditing={0} maxSyncCount={1} coolDownSeconds={2}", maxEditing, maxSyncCount, cooldownSeconds);
            }));

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

            //CURRENT VEHICLE/PRESET HANDLER
            if (IsPedInAnyVehicle(playerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(playerPed, false);
                
                if (IsThisModelACar((uint)GetEntityModel(vehicle)) && GetPedInVehicleSeat(vehicle, -1) == playerPed && !IsEntityDead(vehicle))
                {
                    if (vehicle != currentVehicle)
                    {
                        //ATTEMPT TO FIX BROKEN DEFAULTS IF CHANGE VEHICLE WHILE SYNCHED
                        /*if (synchedPresets.ContainsKey(playerID))
                        {
                            TriggerServerEvent("vstancer:clientUnsync");
                            while (synchedPresets.ContainsKey(playerID))
                                await Delay(0); //UNSAFE, MAY CAUSE DEADLOCKS >.<
                        }*/

                        currentPreset.ResetDefault();
                        RefreshCurrentPreset();

                        currentPreset = CreatePresetFromVehicle(vehicle);
                        currentVehicle = vehicle;
                        InitialiseMenu();
                    }
                    else
                    {
                        if (synchedPresets.ContainsKey(playerID))
                            currentPreset = synchedPresets[playerID];
                    }

                    if (IsControlJustPressed(1, 167) || IsDisabledControlJustPressed(1, 167)) // TOGGLE MENU VISIBLE
                        wheelsEditorMenu.Visible = !wheelsEditorMenu.Visible;
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

            if (currentPreset.HasBeenEdited)
                RefreshCurrentPreset();

            RefreshSynchedPresets();
            await Task.FromResult(0);
        }

        public static async void CoolDown()
        {
            if(syncCount > 0)
            {
                await Delay(cooldownSeconds*1000);
                syncCount -= 1;
            }
        }

        public static async void RemoveSynchedPreset(int ID)
        {
            if (synchedPresets.ContainsKey(ID))
            {
                bool removed = synchedPresets.Remove(ID);
                if (removed)
                    Debug.WriteLine("VSTANCER: Removed preset for Player ID={0}", ID);
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

        public async void SynchPreset()
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
            Debug.WriteLine("VSTANCER: Sent preset to the server ID={0}", playerID);
            await Task.FromResult(0);
        }

        public static async void SaveSynchedPreset(int ID, int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            vstancerPreset preset = new vstancerPreset(count, currentRotFront, currentRotRear, currentOffFront, currentOffRear, defRotFront, defRotRear, defOffFront, defOffRear);
            synchedPresets[ID] = preset;
            Debug.WriteLine("VSTANCER: Received preset for Player ID={0}", ID);

            await Task.FromResult(0);
        }

        public async void RefreshCurrentPreset()
        {
            if (DoesEntityExist(currentVehicle))
            {
                if (!synchedPresets.ContainsKey(playerID))
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

        public async void RefreshSynchedPresets()
        {
            foreach (int ID in synchedPresets.Keys)
            {
                int player = GetPlayerFromServerId(ID);             
                int ped = GetPlayerPed(player);

                currentCoords = GetEntityCoords(playerPed, true);
                Vector3 coords = GetEntityCoords(ped, true);

                if (Vector3.Distance(currentCoords, coords) <= maxSyncDistance)
                {
                    int vehicle = GetVehiclePedIsIn(ped, false);
                    if (DoesEntityExist(vehicle))
                    {
                        vstancerPreset vehPreset = synchedPresets[ID];
                        for (int index = 0; index < vehPreset.wheelsCount; index++)
                        {
                            SetVehicleWheelXOffset(vehicle, index, vehPreset.currentWheelsOffset[index]);
                            SetVehicleWheelXrot(vehicle, index, vehPreset.currentWheelsRot[index]);
                        }
                    }
                }
            }
            await Task.FromResult(0);
        }

        //WILL BE USED TO PRINT THE STATUS IN THE UI
        public string GetStatus
        {
            get
            {
                if (synchedPresets.ContainsKey(playerID))
                {
                    vstancerPreset preset = synchedPresets[playerID];
                    if (currentPreset == preset)
                        return "~g~Synched";
                    else return "~y~Needs Re-Sync";
                }
                else return "~r~Not Synched";
            }
        }

        public static async void PrintDictionary()
        {
            Debug.WriteLine("VSTANCER: Synched Presets Count={0}", synchedPresets.Count.ToString());
            foreach (int ID in synchedPresets.Keys)
            {
                int player = GetPlayerFromServerId(ID);
                string name = GetPlayerName(player);

                Debug.WriteLine("Player: {0}({1})", name, ID);
            }
            await Task.FromResult(0);
        }
    }
}
