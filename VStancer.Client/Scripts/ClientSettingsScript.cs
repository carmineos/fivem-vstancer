using CitizenFX.FiveM.Client;
using System;
using System.Text.Json;
using VStancer.Client.Menus;

namespace VStancer.Client.Scripts;

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

            case nameof(ClientSettings.AllowStockPresets):
                ClientSettings.AllowStockPresets = value;
                break;

            case nameof(ClientSettings.MenuLeftAlignment):
                ClientSettings.MenuLeftAlignment = value;
                break;
        }
    }

    private bool Load(out ClientSettings settings)
    {
        settings = null;

        string value = Native.GetResourceKvpString(ClientSettingsID);

        if (string.IsNullOrEmpty(value))
            return false;

        settings = JsonSerializer.Deserialize<ClientSettings>(value, JsonSerializerOptions.Web);

        if (settings == null)
            return false;

        return true;
    }

    private bool Save()
    {
        if (ClientSettings == null)
            return false;

        var json = JsonSerializer.Serialize(ClientSettings, JsonSerializerOptions.Web);

        if (string.IsNullOrEmpty(json))
            return false;

        if (Native.GetResourceKvpString(ClientSettingsID) != null)
            Native.DeleteResourceKvp(ClientSettingsID);

        Native.SetResourceKvp(ClientSettingsID, json);

        return true;
    }
}
