using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VStancer.Client.Data;
using VStancer.Client.Preset;
using VStancer.Client.UI;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client.Scripts
{
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

        internal VStancerConfig Config => _mainScript.Config;
        internal WheelModMenu Menu { get; private set; }
        public bool DataIsValid => _playerVehicleHandle != -1 && WheelModData != null && VehicleWheelMod != -1;

        internal const string ExtraResetID = "vstancer_extra_reset";

        internal const string WheelWidthID = "vstancer_extra_width";
        internal const string WheelSizeID = "vstancer_extra_size";
        internal const string DefaultWidthID = "vstancer_extra_width_def";
        internal const string DefaultSizeID = "vstancer_extra_size_def";

        internal const string FrontTireColliderWidthID = "vstancer_extra_tirecollider_width_f";
        internal const string FrontTireColliderSizeID = "vstancer_extra_tirecollider_size_f";
        internal const string FrontRimColliderSizeID = "vstancer_extra_rimcollider_size_f";

        internal const string RearTireColliderWidthID = "vstancer_extra_tirecollider_width_r";
        internal const string RearTireColliderSizeID = "vstancer_extra_tirecollider_size_r";
        internal const string RearRimColliderSizeID = "vstancer_extra_rimcollider_size_r";

        internal const string DefaultFrontTireColliderWidthID = "vstancer_extra_tirecollider_width_f_def";
        internal const string DefaultFrontTireColliderSizeID = "vstancer_extra_tirecollider_size_f_def";
        internal const string DefaultFrontRimColliderSizeID = "vstancer_extra_rimcollider_size_f_def";

        internal const string DefaultRearTireColliderWidthID = "vstancer_extra_tirecollider_width_r_def";
        internal const string DefaultRearTireColliderSizeID = "vstancer_extra_tirecollider_size_r_def";
        internal const string DefaultRearRimColliderSizeID = "vstancer_extra_rimcollider_size_r_def";

        public event EventHandler WheelModDataChanged;

        internal WheelModScript(MainScript mainScript)
        {
            _mainScript = mainScript;

            RegisterDecorators();

            _lastTime = GetGameTimer();

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

            VehicleWheelMod = GetVehicleMod(_playerVehicleHandle, 23);

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
            long currentTime = (GetGameTimer() - _lastTime);

            if (currentTime > _mainScript.Config.Timer)
            {
                UpdateWorldVehiclesUsingDecorators();

                _lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        private void UpdateVehicleUsingData(int vehicle, WheelModData data)
        {
            if (!DoesEntityExist(vehicle) || data == null)
                return;

            //if (!MathUtil.WithinEpsilon(GetVehicleWheelWidth(vehicle), data.WheelWidth, VStancerUtilities.Epsilon))
                SetVehicleWheelWidth(vehicle, data.WheelWidth);

            //if (!MathUtil.WithinEpsilon(GetVehicleWheelSize(vehicle), data.WheelSize, VStancerUtilities.Epsilon))
                SetVehicleWheelSize(vehicle, data.WheelSize);

            for (int i = 0; i < data.FrontWheelsCount; i++)
            {
                SetVehicleWheelTireColliderWidth(vehicle, i, data.FrontTireColliderWidth);
                SetVehicleWheelTireColliderSize(vehicle, i, data.FrontTireColliderSize);
                SetVehicleWheelRimColliderSize(vehicle, i, data.FrontRimColliderSize);
            }

            for (int i = data.FrontWheelsCount; i < data.WheelsCount; i++)
            {
                SetVehicleWheelTireColliderWidth(vehicle, i, data.RearTireColliderWidth);
                SetVehicleWheelTireColliderSize(vehicle, i, data.RearTireColliderSize);
                SetVehicleWheelRimColliderSize(vehicle, i, data.RearRimColliderSize);
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
            if (!DoesEntityExist(vehicle))
                return null;

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            float wheelWidth_def;
            float wheelSize_def;

            // wait for data to actually update, required if wheel mod is changed too fast
            // and data read by GetVehicleWheelWidth and GetVehicleWheelSize didn't update yet resulting in 0
            if (DecorExistOn(vehicle, DefaultWidthID))
            {
                wheelWidth_def = DecorGetFloat(vehicle, DefaultWidthID);
            }
            else
            {
                do
                {
                    wheelWidth_def = GetVehicleWheelWidth(vehicle);
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
                    wheelSize_def = GetVehicleWheelSize(vehicle);
                    await Delay(100);
                } while (MathUtil.IsZero(wheelSize_def) || MathUtil.IsOne(wheelSize_def));
            }

            float frontTireColliderWidth_def = DecorExistOn(vehicle, DefaultFrontTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultFrontTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, 0);
            float frontTireColliderSize_def = DecorExistOn(vehicle, DefaultFrontTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultFrontTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, 0);
            float frontRimColliderSize_def = DecorExistOn(vehicle, DefaultFrontRimColliderSizeID) ? DecorGetFloat(vehicle, DefaultFrontRimColliderSizeID) : GetVehicleWheelRimColliderSize(vehicle, 0);

            float rearTireColliderWidth_def = DecorExistOn(vehicle, DefaultRearTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultRearTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, frontCount);
            float rearTireColliderSize_def = DecorExistOn(vehicle, DefaultRearTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, frontCount);
            float rearRimColliderSize_def = DecorExistOn(vehicle, DefaultRearRimColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearRimColliderSizeID) : GetVehicleWheelRimColliderSize(vehicle, frontCount);

            // Create the preset with the default values
            return new WheelModData(wheelsCount, wheelWidth_def, wheelSize_def,
                frontTireColliderWidth_def, frontTireColliderSize_def, frontRimColliderSize_def,
                rearTireColliderWidth_def, rearTireColliderSize_def, rearRimColliderSize_def)
            {
                // Assign the current values
                WheelWidth = DecorExistOn(vehicle, WheelWidthID) ? DecorGetFloat(vehicle, WheelWidthID) : wheelWidth_def,
                WheelSize = DecorExistOn(vehicle, WheelSizeID) ? DecorGetFloat(vehicle, WheelSizeID) : wheelSize_def,

                FrontTireColliderWidth = DecorExistOn(vehicle, FrontTireColliderWidthID) ? DecorGetFloat(vehicle, FrontTireColliderWidthID) : frontTireColliderWidth_def,
                FrontTireColliderSize = DecorExistOn(vehicle, FrontTireColliderSizeID) ? DecorGetFloat(vehicle, FrontTireColliderSizeID) : frontTireColliderSize_def,
                FrontRimColliderSize = DecorExistOn(vehicle, FrontRimColliderSizeID) ? DecorGetFloat(vehicle, FrontRimColliderSizeID) : frontRimColliderSize_def,

                RearTireColliderWidth = DecorExistOn(vehicle, RearTireColliderWidthID) ? DecorGetFloat(vehicle, RearTireColliderWidthID) : rearTireColliderWidth_def,
                RearTireColliderSize = DecorExistOn(vehicle, RearTireColliderSizeID) ? DecorGetFloat(vehicle, RearTireColliderSizeID) : rearTireColliderSize_def,
                RearRimColliderSize = DecorExistOn(vehicle, RearRimColliderSizeID) ? DecorGetFloat(vehicle, RearRimColliderSizeID) : rearRimColliderSize_def
            };
        }

        private void SetWheelWidthUsingData(int vehicle, WheelModData data)
        {
            float value = data.WheelWidth;

            bool result = SetVehicleWheelWidth(vehicle, value);
            if (result)
            {
                float defValue = data.DefaultWheelWidth;
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultWidthID, defValue, value);
                VStancerUtilities.UpdateFloatDecorator(vehicle, WheelWidthID, value, defValue);
            }
        }

        private void SetWheelSizeUsingData(int vehicle, WheelModData data)
        {
            float value = data.WheelSize;

            bool result = SetVehicleWheelSize(vehicle, value);
            if (result)
            {
                float defValue = data.DefaultWheelSize;
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultSizeID, defValue, value);
                VStancerUtilities.UpdateFloatDecorator(vehicle, WheelSizeID, value, defValue);
            }
        }

        private void SetFrontTireColliderWidthUsingData(int vehicle, WheelModData data)
        {
            float value = data.FrontTireColliderWidth;
            float defValue = data.DefaultFrontTireColliderWidth;

            for (int i = 0; i < WheelModData.FrontWheelsCount; i++)
                SetVehicleWheelTireColliderWidth(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTireColliderWidthID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTireColliderWidthID, value, defValue);
        }

        private void SetFrontTireColliderSizeUsingData(int vehicle, WheelModData data)
        {
            float value = data.FrontTireColliderSize;
            float defValue = data.DefaultFrontTireColliderSize;

            for (int i = 0; i < WheelModData.FrontWheelsCount; i++)
                SetVehicleWheelTireColliderSize(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTireColliderSizeID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTireColliderSizeID, value, defValue);
        }

        private void SetFrontRimColliderSizeUsingData(int vehicle, WheelModData data)
        {
            float value = data.FrontRimColliderSize;
            float defValue = data.DefaultFrontRimColliderSize;

            for (int i = 0; i < WheelModData.FrontWheelsCount; i++)
                SetVehicleWheelRimColliderSize(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontRimColliderSizeID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontRimColliderSizeID, value, defValue);
        }

        private void SetRearTireColliderWidthUsingData(int vehicle, WheelModData data)
        {
            float value = data.RearTireColliderWidth;
            float defValue = data.DefaultRearTireColliderWidth;

            for (int i = WheelModData.FrontWheelsCount; i < WheelModData.WheelsCount; i++)
                SetVehicleWheelTireColliderWidth(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTireColliderWidthID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearTireColliderWidthID, value, defValue);
        }

        private void SetRearTireColliderSizeUsingData(int vehicle, WheelModData data)
        {
            float value = data.RearTireColliderSize;
            float defValue = data.DefaultRearTireColliderSize;

            for (int i = WheelModData.FrontWheelsCount; i < WheelModData.WheelsCount; i++)
                SetVehicleWheelTireColliderSize(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTireColliderSizeID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearTireColliderSizeID, value, defValue);
        }

        private void SetRearRimColliderSizeUsingData(int vehicle, WheelModData data)
        {
            float value = data.RearRimColliderSize;
            float defValue = data.DefaultRearRimColliderSize;

            for (int i = WheelModData.FrontWheelsCount; i < WheelModData.WheelsCount; i++)
                SetVehicleWheelRimColliderSize(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearRimColliderSizeID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearRimColliderSizeID, value, defValue);
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
                    SetWheelWidthUsingData(_playerVehicleHandle, WheelModData);
                    SetWheelSizeUsingData(_playerVehicleHandle, WheelModData);
                    SetFrontTireColliderWidthUsingData(_playerVehicleHandle, WheelModData);
                    SetFrontTireColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                    SetFrontRimColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                    SetRearTireColliderWidthUsingData(_playerVehicleHandle, WheelModData);
                    SetRearTireColliderSizeUsingData(_playerVehicleHandle, WheelModData);
                    SetRearRimColliderSizeUsingData(_playerVehicleHandle, WheelModData);

                    RemoveDecoratorsFromVehicle(_playerVehicleHandle);

                    WheelModDataChanged?.Invoke(this, EventArgs.Empty);
                    break;

                default:
                    break;
            }
        }

        private void UpdateVehicleUsingDecorators(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontWheelsCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            if (DecorExistOn(vehicle, WheelSizeID))
                SetVehicleWheelSize(vehicle, DecorGetFloat(vehicle, WheelSizeID));

            if (DecorExistOn(vehicle, WheelWidthID))
                SetVehicleWheelWidth(vehicle, DecorGetFloat(vehicle, WheelWidthID));

            if (DecorExistOn(vehicle, FrontTireColliderWidthID))
            {
                float value = DecorGetFloat(vehicle, FrontTireColliderWidthID);
                for (int i = 0; i < frontWheelsCount; i++)
                    SetVehicleWheelTireColliderWidth(vehicle, i, value);
            }

            if (DecorExistOn(vehicle, FrontTireColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, FrontTireColliderSizeID);
                for (int i = 0; i < frontWheelsCount; i++)
                    SetVehicleWheelTireColliderSize(vehicle, i, value);
            }

            if (DecorExistOn(vehicle, FrontRimColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, FrontRimColliderSizeID);
                for (int i = 0; i < frontWheelsCount; i++)
                    SetVehicleWheelRimColliderSize(vehicle, i, value);
            }

            if (DecorExistOn(vehicle, RearTireColliderWidthID))
            {
                float value = DecorGetFloat(vehicle, RearTireColliderWidthID);
                for (int i = frontWheelsCount; i < wheelsCount; i++)
                    SetVehicleWheelTireColliderWidth(vehicle, i, value);
            }

            if (DecorExistOn(vehicle, RearTireColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, RearTireColliderSizeID);
                for (int i = frontWheelsCount; i < wheelsCount; i++)
                    SetVehicleWheelTireColliderSize(vehicle, i, value);
            }

            if (DecorExistOn(vehicle, RearRimColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, RearRimColliderSizeID);
                for (int i = frontWheelsCount; i < wheelsCount; i++)
                    SetVehicleWheelRimColliderSize(vehicle, i, value);
            }
        }

        private void RegisterDecorators()
        {
            DecorRegister(WheelWidthID, 1);
            DecorRegister(DefaultWidthID, 1);

            DecorRegister(WheelSizeID, 1);
            DecorRegister(DefaultSizeID, 1);

            DecorRegister(FrontTireColliderWidthID, 1);
            DecorRegister(FrontTireColliderSizeID, 1);
            DecorRegister(FrontRimColliderSizeID, 1);
            DecorRegister(DefaultFrontTireColliderWidthID, 1);
            DecorRegister(DefaultFrontTireColliderSizeID, 1);
            DecorRegister(DefaultFrontRimColliderSizeID, 1);

            DecorRegister(RearTireColliderWidthID, 1);
            DecorRegister(RearTireColliderSizeID, 1);
            DecorRegister(RearRimColliderSizeID, 1);
            DecorRegister(DefaultRearTireColliderWidthID, 1);
            DecorRegister(DefaultRearTireColliderSizeID, 1);
            DecorRegister(DefaultRearRimColliderSizeID, 1);
        }

        private void RemoveDecoratorsFromVehicle(int vehicle)
        {
            if (DecorExistOn(vehicle, WheelSizeID))
                DecorRemove(vehicle, WheelSizeID);

            if (DecorExistOn(vehicle, WheelWidthID))
                DecorRemove(vehicle, WheelWidthID);

            if (DecorExistOn(vehicle, DefaultSizeID))
                DecorRemove(vehicle, DefaultSizeID);

            if (DecorExistOn(vehicle, DefaultWidthID))
                DecorRemove(vehicle, DefaultWidthID);

            if (DecorExistOn(vehicle, FrontTireColliderWidthID))
                DecorRemove(vehicle, FrontTireColliderWidthID);

            if (DecorExistOn(vehicle, FrontTireColliderSizeID))
                DecorRemove(vehicle, FrontTireColliderSizeID);

            if (DecorExistOn(vehicle, FrontRimColliderSizeID))
                DecorRemove(vehicle, FrontRimColliderSizeID);

            if (DecorExistOn(vehicle, DefaultFrontTireColliderWidthID))
                DecorRemove(vehicle, DefaultFrontTireColliderWidthID);

            if (DecorExistOn(vehicle, DefaultFrontTireColliderSizeID))
                DecorRemove(vehicle, DefaultFrontTireColliderSizeID);

            if (DecorExistOn(vehicle, DefaultFrontRimColliderSizeID))
                DecorRemove(vehicle, DefaultFrontRimColliderSizeID);

            if (DecorExistOn(vehicle, RearTireColliderWidthID))
                DecorRemove(vehicle, RearTireColliderWidthID);

            if (DecorExistOn(vehicle, RearTireColliderSizeID))
                DecorRemove(vehicle, RearTireColliderSizeID);

            if (DecorExistOn(vehicle, RearRimColliderSizeID))
                DecorRemove(vehicle, RearRimColliderSizeID);

            if (DecorExistOn(vehicle, DefaultRearTireColliderWidthID))
                DecorRemove(vehicle, DefaultRearTireColliderWidthID);

            if (DecorExistOn(vehicle, DefaultRearTireColliderSizeID))
                DecorRemove(vehicle, DefaultRearTireColliderSizeID);

            if (DecorExistOn(vehicle, DefaultRearRimColliderSizeID))
                DecorRemove(vehicle, DefaultRearRimColliderSizeID);
        }

        private void OnMenuCommandInvoked(string commandID)
        {
            if (!DataIsValid)
                return;

            switch (commandID)
            {
                case ExtraResetID:
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
                DecorExistOn(entity, WheelSizeID) ||
                DecorExistOn(entity, WheelWidthID) ||
                DecorExistOn(entity, DefaultSizeID) ||
                DecorExistOn(entity, DefaultWidthID) ||
                DecorExistOn(entity, FrontTireColliderWidthID) ||
                DecorExistOn(entity, FrontTireColliderSizeID) ||
                DecorExistOn(entity, FrontRimColliderSizeID) ||
                DecorExistOn(entity, DefaultFrontTireColliderWidthID) ||
                DecorExistOn(entity, DefaultFrontTireColliderSizeID) ||
                DecorExistOn(entity, DefaultFrontRimColliderSizeID) ||
                DecorExistOn(entity, RearTireColliderWidthID) ||
                DecorExistOn(entity, RearTireColliderSizeID) ||
                DecorExistOn(entity, RearRimColliderSizeID) ||
                DecorExistOn(entity, DefaultRearTireColliderWidthID) ||
                DecorExistOn(entity, DefaultRearTireColliderSizeID) ||
                DecorExistOn(entity, DefaultRearRimColliderSizeID)
                );
        }

        internal void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => EntityHasDecorators(entity));

            Debug.WriteLine($"{nameof(WheelModScript)}: Vehicles with decorators: {entities.Count()}");

            foreach (int item in entities)
                Debug.WriteLine($"Vehicle: {item}");
        }

        internal void PrintDecoratorsInfo(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
            {
                Debug.WriteLine($"{nameof(WheelModScript)}: Can't find vehicle with handle {vehicle}");
                return;
            }

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int netID = NetworkGetNetworkIdFromEntity(vehicle);
            StringBuilder s = new StringBuilder();
            s.AppendLine($"{nameof(WheelModScript)}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

            if (DecorExistOn(vehicle, WheelSizeID))

                s.AppendLine($"{WheelSizeID}: {DecorGetFloat(vehicle, WheelSizeID)}");

            if (DecorExistOn(vehicle, DefaultSizeID))
                s.AppendLine($"{DefaultSizeID}: {DecorGetFloat(vehicle, DefaultSizeID)}");

            if (DecorExistOn(vehicle, WheelWidthID))
                s.AppendLine($"{WheelWidthID}: {DecorGetFloat(vehicle, WheelWidthID)}");

            if (DecorExistOn(vehicle, DefaultWidthID))
                s.AppendLine($"{DefaultWidthID}: {DecorGetFloat(vehicle, DefaultWidthID)}");

            if (DecorExistOn(vehicle, FrontTireColliderWidthID))
                s.AppendLine($"{FrontTireColliderWidthID}: {DecorGetFloat(vehicle, FrontTireColliderWidthID)}");

            if (DecorExistOn(vehicle, DefaultFrontTireColliderWidthID))
                s.AppendLine($"{DefaultFrontTireColliderWidthID}: {DecorGetFloat(vehicle, DefaultFrontTireColliderWidthID)}");

            if (DecorExistOn(vehicle, RearTireColliderWidthID))
                s.AppendLine($"{RearTireColliderWidthID}: {DecorGetFloat(vehicle, RearTireColliderWidthID)}");

            if (DecorExistOn(vehicle, DefaultRearTireColliderWidthID))
                s.AppendLine($"{DefaultRearTireColliderWidthID}: {DecorGetFloat(vehicle, DefaultRearTireColliderWidthID)}");

            if (DecorExistOn(vehicle, FrontTireColliderSizeID))
                s.AppendLine($"{FrontTireColliderSizeID}: {DecorGetFloat(vehicle, FrontTireColliderSizeID)}");

            if (DecorExistOn(vehicle, DefaultFrontTireColliderSizeID))
                s.AppendLine($"{DefaultFrontTireColliderSizeID}: {DecorGetFloat(vehicle, DefaultFrontTireColliderSizeID)}");

            if (DecorExistOn(vehicle, RearTireColliderSizeID))
                s.AppendLine($"{RearTireColliderSizeID}: {DecorGetFloat(vehicle, RearTireColliderSizeID)}");

            if (DecorExistOn(vehicle, DefaultRearTireColliderSizeID))
                s.AppendLine($"{DefaultRearTireColliderSizeID}: {DecorGetFloat(vehicle, DefaultRearTireColliderSizeID)}");

            if (DecorExistOn(vehicle, FrontRimColliderSizeID))
                s.AppendLine($"{FrontRimColliderSizeID}: {DecorGetFloat(vehicle, FrontRimColliderSizeID)}");

            if (DecorExistOn(vehicle, DefaultFrontRimColliderSizeID))
                s.AppendLine($"{DefaultFrontRimColliderSizeID}: {DecorGetFloat(vehicle, DefaultFrontRimColliderSizeID)}");

            if (DecorExistOn(vehicle, RearRimColliderSizeID))
                s.AppendLine($"{RearRimColliderSizeID}: {DecorGetFloat(vehicle, RearRimColliderSizeID)}");

            if (DecorExistOn(vehicle, DefaultRearRimColliderSizeID))
                s.AppendLine($"{DefaultRearRimColliderSizeID}: {DecorGetFloat(vehicle, DefaultRearRimColliderSizeID)}");

            Debug.WriteLine(s.ToString());
        }

        internal WheelModPreset GetWheelModPreset()
        {
            if (!DataIsValid)
                return null;

            // Only required to avoid saving this preset locally when not required
            if (!WheelModData.IsEdited)
                return null;

            return new WheelModPreset(WheelModData);
        }

        internal async Task SetWheelModPreset(WheelModPreset preset)
        {
            if (!DataIsValid || preset == null)
                return;

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
}
