using System;
using System.Collections.Generic;
using System.Linq;

namespace vstancer_client
{
    public class Config
    {
        public float editingFactor { get; set; }
        public float maxSyncDistance { get; set; }
        public int toggleMenu { get; set; }
        public float maxOffset { get; set; }
        public float maxCamber { get; set; }
        public long timer { get; set; }
        public bool debug { get; set; }
        public float screenPosX { get; set; }
        public float screenPosY { get; set; }
        public string title { get; set; }
        public string description { get; set; }

        public Config()
        {
            editingFactor = 0.01f;
            maxSyncDistance = 150.0f;
            maxOffset = 0.30f;
            maxCamber = 0.25f;
            toggleMenu = 167;
            timer = 1000;
            debug = false;
            screenPosX = 1.0f;
            screenPosY = 0.0f;
            title = "Wheels Editor";
            description = "~b~Track Width & Camber";
        }

        public void ParseConfigFile(string content)
        {
            Dictionary<string, string> Entries = new Dictionary<string, string>();

            if (content?.Any() ?? false)
            {
                var splitted = content
                 .Split('\n')
                 .Where((line) => !line.Trim().StartsWith("#"))
                 .Select((line) => line.Trim().Split('='))
                 .Where((line) => line.Length == 2);

                foreach (var tuple in splitted)
                    Entries.Add(tuple[0], tuple[1]);
            }

            if (Entries.ContainsKey("editingFactor"))
                editingFactor = float.Parse(Entries["editingFactor"]);

            if (Entries.ContainsKey("maxSyncDistance"))
                maxSyncDistance = float.Parse(Entries["maxSyncDistance"]);

            if (Entries.ContainsKey("maxOffset"))
                maxOffset = float.Parse(Entries["maxOffset"]);

            if (Entries.ContainsKey("maxCamber"))
                maxCamber = float.Parse(Entries["maxCamber"]);

            if (Entries.ContainsKey("toggleMenu"))
                toggleMenu = int.Parse(Entries["toggleMenu"]);

            if (Entries.ContainsKey("timer"))
                timer = long.Parse(Entries["timer"]);

            if (Entries.ContainsKey("debug"))
                debug = bool.Parse(Entries["debug"]);

            if (Entries.ContainsKey("screenPosX"))
                screenPosX = float.Parse(Entries["screenPosX"]);

            if (Entries.ContainsKey("screenPosY"))
                screenPosY = float.Parse(Entries["screenPosY"]);

            if (Entries.ContainsKey("title"))
                title = Entries["title"].Trim();

            if (Entries.ContainsKey("description"))
                description = Entries["description"].Trim();
        }
    }
}
