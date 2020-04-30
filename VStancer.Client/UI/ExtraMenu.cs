using System;
using MenuAPI;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class ExtraMenu : Menu
    {
        private readonly VStancerEditor _vstancerEditor;

        internal ExtraMenu(VStancerEditor editor, string name = Globals.ScriptName, string subtitle = "Extra") : base(name, subtitle)
        {
            _vstancerEditor = editor;

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

        private MenuDynamicListItem WheelSizeListItem { get; set; }
        private MenuDynamicListItem WheelWidthListItem { get; set; }
        private MenuItem ResetItem { get; set; }

        internal event FloatPropertyChanged FloatPropertyChangedEvent;
        internal event EventHandler<string> ResetPropertiesEvent;

        internal void Update()
        {
            ClearMenuItems();

            if (!_vstancerEditor.CurrentExtraIsValid)
                return;

            WheelSizeListItem = MenuUtilities.CreateDynamicFloatList("Wheel Size",
                _vstancerEditor.CurrentExtra.DefaultWheelSize,
                _vstancerEditor.CurrentExtra.WheelSize,
                _vstancerEditor.Config.Extra.WheelSize,
                VStancerEditor.ExtraSizeID,
                _vstancerEditor.Config.FloatStep);

            WheelWidthListItem = MenuUtilities.CreateDynamicFloatList("Wheel Width",
                _vstancerEditor.CurrentExtra.DefaultWheelWidth,
                _vstancerEditor.CurrentExtra.WheelWidth,
                _vstancerEditor.Config.Extra.WheelWidth,
                VStancerEditor.ExtraWidthID,
                _vstancerEditor.Config.FloatStep);

            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = VStancerEditor.ExtraResetID };

            AddMenuItem(WheelSizeListItem);
            AddMenuItem(WheelWidthListItem);
            AddMenuItem(ResetItem);
        }
    }
}
