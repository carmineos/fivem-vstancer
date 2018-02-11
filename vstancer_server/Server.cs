using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using vstancer_shared;
using System.Threading;
using System.Globalization;

namespace vstancer_server
{
    public class Server : BaseScript
    {
        private static float maxEditing = 0.30f;
        private static int maxSyncCount = 1;
        private static int coolDownSeconds = 30;

        private static Dictionary<int, vstancerPreset> presetsDictionary = new Dictionary<int, vstancerPreset>();

        public Server()
        {
            EventHandlers["vstancer:clientUnsync"] += new Action<Player>(BroadcastRemovePreset);
            EventHandlers["vstancer:clientSync"] += new Action<Player, int, float, float, float, float, float, float, float, float>(BroadcastAddPreset);
            EventHandlers["vstancer:clientReady"] += new Action<Player>(BroadcastDictionary);
            EventHandlers["playerDropped"] += new Action<Player>(BroadcastRemovePreset);

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintDictionary();
            }), false);

            
            RegisterCommand("vstancer_maxEditing", new Action<int, dynamic>((source, args) =>
            {
                maxEditing = float.Parse(args[0]);
                TriggerClientEvent("vstancer:maxEditing", maxEditing);
            }), false);

            
            RegisterCommand("vstancer_maxSyncCount", new Action<int, dynamic>((source, args) =>
            {
                maxSyncCount = int.Parse(args[0]);
                TriggerClientEvent("vstancer:maxSyncCount", maxSyncCount);
            }), false);

            RegisterCommand("vstancer_cooldown", new Action<int, dynamic>((source, args) =>
            {
                int coolDownSeconds = int.Parse(args[0]);
                TriggerClientEvent("vstancer:cooldown", coolDownSeconds);
            }), false);

        }

        private static async void BroadcastDictionary([FromSource]Player player)
        {
            TriggerClientEvent(player,"vstancer:settings", maxEditing, maxSyncCount, coolDownSeconds);
            Debug.WriteLine("VSTANCER: Settings sent to Player={0}({1})", player.Name, player.Handle);

            foreach (int ID in presetsDictionary.Keys)
            {
                vstancerPreset preset = presetsDictionary[ID];
                int frontCount = preset.frontCount;

                TriggerClientEvent(player, "vstancer:addPreset",
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
                    TriggerClientEvent("vstancer:removePreset", playerID);
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

            TriggerClientEvent("vstancer:addPreset",
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
