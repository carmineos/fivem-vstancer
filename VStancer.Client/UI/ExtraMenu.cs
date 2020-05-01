using System;
using MenuAPI;
using VStancer.Client.Scripts;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class ExtraMenu : Menu
    {
        private readonly VStancerExtraScript _script;

        internal ExtraMenu(VStancerExtraScript script, string name = Globals.ScriptName, string subtitle = "Extra Menu") : base(name, subtitle)
        {
            _script = script;

            _script.VStancerExtraChanged += new EventHandler((sender, args) =>
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

        private MenuDynamicListItem WheelSizeListItem { get; set; }
        private MenuDynamicListItem WheelWidthListItem { get; set; }
        private MenuItem ResetItem { get; set; }

        internal event FloatPropertyChanged FloatPropertyChangedEvent;
        internal event EventHandler<string> ResetPropertiesEvent;

        internal void Update()
        {
            ClearMenuItems();

            if (!_script.ExtraIsValid)
                return;

            WheelSizeListItem = CreateDynamicFloatList("Wheel Size",
                _script.VStancerExtra.DefaultWheelSize,
                _script.VStancerExtra.WheelSize,
                _script.Config.Extra.WheelSize,
                VStancerExtraScript.ExtraSizeID,
                _script.Config.FloatStep);

            WheelWidthListItem = CreateDynamicFloatList("Wheel Width",
                _script.VStancerExtra.DefaultWheelWidth,
                _script.VStancerExtra.WheelWidth,
                _script.Config.Extra.WheelWidth,
                VStancerExtraScript.ExtraWidthID,
                _script.Config.FloatStep);

            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = VStancerExtraScript.ExtraResetID };

            AddMenuItem(WheelSizeListItem);
            AddMenuItem(WheelWidthListItem);
            AddMenuItem(ResetItem);
        }
    }
}
