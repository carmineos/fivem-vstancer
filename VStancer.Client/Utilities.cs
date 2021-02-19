using System.Collections.Generic;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client
{
    public static class Utilities
    {
        public const float Epsilon = 0.001f;

        public delegate void PropertyChanged<T>(string id, T value);

        public static int CalculateFrontWheelsCount(int wheelsCount)
        {
            int _frontWheelsCount = wheelsCount / 2;

            if (_frontWheelsCount % 2 != 0)
                _frontWheelsCount -= 1;

            return _frontWheelsCount;
        }

        public static List<string> GetKeyValuePairs(string prefix)
        {
            List<string> pairs = new List<string>();

            int handle = StartFindKvp(prefix);

            if (handle != -1)
            {
                string kvp;
                do
                {
                    kvp = FindKvp(handle);

                    if (kvp != null)
                        pairs.Add(kvp);
                }
                while (kvp != null);
                EndFindKvp(handle);
            }

            return pairs;
        }

        public static List<int> GetWorldVehicles()
        {
            List<int> handles = new List<int>();

            int entity = -1;
            int handle = FindFirstVehicle(ref entity);

            if (handle != -1)
            {
                do handles.Add(entity);
                while (FindNextVehicle(handle, ref entity));

                EndFindVehicle(handle);
            }

            return handles;
        }

        public static void UpdateFloatDecorator(int vehicle, string name, float currentValue, float defaultValue)
        {
            // Decorator exists but needs to be updated
            if (DecorExistOn(vehicle, name))
            {
                float decorValue = DecorGetFloat(vehicle, name);
                if (!MathUtil.WithinEpsilon(currentValue, decorValue, Epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
#if DEBUG
                    Debug.WriteLine($"Updated decorator {name} from {decorValue} to {currentValue} on vehicle {vehicle}");
#endif
                }
            }
            else // Decorator doesn't exist, create it if required
            {
                if (!MathUtil.WithinEpsilon(currentValue, defaultValue, Epsilon))
                {
                    DecorSetFloat(vehicle, name, currentValue);
#if DEBUG
                    Debug.WriteLine($"Added decorator {name} with value {currentValue} to vehicle {vehicle}");
#endif
                }
            }
        }
    }

    // TODO: Replace sync through decors with statebag values
    //public static class StateBagUtilities
    //{
    //    public static void UpdateOrSet(Entity entity, string keyName, float currentValue, float defaultValue)
    //    {
    //        if (!entity.Exists())
    //            return;
    //
    //        StateBag state = entity.State;
    //        dynamic stateValue = state.Get(keyName);
    //
    //        // State exists
    //        if (stateValue is float value)
    //        {
    //            // if existing state is different from new one
    //            if (!MathUtil.WithinEpsilon(currentValue, value, Utilities.Epsilon))
    //                state.Set(keyName, currentValue, true);
    //        }
    //        else // State doesn't exist
    //        {
    //            // if current value is not the default one, create a state
    //            if (!MathUtil.WithinEpsilon(currentValue, defaultValue, Utilities.Epsilon))
    //                state.Set(keyName, currentValue, true);
    //        }
    //    }
    //
    //    public static bool TryGetValue<T>(Entity entity, string keyName, out T value) where T : unmanaged
    //    {
    //        dynamic stateValue = entity.State.Get(keyName);
    //
    //        if (stateValue is T)
    //        {
    //            value = stateValue;
    //            return true;
    //        }
    //
    //        value = default;
    //        return false;
    //    }
    //
    //    public static void RemoveValue(Entity entity, string keyName)
    //    {
    //        dynamic stateValue = entity.State.Get(keyName);
    //
    //        if (stateValue != null)
    //            entity.State.Set(keyName, null, true);
    //    }
    //}
}
