using System;
using MenuAPI;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using VStancer.Client.Scripts;

namespace VStancer.Client.UI
{
    internal class ClientPresetsMenu : Menu
    {
        private readonly ClientPresetsScript _script;

        internal ClientPresetsMenu(ClientPresetsScript script, string name = Globals.ScriptName, string subtitle = "Client Presets Menu") : base(name, subtitle)
        {
            _script = script;

            _script.Presets.PresetsCollectionChanged += new EventHandler((sender, args) => Update());

            Update();

            AddTextEntry("VSTANCER_ENTER_PRESET_NAME", "Enter a name for the preset");

            OnItemSelect += ItemSelect;
            InstructionalButtons.Add(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE"));
            InstructionalButtons.Add(Control.PhoneOption, GetLabelText("ITEM_DEL"));

            // Disable Controls binded on the same key
            ButtonPressHandlers.Add(new ButtonPressHandler(Control.SelectWeapon, ControlPressCheckType.JUST_RELEASED, new Action<Menu, Control>((sender, control) =>
            {
            }), true));

            ButtonPressHandlers.Add(new ButtonPressHandler(Control.VehicleExit, ControlPressCheckType.JUST_RELEASED, new Action<Menu, Control>((sender, control) =>
            {
            }), true));

            ButtonPressHandlers.Add(new ButtonPressHandler(Control.PhoneExtraOption, ControlPressCheckType.JUST_RELEASED, new Action<Menu, Control>(async (sender, control) =>
            {
                string presetName = await _script.GetPresetNameFromUser("VSTANCER_ENTER_PRESET_NAME", "");

                if (string.IsNullOrEmpty(presetName))
                    return;

                SavePresetEvent?.Invoke(this, presetName.Trim());
            }), true));

            ButtonPressHandlers.Add(new ButtonPressHandler(Control.PhoneOption, ControlPressCheckType.JUST_RELEASED, new Action<Menu, Control>((sender, control) =>
            {
                if (GetMenuItems().Count > 0)
                {
                    string presetName = GetMenuItems()[CurrentIndex].ItemData;
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

            if (_script.Presets == null)
                return;

            foreach (var key in _script.Presets.GetKeys())
            {
                AddMenuItem(new MenuItem(key) { ItemData = key });
            }
        }

        private void ItemSelect(Menu menu, MenuItem menuItem, int itemIndex) => ApplyPresetEvent?.Invoke(menu, menuItem.ItemData);
    }
}