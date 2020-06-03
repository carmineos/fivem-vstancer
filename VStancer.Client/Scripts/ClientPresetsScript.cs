using System.Threading.Tasks;

using VStancer.Client.UI;

using CitizenFX.Core.UI;
using VStancer.Client.Preset;
using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client.Scripts
{
    internal class ClientPresetsScript
    {
        private readonly MainScript _mainScript;

        internal IPresetsCollection<string, VStancerPreset> Presets { get; private set; }
        internal ClientPresetsMenu Menu { get; private set; }

        public ClientPresetsScript(MainScript mainScript)
        {
            _mainScript = mainScript;
            Presets = new KvpPresetsCollection(Globals.KvpPrefix);

            if (!_mainScript.Config.DisableMenu)
            {
                Menu = new ClientPresetsMenu(this);

                Menu.DeletePresetEvent += (sender, presetID) => OnDeletePresetInvoked(presetID);
                Menu.SavePresetEvent += (sender, presetID) => OnSavePresetInvoked(presetID);
                Menu.ApplyPresetEvent += (sender, presetID) => OnApplyPresetInvoked(presetID);
            }
        }

        internal async Task<string> GetPresetNameFromUser(string title, string defaultText)
        {
            return await _mainScript.GetOnScreenString(title, defaultText);
        }

        private void OnDeletePresetInvoked(string presetKey)
        {
            if (Presets.Delete(presetKey))
                Screen.ShowNotification($"Client preset ~r~{presetKey}~w~ deleted");
            else
                Screen.ShowNotification($"~r~ERROR~w~ No preset found with {presetKey} key.");
        }

        private void OnSavePresetInvoked(string presetKey)
        {
            var wheelPreset = _mainScript.WheelScript?.GetWheelPreset();
            var wheelModPreset = _mainScript.WheelModScript?.GetWheelModPreset();
            
            if(wheelPreset == null && wheelModPreset == null)
            {
                Screen.ShowNotification($"~r~ERROR~w~ Nothing to save, be sure your vehicle is edited!");
                return;
            }

            if (wheelPreset == null)
                Screen.ShowNotification($"~y~WARNING~w~ The preset doesn't contain any wheel data.");

            if (wheelModPreset == null)
                Screen.ShowNotification($"~y~WARNING~w~ The preset doesn't contain any wheel mod data.");

            VStancerPreset preset = new VStancerPreset
            {
                WheelPreset = wheelPreset,
                WheelModPreset = wheelModPreset,
            };

            if (Presets.Save(presetKey, preset))
                Screen.ShowNotification($"Client preset ~g~{presetKey}~w~ saved");
            else
                Screen.ShowNotification($"~r~ERROR~w~ The name {presetKey} is invalid or already used.");
        }

        private async void OnApplyPresetInvoked(string presetKey)
        {
            if (!Presets.Load(presetKey, out VStancerPreset loadedPreset))
            {
                Screen.ShowNotification($"~r~ERROR~w~ No Client preset with name ~b~{presetKey}~w~ found");
                return;
            }

            if (loadedPreset == null)
            {
                Screen.ShowNotification($"~r~ERROR~w~ Client preset ~b~{presetKey}~w~ corrupted");
                return;
            }

            await _mainScript.WheelScript.SetWheelPreset(loadedPreset.WheelPreset);
            await _mainScript.WheelModScript.SetWheelModPreset(loadedPreset.WheelModPreset);

            Screen.ShowNotification($"Client preset ~b~{presetKey}~w~ applied");
        }

        internal bool API_DeletePreset(string presetKey)
        {
            return Presets.Delete(presetKey);
        }

        internal bool API_SavePreset(string presetKey, int vehicle)
        {
            if (_mainScript.WheelScript == null)
                return false;

            if (!_mainScript.WheelScript.API_GetWheelPreset(vehicle, out WheelPreset wheelPreset))
                return false;

            if(wheelPreset != null)
            {
                VStancerPreset preset = new VStancerPreset
                {
                    WheelPreset = wheelPreset
                };

                return Presets.Save(presetKey, preset);
            }

            return false;
        }

        internal bool API_LoadPreset(string presetKey, int vehicle)
        {
            if (!Presets.Load(presetKey, out VStancerPreset loadedPreset))
                return false;

            if (loadedPreset == null)
                return false;

            if(_mainScript.WheelScript != null)
            {
                return _mainScript.WheelScript.API_SetWheelPreset(vehicle, loadedPreset.WheelPreset);
            }

            // TODO: Load wheel mod preset on vehicle

            return false;
        }

        internal IEnumerable<string> API_GetClientPresetList()
        {
            return Presets.GetKeys();
        }
    }
}
