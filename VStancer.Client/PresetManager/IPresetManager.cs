using System;
using System.Collections.Generic;

namespace VStancer.Client
{
    public interface IPresetManager<TKey, TValue>
    {
        /// <summary>
        /// Invoked when an element is saved or deleted
        /// </summary>
        event EventHandler PresetsListChanged;

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
        /// Loads and the returns the <see cref="VStancerPreset"/> named <paramref name="name"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        TValue Load(TKey name);

        /// <summary>
        /// Returns the list of all the saved keys
        /// </summary>
        /// <returns></returns>
        IEnumerable<TKey> GetKeys();
    }
}
