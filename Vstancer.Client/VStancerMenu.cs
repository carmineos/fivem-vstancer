using System;
using System.Threading.Tasks;
using MenuAPI;
using CitizenFX.Core;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace Vstancer.Client
{
    internal class VStancerMenu : BaseScript
    {
        #region Private Fields

        private VStancerEditor _vstancerEditor;

        private MenuController menuController;
        private Menu editorMenu;

        #endregion

        #region Public Events

        public delegate void MenuPresetValueChangedEvent(string id, string value);

        public event MenuPresetValueChangedEvent MenuPresetValueChanged;
        public event EventHandler MenuResetPresetButtonPressed;

        #endregion

        #region Editor Properties

        private string ResetID => VStancerEditor.ResetID;
        private string FrontOffsetID => VStancerEditor.FrontOffsetID;
        private string FrontRotationID => VStancerEditor.FrontRotationID;
        private string RearOffsetID => VStancerEditor.RearOffsetID;
        private string RearRotationID => VStancerEditor.RearRotationID;
        private string ScriptName => VStancerEditor.ScriptName;
        private float frontMaxOffset => _vstancerEditor.frontMaxOffset;
        private float frontMaxCamber => _vstancerEditor.frontMaxCamber;
        private float rearMaxOffset => _vstancerEditor.rearMaxOffset;
        private float rearMaxCamber => _vstancerEditor.rearMaxCamber;
        private bool CurrentPresetIsValid => _vstancerEditor.CurrentPresetIsValid;
        private VStancerPreset currentPreset => _vstancerEditor.currentPreset;
        private int toggleMenu => _vstancerEditor.toggleMenu;
        private float FloatStep => _vstancerEditor.FloatStep;

        #endregion

        #region Private Methods

        private MenuDynamicListItem.ChangeItemCallback FloatChangeCallback(string name, float defaultValue, float value, float maxEditing)
        {
            string callback(MenuDynamicListItem sender, bool left)
            {
                var newvalue = value;
                float min = defaultValue - maxEditing;
                float max = defaultValue + maxEditing;

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

        private MenuDynamicListItem AddDynamicFloatList(Menu menu, string name, float defaultValue, float value, float maxEditing, string id)
        {
            var callback = FloatChangeCallback(name, defaultValue, value, maxEditing);

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
                editorMenu = new Menu(ScriptName, "Editor");

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

            UpdateEditorMenu();

            if (menuController == null)
            {
                menuController = new MenuController();
                MenuController.AddMenu(editorMenu);
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
                MenuController.MenuToggleKey = (Control)toggleMenu;
                MenuController.EnableMenuToggleKeyOnController = false;
                MenuController.MainMenu = editorMenu;
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

            if (!CurrentPresetIsValid)
                return;

            AddDynamicFloatList(editorMenu, "Front Track Width", -currentPreset.DefaultOffsetX[0], -currentPreset.OffsetX[0], frontMaxOffset, FrontOffsetID);
            AddDynamicFloatList(editorMenu, "Rear Track Width", -currentPreset.DefaultOffsetX[currentPreset.FrontWheelsCount], -currentPreset.OffsetX[currentPreset.FrontWheelsCount], rearMaxOffset, RearOffsetID);
            AddDynamicFloatList(editorMenu, "Front Camber", currentPreset.DefaultRotationY[0], currentPreset.RotationY[0], frontMaxCamber, FrontRotationID);
            AddDynamicFloatList(editorMenu, "Rear Camber", currentPreset.DefaultRotationY[currentPreset.FrontWheelsCount], currentPreset.RotationY[currentPreset.FrontWheelsCount], rearMaxCamber, RearRotationID);
            editorMenu.AddMenuItem(new MenuItem("Reset", "Restores the default values") { ItemData = ResetID });
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor
        /// </summary>
        public VStancerMenu()
        {

        }

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        /// <param name="script">The script which owns this menu</param>
        public VStancerMenu(VStancerEditor script)
        {
            _vstancerEditor = script;
            _vstancerEditor.PresetChanged += new EventHandler((sender,args) => UpdateEditorMenu());
            InitializeMenu();

            Tick += OnTick;
        }

        #endregion

        #region Tasks

        /// <summary>
        /// The task that checks if the menu can be open
        /// </summary>
        /// <returns></returns>
        private async Task OnTick()
        {
            if (!CurrentPresetIsValid)
            {
                if (MenuController.IsAnyMenuOpen())
                    MenuController.CloseAllMenus();
            }

            await Task.FromResult(0);
        }

        #endregion
    }
}
