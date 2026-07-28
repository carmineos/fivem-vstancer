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

internal class WheelScript : BaseScript
{
    private readonly MainScript _mainScript;

    private long _lastTime;
    private int _playerVehicleHandle;

    private WheelData _wheelData;
    internal WheelData WheelData
    {
        get => _wheelData;
        set
        {
            if (Equals(_wheelData, value))
                return;

            _wheelData = value;
            WheelDataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal Config Config => _mainScript.Config;
    internal WheelMenu Menu { get; private set; }

    internal bool DataIsValid => _playerVehicleHandle != -1 && WheelData != null;

    internal const string FrontTrackWidthID = "vstancer_trackwidth_f";
    internal const string RearTrackWidthID = "vstancer_trackwidth_r";
    internal const string FrontCamberID = "vstancer_camber_f";
    internal const string RearCamberID = "vstancer_camber_r";

    internal const string DefaultFrontTrackWidthID = "vstancer_trackwidth_f_def";
    internal const string DefaultRearTrackWidthID = "vstancer_trackwidth_r_def";
    internal const string DefaultFrontCamberID = "vstancer_camber_f_def";
    internal const string DefaultRearCamberID = "vstancer_camber_r_def";

    internal const string ResetID = "vstancer_reset";

    internal event EventHandler WheelDataChanged;

    internal WheelScript(MainScript mainScript)
    {
        _mainScript = mainScript;

        _lastTime = GetGameTimer();
        _playerVehicleHandle = -1;

        RegisterDecorators();

        if (!_mainScript.Config.DisableMenu)
        {
            Menu = new WheelMenu(this);
            Menu.FloatPropertyChangedEvent += OnMenuFloatPropertyChanged;
            Menu.ResetPropertiesEvent += (sender, id) => OnMenuCommandInvoked(id);
        }

        Tick += UpdateWorldVehiclesTask;
        Tick += UpdatePlayerVehicleTask;
        //Tick += TimedTask;

        mainScript.PlayerVehicleHandleChanged += (sender, handle) => PlayerVehicleChanged(handle);
        PlayerVehicleChanged(_mainScript.PlayerVehicleHandle);

        WheelDataChanged += (sender, args) => OnWheelDataChanged();
    }

    private void OnWheelDataChanged()
    {
        if (WheelData != null)
            WheelData.PropertyChanged += OnWheelDataPropertyChanged;
    }

    private void PlayerVehicleChanged(int vehicle)
    {
        if (vehicle == _playerVehicleHandle)
            return;

        _playerVehicleHandle = vehicle;

        if (WheelData != null)
            WheelData.PropertyChanged -= OnWheelDataPropertyChanged;

        if (_playerVehicleHandle == -1)
        {
            WheelData = null;
            Tick -= UpdatePlayerVehicleTask;
            return;
        }

        WheelData = GetWheelDataFromEntity(vehicle);

        Tick += UpdatePlayerVehicleTask;
    }

    private async Task UpdatePlayerVehicleTask()
    {
        await Task.FromResult(0);

        // Check if current vehicle needs to be refreshed
        if (DataIsValid && WheelData.IsEdited)
            UpdateVehicleUsingWheelData(_playerVehicleHandle, WheelData);
    }

    private async Task UpdateWorldVehiclesTask()
    {
        await Task.FromResult(0);

        foreach (int entity in _mainScript.GetCloseVehicleHandles())
        {
            if (entity == _playerVehicleHandle)
                continue;

            UpdateVehicleUsingDecorators(entity);
        }
    }

    //private async Task TimedTask()
    //{
    //    long currentTime = (GetGameTimer() - _lastTime);
    //
    //    if (currentTime > _mainScript.Config.Timer)
    //    {
    //        // Check if decorators needs to be updated
    //        //if (DataIsValid)
    //        //    UpdateVehicleDecorators(_playerVehicleHandle, WheelData);
    //
    //        _lastTime = GetGameTimer();
    //    }
    //
    //    await Task.FromResult(0);
    //}

    private async void OnWheelDataPropertyChanged(string propertyName, float value)
    {
        if (!DataIsValid)
            return;

        switch (propertyName)
        {
            case nameof(WheelData.Reset):
                RemoveDecoratorsFromVehicle(_playerVehicleHandle);
                UpdateVehicleUsingWheelData(_playerVehicleHandle, WheelData);
                await Delay(50);
                WheelDataChanged?.Invoke(this, EventArgs.Empty);
                break;

            case nameof(WheelData.FrontCamber):
                SetFrontCamberUsingData(_playerVehicleHandle, WheelData);
                break;
            case nameof(WheelData.RearCamber):
                SetRearCamberUsingData(_playerVehicleHandle, WheelData);
                break;
            case nameof(WheelData.FrontTrackWidth):
                SetFrontTrackWidthUsingData(_playerVehicleHandle, WheelData);
                break;
            case nameof(WheelData.RearTrackWidth):
                SetRearTrackWidthUsingData(_playerVehicleHandle, WheelData);
                break;
        }
    }

    private void SetFrontCamberUsingData(int vehicle, WheelData data)
    {
        if (!Native.DoesEntityExist(vehicle) || data == null)
            return;

        int frontWheelsCount = data.FrontWheelsCount;
        WheelDataNode[] nodes = data.GetNodes();

        for (int i = 0; i < frontWheelsCount; i++)
            Native.SetVehicleWheelYRotation(vehicle, i, nodes[i].RotationY);

        Utilities.UpdateFloatDecorator(vehicle, DefaultFrontCamberID, data.DefaultFrontCamber, data.FrontCamber);
        Utilities.UpdateFloatDecorator(vehicle, FrontCamberID, data.FrontCamber, data.DefaultFrontCamber);
    }

    private void SetRearCamberUsingData(int vehicle, WheelData data)
    {
        if (!Native.DoesEntityExist(vehicle) || data == null)
            return;

        int wheelsCount = data.WheelsCount;
        int frontWheelsCount = data.FrontWheelsCount;
        WheelDataNode[] nodes = data.GetNodes();

        for (int i = frontWheelsCount; i < wheelsCount; i++)
            Native.SetVehicleWheelYRotation(vehicle, i, nodes[i].RotationY);

        Utilities.UpdateFloatDecorator(vehicle, DefaultRearCamberID, data.DefaultRearCamber, data.RearCamber);
        Utilities.UpdateFloatDecorator(vehicle, RearCamberID, data.RearCamber, data.DefaultRearCamber);
    }

    private void SetFrontTrackWidthUsingData(int vehicle, WheelData data)
    {
        if (!Native.DoesEntityExist(vehicle) || data == null)
            return;

        int frontWheelsCount = data.FrontWheelsCount;
        WheelDataNode[] nodes = data.GetNodes();

        for (int i = 0; i < frontWheelsCount; i++)
            Native.SetVehicleWheelXOffset(vehicle, i, nodes[i].PositionX);

        Utilities.UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, data.DefaultFrontTrackWidth, data.FrontTrackWidth);
        Utilities.UpdateFloatDecorator(vehicle, FrontTrackWidthID, data.FrontTrackWidth, data.DefaultFrontTrackWidth);
    }

    private void SetRearTrackWidthUsingData(int vehicle, WheelData data)
    {
        if (!Native.DoesEntityExist(vehicle) || data == null)
            return;

        int wheelsCount = data.WheelsCount;
        int frontWheelsCount = data.FrontWheelsCount;
        WheelDataNode[] nodes = data.GetNodes();

        for (int i = frontWheelsCount; i < wheelsCount; i++)
            Native.SetVehicleWheelXOffset(vehicle, i, nodes[i].PositionX);

        Utilities.UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, data.DefaultRearTrackWidth, data.RearTrackWidth);
        Utilities.UpdateFloatDecorator(vehicle, RearTrackWidthID, data.RearTrackWidth, data.DefaultRearTrackWidth);
    }

