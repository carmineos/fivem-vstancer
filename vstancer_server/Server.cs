using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using vstancer_shared;

namespace vstancer_server
{
    public class Server : BaseScript
    {
        private static Dictionary<int, vstancerPreset> presetsDictionary = new Dictionary<int, vstancerPreset>();

        public Server()
        {
            EventHandlers["sendWheelEditorPreset"] += new Action<Player, int, int, float, float, float, float, float, float, float, float>(BroadcastPreset);
            EventHandlers["clientWheelsEditorReady"] += new Action<Player>(SendDictionary);
        }

        private static async void SendDictionary([FromSource]Player player)
        {
            Debug.WriteLine("WHEELS EDITOR: Sent presets dictionary ({0}) to player={1}({2})", presetsDictionary.Count, player.Name, player.Handle);
            foreach (int netID in presetsDictionary.Keys)
            {
                vstancerPreset preset = presetsDictionary[netID];

                TriggerClientEvent(player, "syncWheelEditorPreset",
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
            }
            await Task.FromResult(0);
        }

        private static async void BroadcastPreset([FromSource]Player player, int netID, int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            vstancerPreset preset = new vstancerPreset(count, currentRotFront, currentRotRear, currentOffFront, currentOffRear, defRotFront, defRotRear, defOffFront, defOffRear);

            presetsDictionary[netID] = preset;

            TriggerClientEvent("syncWheelEditorPreset",
                netID,
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
            Debug.WriteLine("WHEELS EDITOR: Preset broadcasted netID={0} player={1}({2})", netID, player.Name, player.Handle);

            await Task.FromResult(0);
        }
    }
}
