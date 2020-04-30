using System;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MenuAPI;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class MainMenu : Menu
    {
        private readonly MainScript _mainScript;

        private EditorMenu EditorMenu { get; set; }
        private ExtraMenu ExtraMenu { get; set; }
        private PresetsMenu PresetsMenu { get; set; }

        private MenuItem EditorMenuItem { get; set; }
        private MenuItem ExtraMenuItem { get; set; }
        private MenuItem PresetsMenuItem { get; set; }


        internal MainMenu(MainScript mainScript, string name = Globals.ScriptName, string subtitle = "Main Menu") : base(name, subtitle)
        {
            _mainScript = mainScript;

            _mainScript.ToggleMenuVisibility += new EventHandler((sender, args) =>
            {
                var currentMenu = MenuController.MainMenu;

                if (currentMenu == null)
                    return;

                currentMenu.Visible = !currentMenu.Visible;
            });

            _mainScript.VStancerExtraManager.VStancerExtraChanged += (sender, args) => UpdateExtraMenuItem();

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.MenuToggleKey = (Control)_mainScript.Config.ToggleMenuControl;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.DontOpenAnyMenu = true;
            MenuController.MainMenu = this;

            EditorMenu = _mainScript.VStancerDataManager.EditorMenu;
            ExtraMenu = _mainScript.VStancerExtraManager.ExtraMenu;
            PresetsMenu = _mainScript.LocalPresetsManager.PresetsMenu;

            Update();
        }

        internal void Update()
        {
            ClearMenuItems();

            EditorMenuItem = new MenuItem("Editor Menu", "The menu to edit main properties.")
            {
                Label = "→→→"
            };

            ExtraMenuItem = new MenuItem("Extra Menu")
            {
                Label = "→→→"
            };
            UpdateExtraMenuItem();
            
            
            PresetsMenuItem = new MenuItem("Personal Presets", "The menu to manage the presets saved by you.")
            {
                Label = "→→→"
            };

            AddMenuItem(EditorMenuItem);
            AddMenuItem(ExtraMenuItem);
            AddMenuItem(PresetsMenuItem);

            MenuController.Menus.Clear();
            MenuController.AddMenu(this);
            MenuController.AddSubmenu(this, EditorMenu);
            MenuController.AddSubmenu(this, ExtraMenu);
            MenuController.AddSubmenu(this, PresetsMenu);
            MenuController.BindMenuItem(this, EditorMenu, EditorMenuItem);
            MenuController.BindMenuItem(this, ExtraMenu, ExtraMenuItem);
            MenuController.BindMenuItem(this, PresetsMenu, PresetsMenuItem);
        }

        internal bool HideMenu
        {
            get => MenuController.DontOpenAnyMenu;
            set
            {
                MenuController.DontOpenAnyMenu = value;
            }
        }

        private void UpdateExtraMenuItem()
        {
            if (ExtraMenuItem == null)
                return;
        
            var enabled = _mainScript.VStancerExtraManager.ExtraIsValid;
        
            ExtraMenuItem.Enabled = enabled;
            ExtraMenuItem.RightIcon = enabled ? MenuItem.Icon.NONE : MenuItem.Icon.LOCK;
            ExtraMenuItem.Label = enabled ? "→→→" : string.Empty;
            ExtraMenuItem.Description = enabled ? "The menu to edit extra properties." : "Install a wheel mod to access to this menu";
        }
    }
}
