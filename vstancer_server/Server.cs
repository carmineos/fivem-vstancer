using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using vstancer_shared;

namespace vstancer_server
{
    public class Server : BaseScript
    {
        #region CONFIG
        private static float maxOffset;
        private static float maxCamber;
        private static long timer;
        private static bool debug;
        #endregion

        private static Dictionary<int, vstancerPreset> presetsDictionary = new Dictionary<int, vstancerPreset>();

        public Server()
        {
            LoadConfig();

            EventHandlers["vstancer:clientUnsync"] += new Action<Player,int>(BroadcastRemovePreset);
            EventHandlers["vstancer:clientSync"] += new Action<Player, int, int, float, float, float, float, float, float, float, float>(BroadcastAddPreset);
            EventHandlers["vstancer:clientReady"] += new Action<Player>(BroadcastDictionary);
            //EventHandlers["playerDropped"] += new Action<Player>(BroadcastRemovePreset);

            RegisterCommand("vstancer_print", new Action<int, dynamic>((source, args) =>
            {
                PrintDictionary();
            }), false);
            RegisterCommand("vstancer_maxOffset", new Action<int, dynamic>((source, args) =>
            {
                maxOffset = float.Parse(args[0]);
                TriggerClientEvent("vstancer:maxOffset", maxOffset);
                Debug.WriteLine("VSTANCER: Received new maxOffset value {0}", maxOffset);
            }), false);
            RegisterCommand("vstancer_maxCamber", new Action<int, dynamic>((source, args) =>
            {
                maxCamber = float.Parse(args[0]);
                TriggerClientEvent("vstancer:maxCamber", maxCamber);
                Debug.WriteLine("VSTANCER: Received new maxCamber value {0}", maxCamber);
            }), false);
            RegisterCommand("vstancer_timer", new Action<int, dynamic>((source, args) =>
            {
                timer = long.Parse(args[0]);
                TriggerClientEvent("vstancer:timer", timer);
                Debug.WriteLine("VSTANCER: Received new timer value {0}", timer);
            }), false);
            RegisterCommand("vstancer_debug", new Action<int, dynamic>((source, args) =>
            {
                debug = bool.Parse(args[0]);
                Debug.WriteLine("VSTANCER: Received new debug value {0}", debug);
            }), false);
        }

        private static async void BroadcastDictionary([FromSource]Player player)
        {
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

        private static async void BroadcastRemovePreset([FromSource]Player player,int netID)
        {
            if (presetsDictionary.ContainsKey(netID))
            {
                bool removed = presetsDictionary.Remove(netID);
                if (removed)
                {
                    TriggerClientEvent("vstancer:removePreset", netID);

                    if (debug)
                        Debug.WriteLine("VSTANCER: Removed preset for netID={0}", netID);
                }
            }
            await Task.FromResult(0);
        }

        private static async void BroadcastAddPreset([FromSource]Player player, int netID, int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            //int playerID = int.Parse(player.Handle);
            vstancerPreset preset = new vstancerPreset(count, currentRotFront, currentRotRear, currentOffFront, currentOffRear, defRotFront, defRotRear, defOffFront, defOffRear);

            presetsDictionary[netID] = preset;

            TriggerClientEvent("vstancer:addPreset",
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

            if (debug)
                Debug.WriteLine("VSTANCER: Added preset for netID={0}", netID);

            await Task.FromResult(0);
        }

        public static async void PrintDictionary()
        {
            Debug.WriteLine("VSTANCER: Synched Presets Count={0}", presetsDictionary.Count.ToString());
            Debug.WriteLine("VSTANCER: Settings maxOffset={0} maxCamber={1} timer={2} debug={3}", maxOffset, maxCamber, timer, debug);
            foreach (int ID in presetsDictionary.Keys)
                Debug.WriteLine("Preset netID={0}", ID);
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
                maxOffset = config.maxOffset;
                maxCamber = config.maxCamber;
                timer = config.timer;
                debug = config.debug;

                Debug.WriteLine("VSTANCER: Settings maxOffset={0} maxCamber={1} timer={2} debug={3}", maxOffset, maxCamber, timer, debug);
            }
        }
    }
}