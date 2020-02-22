using System;
using System.Collections;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace Vstancer.Client
{
    public class VehicleEnumerable : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            int entity = -1;
            int handle = FindFirstVehicle(ref entity);

            if (handle != -1)
            {
                do yield return entity;
                while (FindNextVehicle(handle, ref entity));

                EndFindVehicle(handle);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class Helpers
    {
        public static string RemoveByteOrderMarks(string xml)
        {
            /*
            string bom = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            if (xml.StartsWith(bom))
                xml = xml.Remove(0, bom.Length);
            */

            // Workaround 
            if (!xml.StartsWith("<", StringComparison.Ordinal))
                xml = xml.Substring(xml.IndexOf("<"));
            return xml;
        }
    }
}