    private WheelData GetWheelDataFromEntity(int vehicle)
    {
        if (!Native.DoesEntityExist(vehicle))
            return null;

        int wheelsCount = Native.GetVehicleNumberOfWheels(vehicle);
        int frontCount = Utilities.CalculateFrontWheelsCount(wheelsCount);

        // Get default values first
        float frontTrackWidth_def = Native.DecorExistOn(vehicle, DefaultFrontTrackWidthID) ? Native.DecorGetFloat(vehicle, DefaultFrontTrackWidthID) : Native.GetVehicleWheelXOffset(vehicle, 0);
        float frontCamber_def = Native.DecorExistOn(vehicle, DefaultFrontCamberID) ? Native.DecorGetFloat(vehicle, DefaultFrontCamberID) : Native.GetVehicleWheelYRotation(vehicle, 0);
        float rearTrackWidth_def = Native.DecorExistOn(vehicle, DefaultRearTrackWidthID) ? Native.DecorGetFloat(vehicle, DefaultRearTrackWidthID) : Native.GetVehicleWheelXOffset(vehicle, frontCount);
        float rearCamber_def = Native.DecorExistOn(vehicle, DefaultRearCamberID) ? Native.DecorGetFloat(vehicle, DefaultRearCamberID) : Native.GetVehicleWheelYRotation(vehicle, frontCount);

        float frontTrackWidth = Native.DecorExistOn(vehicle, FrontTrackWidthID) ? Native.DecorGetFloat(vehicle, FrontTrackWidthID) : frontTrackWidth_def;
        float frontCamber = Native.DecorExistOn(vehicle, FrontCamberID) ? Native.DecorGetFloat(vehicle, FrontCamberID) : frontCamber_def;
        float rearTrackWdith = Native.DecorExistOn(vehicle, RearTrackWidthID) ? Native.DecorGetFloat(vehicle, RearTrackWidthID) : rearTrackWidth_def;
        float rearCamber = Native.DecorExistOn(vehicle, RearCamberID) ? Native.DecorGetFloat(vehicle, RearCamberID) : rearCamber_def;

        return new WheelData(wheelsCount, frontTrackWidth_def, frontCamber_def, rearTrackWidth_def, rearCamber_def)
        {
            FrontTrackWidth = frontTrackWidth,
            FrontCamber = frontCamber,
            RearTrackWidth = rearTrackWdith,
            RearCamber = rearCamber,
        };
    }

