using System;
using MenuAPI;
using VStancer.Client.Scripts;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class DataMenu : Menu
    {
        private readonly VStancerDataScript _script;

        internal DataMenu(VStancerDataScript script, string name = Globals.ScriptName, string subtitle = "Wheel Menu") : base(name, subtitle)
        {
            _script = script;

            _script.VStancerDataChanged += new EventHandler((sender, args) => Update());

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
                -_script.VStancerData.DefaultFrontTrackWidth,
                -_script.VStancerData.FrontTrackWidth,
                _script.Config.FrontLimits.PositionX,
                VStancerDataScript.FrontTrackWidthID,
                _script.Config.FloatStep);

            RearTrackWidthListItem = CreateDynamicFloatList("Rear Track Width",
                -_script.VStancerData.DefaultRearTrackWidth,
                -_script.VStancerData.RearTrackWidth,
                _script.Config.RearLimits.PositionX,
                VStancerDataScript.RearTrackWidthID,
                _script.Config.FloatStep);

            FrontCamberListItem = CreateDynamicFloatList("Front Camber",
                _script.VStancerData.DefaultFrontCamber,
                _script.VStancerData.FrontCamber,
                _script.Config.FrontLimits.RotationY,
                VStancerDataScript.FrontCamberID,
                _script.Config.FloatStep);

            RearCamberListItem = CreateDynamicFloatList("Rear Camber",
                _script.VStancerData.DefaultRearCamber,
                _script.VStancerData.RearCamber,
                _script.Config.RearLimits.RotationY,
                VStancerDataScript.RearCamberID,
                _script.Config.FloatStep);

            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = VStancerDataScript.ResetID };

            AddMenuItem(FrontTrackWidthListItem);
            AddMenuItem(RearTrackWidthListItem);
            AddMenuItem(FrontCamberListItem);
            AddMenuItem(RearCamberListItem);
            AddMenuItem(ResetItem);
        }
    }
}
