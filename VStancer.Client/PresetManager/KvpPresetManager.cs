using Newtonsoft.Json;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client
{
    /// <summary>
    /// The vstancer preset manager which saves the presets as key-value pairs built-in FiveM
    /// </summary>
    public class KvpPresetManager : IPresetManager<string, VStancerPreset>
    {
        private string mKvpPrefix;

        public KvpPresetManager(string prefix)
        {
            mKvpPrefix = prefix;
        }

        public bool Delete(string name)
        {
            // Check if the preset ID is valid
            if (string.IsNullOrEmpty(name))
                return false;

            // Get the KVP key
            string key = string.Concat(mKvpPrefix, name);

            // Check if a KVP with the given key exists
            if (GetResourceKvpString(key) == null)
                return false;

            // Delete the KVP
            DeleteResourceKvp(key);

            return true;
        }

        public bool Save(string name, VStancerPreset preset)
        {
            // Check if the preset and the ID are valid
            if (string.IsNullOrEmpty(name) || preset == null)
                return false;

            // Get the KVP key
            string key = string.Concat(mKvpPrefix, name);

            // Be sure the key isn't already used
            if (GetResourceKvpString(key) != null)
                return false;

            // Get the Json
            var json = JsonConvert.SerializeObject(preset);

            // Save the KVP
            SetResourceKvp(key, json);

            return true;
        }

        public VStancerPreset Load(string name)
        {
            // Check if the preset ID is valid
            if (string.IsNullOrEmpty(name))
                return null;

            // Get the KVP key
            string key = string.Concat(mKvpPrefix, name);

            // Get the KVP value
            string value = GetResourceKvpString(key);

            // Check if the value is valid
            if (string.IsNullOrEmpty(value))
                return null;

            // Create a preset
            VStancerPreset preset = JsonConvert.DeserializeObject<VStancerPreset>(value);

            return preset;
        }

        public IEnumerable<string> GetKeys()
        {
            return new KvpEnumerable(mKvpPrefix);
        }
    }
}
