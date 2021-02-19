using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VStancer.Client.Scripts;
using MenuAPI;
using static VStancer.Client.UI.MenuUtilities;
using static VStancer.Client.Utilities;

namespace VStancer.Client.UI
{
    internal class ClientSettingsMenu : Menu
    {
        private readonly ClientSettingsScript _script;

        private MenuCheckboxItem IgnoreEmptyPresetsCheckboxItem { get; set; }
        private MenuCheckboxItem AllowStockPresetsCheckboxItem { get; set; }
        private MenuCheckboxItem MenuLeftAlignmentCheckboxItem { get; set; }

        internal event PropertyChanged<bool> BoolPropertyChanged;

        public ClientSettingsMenu(ClientSettingsScript script, string name = Globals.ScriptName, string subtitle = "Client Settings Menu") : base(name, subtitle)
        {
            _script = script;

            _script.ClientSettingsChanged += (sender, args) => { Update(); };

            Update();

            OnCheckboxChange += CheckboxChange;
        }

        private void CheckboxChange(Menu menu, MenuCheckboxItem menuItem, int itemIndex, bool newCheckedState)
        {
            if (string.IsNullOrEmpty(menuItem.ItemData))
                return;

            BoolPropertyChanged?.Invoke(menuItem.ItemData, newCheckedState);
        }

        internal void Update()
        {
            ClearMenuItems();

            if (_script.ClientSettings == null)
                return;

            IgnoreEmptyPresetsCheckboxItem = new MenuCheckboxItem(
                "Ignore Empty Presets",
                "If checked, when an incomplete preset is applied, missing data won't be reset.",
                _script.ClientSettings.IgnoreEmptyPresets)
            {
                ItemData = nameof(_script.ClientSettings.IgnoreEmptyPresets)
            };

            AddMenuItem(IgnoreEmptyPresetsCheckboxItem);

            AllowStockPresetsCheckboxItem = new MenuCheckboxItem(
                "Allow Stock Presets",
                "If checked, will allow saving of unedited presets.",
                _script.ClientSettings.AllowStockPresets)
            {
                ItemData = nameof(_script.ClientSettings.AllowStockPresets)
            };

            AddMenuItem(AllowStockPresetsCheckboxItem);

            MenuLeftAlignmentCheckboxItem = new MenuCheckboxItem(
                "Menu Left Alignment",
                "If checked, the Menu will be aligned to the left of the screen.",
                _script.ClientSettings.MenuLeftAlignment)
            {
                ItemData = nameof(_script.ClientSettings.MenuLeftAlignment)
            };

            AddMenuItem(MenuLeftAlignmentCheckboxItem);
        }
    }
}
