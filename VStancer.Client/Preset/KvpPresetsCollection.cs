using CitizenFX.FiveM.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace VStancer.Client.Preset;

/// <summary>
/// The vstancer preset manager which saves the presets as key-value pairs built-in FiveM
/// </summary>
public class KvpPresetsCollection : IPresetsCollection<string, VStancerPreset>
{
    private readonly string mKvpPrefix;

    public event EventHandler PresetsCollectionChanged;

    public KvpPresetsCollection(string prefix)
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
        if (Native.GetResourceKvpString(key) == null)
            return false;

        // Delete the KVP
        Native.DeleteResourceKvp(key);

        // Invoke the event
        PresetsCollectionChanged?.Invoke(this, EventArgs.Empty);

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
        if (Native.GetResourceKvpString(key) != null)
            return false;

        // Get the Json
        var json = JsonSerializer.Serialize(preset, JsonSerializerOptions.Web);

        // Save the KVP
        Native.SetResourceKvp(key, json);

        // Invoke the event
        PresetsCollectionChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    public bool Load(string name, out VStancerPreset preset)
    {
        preset = null;

        // Check if the preset ID is valid
        if (string.IsNullOrEmpty(name))
            return false;

        // Get the KVP key
        string key = string.Concat(mKvpPrefix, name);

        // Get the KVP value
        string value = Native.GetResourceKvpString(key);

        // Check if the value is valid
        if (string.IsNullOrEmpty(value))
            return false;

        // Create a preset
        preset = JsonSerializer.Deserialize<VStancerPreset>(value, JsonSerializerOptions.Web);
        return true;
    }

    public IEnumerable<string> GetKeys()
    {
        return Utilities.GetKeyValuePairs(mKvpPrefix).Select(key => key.Remove(0, mKvpPrefix.Length));
    }
}