    private void UpdateVehicleUsingDecorators(int vehicle)
    {
        int wheelsCount = Native.GetVehicleNumberOfWheels(vehicle);
        int frontCount = Utilities.CalculateFrontWheelsCount(wheelsCount);

        if (Native.DecorExistOn(vehicle, FrontTrackWidthID))
        {
            float value = Native.DecorGetFloat(vehicle, FrontTrackWidthID);

            for (int index = 0; index < frontCount; index++)
            {
                if (index % 2 == 0)
                    Native.SetVehicleWheelXOffset(vehicle, index, value);
                else
                    Native.SetVehicleWheelXOffset(vehicle, index, -value);
            }
        }

        if (Native.DecorExistOn(vehicle, FrontCamberID))
        {
            float value = Native.DecorGetFloat(vehicle, FrontCamberID);

            for (int index = 0; index < frontCount; index++)
            {
                if (index % 2 == 0)
                    Native.SetVehicleWheelYRotation(vehicle, index, value);
                else
                    Native.SetVehicleWheelYRotation(vehicle, index, -value);
            }
        }

        if (Native.DecorExistOn(vehicle, RearTrackWidthID))
        {
            float value = Native.DecorGetFloat(vehicle, RearTrackWidthID);

            for (int index = frontCount; index < wheelsCount; index++)
            {
                if (index % 2 == 0)
                    Native.SetVehicleWheelXOffset(vehicle, index, value);
                else
                    Native.SetVehicleWheelXOffset(vehicle, index, -value);
            }
        }

        if (Native.DecorExistOn(vehicle, RearCamberID))
        {
            float value = Native.DecorGetFloat(vehicle, RearCamberID);

            for (int index = frontCount; index < wheelsCount; index++)
            {
                if (index % 2 == 0)
                    Native.SetVehicleWheelYRotation(vehicle, index, value);
                else
                    Native.SetVehicleWheelYRotation(vehicle, index, -value);
            }
        }
    }

    private void UpdateVehicleUsingWheelData(int vehicle, WheelData data)
    {
        if (!Native.DoesEntityExist(vehicle) || data == null)
            return;

        int wheelsCount = data.WheelsCount;
        WheelDataNode[] nodes = data.GetNodes();

        for (int index = 0; index < wheelsCount; index++)
        {
            Native.SetVehicleWheelXOffset(vehicle, index, nodes[index].PositionX);
            Native.SetVehicleWheelYRotation(vehicle, index, nodes[index].RotationY);
        }
    }

