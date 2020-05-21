using System;
using MenuAPI;
using VStancer.Client.Scripts;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class WheelMenu : Menu
    {
        private readonly WheelScript _script;

        internal WheelMenu(WheelScript script, string name = Globals.ScriptName, string subtitle = "Wheel Menu") : base(name, subtitle)
        {
            _script = script;

            _script.WheelDataChanged += new EventHandler((sender, args) => Update());

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

        private MenuDynamicListItem FrontCamberListItem { get; set; }
        private MenuDynamicListItem RearCamberListItem { get; set; }
        private MenuDynamicListItem FrontTrackWidthListItem { get; set; }
        private MenuDynamicListItem RearTrackWidthListItem { get; set; }
        private MenuItem ResetItem { get; set; }

        internal event FloatPropertyChanged FloatPropertyChangedEvent;
        internal event EventHandler<string> ResetPropertiesEvent;

        internal void Update()
        {
            ClearMenuItems();

            if (!_script.DataIsValid)
                return;

            FrontTrackWidthListItem = CreateDynamicFloatList("Front Track Width",
                -_script.WheelData.DefaultFrontTrackWidth,
                -_script.WheelData.FrontTrackWidth,
                _script.Config.WheelLimits.FrontTrackWidth,
                WheelScript.FrontTrackWidthID,
                _script.Config.FloatStep);

            RearTrackWidthListItem = CreateDynamicFloatList("Rear Track Width",
                -_script.WheelData.DefaultRearTrackWidth,
                -_script.WheelData.RearTrackWidth,
                _script.Config.WheelLimits.RearTrackWidth,
                WheelScript.RearTrackWidthID,
                _script.Config.FloatStep);

            FrontCamberListItem = CreateDynamicFloatList("Front Camber",
                _script.WheelData.DefaultFrontCamber,
                _script.WheelData.FrontCamber,
                _script.Config.WheelLimits.FrontCamber,
                WheelScript.FrontCamberID,
                _script.Config.FloatStep);

            RearCamberListItem = CreateDynamicFloatList("Rear Camber",
                _script.WheelData.DefaultRearCamber,
                _script.WheelData.RearCamber,
                _script.Config.WheelLimits.RearCamber,
                WheelScript.RearCamberID,
                _script.Config.FloatStep);

            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = WheelScript.ResetID };

            AddMenuItem(FrontTrackWidthListItem);
            AddMenuItem(RearTrackWidthListItem);
            AddMenuItem(FrontCamberListItem);
            AddMenuItem(RearCamberListItem);
            AddMenuItem(ResetItem);
        }
    }
}
