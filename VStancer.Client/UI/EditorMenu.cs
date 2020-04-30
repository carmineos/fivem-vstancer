using System;
using MenuAPI;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class EditorMenu : Menu
    {
        private readonly VStancerDataManager _manager;

        internal EditorMenu(VStancerDataManager manager, string name = Globals.ScriptName, string subtitle = "Editor Menu") : base(name, subtitle)
        {
            _manager = manager;

            _manager.VStancerDataChanged += new EventHandler((sender, args) => Update());

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
            
            if (!_manager.DataIsValid)
                return;

            FrontTrackWidthListItem = MenuUtilities.CreateDynamicFloatList("Front Track Width",
                -_manager.VStancerData.DefaultFrontTrackWidth,
                -_manager.VStancerData.FrontTrackWidth,
                _manager.Config.FrontLimits.PositionX,
                VStancerDataManager.FrontTrackWidthID,
                _manager.Config.FloatStep);

            RearTrackWidthListItem = MenuUtilities.CreateDynamicFloatList("Rear Track Width",
                -_manager.VStancerData.DefaultRearTrackWidth,
                -_manager.VStancerData.RearTrackWidth,
                _manager.Config.RearLimits.PositionX,
                VStancerDataManager.RearTrackWidthID,
                _manager.Config.FloatStep);

            FrontCamberListItem = MenuUtilities.CreateDynamicFloatList("Front Camber",
                _manager.VStancerData.DefaultFrontCamber,
                _manager.VStancerData.FrontCamber,
                _manager.Config.FrontLimits.RotationY,
                VStancerDataManager.FrontCamberID,
                _manager.Config.FloatStep);

            RearCamberListItem = MenuUtilities.CreateDynamicFloatList("Rear Camber",
                _manager.VStancerData.DefaultRearCamber,
                _manager.VStancerData.RearCamber,
                _manager.Config.RearLimits.RotationY,
                VStancerDataManager.RearCamberID,
                _manager.Config.FloatStep);
                
            ResetItem = new MenuItem("Reset", "Restores the default values") { ItemData = VStancerDataManager.ResetID };

            AddMenuItem(FrontTrackWidthListItem);
            AddMenuItem(RearTrackWidthListItem);
            AddMenuItem(FrontCamberListItem);
            AddMenuItem(RearCamberListItem);
            AddMenuItem(ResetItem);
        }
    }
}
