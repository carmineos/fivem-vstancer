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
        #region Private Fields

        /// <summary>
        /// The script which owns this menu
        /// </summary>
        private readonly VStancerEditor vstancerEditor;

        /// <summary>
        /// The controller of the menu
        /// </summary>
        private MenuController menuController;

        /// <summary>
        /// The main menu
        /// </summary>
        private Menu editorMenu;

        private Menu personalPresetsMenu;

        #endregion

        #region Public Events

        /// <summary>
        /// Triggered when a property has its value changed in the UI
        /// </summary>
        /// <param name="id">The id of the property</param>
        /// <param name="value">The new value of the property</param>
        public delegate void MenuPresetValueChangedEvent(string id, string value);

        /// <summary>
        /// Triggered when a property has its value changed in the UI
        /// </summary>
        public event MenuPresetValueChangedEvent MenuPresetValueChanged;

        /// <summary>
        /// Triggered when the reset button is pressed in the UI
        /// </summary>
        public event EventHandler MenuResetPresetButtonPressed;

        public event EventHandler<string> MenuApplyPersonalPresetButtonPressed;
        public event EventHandler<string> MenuSavePersonalPresetButtonPressed;
        public event EventHandler<string> MenuDeletePersonalPresetButtonPressed;
        #endregion


        #region Editor Properties

        private string ResetID => VStancerEditor.ResetID;
        private string FrontOffsetID => VStancerEditor.FrontOffsetID;
        private string FrontRotationID => VStancerEditor.FrontRotationID;
        private string RearOffsetID => VStancerEditor.RearOffsetID;
        private string RearRotationID => VStancerEditor.RearRotationID;
        private VStancerPreset CurrentPreset => vstancerEditor.CurrentPreset;
        private float FloatStep => vstancerEditor.Config.FloatStep;

        #endregion

        #region Private Methods

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
            if (editorMenu == null)
            {
                editorMenu = new Menu(Globals.ScriptName, "Editor");

                // When the value of a MenuDynamicListItem is changed
                editorMenu.OnDynamicListItemCurrentItemChange += (menu, dynamicListItem, oldValue, newValue) =>
                {
                    string id = dynamicListItem.ItemData as string;
                    MenuPresetValueChanged?.Invoke(id, newValue);
                };

                // When a MenuItem is selected
                editorMenu.OnItemSelect += (menu, menuItem, itemIndex) =>
                {
                    // If the selected item is the reset button
                    if (menuItem.ItemData as string == ResetID)
                        MenuResetPresetButtonPressed.Invoke(this, EventArgs.Empty);
                };
            }

            if (personalPresetsMenu == null)
            {
                personalPresetsMenu = new Menu(Globals.ScriptName, "Personal Presets");

                personalPresetsMenu.OnItemSelect += PersonalPresetsMenu_OnItemSelect;

                #region Save/Delete Handler

                personalPresetsMenu.InstructionalButtons.Add(Control.PhoneExtraOption, GetLabelText("ITEM_SAVE"));
                personalPresetsMenu.InstructionalButtons.Add(Control.PhoneOption, GetLabelText("ITEM_DEL"));

                // Disable Controls binded on the same key
                personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.SelectWeapon, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));
                personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.VehicleExit, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) => { }), true));

                personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneExtraOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>(async (sender, control) =>
                {
                    string presetName = await vstancerEditor.GetOnScreenString("");
                    MenuSavePersonalPresetButtonPressed?.Invoke(personalPresetsMenu, presetName);
                }), true));
                personalPresetsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.PhoneOption, Menu.ControlPressCheckType.JUST_PRESSED, new Action<Menu, Control>((sender, control) =>
                {
                    if (personalPresetsMenu.GetMenuItems().Count > 0)
                    {
                        string presetName = personalPresetsMenu.GetMenuItems()[personalPresetsMenu.CurrentIndex].Text;
                        MenuDeletePersonalPresetButtonPressed?.Invoke(personalPresetsMenu, presetName);
                    }
                }), true));

                #endregion
            }

            UpdatePersonalPresetsMenu();
            UpdateEditorMenu();

            if (menuController == null)
            {
                menuController = new MenuController();
                MenuController.AddMenu(editorMenu);
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
                MenuController.MenuToggleKey = (Control)vstancerEditor.Config.ToggleMenuControl;
                MenuController.EnableMenuToggleKeyOnController = false;
                MenuController.MainMenu = editorMenu;
            }
        }

        private void PersonalPresetsMenu_OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex) => MenuApplyPersonalPresetButtonPressed?.Invoke(menu, menuItem.Text);

        /// <summary>
        /// Rebuild the personal presets menu
        /// </summary>
        private void UpdatePersonalPresetsMenu()
        {
            if (personalPresetsMenu == null)
                return;

            personalPresetsMenu.ClearMenuItems();

            foreach (var key in vstancerEditor.LocalPresetsManager.GetKeys())
            {
                personalPresetsMenu.AddMenuItem(new MenuItem(key.Remove(0, Globals.KvpPrefix.Length)) { ItemData = key });
            }
        }

        /// <summary>
        /// Update the items of the main menu
        /// </summary>
        private void UpdateEditorMenu()
        {
            if (editorMenu == null)
                return;

            editorMenu.ClearMenuItems();

            if (!vstancerEditor.CurrentPresetIsValid)
                return;

            AddDynamicFloatList(editorMenu, "Front Track Width", -CurrentPreset.DefaultNodes[0].PositionX, -CurrentPreset.Nodes[0].PositionX, vstancerEditor.Config.FrontLimits.PositionX, FrontOffsetID);
            AddDynamicFloatList(editorMenu, "Rear Track Width", -CurrentPreset.DefaultNodes[CurrentPreset.FrontWheelsCount].PositionX, -CurrentPreset.Nodes[CurrentPreset.FrontWheelsCount].PositionX, vstancerEditor.Config.RearLimits.PositionX, RearOffsetID);
            AddDynamicFloatList(editorMenu, "Front Camber", CurrentPreset.DefaultNodes[0].RotationY, CurrentPreset.Nodes[0].RotationY, vstancerEditor.Config.FrontLimits.RotationY, FrontRotationID);
            AddDynamicFloatList(editorMenu, "Rear Camber", CurrentPreset.DefaultNodes[CurrentPreset.FrontWheelsCount].RotationY, CurrentPreset.Nodes[CurrentPreset.FrontWheelsCount].RotationY, vstancerEditor.Config.RearLimits.RotationY, RearRotationID);
            editorMenu.AddMenuItem(new MenuItem("Reset", "Restores the default values") { ItemData = ResetID });

            // Create Personal Presets sub menu and bind item to a button
            var PersonalPresetsItem = new MenuItem("Personal Presets", "The vstancer presets saved by you.")
            {
                Label = "→→→"
            };
            editorMenu.AddMenuItem(PersonalPresetsItem);
            MenuController.BindMenuItem(editorMenu, personalPresetsMenu, PersonalPresetsItem);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        /// <param name="script">The script which owns this menu</param>
        internal VStancerMenu(VStancerEditor script)
        {
            vstancerEditor = script;
            vstancerEditor.PresetChanged += new EventHandler((sender,args) => UpdateEditorMenu());
            vstancerEditor.ToggleMenuVisibility += new EventHandler((sender,args) => 
            {
                if (editorMenu == null)
                    return;

                editorMenu.Visible = !editorMenu.Visible;
            });
            InitializeMenu();

            vstancerEditor.PersonalPresetsListChanged += new EventHandler((sender, args) => UpdatePersonalPresetsMenu());
        }

        #endregion

        public void HideUI()
        {
            if (MenuController.IsAnyMenuOpen())
                MenuController.CloseAllMenus();
        }
    }
}
