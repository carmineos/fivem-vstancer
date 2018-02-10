using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NativeUI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using vstancer_shared;

namespace vstancer_client
{

    public class Client : BaseScript
    {
        private static float editingFactor = 0.01f;
        private static float maxEditing = 0.30f;
        private static float maxSyncDistance = 150.0f;

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
                    AddPreset();
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
            var newitem = new UIMenuItem("Sync Preset", "Syncs the presets with the server.");
            newitem.SetRightBadge(UIMenuItem.BadgeStyle.Tick);
            menu.AddItem(newitem);
            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    SendPreset(currentPreset);
                    CitizenFX.Core.UI.Screen.ShowNotification("Vehicle synched");
                }
            };
        }

        public void AddMenuReset(UIMenu menu)
        {
            var newitem = new UIMenuItem("Reset Default", "Restores default values.");
            newitem.SetRightBadge(UIMenuItem.BadgeStyle.Alert);
            menu.AddItem(newitem);

            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    currentPreset.ResetDefault();         

                    InitialiseMenu();
                    wheelsEditorMenu.Visible = true;
                    
                    TriggerServerEvent("ClientRemovedPreset");
                    RemovePreset(playerID); //IN CASE IT WASN'S SYNCHED
                    
                    CitizenFX.Core.UI.Screen.ShowNotification("Preset resetted");
                }
            };
        }

        public void InitialiseMenu()
        {
            _menuPool = new MenuPool();
            wheelsEditorMenu = new UIMenu("Wheels Editor", "~b~Track Width & Camber");
            _menuPool.Add(wheelsEditorMenu);
            //editingFactorGUI = AddEditingFactorValues(wheelsEditorMenu);
            frontOffsetGUI = AddMenuListValues(wheelsEditorMenu, "Front Track Width", 0, currentPreset.currentWheelsOffset[0]);
            frontRotationGUI = AddMenuListValues(wheelsEditorMenu, "Front Camber", 2, currentPreset.currentWheelsRot[0]);
            rearOffsetGUI = AddMenuListValues(wheelsEditorMenu, "Rear Track Width", 1, currentPreset.currentWheelsOffset[2]);
            rearRotationGUI = AddMenuListValues(wheelsEditorMenu, "Rear Camber", 3, currentPreset.currentWheelsRot[2]);

            AddMenuSync(wheelsEditorMenu);
            AddMenuReset(wheelsEditorMenu);
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

            EventHandlers.Add("BroadcastAddPreset", new Action<int, int, float, float, float, float, float, float, float, float>(SavePresetFromServer));
            EventHandlers.Add("BroadcastRemovePreset", new Action<int>(RemovePreset));
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
                TriggerServerEvent("ClientWheelsEditorReady");
            }

            //CURRENT VEHICLE/PRESET HANDLER
            if (IsPedInAnyVehicle(playerPed, false))
            {
                int vehicle = GetVehiclePedIsIn(playerPed, false);
                if (IsThisModelACar((uint)GetEntityModel(vehicle)) && GetPedInVehicleSeat(vehicle, -1) == playerPed && !IsEntityDead(vehicle))
                {
                    GetCurrentVehicle();
                    GetPreset();

                    if (IsControlJustPressed(1, 167) || IsDisabledControlJustPressed(1, 167)) // TOGGLE MENU VISIBLE
                        wheelsEditorMenu.Visible = !wheelsEditorMenu.Visible;
                }
            }
            else
            {
                //CLOSE MENU IF NOT IN VEHICLE
                if (wheelsEditorMenu.Visible)
                    wheelsEditorMenu.Visible = false;

                if(currentPreset.HasBeenEdited)
                    currentPreset.ResetDefault();

                currentVehicle = 0;
                RemovePreset(playerID);
                InitialiseMenu();
            }

            RefreshEntities();
            await Task.FromResult(0);
        }

        public async void GetCurrentVehicle()
        {
            int vehicle = GetVehiclePedIsIn(playerPed, false);
            if (vehicle != currentVehicle)
            {
                RemovePreset(playerID);
                currentPreset.ResetDefault();
                currentPreset = CreatePresetFromVehicle(vehicle);
                
                InitialiseMenu();
            }
            currentVehicle = vehicle;
            await Task.FromResult(0);
        }

        public async void GetPreset()
        {
            if (synchedPresets.ContainsKey(playerID))
                currentPreset = synchedPresets[playerID];
            else
                currentPreset = CreatePresetFromVehicle(currentVehicle);

            await Task.FromResult(0);
        }

        public async void AddPreset()
        {
            if (currentPreset.HasBeenEdited)
            {
                synchedPresets[playerID] = currentPreset;
            }
            await Task.FromResult(0);
        }

        public static async void RemovePreset(int ID)
        {
            if (synchedPresets.ContainsKey(ID))
            {
                bool removed = synchedPresets.Remove(ID);
                if (removed)
                    Debug.WriteLine("WHEELS EDITOR: REMOVED PRESET ID={0} FROM DICTIONARY ", ID);
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

        public static async void SendPreset(vstancerPreset preset)
        {
            if (synchedPresets.ContainsKey(playerID))
            {
                int frontCount = preset.frontCount;

                TriggerServerEvent("ClientAddedPreset",
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
                Debug.WriteLine("WHEELS EDITOR: PRESET SENT TO SERVER");
            }
            await Task.FromResult(0);
        }

        public static async void SavePresetFromServer(int ID, int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            vstancerPreset preset = new vstancerPreset(count, currentRotFront, currentRotRear, currentOffFront, currentOffRear, defRotFront, defRotRear, defOffFront, defOffRear);
            synchedPresets[ID] = preset;
            Debug.WriteLine("WHEELS EDITOR: RECEIVED PRESET FROM SERVER ID={0}", ID);

            await Task.FromResult(0);
        }

        public async void RefreshEntities()
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

        public static async void PrintDictionary()
        {
            Debug.WriteLine("WHEELS EDITOR: CLIENT'S DICTIONARY LENGHT={0} ", synchedPresets.Count.ToString());
            foreach (int player in synchedPresets.Keys)
            {
                Debug.WriteLine("WHEELS EDITOR: PRESET FOR PLAYER ({0})", player);
            }
            await Task.FromResult(0);
        }
    }
}
