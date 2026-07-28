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

internal class WheelModScript : BaseScript
{
    private readonly MainScript _mainScript;

    private long _lastTime;
    private int _playerVehicleHandle;
    private int _vehicleWheelMod;

    internal int VehicleWheelMod
    {
        get => _vehicleWheelMod;
        set
        {
            if (Equals(_vehicleWheelMod, value))
                return;

            _vehicleWheelMod = value;
            VehicleWheelModChanged();
        }
    }

    private WheelModData _wheelModData;
    internal WheelModData WheelModData
    {
        get => _wheelModData;
        set
        {
            if (Equals(_wheelModData, value))
                return;

            _wheelModData = value;
            WheelModDataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal Config Config => _mainScript.Config;
    internal WheelModMenu Menu { get; private set; }
    public bool DataIsValid => _playerVehicleHandle != -1 && WheelModData != null && VehicleWheelMod != -1;

    internal const string WheelModResetID = "vstancer_wheelmod_reset";

    internal const string WheelWidthID = "vstancer_wheelmod_width";
    internal const string WheelSizeID = "vstancer_wheelmod_size";
    internal const string DefaultWidthID = "vstancer_wheelmod_width_def";
    internal const string DefaultSizeID = "vstancer_wheelmod_size_def";

    internal const string FrontTireColliderWidthID = "vstancer_wheelmod_tirecollider_width_f";
    internal const string FrontTireColliderSizeID = "vstancer_wheelmod_tirecollider_size_f";
    internal const string FrontRimColliderSizeID = "vstancer_wheelmod_rimcollider_size_f";

    internal const string RearTireColliderWidthID = "vstancer_wheelmod_tirecollider_width_r";
    internal const string RearTireColliderSizeID = "vstancer_wheelmod_tirecollider_size_r";
    internal const string RearRimColliderSizeID = "vstancer_wheelmod_rimcollider_size_r";

    internal const string DefaultFrontTireColliderWidthID = "vstancer_wheelmod_tirecollider_width_f_def";
    internal const string DefaultFrontTireColliderSizeID = "vstancer_wheelmod_tirecollider_size_f_def";
    internal const string DefaultFrontRimColliderSizeID = "vstancer_wheelmod_rimcollider_size_f_def";

    internal const string DefaultRearTireColliderWidthID = "vstancer_wheelmod_tirecollider_width_r_def";
    internal const string DefaultRearTireColliderSizeID = "vstancer_wheelmod_tirecollider_size_r_def";
    internal const string DefaultRearRimColliderSizeID = "vstancer_wheelmod_rimcollider_size_r_def";

    public event EventHandler WheelModDataChanged;

    internal WheelModScript(MainScript mainScript)
    {
        _mainScript = mainScript;

        RegisterDecorators();

        _lastTime = Native.GetGameTimer();

        _playerVehicleHandle = -1;
        VehicleWheelMod = -1;
        WheelModData = null;

        if (!_mainScript.Config.DisableMenu)
        {
            Menu = new WheelModMenu(this);
            Menu.FloatPropertyChangedEvent += OnMenuFloatPropertyChanged;
            Menu.ResetPropertiesEvent += (sender, id) => OnMenuCommandInvoked(id);
        }

        // TODO: Consider using a task as workaround for values resetting when any tuning part is installed
        //Tick += TickTask;
        Tick += TimedTask;

        mainScript.PlayerVehicleHandleChanged += (sender, handle) => PlayerVehicleChanged(handle);

        WheelModDataChanged += (sender, args) => OnWheelModDataChanged();

        PlayerVehicleChanged(_mainScript.PlayerVehicleHandle);
    }

    //private async Task TickTask()
    //{
    //    await Task.FromResult(0);
    //
    //    if (!DataIsValid)
    //        return;
    //
    //    UpdateVehicleUsingData(_playerVehicleHandle, WheelModData);
    //}

    private async Task GetVehicleWheelModTask()
    {
        if (_playerVehicleHandle == -1)
            return;

        VehicleWheelMod = Native.GetVehicleMod(_playerVehicleHandle, 23);

        await Task.FromResult(0);
    }

    private void OnWheelModDataChanged()
    {
        if (WheelModData != null)
            WheelModData.PropertyChanged += OnWheelModDataPropertyChanged;
    }

    private async void VehicleWheelModChanged()
    {
        if (_playerVehicleHandle == -1)
            return;

        if (WheelModData != null)
            WheelModData.PropertyChanged -= OnWheelModDataPropertyChanged;

        if (VehicleWheelMod == -1)
        {
            WheelModData = null;
            RemoveDecoratorsFromVehicle(_playerVehicleHandle);
            return;
        }

        WheelModData = await GetWheelModDataFromEntity(_playerVehicleHandle);
    }

    private void PlayerVehicleChanged(int vehicle)
    {
        if (vehicle == _playerVehicleHandle)
            return;

        _playerVehicleHandle = vehicle;

        if (WheelModData != null)
            WheelModData.PropertyChanged -= OnWheelModDataPropertyChanged;

        if (_playerVehicleHandle == -1)
        {
            VehicleWheelMod = -1;
            WheelModData = null;
            Tick -= GetVehicleWheelModTask;
            return;
        }

        Tick += GetVehicleWheelModTask;
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

    private void UpdateVehicleUsingData(int vehicle, WheelModData data)
    {
        if (!Native.DoesEntityExist(vehicle) || data == null)
            return;

        //if (!MathUtil.WithinEpsilon(GetVehicleWheelWidth(vehicle), data.WheelWidth, VStancerUtilities.Epsilon))
        Native.SetVehicleWheelWidth(vehicle, data.WheelWidth);

        //if (!MathUtil.WithinEpsilon(GetVehicleWheelSize(vehicle), data.WheelSize, VStancerUtilities.Epsilon))
        Native.SetVehicleWheelSize(vehicle, data.WheelSize);

        for (int i = 0; i < data.FrontWheelsCount; i++)
        {
            Native.SetVehicleWheelTireColliderWidth(vehicle, i, data.FrontTireColliderWidth);
            Native.SetVehicleWheelTireColliderSize(vehicle, i, data.FrontTireColliderSize);
            Native.SetVehicleWheelRimColliderSize(vehicle, i, data.FrontRimColliderSize);
        }

        for (int i = data.FrontWheelsCount; i < data.WheelsCount; i++)
        {
            Native.SetVehicleWheelTireColliderWidth(vehicle, i, data.RearTireColliderWidth);
            Native.SetVehicleWheelTireColliderSize(vehicle, i, data.RearTireColliderSize);
            Native.SetVehicleWheelRimColliderSize(vehicle, i, data.RearRimColliderSize);
        }
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

    private async Task<WheelModData> GetWheelModDataFromEntity(int vehicle)
    {
        if (!Native.DoesEntityExist(vehicle))
            return null;

        int wheelsCount = Native.GetVehicleNumberOfWheels(vehicle);
        int frontCount = Utilities.CalculateFrontWheelsCount(wheelsCount);

        float wheelWidth_def;
        float wheelSize_def;

        // wait for data to actually update, required if wheel mod is changed too fast
        // and data read by GetVehicleWheelWidth and GetVehicleWheelSize didn't update yet resulting in 0
        if (Native.DecorExistOn(vehicle, DefaultWidthID))
        {
            wheelWidth_def = Native.DecorGetFloat(vehicle, DefaultWidthID);
        }
        else
        {
            do
            {
                if (!Native.DoesEntityExist(vehicle))
                    return null;

                wheelWidth_def = Native.GetVehicleWheelWidth(vehicle);
                await Delay(100);
            } while (MathUtil.IsZero(wheelWidth_def) || MathUtil.IsOne(wheelWidth_def));
        }

        if (DecorExistOn(vehicle, DefaultSizeID))
        {
            wheelSize_def = DecorGetFloat(vehicle, DefaultSizeID);
        }
        else
        {
            do
            {
                if (!Native.DoesEntityExist(vehicle))
                    return null;

                wheelSize_def = Native.GetVehicleWheelSize(vehicle);
                await Delay(100);
            } while (MathUtil.IsZero(wheelSize_def) || MathUtil.IsOne(wheelSize_def));
        }

        float frontTireColliderWidth_def = Native.DecorExistOn(vehicle, DefaultFrontTireColliderWidthID) ? Native.DecorGetFloat(vehicle, DefaultFrontTireColliderWidthID) : Native.GetVehicleWheelTireColliderWidth(vehicle, 0);
        float frontTireColliderSize_def = Native.DecorExistOn(vehicle, DefaultFrontTireColliderSizeID) ? Native.DecorGetFloat(vehicle, DefaultFrontTireColliderSizeID) : Native.GetVehicleWheelTireColliderSize(vehicle, 0);
        float frontRimColliderSize_def = Native.DecorExistOn(vehicle, DefaultFrontRimColliderSizeID) ? Native.DecorGetFloat(vehicle, DefaultFrontRimColliderSizeID) : Native.GetVehicleWheelRimColliderSize(vehicle, 0);

        float rearTireColliderWidth_def = Native.DecorExistOn(vehicle, DefaultRearTireColliderWidthID) ? Native.DecorGetFloat(vehicle, DefaultRearTireColliderWidthID) : Native.GetVehicleWheelTireColliderWidth(vehicle, frontCount);
        float rearTireColliderSize_def = Native.DecorExistOn(vehicle, DefaultRearTireColliderSizeID) ? Native.DecorGetFloat(vehicle, DefaultRearTireColliderSizeID) : Native.GetVehicleWheelTireColliderSize(vehicle, frontCount);
        float rearRimColliderSize_def = Native.DecorExistOn(vehicle, DefaultRearRimColliderSizeID) ? Native.DecorGetFloat(vehicle, DefaultRearRimColliderSizeID) : Native.GetVehicleWheelRimColliderSize(vehicle, frontCount);

        // Create the preset with the default values
        return new WheelModData(wheelsCount, wheelWidth_def, wheelSize_def,
            frontTireColliderWidth_def, frontTireColliderSize_def, frontRimColliderSize_def,
            rearTireColliderWidth_def, rearTireColliderSize_def, rearRimColliderSize_def)
        {
            // Assign the current values
            WheelWidth = Native.DecorExistOn(vehicle, WheelWidthID) ? Native.DecorGetFloat(vehicle, WheelWidthID) : wheelWidth_def,
            WheelSize = Native.DecorExistOn(vehicle, WheelSizeID) ? Native.DecorGetFloat(vehicle, WheelSizeID) : wheelSize_def,

            FrontTireColliderWidth = Native.DecorExistOn(vehicle, FrontTireColliderWidthID) ? Native.DecorGetFloat(vehicle, FrontTireColliderWidthID) : frontTireColliderWidth_def,
            FrontTireColliderSize = Native.DecorExistOn(vehicle, FrontTireColliderSizeID) ? Native.DecorGetFloat(vehicle, FrontTireColliderSizeID) : frontTireColliderSize_def,
            FrontRimColliderSize = Native.DecorExistOn(vehicle, FrontRimColliderSizeID) ? Native.DecorGetFloat(vehicle, FrontRimColliderSizeID) : frontRimColliderSize_def,

            RearTireColliderWidth = Native.DecorExistOn(vehicle, RearTireColliderWidthID) ? Native.DecorGetFloat(vehicle, RearTireColliderWidthID) : rearTireColliderWidth_def,
            RearTireColliderSize = Native.DecorExistOn(vehicle, RearTireColliderSizeID) ? Native.DecorGetFloat(vehicle, RearTireColliderSizeID) : rearTireColliderSize_def,
            RearRimColliderSize = Native.DecorExistOn(vehicle, RearRimColliderSizeID) ? Native.DecorGetFloat(vehicle, RearRimColliderSizeID) : rearRimColliderSize_def
        };
    }

    private void SetWheelWidthUsingData(int vehicle, WheelModData data)
    {
        float value = data.WheelWidth;
        float defValue = data.DefaultWheelWidth;

        Native.SetVehicleWheelWidth(vehicle, value);
        Utilities.UpdateFloatDecorator(vehicle, DefaultWidthID, defValue, value);
        Utilities.UpdateFloatDecorator(vehicle, WheelWidthID, value, defValue);
    }

    private void SetWheelSizeUsingData(int vehicle, WheelModData data)
    {
        float value = data.WheelSize;
        float defValue = data.DefaultWheelSize;

        Native.SetVehicleWheelSize(vehicle, value);
        Utilities.UpdateFloatDecorator(vehicle, DefaultSizeID, defValue, value);
        Utilities.UpdateFloatDecorator(vehicle, WheelSizeID, value, defValue);
    }

    private void SetFrontTireColliderWidthUsingData(int vehicle, WheelModData data)
    {
        float value = data.FrontTireColliderWidth;
        float defValue = data.DefaultFrontTireColliderWidth;

        for (int i = 0; i < WheelModData.FrontWheelsCount; i++)
            Native.SetVehicleWheelTireColliderWidth(vehicle, i, value);

        Utilities.UpdateFloatDecorator(vehicle, DefaultFrontTireColliderWidthID, defValue, value);
        Utilities.UpdateFloatDecorator(vehicle, FrontTireColliderWidthID, value, defValue);
    }

    private void SetFrontTireColliderSizeUsingData(int vehicle, WheelModData data)
    {
        float value = data.FrontTireColliderSize;
        float defValue = data.DefaultFrontTireColliderSize;

        for (int i = 0; i < WheelModData.FrontWheelsCount; i++)
            Native.SetVehicleWheelTireColliderSize(vehicle, i, value);

        Utilities.UpdateFloatDecorator(vehicle, DefaultFrontTireColliderSizeID, defValue, value);
        Utilities.UpdateFloatDecorator(vehicle, FrontTireColliderSizeID, value, defValue);
    }

    private void SetFrontRimColliderSizeUsingData(int vehicle, WheelModData data)
    {
        float value = data.FrontRimColliderSize;
        float defValue = data.DefaultFrontRimColliderSize;

        for (int i = 0; i < WheelModData.FrontWheelsCount; i++)
            Native.SetVehicleWheelRimColliderSize(vehicle, i, value);

        Utilities.UpdateFloatDecorator(vehicle, DefaultFrontRimColliderSizeID, defValue, value);
        Utilities.UpdateFloatDecorator(vehicle, FrontRimColliderSizeID, value, defValue);
    }

    private void SetRearTireColliderWidthUsingData(int vehicle, WheelModData data)
    {
        float value = data.RearTireColliderWidth;
        float defValue = data.DefaultRearTireColliderWidth;

        for (int i = WheelModData.FrontWheelsCount; i < WheelModData.WheelsCount; i++)
            Native.SetVehicleWheelTireColliderWidth(vehicle, i, value);

        Utilities.UpdateFloatDecorator(vehicle, DefaultRearTireColliderWidthID, defValue, value);
        Utilities.UpdateFloatDecorator(vehicle, RearTireColliderWidthID, value, defValue);
    }

    private void SetRearTireColliderSizeUsingData(int vehicle, WheelModData data)
    {
        float value = data.RearTireColliderSize;
        float defValue = data.DefaultRearTireColliderSize;

        for (int i = WheelModData.FrontWheelsCount; i < WheelModData.WheelsCount; i++)
            Native.SetVehicleWheelTireColliderSize(vehicle, i, value);

        Utilities.UpdateFloatDecorator(vehicle, DefaultRearTireColliderSizeID, defValue, value);
        Utilities.UpdateFloatDecorator(vehicle, RearTireColliderSizeID, value, defValue);
    }

    private void SetRearRimColliderSizeUsingData(int vehicle, WheelModData data)
    {
        float value = data.RearRimColliderSize;
        float defValue = data.DefaultRearRimColliderSize;

        for (int i = WheelModData.FrontWheelsCount; i < WheelModData.WheelsCount; i++)
            Native.SetVehicleWheelRimColliderSize(vehicle, i, value);

        Utilities.UpdateFloatDecorator(vehicle, DefaultRearRimColliderSizeID, defValue, value);
        Utilities.UpdateFloatDecorator(vehicle, RearRimColliderSizeID, value, defValue);
    }

    private void OnWheelModDataPropertyChanged(string propertyName, float value)
    {
        switch (propertyName)
        {
            case nameof(WheelModData.WheelWidth):
                SetWheelWidthUsingData(_playerVehicleHandle, WheelModData);
                break;

            case nameof(WheelModData.WheelSize):
                SetWheelSizeUsingData(_playerVehicleHandle, WheelModData);
                break;

            case nameof(WheelModData.FrontTireColliderWidth):
                SetFrontTireColliderWidthUsingData(_playerVehicleHandle, WheelModData);
                break;
            case nameof(WheelModData.FrontTireColliderSize):
                SetFrontTireColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                break;
            case nameof(WheelModData.FrontRimColliderSize):
                SetFrontRimColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                break;

            case nameof(WheelModData.RearTireColliderWidth):
                SetRearTireColliderWidthUsingData(_playerVehicleHandle, WheelModData);
                break;
            case nameof(WheelModData.RearTireColliderSize):
                SetRearTireColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                break;
            case nameof(WheelModData.RearRimColliderSize):
                SetRearRimColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                break;

            case nameof(WheelModData.Reset):

                // TODO: Avoid updating decorators if we have to remove them anyway
                //SetWheelWidthUsingData(_playerVehicleHandle, WheelModData);
                //SetWheelSizeUsingData(_playerVehicleHandle, WheelModData);
                //SetFrontTireColliderWidthUsingData(_playerVehicleHandle, WheelModData);
                //SetFrontTireColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                //SetFrontRimColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                //SetRearTireColliderWidthUsingData(_playerVehicleHandle, WheelModData);
                //SetRearTireColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                //SetRearRimColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                UpdateVehicleUsingData(_playerVehicleHandle, WheelModData);
                RemoveDecoratorsFromVehicle(_playerVehicleHandle);

                WheelModDataChanged?.Invoke(this, EventArgs.Empty);
                break;

            default:
                break;
        }
    }

    private void UpdateVehicleUsingDecorators(int vehicle)
    {
        int wheelsCount = Native.GetVehicleNumberOfWheels(vehicle);
        int frontWheelsCount = Utilities.CalculateFrontWheelsCount(wheelsCount);

        if (Native.DecorExistOn(vehicle, WheelSizeID))
            Native.SetVehicleWheelSize(vehicle, Native.DecorGetFloat(vehicle, WheelSizeID));

        if (Native.DecorExistOn(vehicle, WheelWidthID))
            Native.SetVehicleWheelWidth(vehicle, Native.DecorGetFloat(vehicle, WheelWidthID));

        if (Native.DecorExistOn(vehicle, FrontTireColliderWidthID))
        {
            float value = Native.DecorGetFloat(vehicle, FrontTireColliderWidthID);
            for (int i = 0; i < frontWheelsCount; i++)
                Native.SetVehicleWheelTireColliderWidth(vehicle, i, value);
        }

        if (Native.DecorExistOn(vehicle, FrontTireColliderSizeID))
        {
            float value = Native.DecorGetFloat(vehicle, FrontTireColliderSizeID);
            for (int i = 0; i < frontWheelsCount; i++)
                Native.SetVehicleWheelTireColliderSize(vehicle, i, value);
        }

        if (Native.DecorExistOn(vehicle, FrontRimColliderSizeID))
        {
            float value = Native.DecorGetFloat(vehicle, FrontRimColliderSizeID);
            for (int i = 0; i < frontWheelsCount; i++)
                Native.SetVehicleWheelRimColliderSize(vehicle, i, value);
        }

        if (Native.DecorExistOn(vehicle, RearTireColliderWidthID))
        {
            float value = Native.DecorGetFloat(vehicle, RearTireColliderWidthID);
            for (int i = frontWheelsCount; i < wheelsCount; i++)
                Native.SetVehicleWheelTireColliderWidth(vehicle, i, value);
        }

        if (Native.DecorExistOn(vehicle, RearTireColliderSizeID))
        {
            float value = Native.DecorGetFloat(vehicle, RearTireColliderSizeID);
            for (int i = frontWheelsCount; i < wheelsCount; i++)
                Native.SetVehicleWheelTireColliderSize(vehicle, i, value);
        }

        if (Native.DecorExistOn(vehicle, RearRimColliderSizeID))
        {
            float value = Native.DecorGetFloat(vehicle, RearRimColliderSizeID);
            for (int i = frontWheelsCount; i < wheelsCount; i++)
                Native.SetVehicleWheelRimColliderSize(vehicle, i, value);
        }
    }

    private void RegisterDecorators()
    {
        Native.DecorRegister(WheelWidthID, 1);
        Native.DecorRegister(DefaultWidthID, 1);

        Native.DecorRegister(WheelSizeID, 1);
        Native.DecorRegister(DefaultSizeID, 1);

        Native.DecorRegister(FrontTireColliderWidthID, 1);
        Native.DecorRegister(FrontTireColliderSizeID, 1);
        Native.DecorRegister(FrontRimColliderSizeID, 1);
        Native.DecorRegister(DefaultFrontTireColliderWidthID, 1);
        Native.DecorRegister(DefaultFrontTireColliderSizeID, 1);
        Native.DecorRegister(DefaultFrontRimColliderSizeID, 1);

        Native.DecorRegister(RearTireColliderWidthID, 1);
        Native.DecorRegister(RearTireColliderSizeID, 1);
        Native.DecorRegister(RearRimColliderSizeID, 1);
        Native.DecorRegister(DefaultRearTireColliderWidthID, 1);
        Native.DecorRegister(DefaultRearTireColliderSizeID, 1);
        Native.DecorRegister(DefaultRearRimColliderSizeID, 1);
    }

    private void RemoveDecoratorsFromVehicle(int vehicle)
    {
        if (Native.DecorExistOn(vehicle, WheelSizeID))
            Native.DecorRemove(vehicle, WheelSizeID);

        if (Native.DecorExistOn(vehicle, WheelWidthID))
            Native.DecorRemove(vehicle, WheelWidthID);

        if (Native.DecorExistOn(vehicle, DefaultSizeID))
            Native.DecorRemove(vehicle, DefaultSizeID);

        if (Native.DecorExistOn(vehicle, DefaultWidthID))
            Native.DecorRemove(vehicle, DefaultWidthID);

        if (Native.DecorExistOn(vehicle, FrontTireColliderWidthID))
            Native.DecorRemove(vehicle, FrontTireColliderWidthID);

        if (Native.DecorExistOn(vehicle, FrontTireColliderSizeID))
            Native.DecorRemove(vehicle, FrontTireColliderSizeID);

        if (Native.DecorExistOn(vehicle, FrontRimColliderSizeID))
            Native.DecorRemove(vehicle, FrontRimColliderSizeID);

        if (Native.DecorExistOn(vehicle, DefaultFrontTireColliderWidthID))
            Native.DecorRemove(vehicle, DefaultFrontTireColliderWidthID);

        if (Native.DecorExistOn(vehicle, DefaultFrontTireColliderSizeID))
            Native.DecorRemove(vehicle, DefaultFrontTireColliderSizeID);

        if (Native.DecorExistOn(vehicle, DefaultFrontRimColliderSizeID))
            Native.DecorRemove(vehicle, DefaultFrontRimColliderSizeID);

        if (Native.DecorExistOn(vehicle, RearTireColliderWidthID))
            Native.DecorRemove(vehicle, RearTireColliderWidthID);

        if (Native.DecorExistOn(vehicle, RearTireColliderSizeID))
            Native.DecorRemove(vehicle, RearTireColliderSizeID);

        if (Native.DecorExistOn(vehicle, RearRimColliderSizeID))
            Native.DecorRemove(vehicle, RearRimColliderSizeID);

        if (Native.DecorExistOn(vehicle, DefaultRearTireColliderWidthID))
            Native.DecorRemove(vehicle, DefaultRearTireColliderWidthID);

        if (Native.DecorExistOn(vehicle, DefaultRearTireColliderSizeID))
            Native.DecorRemove(vehicle, DefaultRearTireColliderSizeID);

        if (Native.DecorExistOn(vehicle, DefaultRearRimColliderSizeID))
            Native.DecorRemove(vehicle, DefaultRearRimColliderSizeID);
    }

    private void OnMenuCommandInvoked(string commandID)
    {
        if (!DataIsValid)
            return;

        switch (commandID)
        {
            case WheelModResetID:
                WheelModData.Reset();
                break;
        }
    }

    private void OnMenuFloatPropertyChanged(string id, float value)
    {
        if (!DataIsValid)
            return;

        switch (id)
        {
            case WheelSizeID:
                WheelModData.WheelSize = value;
                WheelModData.FrontTireColliderSize = value / WheelModData.DefaultFrontTireColliderSizeRatio;
                WheelModData.FrontRimColliderSize = value / WheelModData.DefaultFrontRimColliderSizeRatio;
                WheelModData.RearTireColliderSize = value / WheelModData.DefaultRearTireColliderSizeRatio;
                WheelModData.RearRimColliderSize = value / WheelModData.DefaultRearRimColliderSizeRatio;
                break;
            case WheelWidthID:
                WheelModData.WheelWidth = value;
                WheelModData.FrontTireColliderWidth = value / WheelModData.DefaultFrontTireColliderWidthRatio;
                WheelModData.RearTireColliderWidth = value / WheelModData.DefaultRearTireColliderWidthRatio;
                break;

                // Update colliders with visual but keep visual/collider ratio constant as with default wheels
                // This ratio is usually 50% for for vanilla wheel mod

                /*
            case FrontTireColliderWidthID:
                WheelModData.FrontTireColliderWidth = value;
                break;
            case FrontTireColliderSizeID:
                WheelModData.FrontTireColliderSize = value;
                break;
            case FrontRimColliderSizeID:
                WheelModData.FrontRimColliderSize = value;
                break;

            case RearTireColliderWidthID:
                WheelModData.RearTireColliderWidth = value;
                break;
            case RearTireColliderSizeID:
                WheelModData.RearTireColliderSize = value;
                break;
            case RearRimColliderSizeID:
                WheelModData.RearRimColliderSize = value;
                break;
                */
        }
    }

    private bool EntityHasDecorators(int entity)
    {
        return (
            Native.DecorExistOn(entity, WheelSizeID) ||
            Native.DecorExistOn(entity, WheelWidthID) ||
            Native.DecorExistOn(entity, DefaultSizeID) ||
            Native.DecorExistOn(entity, DefaultWidthID) ||
            Native.DecorExistOn(entity, FrontTireColliderWidthID) ||
            Native.DecorExistOn(entity, FrontTireColliderSizeID) ||
            Native.DecorExistOn(entity, FrontRimColliderSizeID) ||
            Native.DecorExistOn(entity, DefaultFrontTireColliderWidthID) ||
            Native.DecorExistOn(entity, DefaultFrontTireColliderSizeID) ||
            Native.DecorExistOn(entity, DefaultFrontRimColliderSizeID) ||
            Native.DecorExistOn(entity, RearTireColliderWidthID) ||
            Native.DecorExistOn(entity, RearTireColliderSizeID) ||
            Native.DecorExistOn(entity, RearRimColliderSizeID) ||
            Native.DecorExistOn(entity, DefaultRearTireColliderWidthID) ||
            Native.DecorExistOn(entity, DefaultRearTireColliderSizeID) ||
            Native.DecorExistOn(entity, DefaultRearRimColliderSizeID)
            );
    }

    internal void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
    {
        IEnumerable<int> entities = vehiclesList.Where(entity => EntityHasDecorators(entity));

        Debug.WriteLine($"{nameof(WheelModScript)}: Vehicles with decorators: {entities.Count()}");

        foreach (int item in entities)
            Debug.WriteLine($"Vehicle: {item}, netID: {Native.NetworkGetNetworkIdFromEntity(item)}");
    }

    internal void PrintDecoratorsInfo(int vehicle)
    {
        if (!Native.DoesEntityExist(vehicle))
        {
            Debug.WriteLine($"{nameof(WheelModScript)}: Can't find vehicle with handle {vehicle}");
            return;
        }

        int wheelsCount = Native.GetVehicleNumberOfWheels(vehicle);
        int netID = Native.NetworkGetNetworkIdFromEntity(vehicle);
        StringBuilder s = new StringBuilder();
        s.AppendLine($"{nameof(WheelModScript)}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

        if (Native.DecorExistOn(vehicle, WheelSizeID))

            s.AppendLine($"{WheelSizeID}: {Native.DecorGetFloat(vehicle, WheelSizeID)}");

        if (Native.DecorExistOn(vehicle, DefaultSizeID))
            s.AppendLine($"{DefaultSizeID}: {Native.DecorGetFloat(vehicle, DefaultSizeID)}");

        if (Native.DecorExistOn(vehicle, WheelWidthID))
            s.AppendLine($"{WheelWidthID}: {Native.DecorGetFloat(vehicle, WheelWidthID)}");

        if (Native.DecorExistOn(vehicle, DefaultWidthID))
            s.AppendLine($"{DefaultWidthID}: {Native.DecorGetFloat(vehicle, DefaultWidthID)}");

        if (Native.DecorExistOn(vehicle, FrontTireColliderWidthID))
            s.AppendLine($"{FrontTireColliderWidthID}: {Native.DecorGetFloat(vehicle, FrontTireColliderWidthID)}");

        if (Native.DecorExistOn(vehicle, DefaultFrontTireColliderWidthID))
            s.AppendLine($"{DefaultFrontTireColliderWidthID}: {Native.DecorGetFloat(vehicle, DefaultFrontTireColliderWidthID)}");

        if (Native.DecorExistOn(vehicle, RearTireColliderWidthID))
            s.AppendLine($"{RearTireColliderWidthID}: {Native.DecorGetFloat(vehicle, RearTireColliderWidthID)}");

        if (Native.DecorExistOn(vehicle, DefaultRearTireColliderWidthID))
            s.AppendLine($"{DefaultRearTireColliderWidthID}: {Native.DecorGetFloat(vehicle, DefaultRearTireColliderWidthID)}");

        if (Native.DecorExistOn(vehicle, FrontTireColliderSizeID))
            s.AppendLine($"{FrontTireColliderSizeID}: {Native.DecorGetFloat(vehicle, FrontTireColliderSizeID)}");

        if (Native.DecorExistOn(vehicle, DefaultFrontTireColliderSizeID))
            s.AppendLine($"{DefaultFrontTireColliderSizeID}: {Native.DecorGetFloat(vehicle, DefaultFrontTireColliderSizeID)}");

        if (Native.DecorExistOn(vehicle, RearTireColliderSizeID))
            s.AppendLine($"{RearTireColliderSizeID}: {Native.DecorGetFloat(vehicle, RearTireColliderSizeID)}");

        if (Native.DecorExistOn(vehicle, DefaultRearTireColliderSizeID))
            s.AppendLine($"{DefaultRearTireColliderSizeID}: {Native.DecorGetFloat(vehicle, DefaultRearTireColliderSizeID)}");

        if (Native.DecorExistOn(vehicle, FrontRimColliderSizeID))
            s.AppendLine($"{FrontRimColliderSizeID}: {Native.DecorGetFloat(vehicle, FrontRimColliderSizeID)}");

        if (Native.DecorExistOn(vehicle, DefaultFrontRimColliderSizeID))
            s.AppendLine($"{DefaultFrontRimColliderSizeID}: {Native.DecorGetFloat(vehicle, DefaultFrontRimColliderSizeID)}");

        if (Native.DecorExistOn(vehicle, RearRimColliderSizeID))
            s.AppendLine($"{RearRimColliderSizeID}: {Native.DecorGetFloat(vehicle, RearRimColliderSizeID)}");

        if (Native.DecorExistOn(vehicle, DefaultRearRimColliderSizeID))
            s.AppendLine($"{DefaultRearRimColliderSizeID}: {Native.DecorGetFloat(vehicle, DefaultRearRimColliderSizeID)}");

        Debug.WriteLine(s.ToString());
    }

    internal WheelModPreset GetWheelModPreset(bool allowStockPreset = false)
    {
        if (!DataIsValid)
            return null;

        // Only required to avoid saving this preset locally when not required
        if (!WheelModData.IsEdited && !allowStockPreset)
            return null;

        return new WheelModPreset(WheelModData);
    }

    internal async Task SetWheelModPreset(WheelModPreset preset, bool ignoreEmptyPresets = true)
    {
        if (!DataIsValid)
            return;

        if (preset == null)
        {
            if (!ignoreEmptyPresets)
                WheelModData.Reset();

            return;
        }

        // TODO: Check if values are within limits

        WheelModData.WheelSize = preset.WheelSize;
        WheelModData.WheelWidth = preset.WheelWidth;

        WheelModData.FrontTireColliderWidth = preset.WheelWidth / WheelModData.DefaultFrontTireColliderWidthRatio;
        WheelModData.FrontTireColliderSize = preset.WheelSize / WheelModData.DefaultFrontTireColliderSizeRatio;
        WheelModData.FrontRimColliderSize = preset.WheelSize / WheelModData.DefaultFrontRimColliderSizeRatio;
        WheelModData.RearTireColliderWidth = preset.WheelWidth / WheelModData.DefaultRearTireColliderWidthRatio;
        WheelModData.RearTireColliderSize = preset.WheelSize / WheelModData.DefaultRearTireColliderSizeRatio;
        WheelModData.RearRimColliderSize = preset.WheelSize / WheelModData.DefaultRearRimColliderSizeRatio;

        //WheelModData.FrontTireColliderWidth = preset.FrontTireColliderWidth;
        //WheelModData.FrontTireColliderSize = preset.FrontTireColliderSize;
        //WheelModData.FrontRimColliderSize = preset.FrontRimColliderSize;
        //WheelModData.RearTireColliderWidth = preset.RearTireColliderWidth;
        //WheelModData.RearTireColliderSize = preset.RearTireColliderSize;
        //WheelModData.RearRimColliderSize = preset.RearRimColliderSize;

        Debug.WriteLine($"{nameof(WheelModScript)}: wheel mod preset applied");
        await Delay(200);
        WheelModDataChanged?.Invoke(this, EventArgs.Empty);
    }
}
