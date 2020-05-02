using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VStancer.Client.Data;
using VStancer.Client.UI;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client.Scripts
{
    internal class VStancerExtraScript : BaseScript
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

        private WheelExtra _vstancerExtra;
        internal WheelExtra VStancerExtra 
        { 
            get => _vstancerExtra; 
            set
            {
                if (Equals(_vstancerExtra, value))
                    return;

                _vstancerExtra = value;
                VStancerExtraChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal VStancerConfig Config => _mainScript.Config;
        internal ExtraMenu Menu { get; private set; }
        public bool ExtraIsValid => _playerVehicleHandle != -1 && VStancerExtra != null && VehicleWheelMod != -1;

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

        public event EventHandler VStancerExtraChanged;

        internal VStancerExtraScript(MainScript mainScript)
        {
            _mainScript = mainScript;

            RegisterExtraDecorators();

            _lastTime = GetGameTimer();
            
            _playerVehicleHandle = -1;
            VehicleWheelMod = -1;
            VStancerExtra = null;

            Menu = new ExtraMenu(this);

            Menu.FloatPropertyChangedEvent += OnMenuFloatPropertyChanged;
            Menu.ResetPropertiesEvent += (sender, id) => OnMenuCommandInvoked(id);

            Tick += GetVehicleWheelModTask;
            Tick += TimedTask;

            mainScript.PlayerVehicleHandleChanged += (sender, handle) => PlayerVehicleChanged(handle);
            
            VStancerExtraChanged += (sender, args) => OnVStancerExtraChanged();

            PlayerVehicleChanged(_mainScript.PlayerVehicleHandle);
        }

        private async Task GetVehicleWheelModTask()
        {
            if (_playerVehicleHandle == -1)
                return;

            VehicleWheelMod = GetVehicleMod(_playerVehicleHandle, 23);

            await Task.FromResult(0);
        }

        private void OnVStancerExtraChanged()
        {
            if (VStancerExtra != null)
                VStancerExtra.PropertyChanged += OnExtraPropertyEdited;
        }

        private async void VehicleWheelModChanged()
        {
            if (_playerVehicleHandle == -1)
                return;

            //await Delay(1000);
#if DEBUG
            Debug.WriteLine($"{nameof(VehicleWheelModChanged)}: {VehicleWheelMod}");
            Debug.WriteLine($"Width: {GetVehicleWheelWidth(_playerVehicleHandle)}");
            Debug.WriteLine($"Size: {GetVehicleWheelSize(_playerVehicleHandle)}");
#endif

            if (VStancerExtra != null)
                VStancerExtra.PropertyChanged -= OnExtraPropertyEdited;

            if(VehicleWheelMod == -1)
            {
                VStancerExtra = null;
                RemoveExtraDecoratorsFromVehicle(_playerVehicleHandle);
                return;
            }

            VStancerExtra = await GetVStancerExtraFromHandle(_playerVehicleHandle);
        }

        private void PlayerVehicleChanged(int vehicle)
        {
#if DEBUG
            Debug.WriteLine($"New vehicle received {vehicle}");
#endif
            if (vehicle == _playerVehicleHandle)
                return;

            _playerVehicleHandle = vehicle;

            if (VStancerExtra != null)
                VStancerExtra.PropertyChanged -= OnExtraPropertyEdited;

            if (_playerVehicleHandle == -1)
            {
                VehicleWheelMod = -1;
                VStancerExtra = null;
                return;
            }

            VehicleWheelMod = GetVehicleMod(_playerVehicleHandle, 23);
            //VStancerExtra = VehicleWheelMod == -1 ? null : GetVStancerExtraFromHandle(_playerVehicleHandle);
        }

        private async Task TimedTask()
        {
            long currentTime = (GetGameTimer() - _lastTime);

            // Check if decorators needs to be updated
            if (currentTime > _mainScript.Config.Timer)
            {
                UpdateWorldVehiclesWithExtraDecorators();

                // This might be required as if a script edits and mod on the vehicle, wheel size and width will restore to default values
                //if (ExtraIsValid)
                //    UpdateVehicleUsingVStancerExtra(_playerVehicleHandle, VStancerExtra);

                _lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        private void UpdateVehicleUsingVStancerExtra(int vehicle, WheelExtra extra)
        {
            if (!DoesEntityExist(vehicle) || extra == null)
                return;

            if(!MathUtil.WithinEpsilon(GetVehicleWheelWidth(vehicle), extra.WheelWidth, VStancerUtilities.Epsilon))
                SetVehicleWheelWidth(vehicle, extra.WheelWidth);

            if (!MathUtil.WithinEpsilon(GetVehicleWheelSize(vehicle), extra.WheelSize, VStancerUtilities.Epsilon))
                SetVehicleWheelSize(vehicle, extra.WheelSize);

            // TODO: Also refresh colliders
        }

        private void UpdateWorldVehiclesWithExtraDecorators()
        {
            foreach (int entity in _mainScript.GetCloseVehicleHandles())
            {
                if (entity == _playerVehicleHandle)
                    continue;

                UpdateVehicleExtraUsingDecorators(entity);
            }
        }

        private async Task<WheelExtra> GetVStancerExtraFromHandle(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
                return null;

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);
#if DEBUG
            Debug.WriteLine($"Vehicle has {nameof(DefaultWidthID)}: {DecorExistOn(vehicle, DefaultWidthID)}");
            Debug.WriteLine($"Vehicle has {nameof(DefaultSizeID)}: {DecorExistOn(vehicle, DefaultSizeID)}");
#endif
            await Delay(1000);

            float wheelWidth_def = DecorExistOn(vehicle, DefaultWidthID) ? DecorGetFloat(vehicle, DefaultWidthID) : GetVehicleWheelWidth(vehicle);
            float wheelSize_def = DecorExistOn(vehicle, DefaultSizeID) ? DecorGetFloat(vehicle, DefaultSizeID) : GetVehicleWheelSize(vehicle);
            float frontTireColliderWidth_def = DecorExistOn(vehicle, DefaultFrontTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultFrontTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, 0);
            float frontTireColliderSize_def = DecorExistOn(vehicle, DefaultFrontTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultFrontTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, 0);
            float frontRimColliderSize_def = DecorExistOn(vehicle, DefaultFrontRimColliderSizeID) ? DecorGetFloat(vehicle, DefaultFrontRimColliderSizeID) : GetVehicleWheelRimColliderSize(vehicle, 0);

            float rearTireColliderWidth_def = DecorExistOn(vehicle, DefaultRearTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultRearTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, frontCount);
            float rearTireColliderSize_def = DecorExistOn(vehicle, DefaultRearTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, frontCount);
            float rearRimColliderSize_def = DecorExistOn(vehicle, DefaultRearRimColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearRimColliderSizeID) : GetVehicleWheelRimColliderSize(vehicle, frontCount);

            // Create the preset with the default values
            return new WheelExtra(wheelsCount, wheelWidth_def, wheelSize_def,
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

        private void SetWheelWidthUsingExtra(int vehicle, WheelExtra extra)
        {
            float value = extra.WheelWidth;

            bool result = SetVehicleWheelWidth(vehicle, value);
            if (result)
            {
                float defValue = extra.DefaultWheelWidth;
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultWidthID, defValue, value);
                VStancerUtilities.UpdateFloatDecorator(vehicle, WheelWidthID, value, defValue);
            }
        }

        private void SetWheelSizeUsingExtra(int vehicle, WheelExtra extra)
        {
            float value = extra.WheelSize;

            bool result = SetVehicleWheelSize(vehicle, value);
            if (result)
            {
                float defValue = extra.DefaultWheelSize;
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultSizeID, defValue, value);
                VStancerUtilities.UpdateFloatDecorator(vehicle, WheelSizeID, value, defValue);
            }
        }

        private void SetFrontTireColliderWidthUsingExtra(int vehicle, WheelExtra extra)
        {
            float value = extra.FrontTireColliderWidth;
            float defValue = extra.DefaultFrontTireColliderWidth;

            for (int i = 0; i < VStancerExtra.FrontWheelsCount; i++)
                SetVehicleWheelTireColliderWidth(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTireColliderWidthID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTireColliderWidthID, value, defValue);
        }

        private void SetFrontTireColliderSizeUsingExtra(int vehicle, WheelExtra extra)
        {
            float value = extra.FrontTireColliderSize;
            float defValue = extra.DefaultFrontTireColliderSize;

            for (int i = 0; i < VStancerExtra.FrontWheelsCount; i++)
                SetVehicleWheelTireColliderSize(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTireColliderSizeID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTireColliderSizeID, value, defValue);
        }

        private void SetFrontRimColliderSizeUsingExtra(int vehicle, WheelExtra extra)
        {
            float value = extra.FrontRimColliderSize;
            float defValue = extra.DefaultFrontRimColliderSize;

            for (int i = 0; i < VStancerExtra.FrontWheelsCount; i++)
                SetVehicleWheelRimColliderSize(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontRimColliderSizeID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontRimColliderSizeID, value, defValue);
        }

        private void SetRearTireColliderWidthUsingExtra(int vehicle, WheelExtra extra)
        {
            float value = extra.RearTireColliderWidth;
            float defValue = extra.DefaultRearTireColliderWidth;

            for (int i = VStancerExtra.FrontWheelsCount; i < VStancerExtra.WheelsCount; i++)
                SetVehicleWheelTireColliderWidth(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTireColliderWidthID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearTireColliderWidthID, value, defValue);
        }

        private void SetRearTireColliderSizeUsingExtra(int vehicle, WheelExtra extra)
        {
            float value = extra.RearTireColliderSize;
            float defValue = extra.DefaultRearTireColliderSize;

            for (int i = VStancerExtra.FrontWheelsCount; i < VStancerExtra.WheelsCount; i++)
                SetVehicleWheelTireColliderSize(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTireColliderSizeID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearTireColliderSizeID, value, defValue);
        }

        private void SetRearRimColliderSizeUsingExtra(int vehicle, WheelExtra extra)
        {
            float value = extra.RearRimColliderSize;
            float defValue = extra.DefaultRearRimColliderSize;

            for (int i = VStancerExtra.FrontWheelsCount; i < VStancerExtra.WheelsCount; i++)
                SetVehicleWheelRimColliderSize(vehicle, i, value);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearRimColliderSizeID, defValue, value);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearRimColliderSizeID, value, defValue);
        }

        private void OnExtraPropertyEdited(string propertyName, float value)
        {
            bool result = false;
            switch (propertyName)
            {
                case nameof(VStancerExtra.WheelWidth):
                    SetWheelWidthUsingExtra(_playerVehicleHandle, VStancerExtra);
                    break;

                case nameof(VStancerExtra.WheelSize):
                    SetWheelSizeUsingExtra(_playerVehicleHandle, VStancerExtra);
                    break;

               case nameof(VStancerExtra.FrontTireColliderWidth):
                    SetFrontTireColliderWidthUsingExtra(_playerVehicleHandle, VStancerExtra);
                    break;
                case nameof(VStancerExtra.FrontTireColliderSize):
                    SetFrontTireColliderSizeUsingExtra(_playerVehicleHandle, VStancerExtra);
                    break;
                case nameof(VStancerExtra.FrontRimColliderSize):
                    SetFrontRimColliderSizeUsingExtra(_playerVehicleHandle, VStancerExtra);
                    break;

                case nameof(VStancerExtra.RearTireColliderWidth):
                    SetRearTireColliderWidthUsingExtra(_playerVehicleHandle, VStancerExtra);
                    break;
                case nameof(VStancerExtra.RearTireColliderSize):
                    SetRearTireColliderSizeUsingExtra(_playerVehicleHandle, VStancerExtra);
                    break;
                case nameof(VStancerExtra.RearRimColliderSize):
                    SetRearRimColliderSizeUsingExtra(_playerVehicleHandle, VStancerExtra);
                    break;

                case nameof(VStancerExtra.Reset):

                    // TODO: Avoid updating decorators if we have to remove them anyway
                    SetWheelWidthUsingExtra(_playerVehicleHandle, VStancerExtra);
                    SetWheelSizeUsingExtra(_playerVehicleHandle, VStancerExtra);
                    SetFrontTireColliderWidthUsingExtra(_playerVehicleHandle, VStancerExtra);
                    SetFrontTireColliderSizeUsingExtra(_playerVehicleHandle, VStancerExtra);
                    SetFrontRimColliderSizeUsingExtra(_playerVehicleHandle, VStancerExtra);
                    SetRearTireColliderWidthUsingExtra(_playerVehicleHandle, VStancerExtra);
                    SetRearTireColliderSizeUsingExtra(_playerVehicleHandle, VStancerExtra);
                    SetRearRimColliderSizeUsingExtra(_playerVehicleHandle, VStancerExtra);

                    RemoveExtraDecoratorsFromVehicle(_playerVehicleHandle);

                    VStancerExtraChanged?.Invoke(this, EventArgs.Empty);
                    break;

                default:
                    break;
            }

            if (Config.Debug)
                CitizenFX.Core.Debug.WriteLine($"Edited: {propertyName}, value: {value}, result: {result}");
        }

        private void UpdateVehicleExtraUsingDecorators(int vehicle)
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

        private void RegisterExtraDecorators()
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

        private void RemoveExtraDecoratorsFromVehicle(int vehicle)
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
            if (!ExtraIsValid)
                return;

            switch (commandID)
            {
                case ExtraResetID:
                    VStancerExtra.Reset();
                    break;
            }
        }

        private void OnMenuFloatPropertyChanged(string id, float value)
        {
            if (!ExtraIsValid)
                return;

            switch (id)
            {
                case WheelSizeID:
                    VStancerExtra.WheelSize = value;
                    break;
                case WheelWidthID:
                    VStancerExtra.WheelWidth = value;
                    break;

                case FrontTireColliderWidthID:
                    VStancerExtra.FrontTireColliderWidth = value;
                    break;
                case FrontTireColliderSizeID:
                    VStancerExtra.FrontTireColliderSize = value;
                    break;
                case FrontRimColliderSizeID:
                    VStancerExtra.FrontRimColliderSize = value;
                    break;

                case RearTireColliderWidthID:
                    VStancerExtra.RearTireColliderWidth = value;
                    break;
                case RearTireColliderSizeID:
                    VStancerExtra.RearTireColliderSize = value;
                    break;
                case RearRimColliderSizeID:
                    VStancerExtra.RearRimColliderSize = value;
                    break;
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

            Debug.WriteLine($"{nameof(VStancerExtraScript)}: Vehicles with decorators: {entities.Count()}");

            foreach (int item in entities)
                Debug.WriteLine($"Vehicle: {item}");
        }

        internal void PrintDecoratorsInfo(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
            {
                Debug.WriteLine($"{nameof(VStancerExtraScript)}: Can't find vehicle with handle {vehicle}");
                return;
            }

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int netID = NetworkGetNetworkIdFromEntity(vehicle);
            StringBuilder s = new StringBuilder();
            s.AppendLine($"{nameof(VStancerExtraScript)}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

            if (DecorExistOn(vehicle, WheelSizeID))
            {
                float value = DecorGetFloat(vehicle, WheelSizeID);
                s.AppendLine($"{WheelSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultSizeID))
            {
                float value = DecorGetFloat(vehicle, DefaultSizeID);
                s.AppendLine($"{DefaultSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, WheelWidthID))
            {
                float value = DecorGetFloat(vehicle, WheelWidthID);
                s.AppendLine($"{WheelWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultWidthID))
            {
                float value = DecorGetFloat(vehicle, DefaultWidthID);
                s.AppendLine($"{DefaultWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, FrontTireColliderWidthID))
            {
                float value = DecorGetFloat(vehicle, FrontTireColliderWidthID);
                s.AppendLine($"{FrontTireColliderWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultFrontTireColliderWidthID))
            {
                float value = DecorGetFloat(vehicle, DefaultFrontTireColliderWidthID);
                s.AppendLine($"{DefaultFrontTireColliderWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, RearTireColliderWidthID))
            {
                float value = DecorGetFloat(vehicle, RearTireColliderWidthID);
                s.AppendLine($"{RearTireColliderWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultRearTireColliderWidthID))
            {
                float value = DecorGetFloat(vehicle, DefaultRearTireColliderWidthID);
                s.AppendLine($"{DefaultRearTireColliderWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, FrontTireColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, FrontTireColliderSizeID);
                s.AppendLine($"{FrontTireColliderSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultFrontTireColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, DefaultFrontTireColliderSizeID);
                s.AppendLine($"{DefaultFrontTireColliderSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, RearTireColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, RearTireColliderSizeID);
                s.AppendLine($"{RearTireColliderSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultRearTireColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, DefaultRearTireColliderSizeID);
                s.AppendLine($"{DefaultRearTireColliderSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, FrontRimColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, FrontRimColliderSizeID);
                s.AppendLine($"{FrontRimColliderSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultFrontRimColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, DefaultFrontRimColliderSizeID);
                s.AppendLine($"{DefaultFrontRimColliderSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, RearRimColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, RearRimColliderSizeID);
                s.AppendLine($"{RearRimColliderSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultRearRimColliderSizeID))
            {
                float value = DecorGetFloat(vehicle, DefaultRearRimColliderSizeID);
                s.AppendLine($"{DefaultRearRimColliderSizeID}: {value}");
            }

            Debug.WriteLine(s.ToString());
        }
    }
}
