using System;
using MenuAPI;
using VStancer.Client.Scripts;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class ExtraMenu : Menu
    {
        private readonly VStancerExtraScript _script;

        internal ExtraMenu(VStancerExtraScript script, string name = Globals.ScriptName, string subtitle = "Wheel Mod Menu") : base(name, subtitle)
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

        private MenuDynamicListItem FrontTireColliderWidthListItem { get; set; }
        private MenuDynamicListItem FrontTireColliderSizeListItem { get; set; }
        private MenuDynamicListItem FrontRimColliderSizeListItem { get; set; }
        private MenuDynamicListItem RearTireColliderWidthListItem { get; set; }
        private MenuDynamicListItem RearTireColliderSizeListItem { get; set; }
        private MenuDynamicListItem RearRimColliderSizeListItem { get; set; }
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
                VStancerExtraScript.WheelSizeID,
                _script.Config.FloatStep);

            WheelWidthListItem = CreateDynamicFloatList("Wheel Width",
                _script.VStancerExtra.DefaultWheelWidth,
                _script.VStancerExtra.WheelWidth,
                _script.Config.Extra.WheelWidth,
                VStancerExtraScript.WheelWidthID,
                _script.Config.FloatStep);

            FrontTireColliderWidthListItem = CreateDynamicFloatList("Front Tire Collider Width",
                _script.VStancerExtra.DefaultFrontTireColliderWidth,
                _script.VStancerExtra.FrontTireColliderWidth,
                _script.Config.Extra.FrontWheelModSizeNodeLimit.TireColliderScaleX,
                VStancerExtraScript.FrontTireColliderWidthID,
                _script.Config.FloatStep);

            FrontTireColliderSizeListItem = CreateDynamicFloatList("Front Tire Collider Size",
                _script.VStancerExtra.DefaultFrontTireColliderSize,
                _script.VStancerExtra.FrontTireColliderSize,
                _script.Config.Extra.FrontWheelModSizeNodeLimit.TireColliderScaleYZ,
                VStancerExtraScript.FrontTireColliderSizeID,
                _script.Config.FloatStep);

            FrontRimColliderSizeListItem = CreateDynamicFloatList("Front Rim Collider Size",
                _script.VStancerExtra.DefaultFrontRimColliderSize,
                _script.VStancerExtra.FrontRimColliderSize,
                _script.Config.Extra.FrontWheelModSizeNodeLimit.RimColliderScaleYZ,
                VStancerExtraScript.FrontRimColliderSizeID,
                _script.Config.FloatStep);

            RearTireColliderWidthListItem = CreateDynamicFloatList("Rear Tire Collider Width",
                _script.VStancerExtra.DefaultRearTireColliderWidth,
                _script.VStancerExtra.RearTireColliderWidth,
                _script.Config.Extra.RearWheelModSizeNodeLimit.TireColliderScaleX,
                VStancerExtraScript.RearTireColliderWidthID,
                _script.Config.FloatStep);

            RearTireColliderSizeListItem = CreateDynamicFloatList("Rear Tire Collider Size",
                _script.VStancerExtra.DefaultRearTireColliderSize,
                _script.VStancerExtra.RearTireColliderSize,
                _script.Config.Extra.RearWheelModSizeNodeLimit.TireColliderScaleYZ,
                VStancerExtraScript.RearTireColliderSizeID,
                _script.Config.FloatStep);

            RearRimColliderSizeListItem = CreateDynamicFloatList("Rear Rim Collider Size",
                _script.VStancerExtra.DefaultRearRimColliderSize,
                _script.VStancerExtra.RearRimColliderSize,
                _script.Config.Extra.RearWheelModSizeNodeLimit.RimColliderScaleYZ,
                VStancerExtraScript.RearRimColliderSizeID,
                _script.Config.FloatStep);

            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = VStancerExtraScript.ExtraResetID };

            AddMenuItem(WheelSizeListItem);
            AddMenuItem(WheelWidthListItem);
            AddMenuItem(FrontTireColliderWidthListItem);
            AddMenuItem(FrontTireColliderSizeListItem);
            AddMenuItem(FrontRimColliderSizeListItem);
            AddMenuItem(RearTireColliderWidthListItem);
            AddMenuItem(RearTireColliderSizeListItem);
            AddMenuItem(RearRimColliderSizeListItem);
            AddMenuItem(ResetItem);
        }
    }
}