    //private void UpdateVehicleDecorators(int vehicle, WheelData data)
    //{
    //    VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, data.DefaultFrontTrackWidth, data.FrontTrackWidth);
    //    VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontCamberID, data.DefaultFrontCamber, data.FrontCamber);
    //    VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, data.DefaultRearTrackWidth, data.RearTrackWidth);
    //    VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearCamberID, data.DefaultRearCamber, data.RearCamber);
    //
    //    VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTrackWidthID, data.FrontTrackWidth, data.DefaultFrontTrackWidth);
    //    VStancerUtilities.UpdateFloatDecorator(vehicle, FrontCamberID, data.FrontCamber, data.DefaultFrontCamber);
    //    VStancerUtilities.UpdateFloatDecorator(vehicle, RearTrackWidthID, data.RearTrackWidth, data.DefaultRearTrackWidth);
    //    VStancerUtilities.UpdateFloatDecorator(vehicle, RearCamberID, data.RearCamber, data.DefaultRearCamber);
    //}

    private void RegisterDecorators()
    {
        Native.DecorRegister(FrontTrackWidthID, 1);
        Native.DecorRegister(FrontCamberID, 1);
        Native.DecorRegister(RearTrackWidthID, 1);
        Native.DecorRegister(RearCamberID, 1);

        Native.DecorRegister(DefaultFrontTrackWidthID, 1);
        Native.DecorRegister(DefaultFrontCamberID, 1);
        Native.DecorRegister(DefaultRearTrackWidthID, 1);
        Native.DecorRegister(DefaultRearCamberID, 1);
    }

    private void RemoveDecoratorsFromVehicle(int vehicle)
    {
        if (Native.DecorExistOn(vehicle, FrontTrackWidthID))
            Native.DecorRemove(vehicle, FrontTrackWidthID);

        if (Native.DecorExistOn(vehicle, FrontCamberID))
            Native.DecorRemove(vehicle, FrontCamberID);

        if (Native.DecorExistOn(vehicle, DefaultFrontTrackWidthID))
            Native.DecorRemove(vehicle, DefaultFrontTrackWidthID);

        if (Native.DecorExistOn(vehicle, DefaultFrontCamberID))
            Native.DecorRemove(vehicle, DefaultFrontCamberID);

        if (Native.DecorExistOn(vehicle, RearTrackWidthID))
            Native.DecorRemove(vehicle, RearTrackWidthID);

        if (Native.DecorExistOn(vehicle, RearCamberID))
            Native.DecorRemove(vehicle, RearCamberID);

        if (Native.DecorExistOn(vehicle, DefaultRearTrackWidthID))
            Native.DecorRemove(vehicle, DefaultRearTrackWidthID);

        if (Native.DecorExistOn(vehicle, DefaultRearCamberID))
            Native.DecorRemove(vehicle, DefaultRearCamberID);
    }

    private void OnMenuCommandInvoked(string commandID)
    {
        switch (commandID)
        {
            case ResetID:
                if (!DataIsValid)
                    return;

                WheelData.Reset();
                break;
        }

    }

    private void OnMenuFloatPropertyChanged(string id, float value)
    {
        switch (id)
        {
            case FrontCamberID:
                if (DataIsValid) WheelData.FrontCamber = value;
                break;
            case RearCamberID:
                if (DataIsValid) WheelData.RearCamber = value;
                break;
            case FrontTrackWidthID:
                if (DataIsValid) WheelData.FrontTrackWidth = -value;
                break;
            case RearTrackWidthID:
                if (DataIsValid) WheelData.RearTrackWidth = -value;
                break;
        }
    }

