using System;
using MenuAPI;
using VStancer.Client.Scripts;
using static VStancer.Client.Menus.MenuUtilities;
using static VStancer.Client.Utilities;

namespace VStancer.Client.Menus
{
    internal class SuspensionMenu : Menu
    {
        private readonly SuspensionScript _script;

        internal SuspensionMenu(SuspensionScript script, string name = Globals.ScriptName, string subtitle = "Suspension Menu") : base(name, subtitle)
        {
            _script = script;

            _script.SuspensionDataChanged += new EventHandler((sender, args) =>
            {
                Update();
            });

            Update();

            OnDynamicListItemCurrentItemChange += DynamicListItemCurrentItemChange;
            OnItemSelect += ItemSelect;
        }

        private void ItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
        {
            if (menuItem == ResetItem)
                ResetPropertiesEvent?.Invoke(this, menuItem.ItemData as string);
        }

        private void DynamicListItemCurrentItemChange(Menu menu, MenuDynamicListItem dynamicListItem, string oldValue, string newValue)
        {
            // TODO: Does it need to check if newvalue != oldvalue?
            if (oldValue == newValue)
                return;

            if (float.TryParse(newValue, out float newfloatValue))
                FloatPropertyChangedEvent?.Invoke(dynamicListItem.ItemData as string, newfloatValue);
        }

        private MenuDynamicListItem SuspensionVisualHeightListItem { get; set; }

        private MenuItem ResetItem { get; set; }

        internal event PropertyChanged<float> FloatPropertyChangedEvent;
        internal event EventHandler<string> ResetPropertiesEvent;

        internal void Update()
        {
            ClearMenuItems();

            if (!_script.DataIsValid)
                return;

            SuspensionVisualHeightListItem = CreateDynamicFloatList("Suspension Visual Height",
                _script.SuspensionData.DefaultVisualHeight,
                _script.SuspensionData.VisualHeight,
                _script.Config.SuspensionLimits.SuspensionVisualHeight,
                SuspensionScript.VisualHeightID,
                _script.Config.FloatStep);

            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = SuspensionScript.SuspensionResetID };

            AddMenuItem(SuspensionVisualHeightListItem);
            AddMenuItem(ResetItem);
        }
    }
}
