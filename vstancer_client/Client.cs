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
        private static bool initialised = false;
        private static Dictionary<int, vstancerPreset> synchedPresets = new Dictionary<int, vstancerPreset>();

        private int currentVehicle;
        private int previousVehicle;
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
            float maxValue = 0.30f;
            int countValues = (int)(maxValue / editingFactor);
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
                    int netID = NetworkGetNetworkIdFromEntity(currentVehicle);
                    //Debug.WriteLine("WHEELS EDITOR: Trying to send netID={0}, local={1}", netID, currentVehicle);

                    if (netID != 0)
                    {
                        SendPreset(currentVehicle, currentPreset);
                        CitizenFX.Core.UI.Screen.ShowNotification("Vehicle synched");
                    }
                    else
                        CitizenFX.Core.UI.Screen.ShowNotification("This vehicle can't be synched");
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
                    CitizenFX.Core.UI.Screen.ShowNotification("Preset resetted");
                    InitialiseMenu();
                    wheelsEditorMenu.Visible = true;
                }
            };
        }

        public void InitialiseMenu()
        {
            _menuPool = new MenuPool();
            wheelsEditorMenu = new UIMenu("Wheels Editor", "~b~Offset & Rotation");
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

            currentVehicle = -1;
            previousVehicle = -1;
            currentPreset = new vstancerPreset(4, new float[4] { 0, 0, 0, 0 }, new float[4] { 0, 0, 0, 0 });
            InitialiseMenu();

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintDictionary();
            }),false);

            EventHandlers.Add("syncWheelEditorPreset", new Action<int, int, float, float, float, float, float, float, float, float>(SavePresetFromServer));

            Tick += OnTick;
        }

        public async Task OnTick()
        {
            _menuPool.ProcessMenus();

            Ped playerPed = Game.PlayerPed;

            //FIRST TICK
            if (!initialised)
            {
                initialised = true;
                TriggerServerEvent("clientWheelsEditorReady");
            }

            //VEHICLE SPAWNER HANDLER
            if (IsControlJustPressed(1, 168))
            {
                int vehicle = await SpawnVehicle();
                if (vehicle == -1)
                    CitizenFX.Core.UI.Screen.ShowNotification("Spawning Error");
            }

            //CLOSE MENU IF NOT IN VEHICLE
            if (!playerPed.IsInVehicle() && wheelsEditorMenu.Visible)
                wheelsEditorMenu.Visible = false;

            //CURRENT VEHICLE/PRESET HANDLER
            if (playerPed.IsInVehicle() && playerPed.CurrentVehicle.Model.IsCar &&
                playerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == playerPed && playerPed.CurrentVehicle.IsAlive)
            {
                int netID = NetworkGetNetworkIdFromEntity(playerPed.CurrentVehicle.Handle);
                if (synchedPresets.ContainsKey(netID))
                    currentPreset = synchedPresets[netID];
                else
                    currentPreset = CreatePresetFromVehicle(playerPed.CurrentVehicle.Handle);

                if (playerPed.CurrentVehicle.Handle != currentVehicle)
                {
                    previousVehicle = currentVehicle;
                    InitialiseMenu();
                }
                currentVehicle = playerPed.CurrentVehicle.Handle;

                    if (IsControlJustPressed(1, 167) || IsDisabledControlJustPressed(1, 167)) // TOGGLE MENU VISIBLE
                    wheelsEditorMenu.Visible = !wheelsEditorMenu.Visible;
            }
            RefreshEntities();
            //RefreshCurrentVehicleOnly();
        }

        public async void AddPreset()
        {
            if (currentPreset.HasBeenEdited)
            {
                SetEntityAsMissionEntity(currentVehicle, true, true);
                int netID = NetworkGetNetworkIdFromEntity(currentVehicle);

                if (netID == 0) //TRY TO FIX THIS SHIT
                {
                    Debug.WriteLine("ENTITY CONTROL: " + NetworkRequestControlOfEntity(currentVehicle).ToString());
                    NetworkRegisterEntityAsNetworked(currentVehicle);
                    Debug.WriteLine("HAS A NETWORK ID: "+ NetworkDoesEntityExistWithNetworkId(currentVehicle).ToString());
                    Debug.WriteLine("WHEELS EDITOR: EDITING DISABLED netID={0}, local={1}", netID, currentVehicle);
                } 

                //SetNetworkIdExistsOnAllMachines(netID,true);

                if (netID != 0 && !synchedPresets.ContainsKey(netID))
                    synchedPresets.Add(netID, currentPreset);
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

        public static async void SendPreset(int vehicle, vstancerPreset preset)
        {
            //TEMP DOUBLE CHECK
            int netID = NetworkGetNetworkIdFromEntity(vehicle);
            
            if (netID != 0)
            {
                TriggerServerEvent("sendWheelEditorPreset",
                netID,
                preset.wheelsCount,
                preset.currentWheelsRot[0],
                preset.currentWheelsRot[2],
                preset.currentWheelsOffset[0],
                preset.currentWheelsOffset[2],
                preset.defaultWheelsRot[0],
                preset.defaultWheelsRot[2],
                preset.defaultWheelsOffset[0],
                preset.defaultWheelsOffset[2]
                );
                Debug.WriteLine("WHEELS EDITOR: PRESET SENT TO SERVER netID={0}, local={1}", netID, vehicle);
            }
            await Task.FromResult(0);
        }

        public static async void SavePresetFromServer(int netID, int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            vstancerPreset preset = new vstancerPreset(count, currentRotFront, currentRotRear, currentOffFront, currentOffRear, defRotFront, defRotRear, defOffFront, defOffRear);
            synchedPresets[netID] = preset;
            Debug.WriteLine("WHEELS EDITOR: RECEIVED PRESET FROM SERVER netID={0}", netID);

            await Task.FromResult(0);
        }

        public static async void RefreshEntities()
        {
            foreach (int netID in synchedPresets.Keys)
            {
                int veh = NetworkGetEntityFromNetworkId(netID);
                if (DoesEntityExist(veh))
                {
                    vstancerPreset vehPreset = synchedPresets[netID];

                    for (int index = 0; index < vehPreset.wheelsCount; index++)
                    {
                        SetVehicleWheelXOffset(veh, index, vehPreset.currentWheelsOffset[index]);
                        SetVehicleWheelXrot(veh, index, vehPreset.currentWheelsRot[index]);
                    }
                }
            }
            await Task.FromResult(0);
        }

        public async void RefreshCurrentVehicleOnly()
        {
            if (currentPreset != null && currentPreset.HasBeenEdited)
            {
                int netID = NetworkGetNetworkIdFromEntity(currentVehicle);
                if (!synchedPresets.ContainsKey(netID))
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

        public static async void PrintDictionary()
        {
            Debug.WriteLine("WHEELS EDITOR: CLIENT'S DICTIONARY LENGHT={0} ", synchedPresets.Count.ToString());
            foreach (int netID in synchedPresets.Keys)
            {
                int handle = NetworkGetEntityFromNetworkId(netID);
                int modelHash = GetEntityModel(handle);
                string displayName = GetDisplayNameFromVehicleModel((uint)modelHash);
                Debug.WriteLine("WHEELS EDITOR: PRESET Local={0}, netID={1}, name={2}", handle, netID, displayName);
            }
            await Task.FromResult(0);
        }

        private async Task<int> SpawnVehicle()
        {
            AddTextEntry("WE_SPAWN_TEST", "Enter model name");
            DisplayOnscreenKeyboard(1, "WE_SPAWN_TEST", "", "", "", "", "", 128);
            while (UpdateOnscreenKeyboard() != 1 && UpdateOnscreenKeyboard() != 2) await BaseScript.Delay(0);
            string modelName = GetOnscreenKeyboardResult();

            int playerPed = GetPlayerPed(-1);
            Vector3 pedPosition = GetEntityCoords(playerPed, true);
            uint model = (uint)GetHashKey(modelName);

            if (IsModelInCdimage(model) && IsModelAVehicle(model))
            {
                RequestModel(model);
                while (!HasModelLoaded(model))
                    await Delay(0);
                int vehicle = CreateVehicle(model, pedPosition.X, pedPosition.Y, pedPosition.Z, 0.0f, true, false);

                SetEntityAsMissionEntity(vehicle, true, true);
                SetVehicleOnGroundProperly(vehicle);
                SetVehicleHasBeenOwnedByPlayer(playerPed, true);
                SetPedIntoVehicle(playerPed, vehicle, -1);
                return vehicle;
            }
            else return -1;
        }
    }
}
