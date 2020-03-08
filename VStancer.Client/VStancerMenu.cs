using System;
using System.Threading.Tasks;
using MenuAPI;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client
{
    internal class VStancerMenu
    {
        /// <summary>
        /// The script which owns this menu
        /// </summary>
        private readonly VStancerEditor _vstancerEditor;

        /// <summary>
        /// The controller of the menu
        /// </summary>
        private MenuController _menuController;

        /// <summary>
        /// The editor menu
        /// </summary>
        private Menu _editorMenu;

        /// <summary>
        /// The local presets menu
        /// </summary>
        private Menu _personalPresetsMenu;

        /// <summary>
        /// Invoked when a property has its value changed in the UI
        /// </summary>
        /// <param name="id">The id of the property</param>
        /// <param name="value">The new value of the property</param>
        public delegate void MenuPresetValueChangedEvent(string id, string value);

        /// <summary>
        /// Invoked when a property has its value changed in the UI
        /// </summary>
        public event MenuPresetValueChangedEvent EditorMenuPresetValueChanged;

        /// <summary>
        /// Invoked when the reset button is pressed in the UI
        /// </summary>
        public event EventHandler EditorMenuResetPreset;

        /// <summary>
        /// Invoked when the button to apply a personal preset is pressed
        /// </summary>
        public event EventHandler<string> PersonalPresetsMenuApplyPreset;

        /// <summary>
        /// Invoked when the button to save a personal preset is pressed
        /// </summary>
        public event EventHandler<string> PersonalPresetsMenuSavePreset;

        /// <summary>
        /// Invoked when the button to delete a personal preset is pressed
        /// </summary>
        public event EventHandler<string> PersonalPresetsMenuDeletePreset;

        private string ResetID => VStancerEditor.ResetID;
        private string FrontOffsetID => VStancerEditor.FrontOffsetID;
        private string FrontRotationID => VStancerEditor.FrontRotationID;
        private string RearOffsetID => VStancerEditor.RearOffsetID;
        private string RearRotationID => VStancerEditor.RearRotationID;
        private VStancerPreset CurrentPreset => _vstancerEditor.CurrentPreset;
        private float FloatStep => _vstancerEditor.Config.FloatStep;

        /// <summary>
        /// Create a method to determine the logic for when the left/right arrow are pressed
        /// </summary>
        /// <param name="name">The name of the item</param>
        /// <param name="value">The current value</param>
        /// <param name="minimum">The min allowed value</param>
        /// <param name="maximum">The max allowed value</param>
        /// <returns>The <see cref="MenuDynamicListItem.ChangeItemCallback"/></returns>
        private MenuDynamicListItem.ChangeItemCallback FloatChangeCallback(string name, float value, float minimum, float maximum)
        {
            string callback(MenuDynamicListItem sender, bool left)
            {
                var min = minimum;
                var max = maximum;

                var newvalue = value;

                if (left)
                    newvalue -= FloatStep;
                else if (!left)
                    newvalue += FloatStep;
                else return value.ToString("F3");

                // Hotfix to trim the value to 3 digits
                newvalue = float.Parse((newvalue).ToString("F3"));

                if (newvalue < min)
                    Screen.ShowNotification($"~o~Warning~w~: Min ~b~{name}~w~ value allowed is {min} for this vehicle");
                else if (newvalue > max)
                    Screen.ShowNotification($"~o~Warning~w~: Max ~b~{name}~w~ value allowed is {max} for this vehicle");
                else
                {
                    value = newvalue;
                }
                return value.ToString("F3");
            };
            return callback;
        }

        /// <summary>
        /// Creates a controller for the a float property
        /// </summary>
        /// <param name="menu">The menu to add the controller to</param>
        /// <param name="name">The displayed name of the controller</param>
        /// <param name="defaultValue">The default value of the controller</param>
        /// <param name="value">The current value of the controller</param>
        /// <param name="maxEditing">The max delta allowed relative to the default value</param>
        /// <param name="id">The ID of the property linked to the controller</param>
        /// <returns></returns>
        private MenuDynamicListItem AddDynamicFloatList(Menu menu, string name, float defaultValue, float value, float maxEditing, string id)
        {
            float min = defaultValue - maxEditing;
            float max = defaultValue + maxEditing;

            var callback = FloatChangeCallback(name, value, min, max);

            var newitem = new MenuDynamicListItem(name, value.ToString("F3"), callback) { ItemData = id };
            menu.AddMenuItem(newitem);
            return newitem;
        }

        /// <summary>
        /// Setup the menu
        /// </summary>
        private void InitializeMenu()
        {
            if (_editorMenu == null)
            {
                _editorMenu = new Menu(Globals.ScriptName, "Editor");

                // When the value of a MenuDynamicListItem is changed
                _editorMenu.OnDynamicListItemCurrentItemChange += (menu, dynamicListItem, oldValue, newValue) =>
                {
                    string id = dynamicListItem.ItemData as string;
                    EditorMenuPresetValueChanged?.Invoke(id, newValue);
                };

                // When a MenuItem is selected
                _editorMenu.OnItemSelect += (menu, menuItem, itemIndex) =>
                {
                    // If the selected item is the reset button
                    if (menuItem.ItemData as string == ResetID)
                        EditorMenuResetPreset.Invoke(this, EventArgs.Empty);
                };
            }

            if (_personalPresetsMenu == null)
            {
                _personalPresetsMenu = new Menu(Globals.ScriptName, "Personal Presets");

                _personalPresetsMenu.OnItemSelect += PersonalPresetsMenu_OnItemSelect;

                #region Save/Delete Handler

                _personalPresetsMenu.InstructionalButtons.Add(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE"));
                _personalPresetsMenu.InstructionalButtons.Add(Control.PhoneOption, GetLabelText("ITEM_DEL"));

                // Disable Controls binded on the same key
                _personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.SelectWeapon, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));
                _personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.VehicleExit, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));

                _personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneExtraOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>(async (sender, control) =>
                {
                    string presetName = await _vstancerEditor.GetOnScreenString("VSTANCER_ENTER_PRESET_NAME","");
                    PersonalPresetsMenuSavePreset?.Invoke(_personalPresetsMenu, presetName.Trim());
                }), true));
                _personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) =>
                {
                    if (_personalPresetsMenu.GetMenuItems().Count > 0)
                    {
                        string presetName = _personalPresetsMenu.GetMenuItems()[_personalPresetsMenu.CurrentIndex].Text;
                        PersonalPresetsMenuDeletePreset?.Invoke(_personalPresetsMenu, presetName);
                    }
                }), true));

                #endregion
            }

            UpdatePersonalPresetsMenu();
            UpdateEditorMenu();

            if (_menuController == null)
            {
                _menuController = new MenuController();
                MenuController.AddMenu(_editorMenu);
                MenuController.AddSubmenu(_editorMenu, _personalPresetsMenu);
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
                MenuController.MenuToggleKey = (Control)_vstancerEditor.Config.ToggleMenuControl;
                MenuController.EnableMenuToggleKeyOnController = false;
                MenuController.MainMenu = _editorMenu;
            }
        }

        private void PersonalPresetsMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex) => PersonalPresetsMenuApplyPreset?.Invoke(menu, menuItem.Text);

        /// <summary>
        /// Rebuild the personal presets menu
        /// </summary>
        private void UpdatePersonalPresetsMenu()
        {
            if (_personalPresetsMenu == null)
                return;

            _personalPresetsMenu.ClearMenuItems();

            foreach (var key in _vstancerEditor.LocalPresetsManager.GetKeys())
            {
                _personalPresetsMenu.AddMenuItem(new MenuItem(key.Remove(0, Globals.KvpPrefix.Length)) { ItemData = key });
            }
        }

        /// <summary>
        /// Update the items of the main menu
        /// </summary>
        private void UpdateEditorMenu()
        {
            if (_editorMenu == null)
                return;

            _editorMenu.ClearMenuItems();

            if (!_vstancerEditor.CurrentPresetIsValid)
                return;

            AddDynamicFloatList(_editorMenu, "Front Track Width", -CurrentPreset.DefaultFrontPositionX, -CurrentPreset.FrontPositionX, _vstancerEditor.Config.FrontLimits.PositionX, FrontOffsetID);
            AddDynamicFloatList(_editorMenu, "Rear Track Width", -CurrentPreset.DefaultRearPositionX, -CurrentPreset.RearPositionX, _vstancerEditor.Config.RearLimits.PositionX, RearOffsetID);
            AddDynamicFloatList(_editorMenu, "Front Camber", CurrentPreset.DefaultFrontRotationY, CurrentPreset.FrontRotationY, _vstancerEditor.Config.FrontLimits.RotationY, FrontRotationID);
            AddDynamicFloatList(_editorMenu, "Rear Camber", CurrentPreset.DefaultRearRotationY, CurrentPreset.RearRotationY, _vstancerEditor.Config.RearLimits.RotationY, RearRotationID);
            _editorMenu.AddMenuItem(new MenuItem("Reset", "Restores the default values") { ItemData = ResetID });

            // Create personal presets button and bind it to the submenu
            var personalPresetsItem = new MenuItem("Personal Presets", "The vstancer presets saved by you.")
            {
                Label = "→→→"
            };
            _editorMenu.AddMenuItem(personalPresetsItem);
            MenuController.BindMenuItem(_editorMenu, _personalPresetsMenu, personalPresetsItem);
        }

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        /// <param name="script">The script which owns this menu</param>
        internal VStancerMenu(VStancerEditor script)
        {
            _vstancerEditor = script;
            _vstancerEditor.NewPresetCreated += new EventHandler((sender,args) => UpdateEditorMenu());
            _vstancerEditor.ToggleMenuVisibility += new EventHandler((sender,args) => 
            {
                //var currentMenu = MenuController.GetCurrentMenu();
                var currentMenu = MenuController.MainMenu;

                if (currentMenu == null)
                    return;

                currentMenu.Visible = !currentMenu.Visible;
            });

            AddTextEntry("VSTANCER_ENTER_PRESET_NAME", "Enter a name for the preset");
            InitializeMenu();

            _vstancerEditor.LocalPresetsManager.PresetsListChanged += new EventHandler((sender, args) => UpdatePersonalPresetsMenu());
        }

        public void HideUI()
        {
            if (MenuController.IsAnyMenuOpen())
                MenuController.CloseAllMenus();
        }
    }
}