    internal void PrintDecoratorsInfo(int vehicle)
    {
        if (!Native.DoesEntityExist(vehicle))
        {
            Debug.WriteLine($"{nameof(WheelScript)}: Can't find vehicle with handle {vehicle}");
            return;
        }

        int wheelsCount = Native.GetVehicleNumberOfWheels(vehicle);
        int netID = Native.NetworkGetNetworkIdFromEntity(vehicle);
        StringBuilder s = new StringBuilder();
        s.AppendLine($"{nameof(WheelScript)}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

        if (Native.DecorExistOn(vehicle, FrontTrackWidthID))
            s.AppendLine($"{FrontTrackWidthID}: {Native.DecorGetFloat(vehicle, FrontTrackWidthID)}");

        if (Native.DecorExistOn(vehicle, DefaultFrontTrackWidthID))
            s.AppendLine($"{DefaultFrontTrackWidthID}: {Native.DecorGetFloat(vehicle, DefaultFrontTrackWidthID)}");

        if (Native.DecorExistOn(vehicle, RearTrackWidthID))
            s.AppendLine($"{RearTrackWidthID}: {Native.DecorGetFloat(vehicle, RearTrackWidthID)}");

        if (Native.DecorExistOn(vehicle, DefaultRearTrackWidthID))
            s.AppendLine($"{DefaultRearTrackWidthID}: {Native.DecorGetFloat(vehicle, DefaultRearTrackWidthID)}");

        if (Native.DecorExistOn(vehicle, FrontCamberID))
            s.AppendLine($"{FrontCamberID}: {Native.DecorGetFloat(vehicle, FrontCamberID)}");

        if (Native.DecorExistOn(vehicle, DefaultFrontCamberID))
            s.AppendLine($"{DefaultFrontCamberID}: {Native.DecorGetFloat(vehicle, DefaultFrontCamberID)}");

        if (Native.DecorExistOn(vehicle, RearCamberID))
            s.AppendLine($"{RearCamberID}: {Native.DecorGetFloat(vehicle, RearCamberID)}");

        if (Native.DecorExistOn(vehicle, DefaultRearCamberID))
            s.AppendLine($"{DefaultRearCamberID}: {Native.DecorGetFloat(vehicle, DefaultRearCamberID)}");

        Debug.WriteLine(s.ToString());
    }

    private bool EntityHasDecorators(int entity)
    {
        return (
            Native.DecorExistOn(entity, FrontTrackWidthID) ||
            Native.DecorExistOn(entity, FrontCamberID) ||
            Native.DecorExistOn(entity, RearTrackWidthID) ||
            Native.DecorExistOn(entity, RearCamberID) ||
            Native.DecorExistOn(entity, DefaultFrontTrackWidthID) ||
            Native.DecorExistOn(entity, DefaultFrontCamberID) ||
            Native.DecorExistOn(entity, DefaultRearTrackWidthID) ||
            Native.DecorExistOn(entity, DefaultRearCamberID)
            );
    }

    internal void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
    {
        IEnumerable<int> entities = vehiclesList.Where(entity => EntityHasDecorators(entity));

        Debug.WriteLine($"{nameof(WheelScript)}: Vehicles with decorators: {entities.Count()}");

        foreach (int item in entities)
            Debug.WriteLine($"Vehicle: {item}, netID: {Native.NetworkGetNetworkIdFromEntity(item)}");
    }

    internal WheelPreset GetWheelPreset(bool allowStockPreset = false)
    {
        if (!DataIsValid)
            return null;

        // Only required to avoid saving this preset locally when not required
        if (!WheelData.IsEdited && !allowStockPreset)
            return null;

        return new WheelPreset(WheelData);
    }

    internal async Task SetWheelPreset(WheelPreset preset, bool ignoreEmptyPresets = true)
    {
        if (!DataIsValid)
            return;

        if (preset == null)
        {
            if (!ignoreEmptyPresets)
                WheelData.Reset();

            return;
        }

        // TODO: Check if values are within limits

        WheelData.FrontTrackWidth = preset.FrontTrackWidth;
        WheelData.RearTrackWidth = preset.RearTrackWidth;
        WheelData.FrontCamber = preset.FrontCamber;
        WheelData.RearCamber = preset.RearCamber;

        // Don't refresh, as it's already done by OnWheelDataPropertyChanged

        Debug.WriteLine($"{nameof(WheelScript)}: wheel preset applied");

        await Delay(200);
        WheelDataChanged?.Invoke(this, EventArgs.Empty);
    }

    internal bool API_SetWheelPreset(int vehicle, WheelPreset preset)
    {
        if (preset == null)
            return false;

        if (!Native.DoesEntityExist(vehicle))
            return false;

        float frontTrackWidth = preset.FrontTrackWidth;
        float rearTrackWidth = preset.RearTrackWidth;
        float frontCamber = preset.FrontCamber;
        float rearCamber = preset.RearCamber;

        int wheelsCount = Native.GetVehicleNumberOfWheels(vehicle);
        int frontCount = Utilities.CalculateFrontWheelsCount(wheelsCount);

        float frontTrackWidth_def = Native.DecorExistOn(vehicle, DefaultFrontTrackWidthID) ? Native.DecorGetFloat(vehicle, DefaultFrontTrackWidthID) : Native.GetVehicleWheelXOffset(vehicle, 0);
        float frontCamber_def = Native.DecorExistOn(vehicle, DefaultFrontCamberID) ? Native.DecorGetFloat(vehicle, DefaultFrontCamberID) : Native.GetVehicleWheelYRotation(vehicle, 0);
        float rearTrackWidth_def = Native.DecorExistOn(vehicle, DefaultRearTrackWidthID) ? Native.DecorGetFloat(vehicle, DefaultRearTrackWidthID) : Native.GetVehicleWheelXOffset(vehicle, frontCount);
        float rearCamber_def = Native.DecorExistOn(vehicle, DefaultRearCamberID) ? Native.DecorGetFloat(vehicle, DefaultRearCamberID) : Native.GetVehicleWheelYRotation(vehicle, frontCount);

        if (vehicle == _playerVehicleHandle)
        {
            // TODO: Maybe this is useles and could just use SetWheelPreset instead
            WheelData = new WheelData(wheelsCount, frontTrackWidth_def, frontCamber_def, rearTrackWidth_def, rearCamber_def)
            {
                FrontTrackWidth = frontTrackWidth,
                FrontCamber = frontCamber,
                RearTrackWidth = rearTrackWidth,
                RearCamber = rearCamber
            };
        }
        else
        {
            Utilities.UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, frontTrackWidth_def, frontTrackWidth);
            Utilities.UpdateFloatDecorator(vehicle, DefaultFrontCamberID, frontCamber_def, frontCamber);
            Utilities.UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, rearTrackWidth_def, rearTrackWidth);
            Utilities.UpdateFloatDecorator(vehicle, DefaultRearCamberID, rearCamber_def, rearCamber);

            Utilities.UpdateFloatDecorator(vehicle, FrontTrackWidthID, frontTrackWidth, frontTrackWidth_def);
            Utilities.UpdateFloatDecorator(vehicle, FrontCamberID, frontCamber, frontCamber_def);
            Utilities.UpdateFloatDecorator(vehicle, RearTrackWidthID, rearTrackWidth, rearTrackWidth_def);
            Utilities.UpdateFloatDecorator(vehicle, RearCamberID, rearCamber, rearCamber_def);
        }

