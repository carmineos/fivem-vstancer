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

        private VStancerExtra _vstancerExtra;
        internal VStancerExtra VStancerExtra 
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

        internal const string ExtraWidthID = "vstancer_extra_width";
        internal const string ExtraSizeID = "vstancer_extra_size";
        internal const string DefaultExtraWidthID = "vstancer_extra_width_def";
        internal const string DefaultExtraSizeID = "vstancer_extra_size_def";

        internal const string FrontExtraTireColliderWidthID = "vstancer_extra_tirecollider_width_f";
        internal const string FrontExtraTireColliderSizeID = "vstancer_extra_tirecollider_size_f";
        internal const string FrontExtraRimColliderSizeID = "vstancer_extra_rimcollider_size_f";

        internal const string RearExtraTireColliderWidthID = "vstancer_extra_tirecollider_width_r";
        internal const string RearExtraTireColliderSizeID = "vstancer_extra_tirecollider_size_r";
        internal const string RearExtraRimColliderSizeID = "vstancer_extra_rimcollider_size_r";

        internal const string DefaultFrontExtraTireColliderWidthID = "vstancer_extra_tirecollider_width_f_def";
        internal const string DefaultFrontExtraTireColliderSizeID = "vstancer_extra_tirecollider_size_f_def";
        internal const string DefaultFrontExtraRimColliderSizeZID = "vstancer_extra_rimcollider_size_f_def";

        internal const string DefaultRearExtraTireColliderWidthID = "vstancer_extra_tirecollider_width_r_def";
        internal const string DefaultRearExtraTireColliderSizeID = "vstancer_extra_tirecollider_size_r_def";
        internal const string DefaultRearExtraRimColliderSizeID = "vstancer_extra_rimcollider_size_r_def";

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

            await Delay(1000);
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

            VStancerExtra = GetVStancerExtraFromHandle(_playerVehicleHandle);
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

                _lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
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

        private VStancerExtra GetVStancerExtraFromHandle(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
                return null;

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);
#if DEBUG
            Debug.WriteLine($"Vehicle has {nameof(DefaultExtraWidthID)}: {DecorExistOn(vehicle, DefaultExtraWidthID)}");
            Debug.WriteLine($"Vehicle has {nameof(DefaultExtraSizeID)}: {DecorExistOn(vehicle, DefaultExtraSizeID)}");
