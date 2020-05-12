using System;
using MenuAPI;
using VStancer.Client.Scripts;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class WheelModMenu : Menu
    {
        private readonly WheelModScript _script;
        private bool _enabled;

        internal event EventHandler<string> PropertyChanged;

        internal bool Enabled 
        { 
            get => _enabled;
            private set
            {
                if (Equals(value, _enabled))
                    return;

                _enabled = value;
                PropertyChanged?.Invoke(this, nameof(Enabled));
            } 
        }

        internal WheelModMenu(WheelModScript script, string name = Globals.ScriptName, string subtitle = "Wheel Mod Menu") : base(name, subtitle)
        {
            _script = script;

            _script.WheelModDataChanged += new EventHandler((sender, args) =>
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

        //private MenuDynamicListItem FrontTireColliderWidthListItem { get; set; }
        //private MenuDynamicListItem FrontTireColliderSizeListItem { get; set; }
        //private MenuDynamicListItem FrontRimColliderSizeListItem { get; set; }
        //private MenuDynamicListItem RearTireColliderWidthListItem { get; set; }
        //private MenuDynamicListItem RearTireColliderSizeListItem { get; set; }
        //private MenuDynamicListItem RearRimColliderSizeListItem { get; set; }
        private MenuItem ResetItem { get; set; }

        internal event FloatPropertyChanged FloatPropertyChangedEvent;
        internal event EventHandler<string> ResetPropertiesEvent;

        internal void Update()
        {
            ClearMenuItems();

            Enabled = _script.DataIsValid;
            
            if (!Enabled)
                return;

            WheelSizeListItem = CreateDynamicFloatList("Wheel Size",
                _script.WheelModData.DefaultWheelSize,
                _script.WheelModData.WheelSize,
                _script.Config.WheelModLimits.WheelSize,
                WheelModScript.WheelSizeID,
                _script.Config.FloatStep);

            WheelWidthListItem = CreateDynamicFloatList("Wheel Width",
                _script.WheelModData.DefaultWheelWidth,
                _script.WheelModData.WheelWidth,
                _script.Config.WheelModLimits.WheelWidth,
                WheelModScript.WheelWidthID,
                _script.Config.FloatStep);

            /*
            FrontTireColliderWidthListItem = CreateDynamicFloatList("Front Tire Collider Width",
                _script.WheelModData.DefaultFrontTireColliderWidth,
                _script.WheelModData.FrontTireColliderWidth,
                _script.Config.WheelModLimits.FrontTireColliderWidth,
                WheelModScript.FrontTireColliderWidthID,
                _script.Config.FloatStep);

            FrontTireColliderSizeListItem = CreateDynamicFloatList("Front Tire Collider Size",
                _script.WheelModData.DefaultFrontTireColliderSize,
                _script.WheelModData.FrontTireColliderSize,
                _script.Config.WheelModLimits.FrontTireColliderSize,
                WheelModScript.FrontTireColliderSizeID,
                _script.Config.FloatStep);

            FrontRimColliderSizeListItem = CreateDynamicFloatList("Front Rim Collider Size",
                _script.WheelModData.DefaultFrontRimColliderSize,
                _script.WheelModData.FrontRimColliderSize,
                _script.Config.WheelModLimits.FrontRimColliderSize,
                WheelModScript.FrontRimColliderSizeID,
                _script.Config.FloatStep);
            
            RearTireColliderWidthListItem = CreateDynamicFloatList("Rear Tire Collider Width",
                _script.WheelModData.DefaultRearTireColliderWidth,
                _script.WheelModData.RearTireColliderWidth,
                _script.Config.WheelModLimits.RearTireColliderWidth,
                WheelModScript.RearTireColliderWidthID,
                _script.Config.FloatStep);

            RearTireColliderSizeListItem = CreateDynamicFloatList("Rear Tire Collider Size",
                _script.WheelModData.DefaultRearTireColliderSize,
                _script.WheelModData.RearTireColliderSize,
                _script.Config.WheelModLimits.RearTireColliderSize,
                WheelModScript.RearTireColliderSizeID,
                _script.Config.FloatStep);

            RearRimColliderSizeListItem = CreateDynamicFloatList("Rear Rim Collider Size",
                _script.WheelModData.DefaultRearRimColliderSize,
                _script.WheelModData.RearRimColliderSize,
                _script.Config.WheelModLimits.RearRimColliderSize,
                WheelModScript.RearRimColliderSizeID,
                _script.Config.FloatStep);
            */
            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = WheelModScript.ExtraResetID };

            AddMenuItem(WheelSizeListItem);
            AddMenuItem(WheelWidthListItem);
           //AddMenuItem(FrontTireColliderWidthListItem);
           //AddMenuItem(FrontTireColliderSizeListItem);
           //AddMenuItem(FrontRimColliderSizeListItem);
           //AddMenuItem(RearTireColliderWidthListItem);
           //AddMenuItem(RearTireColliderSizeListItem);
           //AddMenuItem(RearRimColliderSizeListItem);
            AddMenuItem(ResetItem);
        }
    }
}