        return true;
    }

    internal bool API_GetWheelPreset(int vehicle, out WheelPreset preset)
    {
        preset = null;

        if (!Native.DoesEntityExist(vehicle))
            return false;

        WheelData data = (vehicle == _playerVehicleHandle && DataIsValid) ? WheelData : GetWheelDataFromEntity(vehicle);

        if (data == null)
            return false;

        preset = new WheelPreset(data);
        return true;
    }

    internal bool API_ResetWheelPreset(int vehicle)
    {
        if (!Native.DoesEntityExist(vehicle))
            return false;

        if (vehicle != _playerVehicleHandle)
        {
            RemoveDecoratorsFromVehicle(vehicle);
            UpdateVehicleUsingDecorators(vehicle);
            return true;
        }

        if (!DataIsValid)
            return false;

        WheelData.Reset();

        return true;
    }

    internal bool API_SetFrontCamber(int vehicle, float value)
    {
        if (!Native.DoesEntityExist(vehicle))
            return false;

        if (vehicle != _playerVehicleHandle)
        {
            float value_def = Native.DecorExistOn(vehicle, DefaultFrontCamberID) ? Native.DecorGetFloat(vehicle, DefaultFrontCamberID) : Native.GetVehicleWheelYRotation(vehicle, 0);
            Utilities.UpdateFloatDecorator(vehicle, DefaultFrontCamberID, value_def, value);
            Utilities.UpdateFloatDecorator(vehicle, FrontCamberID, value, value_def);
            return true;
        }

        if (!DataIsValid)
            return false;

        WheelData.FrontCamber = value;
        WheelDataChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    internal bool API_SetRearCamber(int vehicle, float value)
    {
        if (!Native.DoesEntityExist(vehicle))
            return false;

        if (vehicle != _playerVehicleHandle)
        {
            int wheelsCount = Native.GetVehicleNumberOfWheels(vehicle);
            int frontCount = Utilities.CalculateFrontWheelsCount(wheelsCount);

            float value_def = Native.DecorExistOn(vehicle, DefaultRearCamberID) ? Native.DecorGetFloat(vehicle, DefaultRearCamberID) : Native.GetVehicleWheelYRotation(vehicle, frontCount);
            Utilities.UpdateFloatDecorator(vehicle, DefaultRearCamberID, value_def, value);
            Utilities.UpdateFloatDecorator(vehicle, RearCamberID, value, value_def);
            return true;
        }

        if (!DataIsValid)
            return false;

        WheelData.RearCamber = value;
        WheelDataChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    internal bool API_SetFrontTrackWidth(int vehicle, float value)
    {
        if (!Native.DoesEntityExist(vehicle))
            return false;

        if (vehicle != _playerVehicleHandle)
        {
            float value_def = Native.DecorExistOn(vehicle, DefaultFrontTrackWidthID) ? Native.DecorGetFloat(vehicle, DefaultFrontTrackWidthID) : Native.GetVehicleWheelXOffset(vehicle, 0);
            Utilities.UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, value_def, value);
            Utilities.UpdateFloatDecorator(vehicle, FrontTrackWidthID, value, value_def);
            return true;
        }

        if (!DataIsValid)
            return false;

        WheelData.FrontTrackWidth = value;
        WheelDataChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    internal bool API_SetRearTrackWidth(int vehicle, float value)
    {
        if (!Native.DoesEntityExist(vehicle))
            return false;

        if (vehicle != _playerVehicleHandle)
        {
            int wheelsCount = Native.GetVehicleNumberOfWheels(vehicle);
            int frontCount = Utilities.CalculateFrontWheelsCount(wheelsCount);

            float value_def = Native.DecorExistOn(vehicle, DefaultRearTrackWidthID) ? Native.DecorGetFloat(vehicle, DefaultRearTrackWidthID) : Native.GetVehicleWheelXOffset(vehicle, frontCount);
            Utilities.UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, value_def, value);
            Utilities.UpdateFloatDecorator(vehicle, RearTrackWidthID, value, value_def);
            return true;
        }

        if (!DataIsValid)
            return false;

        WheelData.RearTrackWidth = value;
        WheelDataChanged?.Invoke(this, EventArgs.Empty);

        return true;
    }

    internal bool API_GetFrontCamber(int vehicle, out float value)
    {
        value = default;

        if (!Native.DoesEntityExist(vehicle))
            return false;

        value = Native.DecorExistOn(vehicle, FrontCamberID) ? Native.DecorGetFloat(vehicle, FrontCamberID) : Native.GetVehicleWheelYRotation(vehicle, 0);
        return true;
    }

    internal bool API_GetRearCamber(int vehicle, out float value)
    {
        value = default;

        if (!Native.DoesEntityExist(vehicle))
            return false;

        int frontCount = Utilities.CalculateFrontWheelsCount(Native.GetVehicleNumberOfWheels(vehicle));
        value = Native.DecorExistOn(vehicle, RearCamberID) ? Native.DecorGetFloat(vehicle, RearCamberID) : Native.GetVehicleWheelYRotation(vehicle, frontCount);
        return true;
    }

    internal bool API_GetFrontTrackWidth(int vehicle, out float value)
    {
        value = default;

        if (!Native.DoesEntityExist(vehicle))
            return false;

        value = Native.DecorExistOn(vehicle, FrontTrackWidthID) ? Native.DecorGetFloat(vehicle, FrontTrackWidthID) : Native.GetVehicleWheelXOffset(vehicle, 0);
        return true;
    }

    internal bool API_GetRearTrackWidth(int vehicle, out float value)
    {
        value = default;

        if (!Native.DoesEntityExist(vehicle))
            return false;

        int frontCount = Utilities.CalculateFrontWheelsCount(Native.GetVehicleNumberOfWheels(vehicle));
        value = Native.DecorExistOn(vehicle, RearTrackWidthID) ? Native.DecorGetFloat(vehicle, RearTrackWidthID) : Native.GetVehicleWheelXOffset(vehicle, frontCount);
        return true;
    }
}
