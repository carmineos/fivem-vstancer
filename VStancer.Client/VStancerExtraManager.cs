using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStancer.Client.UI;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client
{
    internal class VStancerExtraManager : BaseScript
    {
        private readonly MainScript _mainScript;

        private long _lastTime;
        private int _playerVehicleHandle;
        private int _vehicleWheelMod;

        internal VStancerExtra VStancerExtra { get; set; }
        internal VStancerConfig Config => _mainScript.Config;
        internal ExtraMenu ExtraMenu { get; private set; }
        public bool ExtraIsValid => _playerVehicleHandle != -1 && VStancerExtra != null && _vehicleWheelMod != -1;

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

        internal VStancerExtraManager(MainScript mainScript)
        {
            _mainScript = mainScript;

            RegisterExtraDecorators();

            _lastTime = GetGameTimer();
            _playerVehicleHandle = _mainScript.PlayerVehicleHandle;
            _vehicleWheelMod = -1;

            ExtraMenu = new ExtraMenu(this);

            ExtraMenu.FloatPropertyChangedEvent += OnMenuFloatPropertyChanged;
            ExtraMenu.ResetPropertiesEvent += (sender, id) => OnMenuCommandInvoked(id);

            Tick += GetVehicleWheelModTask;
            Tick += TimedTask;

            mainScript.PlayerVehicleHandleChanged += (sender, handle) => PlayerVehicleChanged(handle);
        }

        private async Task GetVehicleWheelModTask()
        {
            await Task.FromResult(0);

            int vehicleWheelMod = GetVehicleMod(_playerVehicleHandle, 23);

            // If wheel mod didn't change
            if (vehicleWheelMod == _vehicleWheelMod)
                return;

            InvalidateExtra();

            if (vehicleWheelMod != -1)
            {
                _vehicleWheelMod = vehicleWheelMod;

                // Get new data from entity
                VStancerExtra = GetVStancerExtraFromHandle(_playerVehicleHandle);
                VStancerExtra.PropertyChanged += OnExtraPropertyEdited;

                // Avoid invoking this if InvalidateExtra already did it
                // Find out why UI doesn't update
                VStancerExtraChanged?.Invoke(this, EventArgs.Empty);
            }
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

        private void InvalidateExtra()
        {
            _vehicleWheelMod = -1;

            if (VStancerExtra != null)
            {
                //VStancerExtra.Reset();
                VStancerExtra.PropertyChanged -= OnExtraPropertyEdited;
                VStancerExtra = null;

                VStancerExtraChanged?.Invoke(this, EventArgs.Empty);
            }       
        }

        private void PlayerVehicleChanged(int vehicle)
        {
            if (vehicle == _playerVehicleHandle)
                return;

            _playerVehicleHandle = vehicle;

            if (_playerVehicleHandle == -1)
                InvalidateExtra();
        }

        private VStancerExtra GetVStancerExtraFromHandle(int vehicle)
        {
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            float wheelWidth_def = DecorExistOn(vehicle, DefaultExtraWidthID) ? DecorGetFloat(vehicle, DefaultExtraWidthID) : GetVehicleWheelWidth(vehicle);
            float wheelSize_def = DecorExistOn(vehicle, DefaultExtraSizeID) ? DecorGetFloat(vehicle, DefaultExtraSizeID) : GetVehicleWheelSize(vehicle);
            float frontTireColliderScaleX_def = DecorExistOn(vehicle, DefaultFrontExtraTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultFrontExtraTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, 0);
            float frontTireColliderScaleYZ_def = DecorExistOn(vehicle, DefaultFrontExtraTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultFrontExtraTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, 0);
            float frontRimColliderScaleYZ_def = DecorExistOn(vehicle, DefaultFrontExtraRimColliderSizeZID) ? DecorGetFloat(vehicle, DefaultFrontExtraRimColliderSizeZID) : GetVehicleWheelRimColliderSize(vehicle, 0);

            float rearTireColliderScaleX_def = DecorExistOn(vehicle, DefaultRearExtraTireColliderWidthID) ? DecorGetFloat(vehicle, DefaultRearExtraTireColliderWidthID) : GetVehicleWheelTireColliderWidth(vehicle, frontCount);
            float rearTireColliderScaleYZ_def = DecorExistOn(vehicle, DefaultRearExtraTireColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearExtraTireColliderSizeID) : GetVehicleWheelTireColliderSize(vehicle, frontCount);
            float rearRimColliderScaleYZ_def = DecorExistOn(vehicle, DefaultRearExtraRimColliderSizeID) ? DecorGetFloat(vehicle, DefaultRearExtraRimColliderSizeID) : GetVehicleWheelRimColliderSize(vehicle, frontCount);

            // Create the preset with the default values
            return new VStancerExtra(wheelsCount, wheelWidth_def, wheelSize_def,
                frontTireColliderScaleX_def, frontTireColliderScaleYZ_def, frontRimColliderScaleYZ_def,
                rearTireColliderScaleX_def, rearTireColliderScaleYZ_def, rearRimColliderScaleYZ_def)
            {
                // Assign the current values
                WheelWidth = DecorExistOn(vehicle, ExtraWidthID) ? DecorGetFloat(vehicle, ExtraWidthID) : wheelWidth_def,
                WheelSize = DecorExistOn(vehicle, ExtraSizeID) ? DecorGetFloat(vehicle, ExtraSizeID) : wheelSize_def,

                //FrontTireColliderScaleX = DecorExistOn(vehicle, FrontWheelModTireColliderScaleXID) ? DecorGetFloat(vehicle, FrontWheelModTireColliderScaleXID) : frontTireColliderScaleX_def,
                //FrontTireColliderScaleYZ = DecorExistOn(vehicle, FrontWheelModTireColliderScaleYZID) ? DecorGetFloat(vehicle, FrontWheelModTireColliderScaleYZID) : frontTireColliderScaleYZ_def,
                //FrontRimColliderScaleYZ = DecorExistOn(vehicle, FrontWheelModRimColliderScaleYZID) ? DecorGetFloat(vehicle, FrontWheelModRimColliderScaleYZID) : frontRimColliderScaleYZ_def,

                //RearTireColliderScaleX = DecorExistOn(vehicle, RearWheelModTireColliderScaleXID) ? DecorGetFloat(vehicle, RearWheelModTireColliderScaleXID) : rearTireColliderScaleX_def,
                //RearTireColliderScaleYZ = DecorExistOn(vehicle, RearWheelModTireColliderScaleYZID) ? DecorGetFloat(vehicle, RearWheelModTireColliderScaleYZID) : rearTireColliderScaleYZ_def,
                //RearRimColliderScaleYZ = DecorExistOn(vehicle, RearWheelModRimColliderScaleYZID) ? DecorGetFloat(vehicle, RearWheelModRimColliderScaleYZID) : rearRimColliderScaleYZ_def
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

            Debug.WriteLine($"{nameof(VStancerExtraManager)}: Vehicles with decorators: {entities.Count()}");

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
            s.AppendLine($"{nameof(VStancerExtraManager)}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

            if (DecorExistOn(vehicle, ExtraSizeID))
            {
                float value = DecorGetFloat(vehicle, ExtraSizeID);
                s.AppendLine($"{ExtraSizeID}: {value}");
            }

            if (DecorExistOn(vehicle, ExtraWidthID))
            {
                float value = DecorGetFloat(vehicle, ExtraWidthID);
                s.AppendLine($"{ExtraWidthID}: {value}");
            }

            Debug.WriteLine(s.ToString());
        }
    }
}
