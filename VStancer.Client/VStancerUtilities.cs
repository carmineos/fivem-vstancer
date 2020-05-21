using System.Collections.Generic;

using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client
{
    public static class VStancerUtilities
    {
        public const float Epsilon = 0.001f;

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
}
