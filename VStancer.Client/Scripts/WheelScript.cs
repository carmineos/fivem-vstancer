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

            switch(propertyName)
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
            if (!DoesEntityExist(vehicle) || data == null)
                return;

            int frontWheelsCount = data.FrontWheelsCount;
            WheelDataNode[] nodes = data.GetNodes();

            for (int i = 0; i < frontWheelsCount; i++)
                SetVehicleWheelYRotation(vehicle, i, nodes[i].RotationY);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontCamberID, data.DefaultFrontCamber, data.FrontCamber);
            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontCamberID, data.FrontCamber, data.DefaultFrontCamber);
        }

        private void SetRearCamberUsingData(int vehicle, WheelData data)
        {
            if (!DoesEntityExist(vehicle) || data == null)
                return;

            int wheelsCount = data.WheelsCount;
            int frontWheelsCount = data.FrontWheelsCount;
            WheelDataNode[] nodes = data.GetNodes();

            for (int i = frontWheelsCount; i < wheelsCount; i++)
                SetVehicleWheelYRotation(vehicle, i, nodes[i].RotationY);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearCamberID, data.DefaultRearCamber, data.RearCamber);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearCamberID, data.RearCamber, data.DefaultRearCamber);
        }

        private void SetFrontTrackWidthUsingData(int vehicle, WheelData data)
        {
            if (!DoesEntityExist(vehicle) || data == null)
                return;

            int frontWheelsCount = data.FrontWheelsCount;
            WheelDataNode[] nodes = data.GetNodes();

            for (int i = 0; i < frontWheelsCount; i++)
                SetVehicleWheelXOffset(vehicle, i, nodes[i].PositionX);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, data.DefaultFrontTrackWidth, data.FrontTrackWidth);
            VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTrackWidthID, data.FrontTrackWidth, data.DefaultFrontTrackWidth);
        }

        private void SetRearTrackWidthUsingData(int vehicle, WheelData data)
        {
            if (!DoesEntityExist(vehicle) || data == null)
                return;

            int wheelsCount = data.WheelsCount;
            int frontWheelsCount = data.FrontWheelsCount;
            WheelDataNode[] nodes = data.GetNodes();

            for (int i = frontWheelsCount; i < wheelsCount; i++)
                SetVehicleWheelXOffset(vehicle, i, nodes[i].PositionX);

            VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, data.DefaultRearTrackWidth, data.RearTrackWidth);
            VStancerUtilities.UpdateFloatDecorator(vehicle, RearTrackWidthID, data.RearTrackWidth, data.DefaultRearTrackWidth);
        }

        private WheelData GetWheelDataFromEntity(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
                return null;

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
            WheelDataNode[] nodes = data.GetNodes();

            for (int index = 0; index < wheelsCount; index++)
            {
                SetVehicleWheelXOffset(vehicle, index, nodes[index].PositionX);
                SetVehicleWheelYRotation(vehicle, index, nodes[index].RotationY);
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
                s.AppendLine($"{FrontTrackWidthID}: {DecorGetFloat(vehicle, FrontTrackWidthID)}");

            if (DecorExistOn(vehicle, DefaultFrontTrackWidthID))
                s.AppendLine($"{DefaultFrontTrackWidthID}: {DecorGetFloat(vehicle, DefaultFrontTrackWidthID)}");

            if (DecorExistOn(vehicle, RearTrackWidthID))
                s.AppendLine($"{RearTrackWidthID}: {DecorGetFloat(vehicle, RearTrackWidthID)}");

            if (DecorExistOn(vehicle, DefaultRearTrackWidthID))
                s.AppendLine($"{DefaultRearTrackWidthID}: {DecorGetFloat(vehicle, DefaultRearTrackWidthID)}");

            if (DecorExistOn(vehicle, FrontCamberID))
                s.AppendLine($"{FrontCamberID}: {DecorGetFloat(vehicle, FrontCamberID)}");

            if (DecorExistOn(vehicle, DefaultFrontCamberID))
                s.AppendLine($"{DefaultFrontCamberID}: {DecorGetFloat(vehicle, DefaultFrontCamberID)}");

            if (DecorExistOn(vehicle, RearCamberID))
                s.AppendLine($"{RearCamberID}: {DecorGetFloat(vehicle, RearCamberID)}");

            if (DecorExistOn(vehicle, DefaultRearCamberID))
                s.AppendLine($"{RearCamberID}: {DecorGetFloat(vehicle, DefaultRearCamberID)}");

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

        internal WheelPreset GetWheelPreset()
        {
            if (!DataIsValid)
                return null;

            // Only required to avoid saving this preset locally when not required
            if (!WheelData.IsEdited)
                return null;

            return new WheelPreset(WheelData);
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

            // Don't refresh, as it's already done by OnWheelDataPropertyChanged

            Debug.WriteLine($"{nameof(WheelScript)}: wheel preset applied");

            await Delay(200);
            WheelDataChanged?.Invoke(this, EventArgs.Empty);
        }

        internal bool API_SetWheelPreset(int vehicle, WheelPreset preset)
        {
            if (preset == null)
                return false;

            if (!DoesEntityExist(vehicle))
                return false;

            float frontTrackWidth = preset.FrontTrackWidth;
            float rearTrackWidth = preset.RearTrackWidth;
            float frontCamber = preset.FrontCamber;
            float rearCamber = preset.RearCamber;

            int wheelsCount = GetVehicleNumberOfWheels(vehicle);
            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

            float frontTrackWidth_def = DecorExistOn(vehicle, DefaultFrontTrackWidthID) ? DecorGetFloat(vehicle, DefaultFrontTrackWidthID) : GetVehicleWheelXOffset(vehicle, 0);
            float frontCamber_def = DecorExistOn(vehicle, DefaultFrontCamberID) ? DecorGetFloat(vehicle, DefaultFrontCamberID) : GetVehicleWheelYRotation(vehicle, 0);
            float rearTrackWidth_def = DecorExistOn(vehicle, DefaultRearTrackWidthID) ? DecorGetFloat(vehicle, DefaultRearTrackWidthID) : GetVehicleWheelXOffset(vehicle, frontCount);
            float rearCamber_def = DecorExistOn(vehicle, DefaultRearCamberID) ? DecorGetFloat(vehicle, DefaultRearCamberID) : GetVehicleWheelYRotation(vehicle, frontCount);

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
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, frontTrackWidth_def, frontTrackWidth);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontCamberID, frontCamber_def, frontCamber);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, rearTrackWidth_def, rearTrackWidth);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearCamberID, rearCamber_def, rearCamber);

                VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTrackWidthID, frontTrackWidth, frontTrackWidth_def);
                VStancerUtilities.UpdateFloatDecorator(vehicle, FrontCamberID, frontCamber, frontCamber_def);
                VStancerUtilities.UpdateFloatDecorator(vehicle, RearTrackWidthID, rearTrackWidth, rearTrackWidth_def);
                VStancerUtilities.UpdateFloatDecorator(vehicle, RearCamberID, rearCamber, rearCamber_def);
            }

            return true;
        }

        internal bool API_GetWheelPreset(int vehicle, out WheelPreset preset)
        {
            preset = null;

            if (!DoesEntityExist(vehicle))
                return false;

            WheelData data = (vehicle == _playerVehicleHandle && DataIsValid) ? WheelData : GetWheelDataFromEntity(vehicle);
            
            if(data == null)
                return false;

            preset = new WheelPreset(data);
            return true;
        }
        
        internal bool API_ResetWheelPreset(int vehicle)
        {
            if (!DoesEntityExist(vehicle))
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
            if (!DoesEntityExist(vehicle))
                return false;

            if (vehicle != _playerVehicleHandle)
            {
                float value_def = DecorExistOn(vehicle, DefaultFrontCamberID) ? DecorGetFloat(vehicle, DefaultFrontCamberID) : GetVehicleWheelYRotation(vehicle, 0);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontCamberID, value_def, value);
                VStancerUtilities.UpdateFloatDecorator(vehicle, FrontCamberID, value, value_def);
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
            if (!DoesEntityExist(vehicle))
                return false;

            if (vehicle != _playerVehicleHandle)
            {
                int wheelsCount = GetVehicleNumberOfWheels(vehicle);
                int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

                float value_def = DecorExistOn(vehicle, DefaultRearCamberID) ? DecorGetFloat(vehicle, DefaultRearCamberID) : GetVehicleWheelYRotation(vehicle, frontCount);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearCamberID, value_def, value);
                VStancerUtilities.UpdateFloatDecorator(vehicle, RearCamberID, value, value_def);
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
            if (!DoesEntityExist(vehicle))
                return false;

            if (vehicle != _playerVehicleHandle)
            {
                float value_def = DecorExistOn(vehicle, DefaultFrontTrackWidthID) ? DecorGetFloat(vehicle, DefaultFrontTrackWidthID) : GetVehicleWheelXOffset(vehicle, 0);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultFrontTrackWidthID, value_def, value);
                VStancerUtilities.UpdateFloatDecorator(vehicle, FrontTrackWidthID, value, value_def); 
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
            if (!DoesEntityExist(vehicle))
                return false;

            if (vehicle != _playerVehicleHandle)
            {
                int wheelsCount = GetVehicleNumberOfWheels(vehicle);
                int frontCount = VStancerUtilities.CalculateFrontWheelsCount(wheelsCount);

                float value_def = DecorExistOn(vehicle, DefaultRearTrackWidthID) ? DecorGetFloat(vehicle, DefaultRearTrackWidthID) : GetVehicleWheelXOffset(vehicle, frontCount);
                VStancerUtilities.UpdateFloatDecorator(vehicle, DefaultRearTrackWidthID, value_def, value);
                VStancerUtilities.UpdateFloatDecorator(vehicle, RearTrackWidthID, value, value_def);
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

            if (!DoesEntityExist(vehicle))
                return false;

            value = DecorExistOn(vehicle, FrontCamberID) ? DecorGetFloat(vehicle, FrontCamberID) : GetVehicleWheelYRotation(vehicle, 0);
            return true;
        }

        internal bool API_GetRearCamber(int vehicle, out float value)
        {
            value = default;

            if (!DoesEntityExist(vehicle))
                return false;

            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(GetVehicleNumberOfWheels(vehicle));
            value = DecorExistOn(vehicle, RearCamberID) ? DecorGetFloat(vehicle, RearCamberID) : GetVehicleWheelYRotation(vehicle, frontCount);
            return true;
        }

        internal bool API_GetFrontTrackWidth(int vehicle, out float value)
        {
            value = default;

            if (!DoesEntityExist(vehicle))
                return false;

            value = DecorExistOn(vehicle, FrontTrackWidthID) ? DecorGetFloat(vehicle, FrontTrackWidthID) : GetVehicleWheelXOffset(vehicle, 0);
            return true;
        }

        internal bool API_GetRearTrackWidth(int vehicle, out float value)
        {
            value = default;

            if (!DoesEntityExist(vehicle))
                return false;

            int frontCount = VStancerUtilities.CalculateFrontWheelsCount(GetVehicleNumberOfWheels(vehicle));
            value = DecorExistOn(vehicle, RearTrackWidthID) ? DecorGetFloat(vehicle, RearTrackWidthID) : GetVehicleWheelXOffset(vehicle, frontCount);
            return true;
        }
    }
}
