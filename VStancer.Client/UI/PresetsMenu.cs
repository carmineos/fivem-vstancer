using System;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client.UI
{
    internal class PresetsMenu : Menu
    {
        private readonly VStancerEditor _vstancerEditor;

        internal PresetsMenu(VStancerEditor editor, string name = Globals.ScriptName, string subtitle = "Personal Presets") : base(name, subtitle)
        {
            _vstancerEditor = editor;

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
                string presetName = await _vstancerEditor.GetOnScreenString("VSTANCER_ENTER_PRESET_NAME", "");
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

            if (_vstancerEditor.LocalPresetsManager == null)
                return;

            foreach (var key in _vstancerEditor.LocalPresetsManager.GetKeys())
            {
                AddMenuItem(new MenuItem(key.Remove(0, Globals.KvpPrefix.Length)) { ItemData = key });
            }
        }

        private void ItemSelect(Menu menu, MenuItem menuItem, int itemIndex) => ApplyPresetEvent?.Invoke(menu, menuItem.Text);
    }
}