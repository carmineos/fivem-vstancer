using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VStancer.Client
{
    public class ClientSettings
    {
        public event EventHandler<string> PropertyChanged;

        private bool _ignoreEmptyPresets;

        public bool IgnoreEmptyPresets
        {
            get => _ignoreEmptyPresets;
            set
            {
                if (Equals(_ignoreEmptyPresets, value))
                    return;

                _ignoreEmptyPresets = value;
                PropertyChanged?.Invoke(this, nameof(IgnoreEmptyPresets));
            }
        }
    }
}
