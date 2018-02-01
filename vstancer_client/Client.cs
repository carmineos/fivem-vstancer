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
        private static Dictionary<int, vstancerPreset> synchedPresets = new Dictionary<int, vstancerPreset>();

        private int currentVehicle;
        private vstancerPreset currentPreset;
        private static bool firstTick = true;
        private MenuPool _menuPool;


        public void AddMenuListFloat(UIMenu menu, string name, int property)
        {
            float maxValue = 0.30f;
            int countValues = (int)(maxValue / editingFactor);
            var values = new List<dynamic>();

            //POSITIVE VALUES
            for (int i = 0; i <= countValues; i++)
                values.Add((i * editingFactor));

            //NEGATIVE VALUES
            for (int i = countValues; i >= 1; i--)
                values.Add((-i * editingFactor));

            //FIX 0.0999999999 WHY??
            values[10] = 0.10f;
            values[51] = -0.10f;

            var newitem = new UIMenuListItem(name, values, 0);
            menu.AddItem(newitem);
            menu.OnListChange += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    switch (property)
                    {
                        case 0:
                            currentPreset.currentWheelsOffset[0] = currentPreset.defaultWheelsOffset[0] + values[index];
                            currentPreset.currentWheelsOffset[1] = currentPreset.defaultWheelsOffset[1] - values[index];
                            break;
                        case 1:
                            currentPreset.currentWheelsOffset[2] = currentPreset.defaultWheelsOffset[2] + values[index];
                            currentPreset.currentWheelsOffset[3] = currentPreset.defaultWheelsOffset[3] - values[index];
                            break;
                        case 2:
                            currentPreset.currentWheelsRot[0] = values[index];
                            currentPreset.currentWheelsRot[1] = -values[index];
                            break;
                        case 3:
                            currentPreset.currentWheelsRot[2] = values[index];
                            currentPreset.currentWheelsRot[3] = -values[index];
                            break;
                    }
                }

            };
        }

        public void AddMenuSync(UIMenu menu)
        {
            var newitem = new UIMenuItem("Sync Preset", "Syncs the presets with the server.");
            newitem.SetRightBadge(UIMenuItem.BadgeStyle.Tick);
            menu.AddItem(newitem);
            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == newitem)
                {
                    SendPreset(currentVehicle, currentPreset);
                    CitizenFX.Core.UI.Screen.ShowNotification("Synched");
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
                    CitizenFX.Core.UI.Screen.ShowNotification("Resetted");
                }
            };

            menu.OnIndexChange += (sender, index) =>
            {
                if (sender.MenuItems[index] == newitem)
                    newitem.SetLeftBadge(UIMenuItem.BadgeStyle.None);
            };
        }

        public Client()
        {
            _menuPool = new MenuPool();
            var wheelsEditorMenu = new UIMenu("Wheels Editor", "~b~Offset & Rotation");
            _menuPool.Add(wheelsEditorMenu);
            AddMenuListFloat(wheelsEditorMenu, "Front Offset", 0);
            AddMenuListFloat(wheelsEditorMenu, "Rear Offset", 1);
            AddMenuListFloat(wheelsEditorMenu, "Front Rotation", 2);
            AddMenuListFloat(wheelsEditorMenu, "Rear Rotation", 3);
            AddMenuSync(wheelsEditorMenu);
            AddMenuReset(wheelsEditorMenu);
            wheelsEditorMenu.MouseEdgeEnabled = false;
            wheelsEditorMenu.MouseControlsEnabled = false;
            _menuPool.RefreshIndex();


            Tick += new Func<Task>(async delegate
            {
                _menuPool.ProcessMenus();

                Ped playerPed = Game.PlayerPed;

                //FIRST TICK
                if (firstTick)
                {
                    firstTick = false;
                    TriggerServerEvent("clientWheelsEditorReady");
                }

                //VEHICLE SPAWNER HANDLER
                if (IsControlJustPressed(1, 168))
                {
                    int vehicle = await SpawnVehicle();
                    if (vehicle != -1)
                        currentVehicle = vehicle;
                    else
                        CitizenFX.Core.UI.Screen.ShowNotification("Spawning Error");
                }

                //CLOSE MENU IF NOT IN VEHICLE
                if (!playerPed.IsInVehicle() && wheelsEditorMenu.Visible)
                    wheelsEditorMenu.Visible = false;

                //CURRENT VEHICLE/PRESET HANDLER
                if (playerPed.IsInVehicle() && playerPed.CurrentVehicle.Model.IsCar &&
                    playerPed.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == playerPed && playerPed.CurrentVehicle.IsAlive)
                {
                    currentVehicle = playerPed.CurrentVehicle.Handle;

                    int netID = NetworkGetNetworkIdFromEntity(currentVehicle);
                    if (netID != 0)
                    {
                        if (!synchedPresets.ContainsKey(netID))
                        {
                            currentPreset = CreatePresetFromVehicle(currentVehicle);
                            synchedPresets.Add(netID, currentPreset);
                        }
                        else
                            currentPreset = synchedPresets[netID];
                    }
                    else //SCRIPT DOESN'T OWN THE ENTITY
                    {
                        currentPreset = CreatePresetFromVehicle(currentVehicle);
                        SetEntityAsNoLongerNeeded(ref currentVehicle);
                        SetEntityAsMissionEntity(currentVehicle, true, true);
                        netID = NetworkGetNetworkIdFromEntity(currentVehicle);
                    }

                    if (IsControlJustPressed(1, 167) || IsDisabledControlJustPressed(1, 167)) // TOGGLE MENU VISIBLE
                    {
                        wheelsEditorMenu.Visible = !wheelsEditorMenu.Visible;
                        //PrintDictionary();
                    }
                }
                EventHandlers.Add("syncWheelEditorPreset", new Action<int, int, float, float, float, float, float, float, float, float>(StorePreset));
                RefreshEntities();
            });
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
            /**DEBUG
            //SetNetworkIdExistsOnAllMachines(netID, true);
            //NetworkRegisterEntityAsNetworked(currentVehicle);
            SetEntityAsNoLongerNeeded(ref currentVehicle);
            SetEntityAsMissionEntity(currentVehicle, true, true);

            bool visible = IsEntityVisibleToScript(currentVehicle);
            Debug.Write("VISIBLE: {0}", visible);

            bool entityControl = NetworkRequestControlOfEntity(currentVehicle);
            Debug.Write("ENTITY CONTROL: {0}", entityControl);
            bool entityNet = NetworkGetEntityIsNetworked(currentVehicle);
            Debug.Write("ENTITY NETWORKED: {0}", entityNet);

            bool test = IsEntityAMissionEntity(currentVehicle);
            Debug.Write("MISSION: {0}", test);

            bool owner = DoesEntityBelongToThisScript(currentVehicle,true);
            Debug.Write("I AM OWNER: {0}", owner);

            //Debug.Write("NETID CONTROL: {0}", netIDControl);
            //int newEntity = NetworkGetEntityFromNetworkId(currentVehicle);
            //Debug.Write("ENTITY: {0}, NETID: {1}, NEWENTITY: {2}", currentVehicle, netID, newEntity);

            //NetworkRegisterEntityAsNetworked(currentVehicle);
            //bool exists = NetworkDoesEntityExistWithNetworkId(currentVehicle);
            //            Debug.Write("EXISTS: {0}", exists);
            
            int newEntity = NetworkGetEntityFromNetworkId(currentVehicle);
            bool netIDControl = NetworkRequestControlOfNetworkId(netID);
            Debug.Write("ENTITY: {0}, NETID: {1}, NEWENTITY: {2}", currentVehicle, netID, newEntity);
            **/

            int netID = NetworkGetNetworkIdFromEntity(vehicle);
            if (netID != 0)
            {
                SetEntityAsMissionEntity(vehicle,true,true);
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
                Debug.WriteLine("WHEELS EDITOR: Preset sent netID={0}, local={1}", netID, vehicle);
            }
            else
                Debug.WriteLine("WHEELS EDITOR: CAN'T SYNCH PRESET netID={0}!!!", netID);

            await Task.FromResult(0);
        }

        public static void StorePreset(int netID, int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            vstancerPreset preset = new vstancerPreset(count, currentRotFront, currentRotRear, currentOffFront, currentOffRear, defRotFront, defRotRear, defOffFront, defOffRear);
            synchedPresets[netID] = preset;
            //Debug.WriteLine("WHEELS EDITOR: Stored preset for netID={0} received from server", netID);
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

        public static async void PrintDictionary()
        {
            Debug.WriteLine("WHEELS EDITOR: Client Presets Dictionary lenght={0} ", synchedPresets.Count.ToString());
            foreach (int netID in synchedPresets.Keys)
            {
                int handle = NetworkGetEntityFromNetworkId(netID);
                int modelHash = GetEntityModel(handle);
                string displayName = GetDisplayNameFromVehicleModel((uint)modelHash);
                Debug.WriteLine("WHEELS EDITOR: Preset found local={0}, netID={1}, name={2}", handle, netID, displayName);
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
