using System;
using System.Threading.Tasks;
using MenuAPI;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace VStancer.Client
{/*
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
        private MenuAPI.Menu _editorMenu;

        /// <summary>
        /// The wheel mod size editor menu
        /// </summary>
        private MenuAPI.Menu _extraMenu;

        /// <summary>
        /// The local presets menu
        /// </summary>
        private MenuAPI.Menu _personalPresetsMenu;

        private MenuItem _extraMenuItem;

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
        public event MenuPresetValueChangedEvent ExtraMenuPresetValueChanged;

        /// <summary>
        /// Invoked when the reset button is pressed in the UI
        /// </summary>
        public event EventHandler EditorMenuResetPreset;
        public event EventHandler ExtraMenuResetPreset;

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


        public bool HideUI
        {
            get => MenuController.DontOpenAnyMenu;
            set
            {
                MenuController.DontOpenAnyMenu = value;
            }
        }

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
                    newvalue -= _vstancerEditor.Config.FloatStep;
                else if (!left)
                    newvalue += _vstancerEditor.Config.FloatStep;
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
        private MenuDynamicListItem AddDynamicFloatList(MenuAPI.Menu menu, string name, float defaultValue, float value, float maxEditing, string id)
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
                _editorMenu = new MenuAPI.Menu(Globals.ScriptName, "Editor");

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
                    if (menuItem.ItemData as string == VStancerEditor.ResetID)
                        EditorMenuResetPreset?.Invoke(this, EventArgs.Empty);
                };
            }

            if(_extraMenu == null)
            {
                _extraMenu = new MenuAPI.Menu(Globals.ScriptName, "Extra");

                // When the value of a MenuDynamicListItem is changed
                _extraMenu.OnDynamicListItemCurrentItemChange += (menu, dynamicListItem, oldValue, newValue) =>
                {
                    string id = dynamicListItem.ItemData as string;
                    ExtraMenuPresetValueChanged?.Invoke(id, newValue);
                };

                _extraMenu.OnItemSelect += (menu, menuItem, itemIndex) =>
                {
                    // If the selected item is the reset button
                    if (menuItem.ItemData as string == VStancerEditor.ExtraResetID)
                        ExtraMenuResetPreset?.Invoke(this, EventArgs.Empty);
                };
            }

            if (_personalPresetsMenu == null)
            {
                _personalPresetsMenu = new MenuAPI.Menu(Globals.ScriptName, "Personal Presets");

                _personalPresetsMenu.OnItemSelect += PersonalPresetsMenu_OnItemSelect;

                #region Save/Delete Handler

                _personalPresetsMenu.InstructionalButtons.Add(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE"));
                _personalPresetsMenu.InstructionalButtons.Add(Control.PhoneOption, GetLabelText("ITEM_DEL"));

                // Disable Controls binded on the same key
                _personalPresetsMenu.ButtonPressHandlers.Add(new MenuAPI.Menu.ButtonPressHandler(Control.SelectWeapon, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, new Action<MenuAPI.Menu, Control>((sender, control) => { }), true));
                _personalPresetsMenu.ButtonPressHandlers.Add(new MenuAPI.Menu.ButtonPressHandler(Control.VehicleExit, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, new Action<MenuAPI.Menu, Control>((sender, control) => { }), true));

                _personalPresetsMenu.ButtonPressHandlers.Add(new MenuAPI.Menu.ButtonPressHandler(Control.PhoneExtraOption, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, new Action<MenuAPI.Menu, Control>(async (sender, control) =>
                {
                    string presetName = await _vstancerEditor.GetOnScreenString("VSTANCER_ENTER_PRESET_NAME","");
                    PersonalPresetsMenuSavePreset?.Invoke(_personalPresetsMenu, presetName.Trim());
                }), true));
                _personalPresetsMenu.ButtonPressHandlers.Add(new MenuAPI.Menu.ButtonPressHandler(Control.PhoneOption, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, new Action<MenuAPI.Menu, Control>((sender, control) =>
                {
                    if (_personalPresetsMenu.GetMenuItems().Count > 0)
                    {
                        string presetName = _personalPresetsMenu.GetMenuItems()[_personalPresetsMenu.CurrentIndex].Text;
                        PersonalPresetsMenuDeletePreset?.Invoke(_personalPresetsMenu, presetName);
                    }
                }), true));

                #endregion
            }

            // This goes here as doesn't need to be created everytime
            UpdatePersonalPresetsMenu();

            UpdateEditorMenu();

            if (_menuController == null)
            {
                _menuController = new MenuController();
                MenuController.AddMenu(_editorMenu);
                MenuController.AddSubmenu(_editorMenu, _extraMenu);
                MenuController.AddSubmenu(_editorMenu, _personalPresetsMenu);
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
                MenuController.MenuToggleKey = (Control)_vstancerEditor.Config.ToggleMenuControl;
                MenuController.EnableMenuToggleKeyOnController = false;
                MenuController.DontOpenAnyMenu = true;
                MenuController.MainMenu = _editorMenu;
            }
        }

        private void PersonalPresetsMenu_OnItemSelect(MenuAPI.Menu menu, MenuItem menuItem, int itemIndex) => PersonalPresetsMenuApplyPreset?.Invoke(menu, menuItem.Text);

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

            AddDynamicFloatList(_editorMenu, "Front Track Width",
                -_vstancerEditor.CurrentPreset.DefaultFrontTrackWidth,
                -_vstancerEditor.CurrentPreset.FrontTrackWidth,
                _vstancerEditor.Config.FrontLimits.PositionX,
                VStancerEditor.FrontTrackWidthID);
            
            AddDynamicFloatList(_editorMenu, "Rear Track Width",
                -_vstancerEditor.CurrentPreset.DefaultRearTrackWidth,
                -_vstancerEditor.CurrentPreset.RearTrackWidth,
                _vstancerEditor.Config.RearLimits.PositionX,
                VStancerEditor.RearTrackWidthID);

            AddDynamicFloatList(_editorMenu, "Front Camber",
                _vstancerEditor.CurrentPreset.DefaultFrontCamber,
                _vstancerEditor.CurrentPreset.FrontCamber,
                _vstancerEditor.Config.FrontLimits.RotationY,
                VStancerEditor.FrontCamberID);

            AddDynamicFloatList(_editorMenu, "Rear Camber",
                _vstancerEditor.CurrentPreset.DefaultRearCamber,
                _vstancerEditor.CurrentPreset.RearCamber,
                _vstancerEditor.Config.RearLimits.RotationY,
                VStancerEditor.RearCamberID);

            _editorMenu.AddMenuItem(new MenuItem("Reset", "Restores the default values") { ItemData = VStancerEditor.ResetID });

            _extraMenuItem = new MenuItem("Extra");
            UpdateExtraMenuItem();

            UpdateExtraMenu();
            _editorMenu.AddMenuItem(_extraMenuItem);
            MenuController.BindMenuItem(_editorMenu, _extraMenu, _extraMenuItem);

            // Create personal presets button and bind it to the submenu
            var personalPresetsItem = new MenuItem("Personal Presets", "The vstancer presets saved by you.")
            {
                Label = "→→→"
            };
            _editorMenu.AddMenuItem(personalPresetsItem);
            MenuController.BindMenuItem(_editorMenu, _personalPresetsMenu, personalPresetsItem);
        }

        private void UpdateExtraMenuItem()
        {
            if(_extraMenuItem == null)
                return;

            var enabled = _vstancerEditor.CurrentExtraIsValid;

            _extraMenuItem.Enabled = enabled;
            _extraMenuItem.RightIcon = enabled ? MenuItem.Icon.NONE : MenuItem.Icon.LOCK;
            _extraMenuItem.Label = enabled ? "→→→" : string.Empty;
            _extraMenuItem.Description = enabled ? "The menu to edit the size of the wheel mods." : "Install a wheel mod to access to this menu";
        }

        private void UpdateExtraMenu()
        {
            if (_extraMenu == null)
                return;

            _extraMenu.ClearMenuItems();

            if (!_vstancerEditor.CurrentExtraIsValid)
                return;

            AddDynamicFloatList(_extraMenu,
                "Wheel Width",
                _vstancerEditor.CurrentExtra.DefaultWheelWidth,
                _vstancerEditor.CurrentExtra.WheelWidth,
                _vstancerEditor.Config.Extra.WheelWidth,
                VStancerEditor.ExtraWidthID);

            AddDynamicFloatList(_extraMenu,
                "Wheel Radius",
                _vstancerEditor.CurrentExtra.DefaultWheelSize,
                _vstancerEditor.CurrentExtra.WheelSize,
                _vstancerEditor.Config.Extra.WheelSize,
                VStancerEditor.ExtraSizeID);

            _extraMenu.AddMenuItem(new MenuItem("Reset", "Restores the default values") { ItemData = VStancerEditor.ExtraResetID });
        }

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        /// <param name="script">The script which owns this menu</param>
        internal VStancerMenu(VStancerEditor script)
        {
            _vstancerEditor = script;
            _vstancerEditor.NewPresetCreated += new EventHandler((sender,args) => UpdateEditorMenu());
            _vstancerEditor.ExtraChanged += new EventHandler((sender,args) => 
            {
                UpdateExtraMenu();
                UpdateExtraMenuItem(); 
            });
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
    }*/
}
