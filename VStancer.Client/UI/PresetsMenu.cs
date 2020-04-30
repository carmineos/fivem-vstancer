using System;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client.UI
{
    internal class PresetsMenu : Menu
    {
        private readonly LocalPresetsManager _manager;

        internal PresetsMenu(LocalPresetsManager manager, string name = Globals.ScriptName, string subtitle = "Personal Presets Menu") : base(name, subtitle)
        {
            _manager = manager;

            _manager.Presets.PresetsCollectionChanged += new EventHandler((sender, args) => Update());

            Update();

            AddTextEntry("VSTANCER_ENTER_PRESET_NAME", "Enter a name for the preset");

            OnItemSelect += ItemSelect;
            InstructionalButtons.Add(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE"));
            InstructionalButtons.Add(Control.PhoneOption, GetLabelText("ITEM_DEL"));

            // Disable Controls binded on the same key
            ButtonPressHandlers.Add(new ButtonPressHandler(Control.SelectWeapon, ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) =>
            { 
            }), true));

            ButtonPressHandlers.Add(new ButtonPressHandler(Control.VehicleExit, ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => 
            { 
            }), true));

            ButtonPressHandlers.Add(new ButtonPressHandler(Control.PhoneExtraOption, ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>(async (sender, control) =>
            {
                string presetName = await _manager.GetPresetNameFromUser("VSTANCER_ENTER_PRESET_NAME", "");
                SavePresetEvent?.Invoke(this, presetName.Trim());
            }), true));

            ButtonPressHandlers.Add(new ButtonPressHandler(Control.PhoneOption, ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) =>
            {
                if (GetMenuItems().Count > 0)
                {
                    string presetName = GetMenuItems()[CurrentIndex].Text;
                    DeletePresetEvent?.Invoke(this, presetName);
                }
            }), true));
        }

        internal event EventHandler<string> ApplyPresetEvent;
        internal event EventHandler<string> SavePresetEvent;
        internal event EventHandler<string> DeletePresetEvent;

        internal void Update()
        {
            ClearMenuItems();

            if (_manager.Presets == null)
                return;

            foreach (var key in _manager.Presets.GetKeys())
            {
                AddMenuItem(new MenuItem(key.Remove(0, Globals.KvpPrefix.Length)) { ItemData = key });
            }
        }

        private void ItemSelect(Menu menu, MenuItem menuItem, int itemIndex) => ApplyPresetEvent?.Invoke(menu, menuItem.Text);
    }
}