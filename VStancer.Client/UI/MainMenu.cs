using System;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using static VStancer.Client.UI.MenuUtilities;
using MenuAPI;
using VStancer.Client.Scripts;

namespace VStancer.Client.UI
{
    internal class MainMenu : Menu
    {
        private readonly MainScript _script;

        private DataMenu DataMenu { get; set; }
        private ExtraMenu ExtraMenu { get; set; }
        private PresetsMenu PresetsMenu { get; set; }

        private MenuItem EditorMenuItem { get; set; }
        private MenuItem ExtraMenuItem { get; set; }
        private MenuItem PresetsMenuItem { get; set; }


        internal MainMenu(MainScript script, string name = Globals.ScriptName, string subtitle = "Main Menu") : base(name, subtitle)
        {
            _script = script;

            _script.ToggleMenuVisibility += new EventHandler((sender, args) =>
            {
                var currentMenu = MenuController.MainMenu;

                if (currentMenu == null)
                    return;

                currentMenu.Visible = !currentMenu.Visible;
            });

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.MenuToggleKey = (Control)_script.Config.ToggleMenuControl;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.DontOpenAnyMenu = true;
            MenuController.MainMenu = this;

            if (_script.VStancerDataScript != null)
                DataMenu = _script.VStancerDataScript.Menu;

            if (_script.VStancerExtraScript != null)
            {
                _script.VStancerExtraScript.VStancerExtraChanged += (sender, args) => UpdateExtraMenuItem();
                ExtraMenu = _script.VStancerExtraScript.Menu;
            }

            if (_script.LocalPresetScript != null)
                PresetsMenu = _script.LocalPresetScript.Menu;

            Update();
        }

        internal void Update()
        {
            ClearMenuItems();

            MenuController.Menus.Clear();
            MenuController.AddMenu(this);

            if (DataMenu != null)
            {
                EditorMenuItem = new MenuItem("Editor Menu", "The menu to edit main properties.")
                {
                    Label = "→→→"
                };

                AddMenuItem(EditorMenuItem);

                MenuController.AddSubmenu(this, DataMenu);
                MenuController.BindMenuItem(this, DataMenu, EditorMenuItem);
            }

            if (ExtraMenu != null)
            {
                ExtraMenuItem = new MenuItem("Extra Menu")
                {
                    Label = "→→→"
                };
                UpdateExtraMenuItem();

                AddMenuItem(ExtraMenuItem);

                MenuController.AddSubmenu(this, ExtraMenu);
                MenuController.BindMenuItem(this, ExtraMenu, ExtraMenuItem);
            }

            if (PresetsMenu != null)
            {
                PresetsMenuItem = new MenuItem("Personal Presets", "The menu to manage the presets saved by you.")
                {
                    Label = "→→→"
                };

                AddMenuItem(PresetsMenuItem);

                MenuController.AddSubmenu(this, PresetsMenu);
                MenuController.BindMenuItem(this, PresetsMenu, PresetsMenuItem);
            }
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

            var enabled = false;

            if (_script.VStancerExtraScript != null)
                enabled = _script.VStancerExtraScript.ExtraIsValid;

            ExtraMenuItem.Enabled = enabled;
            ExtraMenuItem.RightIcon = enabled ? MenuItem.Icon.NONE : MenuItem.Icon.LOCK;
            ExtraMenuItem.Label = enabled ? "→→→" : string.Empty;
            ExtraMenuItem.Description = enabled ? "The menu to edit extra properties." : "Install a wheel mod to access to this menu";
        }
    }
}
