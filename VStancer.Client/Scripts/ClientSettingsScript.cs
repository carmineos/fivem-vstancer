using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStancer.Client.UI;

using static CitizenFX.Core.Native.API;

namespace VStancer.Client.Scripts
{
    internal class ClientSettingsScript
    {
        private readonly MainScript _mainScript;

        internal const string ClientSettingsID = "vstancer_client_settings";

        private ClientSettings _clientSettings;
        internal ClientSettings ClientSettings 
        { 
            get => _clientSettings; 
            private set
            {
                if (Equals(_clientSettings, value))
                    return;

                _clientSettings = value;
                ClientSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        internal ClientSettingsMenu Menu { get; private set; }

        internal event EventHandler ClientSettingsChanged;

        public ClientSettingsScript(MainScript mainScript)
        {
            _mainScript = mainScript;

            if (!_mainScript.Config.DisableMenu)
            {
                Menu = new ClientSettingsMenu(this);
                Menu.BoolPropertyChanged += OnMenuBoolPropertyChangedEvent;
            }

            ClientSettingsChanged += (sender, args) => { OnClientSettingsChanged(); };

            if (Load(out ClientSettings settings))
                ClientSettings = settings;
            else
                ClientSettings = new ClientSettings();
        }

        private void OnClientSettingsChanged()
        {
            if (ClientSettings != null)
                ClientSettings.PropertyChanged += (sender, name) => { Save(); };
        }

        private void OnMenuBoolPropertyChangedEvent(string id, bool value)
        {
            if (ClientSettings == null)
                return;

            switch (id)
            {
                case nameof(ClientSettings.IgnoreEmptyPresets):
                    ClientSettings.IgnoreEmptyPresets = value;
                    break;
            }
        }

        private bool Load(out ClientSettings settings)
        {
            settings = null;

            string value = GetResourceKvpString(ClientSettingsID);

            if (string.IsNullOrEmpty(value))
                return false;

            settings = JsonConvert.DeserializeObject<ClientSettings>(value);

            if (settings == null)
                return false;

            return true;
        }

        private bool Save()
        {
            if (ClientSettings == null)
                return false;

            var json = JsonConvert.SerializeObject(ClientSettings);
            
            if (string.IsNullOrEmpty(json))
                return false;

            if (GetResourceKvpString(ClientSettingsID) != null)
                DeleteResourceKvp(ClientSettingsID);

            SetResourceKvp(ClientSettingsID, json);

            return true;
        }
    }
}
