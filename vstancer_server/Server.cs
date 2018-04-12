using System;
using System.Collections.Generic;
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
            catch
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