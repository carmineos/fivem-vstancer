using CitizenFX.Core.UI;
using System.Threading.Tasks;
using VStancer.Client.UI;

namespace VStancer.Client
{
    internal class LocalPresetsManager
    {
        private readonly MainScript _mainScript;

        internal IPresetsCollection<string, VStancerData> Presets { get; private set; }
        internal PresetsMenu PresetsMenu { get; private set; }

        public LocalPresetsManager(MainScript mainScript)
        {
            _mainScript = mainScript;
            Presets = new KvpPresetsCollection(Globals.KvpPrefix);

            PresetsMenu = new PresetsMenu(this);

            PresetsMenu.DeletePresetEvent += (sender, presetID) => OnDeletePresetInvoked(presetID);
            PresetsMenu.SavePresetEvent += (sender, presetID) => OnSavePresetInvoked(presetID);
            PresetsMenu.ApplyPresetEvent += (sender, presetID) => OnApplyPresetInvoked(presetID);
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
            if (Presets.Save(presetKey, _mainScript.VStancerDataManager.VStancerData))
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

            await _mainScript.VStancerDataManager.LoadPreset(loadedPreset);
            Screen.ShowNotification($"Personal preset ~b~{presetKey}~w~ applied");
        }
    }
}
