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

        private bool _ignoreEmptyPresets = false;
        private bool _allowStockPresets = false;
        private bool _menuLeftAlignment = false;

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

        public bool AllowStockPresets
        {
            get => _allowStockPresets;
            set
            {
                if (Equals(_allowStockPresets, value))
                    return;

                _allowStockPresets = value;
                PropertyChanged?.Invoke(this, nameof(AllowStockPresets));
            }
        }

        public bool MenuLeftAlignment
        {
            get => _menuLeftAlignment;
            set
            {
                if (Equals(_menuLeftAlignment, value))
                    return;

                _menuLeftAlignment = value;
                PropertyChanged?.Invoke(this, nameof(MenuLeftAlignment));
            }
        }
    }
}