#endif
            float wheelWidth_def = DecorExistOn(vehicle, DefaultExtraWidthID) ? DecorGetFloat(vehicle, DefaultExtraWidthID) : GetVehicleWheelWidth(vehicle);
            float wheelSize_def = DecorExistOn(vehicle, DefaultExtraSizeID) ? DecorGetFloat(vehicle, DefaultExtraSizeID) : GetVehicleWheelSize(vehicle);
            float frontTireColliderWidth_def = DecorExistOn(vehicle, DefaultFrontExtraTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultFrontExtraTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, 0);
            float frontTireColliderSize_def = DecorExistOn(vehicle, DefaultFrontExtraTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultFrontExtraTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, 0);
            float frontRimColliderSize_def = DecorExistOn(vehicle, DefaultFrontExtraRimColliderSizeZID) ? DecorGetFloat(vehicle, DefaultFrontExtraRimColliderSizeZID) : GetVehicleWheelRimColliderSize(vehicle, 0);

            float rearTireColliderWidth_def = DecorExistOn(vehicle, DefaultRearExtraTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultRearExtraTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, frontCount);
            float rearTireColliderSize_def = DecorExistOn(vehicle, DefaultRearExtraTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearExtraTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, frontCount);
            float rearRimColliderSize_def = DecorExistOn(vehicle, DefaultRearExtraRimColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearExtraRimColliderSizeID) : GetVehicleWheelRimColliderSize(vehicle, frontCount);

            // Create the preset with the default values
            return new VStancerExtra(wheelsCount, wheelWidth_def, wheelSize_def,
                frontTireColliderWidth_def, frontTireColliderSize_def, frontRimColliderSize_def,
                rearTireColliderWidth_def, rearTireColliderSize_def, rearRimColliderSize_def)
            {
                // Assign the current values
                WheelWidth = DecorExistOn(vehicle, ExtraWidthID) ? DecorGetFloat(vehicle, ExtraWidthID) : wheelWidth_def,
                WheelSize = DecorExistOn(vehicle, ExtraSizeID) ? DecorGetFloat(vehicle, ExtraSizeID) : wheelSize_def,

                FrontTireColliderWidth = DecorExistOn(vehicle, FrontExtraTireColliderWidthID) ? DecorGetFloat(vehicle, FrontExtraTireColliderWidthID) : frontTireColliderWidth_def,
                FrontTireColliderSize = DecorExistOn(vehicle, FrontExtraTireColliderSizeID) ? DecorGetFloat(vehicle, FrontExtraTireColliderSizeID) : frontTireColliderSize_def,
                FrontRimColliderSize = DecorExistOn(vehicle, FrontExtraRimColliderSizeID) ? DecorGetFloat(vehicle, FrontExtraRimColliderSizeID) : frontRimColliderSize_def,

                RearTireColliderWidth = DecorExistOn(vehicle, RearExtraTireColliderWidthID) ? DecorGetFloat(vehicle, RearExtraTireColliderWidthID) : rearTireColliderWidth_def,
                RearTireColliderSize = DecorExistOn(vehicle, RearExtraTireColliderSizeID) ? DecorGetFloat(vehicle, RearExtraTireColliderSizeID) : rearTireColliderSize_def,
                RearRimColliderSize = DecorExistOn(vehicle, RearExtraRimColliderSizeID) ? DecorGetFloat(vehicle, RearExtraRimColliderSizeID) : rearRimColliderSize_def
            };
        }

        private void OnExtraPropertyEdited(string propertyName, float value)
        {
            bool result = false;
            switch (propertyName)
            {
                case nameof(VStancerExtra.WheelWidth):
                    result = SetVehicleWheelWidth(_playerVehicleHandle, value);
                    if (result)
                    {
                        VStancerUtilities.UpdateFloatDecorator(_playerVehicleHandle, DefaultExtraWidthID, VStancerExtra.DefaultWheelWidth, value);
                        VStancerUtilities.UpdateFloatDecorator(_playerVehicleHandle, ExtraWidthID, value, VStancerExtra.DefaultWheelWidth);
                    }
                    break;

                case nameof(VStancerExtra.WheelSize):
                    result = SetVehicleWheelSize(_playerVehicleHandle, value);
                    if (result)
                    {
                        VStancerUtilities.UpdateFloatDecorator(_playerVehicleHandle, DefaultExtraSizeID, VStancerExtra.DefaultWheelSize, value);
                        VStancerUtilities.UpdateFloatDecorator(_playerVehicleHandle, ExtraSizeID, value, VStancerExtra.DefaultWheelSize);
                    }
                    break;

               //case nameof(VStancerExtra.FrontTireColliderWidth):
               //case nameof(VStancerExtra.FrontTireColliderSize):
               //case nameof(VStancerExtra.FrontRimColliderSize):
               //case nameof(VStancerExtra.RearTireColliderWidth):
               //case nameof(VStancerExtra.RearTireColliderSize):
               //case nameof(VStancerExtra.RearRimColliderSize):
               //    break;

                case nameof(VStancerExtra.Reset):
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
            if (DecorExistOn(vehicle, ExtraSizeID))
                SetVehicleWheelSize(vehicle, DecorGetFloat(vehicle, ExtraSizeID));

            if (DecorExistOn(vehicle, ExtraWidthID))
                SetVehicleWheelWidth(vehicle, DecorGetFloat(vehicle, ExtraWidthID));
        }

        private void RegisterExtraDecorators()
        {
            DecorRegister(ExtraWidthID, 1);
            DecorRegister(ExtraSizeID, 1);
            DecorRegister(DefaultExtraWidthID, 1);
            DecorRegister(DefaultExtraSizeID, 1);
        }

        private void RemoveExtraDecoratorsFromVehicle(int vehicle)
        {
            if (DecorExistOn(vehicle, ExtraSizeID))
                DecorRemove(vehicle, ExtraSizeID);

            if (DecorExistOn(vehicle, ExtraWidthID))
                DecorRemove(vehicle, ExtraWidthID);

            if (DecorExistOn(vehicle, DefaultExtraSizeID))
                DecorRemove(vehicle, DefaultExtraSizeID);

            if (DecorExistOn(vehicle, DefaultExtraWidthID))
                DecorRemove(vehicle, DefaultExtraWidthID);
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
                case ExtraSizeID:
                    VStancerExtra.WheelSize = value;
                    break;
                case ExtraWidthID:
                    VStancerExtra.WheelWidth = value;
                    break;
            }
        }

        private bool EntityHasDecorators(int entity)
        {
            return (
                DecorExistOn(entity, ExtraSizeID) ||
                DecorExistOn(entity, ExtraWidthID) ||
                DecorExistOn(entity, DefaultExtraSizeID) ||
                DecorExistOn(entity, DefaultExtraWidthID)
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
                Debug.WriteLine($"{Globals.ScriptName}: Can't find vehicle with handle {vehicle}");
                return;
            }

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int netID = NetworkGetNetworkIdFromEntity(vehicle);
            StringBuilder s = new StringBuilder();
            s.AppendLine($"{nameof(VStancerExtraScript)}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

            if (DecorExistOn(vehicle, ExtraSizeID))
            {
                float value = DecorGetFloat(vehicle, ExtraSizeID);
                s.AppendLine($"{ExtraSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultExtraSizeID))
            {
                float value = DecorGetFloat(vehicle, DefaultExtraSizeID);
                s.AppendLine($"{DefaultExtraSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, ExtraWidthID))
            {
                float value = DecorGetFloat(vehicle, ExtraWidthID);
                s.AppendLine($"{ExtraWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, DefaultExtraWidthID))
            {
                float value = DecorGetFloat(vehicle, DefaultExtraWidthID);
                s.AppendLine($"{DefaultExtraWidthID}: {value}");
            }

            Debug.WriteLine(s.ToString());
        }
    }
}
