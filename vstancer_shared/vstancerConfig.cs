using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vstancer_shared
{
    public class vstancerConfig
    {
        public float editingFactor { get; set; } //CLIENT
        public float maxSyncDistance { get; set; } //CLIENT
        public int toggleMenu { get; set; } //CLIENT
        public float maxOffset { get; set; } //SHARED
        public float maxCamber { get; set; } //SHARED
        public long timer { get; set; } //SHARED
        public bool debug { get; set; } //CLIENT & SERVER

        private Dictionary<string, string> Entries;

        public vstancerConfig()
        {
            editingFactor = 0.01f;
            maxSyncDistance = 150.0f;
            maxOffset = 0.30f;
            maxCamber = 0.25f;
            toggleMenu = 167;
            timer = 1000;
            debug = false;
        }

        public void ParseConfigFile(string content)
        {
            //Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Entries = new Dictionary<string, string>();

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
        }
    }
}
