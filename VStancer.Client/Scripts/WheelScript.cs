﻿using System;
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
    internal class WheelScript : BaseScript
    {
        private readonly MainScript _mainScript;

        private long _lastTime;
        private int _playerVehicleHandle;

        internal WheelData WheelData { get; set; }
        internal VStancerConfig Config => _mainScript.Config;
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

            Menu = new WheelMenu(this);
            Menu.FloatPropertyChangedEvent += OnMenuFloatPropertyChanged;
            Menu.ResetPropertiesEvent += (sender, id) => OnMenuCommandInvoked(id);

            Tick += UpdateWorldVehiclesTask;
            Tick += TimedTask;
            Tick += UpdatePlayerVehicleTask;

            mainScript.PlayerVehicleHandleChanged += (sender, handle) => PlayerVehicleChanged(handle);
            PlayerVehicleChanged(_mainScript.PlayerVehicleHandle);
        }

        private void InvalidateData()
        {
            if (DataIsValid)
            {
                WheelData.PropertyChanged -= OnWheelDataPropertyChanged;
                WheelData = null;

                _playerVehicleHandle = -1;

                Tick -= UpdatePlayerVehicleTask;
            }
        }

        private void PlayerVehicleChanged(int vehicle)
        {
            if (vehicle == _playerVehicleHandle)
                return;

            _playerVehicleHandle = vehicle;

            if (_playerVehicleHandle == -1)
            {
                InvalidateData();
                return;
            }

            WheelData = GetWheelDataFromEntity(vehicle);
            WheelData.PropertyChanged += OnWheelDataPropertyChanged;

            Tick += UpdatePlayerVehicleTask;

            WheelDataChanged?.Invoke(this, EventArgs.Empty);
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

        private async Task TimedTask()
        {
            long currentTime = (GetGameTimer() - _lastTime);

            // Check if decorators needs to be updated
            if (currentTime > _mainScript.Config.Timer)
            {
                if (DataIsValid)
                    UpdateVehicleDecorators(_playerVehicleHandle, WheelData);

                _lastTime = GetGameTimer();
            }

            await Task.FromResult(0);
        }

        private async void OnWheelDataPropertyChanged(object sender, string editedProperty)
        {
            if (!DataIsValid)
                return;

            // If false then this has been invoked by after a reset
            if (editedProperty == nameof(WheelData.Reset))
            {
                RemoveDecoratorsFromVehicle(_playerVehicleHandle);

                await Delay(50);
                WheelDataChanged?.Invoke(this, EventArgs.Empty);
            }

            // Force one single refresh to update rendering at correct position after reset
            // This is required because otherwise the vehicle won't update immediately as
            UpdateVehicleUsingWheelData(_playerVehicleHandle, WheelData);
        }

        private WheelData GetWheelDataFromEntity(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
                return null;

            Debug.WriteLine($"~o~Warning~w~: You are creating a vstancer preset for a damaged vehicle, default data of the wheels might be wrong");

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            // Get default values first
            float frontTrackWidth_def = DecorExistOn(vehicle, DefaultFrontTrackWidthID) ? DecorGetFloat(vehicle, DefaultFrontTrackWidthID) : GetVehicleWheelXOffset(vehicle, 0);
            float frontCamber_def = DecorExistOn(vehicle, DefaultFrontCamberID) ? DecorGetFloat(vehicle, DefaultFrontCamberID) : GetVehicleWheelYRotation(vehicle, 0);
            float rearTrackWidth_def = DecorExistOn(vehicle, DefaultRearTrackWidthID) ? DecorGetFloat(vehicle, DefaultRearTrackWidthID) : GetVehicleWheelXOffset(vehicle, frontCount);
            float rearCamber_def = DecorExistOn(vehicle, DefaultRearCamberID) ? DecorGetFloat(vehicle, DefaultRearCamberID) : GetVehicleWheelYRotation(vehicle, frontCount);

            float frontTrackWidth = DecorExistOn(vehicle, FrontTrackWidthID) ? DecorGetFloat(vehicle, FrontTrackWidthID) : frontTrackWidth_def;
            float frontCamber = DecorExistOn(vehicle, FrontCamberID) ? DecorGetFloat(vehicle, FrontCamberID) : frontCamber_def;
            float rearTrackWdith = DecorExistOn(vehicle, RearTrackWidthID) ? DecorGetFloat(vehicle, RearTrackWidthID) : rearTrackWidth_def;
            float rearCamber = DecorExistOn(vehicle, RearCamberID) ? DecorGetFloat(vehicle, RearCamberID) : rearCamber_def;

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
            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            if (DecorExistOn(vehicle, FrontTrackWidthID))
            {
                float value = DecorGetFloat(vehicle, FrontTrackWidthID);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, FrontCamberID))
            {
                float value = DecorGetFloat(vehicle, FrontCamberID);

                for (int index = 0; index < frontCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelYRotation(vehicle, index, value);
                    else
                        SetVehicleWheelYRotation(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, RearTrackWidthID))
            {
                float value = DecorGetFloat(vehicle, RearTrackWidthID);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelXOffset(vehicle, index, value);
                    else
                        SetVehicleWheelXOffset(vehicle, index, -value);
                }
            }

            if (DecorExistOn(vehicle, RearCamberID))
            {
                float value = DecorGetFloat(vehicle, RearCamberID);

                for (int index = frontCount; index < wheelsCount; index++)
                {
                    if (index % 2 == 0)
                        SetVehicleWheelYRotation(vehicle, index, value);
                    else
                        SetVehicleWheelYRotation(vehicle, index, -value);
                }
            }
        }

        private void UpdateVehicleUsingWheelData(int vehicle, WheelData data)
        {
            if (!DoesEntityExist(vehicle) || data == null)
                return;

            int wheelsCount = data.WheelsCount;
            for (int index = 0; index < wheelsCount; index++)
            {
                SetVehicleWheelXOffset(vehicle, index, data.Nodes[index].PositionX);
                SetVehicleWheelYRotation(vehicle, index, data.Nodes[index].RotationY);
            }
        }

        private void UpdateVehicleDecorators(int vehicle, WheelData data)
        {
            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, data.DefaultFrontTrackWidth, data.FrontTrackWidth);
            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontCamberID, data.DefaultFrontCamber, data.FrontCamber);
            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, data.DefaultRearTrackWidth, data.RearTrackWidth);
            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearCamberID, data.DefaultRearCamber, data.RearCamber);

            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTrackWidthID, data.FrontTrackWidth, data.DefaultFrontTrackWidth);
            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontCamberID, data.FrontCamber, data.DefaultFrontCamber);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearTrackWidthID, data.RearTrackWidth, data.DefaultRearTrackWidth);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearCamberID, data.RearCamber, data.DefaultRearCamber);
        }

        private void RegisterDecorators()
        {
            DecorRegister(FrontTrackWidthID, 1);
            DecorRegister(FrontCamberID, 1);
            DecorRegister(RearTrackWidthID, 1);
            DecorRegister(RearCamberID, 1);

            DecorRegister(DefaultFrontTrackWidthID, 1);
            DecorRegister(DefaultFrontCamberID, 1);
            DecorRegister(DefaultRearTrackWidthID, 1);
            DecorRegister(DefaultRearCamberID, 1);
        }

        private void RemoveDecoratorsFromVehicle(int vehicle)
        {
            if (DecorExistOn(vehicle, FrontTrackWidthID))
                DecorRemove(vehicle, FrontTrackWidthID);

            if (DecorExistOn(vehicle, FrontCamberID))
                DecorRemove(vehicle, FrontCamberID);

            if (DecorExistOn(vehicle, DefaultFrontTrackWidthID))
                DecorRemove(vehicle, DefaultFrontTrackWidthID);

            if (DecorExistOn(vehicle, DefaultFrontCamberID))
                DecorRemove(vehicle, DefaultFrontCamberID);

            if (DecorExistOn(vehicle, RearTrackWidthID))
                DecorRemove(vehicle, RearTrackWidthID);

            if (DecorExistOn(vehicle, RearCamberID))
                DecorRemove(vehicle, RearCamberID);

            if (DecorExistOn(vehicle, DefaultRearTrackWidthID))
                DecorRemove(vehicle, DefaultRearTrackWidthID);

            if (DecorExistOn(vehicle, DefaultRearCamberID))
                DecorRemove(vehicle, DefaultRearCamberID);
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
            if (!DoesEntityExist(vehicle))
            {
                Debug.WriteLine($"{nameof(WheelScript)}: Can't find vehicle with handle {vehicle}");
                return;
            }

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int netID = NetworkGetNetworkIdFromEntity(vehicle);
            StringBuilder s = new StringBuilder();
            s.AppendLine($"{nameof(WheelScript)}: Vehicle:{vehicle} netID:{netID} wheelsCount:{wheelsCount}");

            if (DecorExistOn(vehicle, FrontTrackWidthID))
            {
                float value = DecorGetFloat(vehicle, FrontTrackWidthID);
                s.AppendLine($"{FrontTrackWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, FrontCamberID))
            {
                float value = DecorGetFloat(vehicle, FrontCamberID);
                s.AppendLine($"{FrontCamberID}: {value}");
            }

            if (DecorExistOn(vehicle, RearTrackWidthID))
            {
                float value = DecorGetFloat(vehicle, RearTrackWidthID);
                s.AppendLine($"{RearTrackWidthID}: {value}");
            }

            if (DecorExistOn(vehicle, RearCamberID))
            {
                float value = DecorGetFloat(vehicle, RearCamberID);
                s.AppendLine($"{RearCamberID}: {value}");
            }

            Debug.WriteLine(s.ToString());
        }

        private bool EntityHasDecorators(int entity)
        {
            return (
                DecorExistOn(entity, FrontTrackWidthID) ||
                DecorExistOn(entity, FrontCamberID) ||
                DecorExistOn(entity, RearTrackWidthID) ||
                DecorExistOn(entity, RearCamberID) ||
                DecorExistOn(entity, DefaultFrontTrackWidthID) ||
                DecorExistOn(entity, DefaultFrontCamberID) ||
                DecorExistOn(entity, DefaultRearTrackWidthID) ||
                DecorExistOn(entity, DefaultRearCamberID)
                );
        }

        internal void PrintVehiclesWithDecorators(IEnumerable<int> vehiclesList)
        {
            IEnumerable<int> entities = vehiclesList.Where(entity => EntityHasDecorators(entity));

            Debug.WriteLine($"{nameof(WheelScript)}: Vehicles with decorators: {entities.Count()}");

            foreach (int item in entities)
                Debug.WriteLine($"Vehicle: {item}");
        }

        public void SetVstancerPreset(int vehicle, float frontOffset, float frontRotation, float rearOffset, float rearRotation, object defaultFrontOffset = null, object defaultFrontRotation = null, object defaultRearOffset = null, object defaultRearRotation = null)
        {
#if DEBUG
            Debug.WriteLine($"{nameof(VStancerDataScript)}: SetVstancerPreset parameters {frontOffset} {frontRotation} {rearOffset} {rearRotation} {defaultFrontOffset} {defaultFrontRotation} {defaultRearOffset} {defaultRearRotation}");
#endif
            if (!DoesEntityExist(vehicle))
                return;

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            float off_f_def = defaultFrontOffset is float
                ? (float)defaultFrontOffset
                : DecorExistOn(vehicle, DefaultFrontTrackWidthID)
                ? DecorGetFloat(vehicle, DefaultFrontTrackWidthID)
                : GetVehicleWheelXOffset(vehicle, 0);

            float rot_f_def = defaultFrontRotation is float
                ? (float)defaultFrontRotation
                : DecorExistOn(vehicle, DefaultFrontCamberID)
                ? DecorGetFloat(vehicle, DefaultFrontCamberID)
                : GetVehicleWheelYRotation(vehicle, 0);

            float off_r_def = defaultRearOffset is float
                ? (float)defaultRearOffset
                : DecorExistOn(vehicle, DefaultRearTrackWidthID)
                ? DecorGetFloat(vehicle, DefaultRearTrackWidthID)
                : GetVehicleWheelXOffset(vehicle, frontCount);

            float rot_r_def = defaultRearRotation is float
                ? (float)defaultRearRotation
                : DecorExistOn(vehicle, DefaultRearCamberID)
                ? DecorGetFloat(vehicle, DefaultRearCamberID)
                : GetVehicleWheelYRotation(vehicle, frontCount);

            if (vehicle == _playerVehicleHandle)
            {
                WheelData = new WheelData(wheelsCount, off_f_def, rot_f_def, off_r_def, rot_r_def)
                {
                    FrontTrackWidth = frontOffset,
                    FrontCamber = frontRotation,
                    RearTrackWidth = rearOffset,
                    RearCamber = rearRotation
                };

                WheelData.PropertyChanged += OnWheelDataPropertyChanged;
                WheelDataChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, off_f_def, frontOffset);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontCamberID, rot_f_def, frontRotation);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, off_r_def, rearOffset);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearCamberID, rot_r_def, rearRotation);

                VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTrackWidthID, frontOffset, off_f_def);
                VStancerUtilities.UpdateFloatDecorator(vehicle, FrontCamberID, frontRotation, rot_f_def);
                VStancerUtilities.UpdateFloatDecorator(vehicle, RearTrackWidthID, rearOffset, off_r_def);
                VStancerUtilities.UpdateFloatDecorator(vehicle, RearCamberID, rearRotation, rot_r_def);
            }
        }

        public float[] GetVstancerPreset(int vehicle)
        {
            WheelData preset = (vehicle == _playerVehicleHandle && DataIsValid) ? WheelData : GetWheelDataFromEntity(vehicle);
            return preset?.ToArray();
        }

        internal WheelPreset GetWheelPreset()
        {
            if (!DataIsValid)
                return null;

            if (!WheelData.IsEdited)
                return null;

            return new WheelPreset()
            {
                FrontCamber = WheelData.FrontCamber,
                RearCamber = WheelData.RearCamber,
                FrontTrackWidth = WheelData.FrontTrackWidth,
                RearTrackWidth = WheelData.RearTrackWidth,
            };
        }

        internal async Task SetWheelPreset(WheelPreset preset)
        {
            if (!DataIsValid || preset == null)
                return;

            // TODO: Check if values are within limits

            WheelData.FrontTrackWidth = preset.FrontTrackWidth;
            WheelData.RearTrackWidth = preset.RearTrackWidth;
            WheelData.FrontCamber = preset.FrontCamber;
            WheelData.RearCamber = preset.RearCamber;

            // Force refresh 
            UpdateVehicleUsingWheelData(_playerVehicleHandle, WheelData);

            Debug.WriteLine($"{nameof(WheelScript)}: wheel preset applied");

            await Delay(200);
            WheelDataChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}