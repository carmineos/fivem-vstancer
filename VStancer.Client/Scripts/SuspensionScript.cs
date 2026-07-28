using CitizenFX.FiveM.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStancer.Client.Data;
using VStancer.Client.Menus;
using VStancer.Client.Preset;

namespace VStancer.Client.Scripts;

internal class SuspensionScript : BaseScript
{
    private readonly MainScript _mainScript;

    private long _lastTime;
    private int _playerVehicleHandle;

    private SuspensionData _suspensionData;
    internal SuspensionData SuspensionData
    {
        get => _suspensionData;
        set
        {
            if (Equals(_suspensionData, value))
                return;

            _suspensionData = value;
            SuspensionDataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal Config Config => _mainScript.Config;
    internal SuspensionMenu Menu { get; private set; }
    public bool DataIsValid => _playerVehicleHandle != -1 && SuspensionData != null;

    internal const string SuspensionResetID = "vstancer_suspensions_reset";
    internal const string VisualHeightID = "vstancer_suspensions_visualheight";
    internal const string DefaultVisualHeightID = "vstancer_suspensions_visualheight_def";

    public event EventHandler SuspensionDataChanged;

    internal SuspensionScript(MainScript mainScript)
    {
        _mainScript = mainScript;

        _lastTime = Native.GetGameTimer();
        _playerVehicleHandle = -1;

        RegisterDecorators();

        if (!_mainScript.Config.DisableMenu)
        {
            Menu = new SuspensionMenu(this);
            Menu.FloatPropertyChangedEvent += OnMenuFloatPropertyChanged;
            Menu.ResetPropertiesEvent += (sender, id) => OnMenuCommandInvoked(id);
        }

        Tick += TimedTask;

        mainScript.PlayerVehicleHandleChanged += (sender, handle) => PlayerVehicleChanged(handle);
        PlayerVehicleChanged(_mainScript.PlayerVehicleHandle);

        SuspensionDataChanged += (sender, args) => OnSuspensionDataChanged();
    }

    private void OnSuspensionDataChanged()
    {
        if (SuspensionData != null)
            SuspensionData.PropertyChanged += OnSuspensionDataPropertyChanged;
    }

    private void PlayerVehicleChanged(int vehicle)
    {
        if (vehicle == _playerVehicleHandle)
            return;

        _playerVehicleHandle = vehicle;

        if (SuspensionData != null)
            SuspensionData.PropertyChanged -= OnSuspensionDataPropertyChanged;

        if (_playerVehicleHandle == -1)
        {
            SuspensionData = null;
            return;
        }

        SuspensionData = GetSuspensionDataFromEntity(vehicle);
    }

    private async Task TimedTask()
    {
        long currentTime = (Native.GetGameTimer() - _lastTime);

        if (currentTime > _mainScript.Config.Timer)
        {
            UpdateWorldVehiclesUsingDecorators();

            _lastTime = Native.GetGameTimer();
        }

        await Task.FromResult(0);
    }

    private void UpdateVehicleUsingData(int vehicle, SuspensionData data)
    {
        if (!Native.DoesEntityExist(vehicle) || data == null)
            return;

        Native.SetVehicleSuspensionHeight(vehicle, data.VisualHeight);
    }

    private void UpdateWorldVehiclesUsingDecorators()
    {
        foreach (int entity in _mainScript.GetCloseVehicleHandles())
        {
            if (entity == _playerVehicleHandle)
                continue;

            UpdateVehicleUsingDecorators(entity);
        }
    }

    private SuspensionData GetSuspensionDataFromEntity(int vehicle)
    {
        if (!Native.DoesEntityExist(vehicle))
            return null;


        // Get default values first
        float visualHeight_def = Native.DecorExistOn(vehicle, DefaultVisualHeightID) ? Native.DecorGetFloat(vehicle, DefaultVisualHeightID) : Native.GetVehicleSuspensionHeight(vehicle);

        float visualHeight = Native.DecorExistOn(vehicle, VisualHeightID) ? Native.DecorGetFloat(vehicle, VisualHeightID) : visualHeight_def;

        return new SuspensionData(visualHeight_def)
        {
            VisualHeight = visualHeight,
        };
    }

    private void SetSuspensionHeightUsingData(int vehicle, SuspensionData data)
    {
        float value = data.VisualHeight;
        float defValue = data.DefaultVisualHeight;

        Native.SetVehicleSuspensionHeight(vehicle, value);
        Utilities.UpdateFloatDecorator(vehicle, DefaultVisualHeightID, defValue, value);
        Utilities.UpdateFloatDecorator(vehicle, VisualHeightID, value, defValue);
    }

    private void OnSuspensionDataPropertyChanged(string propertyName, float value)
    {
        switch (propertyName)
        {
            case nameof(SuspensionData.VisualHeight):
                SetSuspensionHeightUsingData(_playerVehicleHandle, SuspensionData);
                break;

            case nameof(SuspensionData.Reset):

                UpdateVehicleUsingData(_playerVehicleHandle, SuspensionData);
                RemoveDecoratorsFromVehicle(_playerVehicleHandle);

                SuspensionDataChanged?.Invoke(this, EventArgs.Empty);
                break;

            default:
                break;
        }
    }

    private void UpdateVehicleUsingDecorators(int vehicle)
    {
        if (Native.DecorExistOn(vehicle, VisualHeightID))
            Native.SetVehicleSuspensionHeight(vehicle, Native.DecorGetFloat(vehicle, VisualHeightID));
    }

    private void RegisterDecorators()
    {
        Native.DecorRegister(VisualHeightID, 1);
        Native.DecorRegister(DefaultVisualHeightID, 1);
    }

    private void RemoveDecoratorsFromVehicle(int vehicle)
    {
        if (Native.DecorExistOn(vehicle, VisualHeightID))
            Native.DecorRemove(vehicle, VisualHeightID);

        if (Native.DecorExistOn(vehicle, DefaultVisualHeightID))
            Native.DecorRemove(vehicle, DefaultVisualHeightID);
    }

    private bool EntityHasDecorators(int entity)
    {
        return (
            Native.DecorExistOn(entity, VisualHeightID) ||
            Native.DecorExistOn(entity, DefaultVisualHeightID)
            );
    }

    private void OnMenuCommandInvoked(string commandID)
    {
        if (!DataIsValid)
            return;

        switch (commandID)
        {
            case SuspensionResetID:
                SuspensionData.Reset();
                break;
        }
    }

    private void OnMenuFloatPropertyChanged(string id, float value)
    {
        if (!DataIsValid)
            return;

        switch (id)
        {
            case VisualHeightID:
                SuspensionData.VisualHeight = value;
                break;
        }
    }

    internal void PrintDecoratorsInfo(int vehicle)
    {
        if (!Native.DoesEntityExist(vehicle))
        {
            Debug.WriteLine($"{nameof(SuspensionScript)}: Can't find vehicle with handle {vehicle}");
            return;
        }

        int netID = Native.NetworkGetNetworkIdFromEntity(vehicle);
        StringBuilder s = new StringBuilder();
        s.AppendLine($"{nameof(SuspensionScript)}: Vehicle:{vehicle} netID:{netID}");

        if (Native.DecorExistOn(vehicle, VisualHeightID))
            s.AppendLine($"{VisualHeightID}: {Native.DecorGetFloat(vehicle, VisualHeightID)}");

        if (Native.DecorExistOn(vehicle, DefaultVisualHeightID))
            s.AppendLine($"{DefaultVisualHeightID}: {Native.DecorGetFloat(vehicle, DefaultVisualHeightID)}");

        Debug.WriteLine(s.ToString());
    }

    internal void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
    {
        IEnumerable<int> entities = vehiclesList.Where(entity => EntityHasDecorators(entity));

        Debug.WriteLine($"{nameof(SuspensionScript)}: Vehicles with decorators: {entities.Count()}");

        foreach (int item in entities)
            Debug.WriteLine($"Vehicle: {item}, netID: {Native.NetworkGetNetworkIdFromEntity(item)}");
    }

    internal SuspensionPreset GetSuspensionPreset(bool allowStockPreset = false)
    {
        if (!DataIsValid)
            return null;

        // Only required to avoid saving this preset locally when not required
        if (!SuspensionData.IsEdited && !allowStockPreset)
            return null;

        return new SuspensionPreset(SuspensionData);
    }

    internal async Task SetSuspensionPreset(SuspensionPreset preset, bool ignoreEmptyPresets = true)
    {
        if (!DataIsValid)
            return;

        if (preset == null)
        {
            if (!ignoreEmptyPresets)
                SuspensionData.Reset();

            return;
        }

        // TODO: Check if values are within limits

        SuspensionData.VisualHeight = preset.VisualHeight;

        // Don't refresh, as it's already done by OnSuspensionDataPropertyChanged

        Debug.WriteLine($"{nameof(SuspensionScript)}: suspension preset applied");

        await Delay(200);
        SuspensionDataChanged?.Invoke(this, EventArgs.Empty);
    }
}
