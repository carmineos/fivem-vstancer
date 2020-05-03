using System.Threading.Tasks;

using VStancer.Client.UI;

using CitizenFX.Core.UI;
using VStancer.Client.Preset;
using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client.Scripts
{
    internal class LocalPresetsScript
    {
        private readonly MainScript _mainScript;

        internal IPresetsCollection<string, VStancerPreset> Presets { get; private set; }
        internal PresetsMenu Menu { get; private set; }

        public LocalPresetsScript(MainScript mainScript)
        {
            _mainScript = mainScript;
            Presets = new KvpPresetsCollection(Globals.KvpPrefix);

            if (!_mainScript.Config.DisableMenu)
            {
                Menu = new PresetsMenu(this);

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
                Screen.ShowNotification($"Personal preset ~r~{presetKey}~w~ deleted");
            else
                Screen.ShowNotification($"~r~ERROR~w~ No preset found with {presetKey} key.");
        }

        private void OnSavePresetInvoked(string presetKey)
        {
            VStancerPreset preset = new VStancerPreset
            {
                WheelPreset = _mainScript.WheelScript?.GetWheelPreset(),
                WheelModPreset = _mainScript.WheelModScript?.GetWheelModPreset()
            };

            if (Presets.Save(presetKey, preset))
                Screen.ShowNotification($"Personal preset ~g~{presetKey}~w~ saved");
            else
                Screen.ShowNotification($"~r~ERROR~w~ The name {presetKey} is invalid or already used.");
        }

        private async void OnApplyPresetInvoked(string presetKey)
        {
            var loadedPreset = Presets.Load(presetKey);

            if (loadedPreset == null)
            {
                await Task.FromResult(0);
                Screen.ShowNotification($"~r~ERROR~w~ Personal preset ~b~{presetKey}~w~ corrupted");
                return;
            }

            await _mainScript.WheelScript.SetWheelPreset(loadedPreset.WheelPreset);
            await _mainScript.WheelModScript.SetWheelModPreset(loadedPreset.WheelModPreset);

            Screen.ShowNotification($"Personal preset ~b~{presetKey}~w~ applied");
        }

        internal bool API_DeletePreset(string presetKey)
        {
            return Presets.Delete(presetKey);
        }

        internal bool API_SavePreset(string presetKey, int vehicle)
        {
            if (!DoesEntityExist(vehicle))
                return false;

            WheelPreset wheelPreset = _mainScript.WheelScript?.API_GetWheelPreset(vehicle);

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
            var loadedPreset = Presets.Load(presetKey);

            if (loadedPreset == null)
                return false;

            if(_mainScript.WheelScript != null)
            {
                return _mainScript.WheelScript.API_SetWheelPreset(vehicle, loadedPreset.WheelPreset);
            }

            // TODO: Load wheel mod preset on vehicle

            return false;
        }

        internal IEnumerable<string> API_GetLocalPresetList()
        {
            return Presets.GetKeys();
        }
    }
}
