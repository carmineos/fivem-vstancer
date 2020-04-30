using System;
using MenuAPI;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class EditorMenu : Menu
    {
        private readonly VStancerEditor _vstancerEditor;

        internal EditorMenu(VStancerEditor editor, string name = Globals.ScriptName, string subtitle = "Editor Menu") : base(name, subtitle)
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
            
            if (!_vstancerEditor.CurrentPresetIsValid)
                return;

            FrontTrackWidthListItem = MenuUtilities.CreateDynamicFloatList("Front Track Width",
                -_vstancerEditor.CurrentPreset.DefaultFrontTrackWidth,
                -_vstancerEditor.CurrentPreset.FrontTrackWidth,
                _vstancerEditor.Config.FrontLimits.PositionX,
                VStancerEditor.FrontTrackWidthID,
                _vstancerEditor.Config.FloatStep);

            RearTrackWidthListItem = MenuUtilities.CreateDynamicFloatList("Rear Track Width",
                -_vstancerEditor.CurrentPreset.DefaultRearTrackWidth,
                -_vstancerEditor.CurrentPreset.RearTrackWidth,
                _vstancerEditor.Config.RearLimits.PositionX,
                VStancerEditor.RearTrackWidthID,
                _vstancerEditor.Config.FloatStep);

            FrontCamberListItem = MenuUtilities.CreateDynamicFloatList("Front Camber",
                _vstancerEditor.CurrentPreset.DefaultFrontCamber,
                _vstancerEditor.CurrentPreset.FrontCamber,
                _vstancerEditor.Config.FrontLimits.RotationY,
                VStancerEditor.FrontCamberID,
                _vstancerEditor.Config.FloatStep);

            RearCamberListItem = MenuUtilities.CreateDynamicFloatList("Rear Camber",
                _vstancerEditor.CurrentPreset.DefaultRearCamber,
                _vstancerEditor.CurrentPreset.RearCamber,
                _vstancerEditor.Config.RearLimits.RotationY,
                VStancerEditor.RearCamberID,
                _vstancerEditor.Config.FloatStep);
                
            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = VStancerEditor.ResetID };

            AddMenuItem(FrontTrackWidthListItem);
            AddMenuItem(RearTrackWidthListItem);
            AddMenuItem(FrontCamberListItem);
            AddMenuItem(RearCamberListItem);
            AddMenuItem(ResetItem);
        }
    }
}
