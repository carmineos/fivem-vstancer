using System;
using MenuAPI;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class ExtraMenu : Menu
    {
        private readonly VStancerExtraManager _manager;

        internal ExtraMenu(VStancerExtraManager manager, string name = Globals.ScriptName, string subtitle = "Extra Menu") : base(name, subtitle)
        {
            _manager = manager;

            _manager.VStancerExtraChanged += new EventHandler((sender, args) =>
            {
                Update();
                //UpdateExtraMenuItem();
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

        private MenuDynamicListItem WheelSizeListItem { get; set; }
        private MenuDynamicListItem WheelWidthListItem { get; set; }
        private MenuItem ResetItem { get; set; }

        internal event FloatPropertyChanged FloatPropertyChangedEvent;
        internal event EventHandler<string> ResetPropertiesEvent;

        internal void Update()
        {
            ClearMenuItems();

            if (!_manager.ExtraIsValid)
                return;

            WheelSizeListItem = MenuUtilities.CreateDynamicFloatList("Wheel Size",
                _manager.VStancerExtra.DefaultWheelSize,
                _manager.VStancerExtra.WheelSize,
                _manager.Config.Extra.WheelSize,
                VStancerExtraManager.ExtraSizeID,
                _manager.Config.FloatStep);

            WheelWidthListItem = MenuUtilities.CreateDynamicFloatList("Wheel Width",
                _manager.VStancerExtra.DefaultWheelWidth,
                _manager.VStancerExtra.WheelWidth,
                _manager.Config.Extra.WheelWidth,
                VStancerExtraManager.ExtraWidthID,
                _manager.Config.FloatStep);

            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = VStancerExtraManager.ExtraResetID };

            AddMenuItem(WheelSizeListItem);
            AddMenuItem(WheelWidthListItem);
            AddMenuItem(ResetItem);
        }
    }
}
