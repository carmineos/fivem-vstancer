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
            Debug.WriteLine("WHEELS EDITOR: PRESETS DICTIONARY({0}) SENT TO player={1}({2})", presetsDictionary.Count, player.Name, player.Handle);
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
                    Debug.WriteLine("WHEELS EDITOR: REMOVED PRESET FOR player={0}({1})", player.Name, player.Handle);
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
            Debug.WriteLine("WHEELS EDITOR: ADDED PRESET FOR player={0}({1})", player.Name, player.Handle);

            await Task.FromResult(0);
        }

        public static async void PrintDictionary()
        {
            Debug.WriteLine("WHEELS EDITOR: SERVER'S DICTIONARY LENGHT={0} ", presetsDictionary.Count.ToString());
            foreach (int netID in presetsDictionary.Keys)
                Debug.WriteLine("netID={0}", netID);
            await Task.FromResult(0);
        }
    }
}
