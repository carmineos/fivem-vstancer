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

        private WheelMenu WheelMenu { get; set; }
        private WheelModMenu WheelModMenu { get; set; }
        private ClientPresetsMenu ClientPresetsMenu { get; set; }

        private MenuItem WheelMenuMenuItem { get; set; }
        private MenuItem WheelModMenuMenuItem { get; set; }
        private MenuItem ClientPresetsMenuMenuItem { get; set; }


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

            if (_script.WheelScript != null)
                WheelMenu = _script.WheelScript.Menu;

            if (_script.WheelModScript != null)
            {
                WheelModMenu = _script.WheelModScript.Menu;
                WheelModMenu.PropertyChanged += (sender, args) => UpdateWheelModMenuMenuItem();
            }

            if (_script.ClientPresetsScript != null)
                ClientPresetsMenu = _script.ClientPresetsScript.Menu;

            Update();
        }

        internal void Update()
        {
            ClearMenuItems();

            MenuController.Menus.Clear();
            MenuController.AddMenu(this);

            if (WheelMenu != null)
            {
                WheelMenuMenuItem = new MenuItem("Wheel Menu", "The menu to edit main properties.")
                {
                    Label = "→→→"
                };

                AddMenuItem(WheelMenuMenuItem);

                MenuController.AddSubmenu(this, WheelMenu);
                MenuController.BindMenuItem(this, WheelMenu, WheelMenuMenuItem);
            }

            if (WheelModMenu != null)
            {
                WheelModMenuMenuItem = new MenuItem("Wheel Mod Menu")
                {
                    Label = "→→→"
                };
                UpdateWheelModMenuMenuItem();

                AddMenuItem(WheelModMenuMenuItem);

                MenuController.AddSubmenu(this, WheelModMenu);
                MenuController.BindMenuItem(this, WheelModMenu, WheelModMenuMenuItem);
            }

            if (ClientPresetsMenu != null)
            {
                ClientPresetsMenuMenuItem = new MenuItem("Client Presets Menu", "The menu to manage the presets saved by you.")
                {
                    Label = "→→→"
                };

                AddMenuItem(ClientPresetsMenuMenuItem);

                MenuController.AddSubmenu(this, ClientPresetsMenu);
                MenuController.BindMenuItem(this, ClientPresetsMenu, ClientPresetsMenuMenuItem);
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

        private void UpdateWheelModMenuMenuItem()
        {
            if (WheelModMenuMenuItem == null)
                return;

            var enabled = WheelModMenu != null ? WheelModMenu.Enabled : false;

            WheelModMenuMenuItem.Enabled = enabled;
            WheelModMenuMenuItem.RightIcon = enabled ? MenuItem.Icon.NONE : MenuItem.Icon.LOCK;
            WheelModMenuMenuItem.Label = enabled ? "→→→" : string.Empty;
            WheelModMenuMenuItem.Description = enabled ? "The menu to edit custom wheel properties." : "Install a custom wheel to access to this menu.";
        }
    }
}
