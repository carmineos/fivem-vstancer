using System.Collections.Generic;

namespace VStancer.Client
{
    public interface IPresetManager<TKey, TValue>
    {
        /// <summary>
        /// Saves the <paramref name="preset"/> using the <paramref name="name"/> as preset name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="preset"></param>
        /// <returns></returns>
        bool Save(TKey name, TValue preset);

        /// <summary>
        /// Deletes the preset with the <paramref name="name"/> as preset name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Delete(TKey name);

        /// <summary>
        /// Loads and the returns the <see cref="HandlingPreset"/> named <paramref name="name"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        TValue Load(TKey name);

        IEnumerable<TKey> GetKeys();
    }
}
