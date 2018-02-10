using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using vstancer_shared;

namespace vstancer_server
{
    public class Server : BaseScript
    {
        private static Dictionary<int, vstancerPreset> presetsDictionary = new Dictionary<int, vstancerPreset>();

        public Server()
        {
            //TODO:Add handlers for when player disconnect

            EventHandlers["ClientRemovedPreset"] += new Action<Player>(BroadcastRemovePreset);
            EventHandlers["ClientAddedPreset"] += new Action<Player, int, float, float, float, float, float, float, float, float>(BroadcastAddPreset);
            EventHandlers["ClientWheelsEditorReady"] += new Action<Player>(BroadcastDictionary);

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintDictionary();
            }), false);

            /*
            RegisterCommand("vstancer_maxEditing", new Action<int, dynamic>((source, args) =>
            {
                float maxEditing = float.Parse(args[1]);
                TriggerClientEvent("BroadcastMaxEditing", maxEditing);
            }), false);

            RegisterCommand("vstancer_maxSyncCount", new Action<int, dynamic>((source, args) =>
            {
                float maxSyncCount = float.Parse(args[1]);
                TriggerClientEvent("BroadcastMaxSyncCount", maxSyncCount);
            }), false);
            */
        }

        private static async void BroadcastDictionary([FromSource]Player player)
        {
            foreach (int ID in presetsDictionary.Keys)
            {
                vstancerPreset preset = presetsDictionary[ID];
                int frontCount = preset.frontCount;

                TriggerClientEvent(player, "BroadcastAddPreset",
                    ID,
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
            }
            Debug.WriteLine("VSTANCER: Sent synched presets({0}) to Player={1}({2})", presetsDictionary.Count, player.Name, player.Handle);
            await Task.FromResult(0);
        }

        private static async void BroadcastRemovePreset([FromSource]Player player)
        {
            int playerID = int.Parse(player.Handle);
            if (presetsDictionary.ContainsKey(playerID))
            {
                bool removed = presetsDictionary.Remove(playerID);
                if (removed)
                {
                    TriggerClientEvent("BroadcastRemovePreset", playerID);
                    Debug.WriteLine("VSTANCER: Removed preset for Player={0}({1})", player.Name, player.Handle);
                }
            }
            await Task.FromResult(0);
        }

        private static async void BroadcastAddPreset([FromSource]Player player, int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            int playerID = int.Parse(player.Handle);
            vstancerPreset preset = new vstancerPreset(count, currentRotFront, currentRotRear, currentOffFront, currentOffRear, defRotFront, defRotRear, defOffFront, defOffRear);

            presetsDictionary[playerID] = preset;

            TriggerClientEvent("BroadcastAddPreset",
                playerID,
                count,
                currentRotFront,
                currentRotRear,
                currentOffFront,
                currentOffRear,
                defRotFront,
                defRotRear,
                defOffFront,
                defOffRear
                );
            Debug.WriteLine("VSTANCER: Added preset for Player={0}({1})", player.Name, player.Handle);

            await Task.FromResult(0);
        }

        public static async void PrintDictionary()
        {
            Debug.WriteLine("VSTANCER: Synched Presets Count={0}", presetsDictionary.Count.ToString());
            foreach (int ID in presetsDictionary.Keys)
                Debug.WriteLine("Player ID={0}", ID);
            await Task.FromResult(0);
        }
    }
}
