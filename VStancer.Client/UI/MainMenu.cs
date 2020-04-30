using System;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MenuAPI;
using static VStancer.Client.UI.MenuUtilities;

namespace VStancer.Client.UI
{
    internal class MainMenu : Menu
    {
        private readonly VStancerEditor _vstancerEditor;

        private EditorMenu EditorMenu { get; set; }
        private ExtraMenu ExtraMenu { get; set; }
        private PresetsMenu PresetsMenu { get; set; }

        private MenuItem EditorMenuItem { get; set; }
        private MenuItem ExtraMenuItem { get; set; }
        private MenuItem PresetsMenuItem { get; set; }

        /// <summary>
        /// Invoked when a property has its value changed in the UI
        /// </summary>
        public event FloatPropertyChanged FloatPropertyChangedEvent;
        public event EventHandler<string> CommandInvokedEvent;

        public event EventHandler<string> PersonalPresetsMenuApplyPreset;
        public event EventHandler<string> PersonalPresetsMenuSavePreset;
        public event EventHandler<string> PersonalPresetsMenuDeletePreset;

        internal MainMenu(VStancerEditor vstancerEditor, string name = Globals.ScriptName, string subtitle = "Main Menu") : base(name, subtitle)
        {
            _vstancerEditor = vstancerEditor;

            _vstancerEditor.NewPresetCreated += new EventHandler((sender, args) => EditorMenu?.Update());

            _vstancerEditor.ExtraChanged += new EventHandler((sender, args) =>
            {
                ExtraMenu?.Update();
                UpdateExtraMenuItem();
            });

            _vstancerEditor.ToggleMenuVisibility += new EventHandler((sender, args) =>
            {
                //var currentMenu = MenuController.GetCurrentMenu();
                var currentMenu = MenuController.MainMenu;

                if (currentMenu == null)
                    return;

                currentMenu.Visible = !currentMenu.Visible;
            });

            _vstancerEditor.LocalPresetsManager.PresetsListChanged += new EventHandler((sender, args) => PresetsMenu?.Update());

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.MenuToggleKey = (Control)_vstancerEditor.Config.ToggleMenuControl;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.DontOpenAnyMenu = true;
            MenuController.MainMenu = this;

            EditorMenu = new EditorMenu(_vstancerEditor);
            ExtraMenu = new ExtraMenu(_vstancerEditor);
            PresetsMenu = new PresetsMenu(_vstancerEditor);

            EditorMenu.FloatPropertyChangedEvent += (id, value) => FloatPropertyChangedEvent?.Invoke(id, value);
            ExtraMenu.FloatPropertyChangedEvent += (id, value) => FloatPropertyChangedEvent?.Invoke(id, value);

            EditorMenu.ResetPropertiesEvent += (sender, id) => CommandInvokedEvent?.Invoke(this, id);
            ExtraMenu.ResetPropertiesEvent += (sender, id) => CommandInvokedEvent?.Invoke(this, id);

            PresetsMenu.ApplyPresetEvent += (sender, id) => PersonalPresetsMenuApplyPreset?.Invoke(this, id);
            PresetsMenu.SavePresetEvent += (sender, id) => PersonalPresetsMenuSavePreset?.Invoke(this, id);
            PresetsMenu.DeletePresetEvent += (sender, id) => PersonalPresetsMenuDeletePreset?.Invoke(this, id);

            Update();
        }

        internal void Update()
        {
            ClearMenuItems();

            EditorMenuItem = new MenuItem("Editor Menu", "The menu to edit main properties.")
            {
                Label = "→→→"
            };

            ExtraMenuItem = new MenuItem("Extra Menu", "The menu to edit extra properties.")
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

            var enabled = _vstancerEditor.CurrentExtraIsValid;

            ExtraMenuItem.Enabled = enabled;
            ExtraMenuItem.RightIcon = enabled ? MenuItem.Icon.NONE : MenuItem.Icon.LOCK;
            ExtraMenuItem.Label = enabled ? "→→→" : string.Empty;
            ExtraMenuItem.Description = enabled ? "The menu to edit the size of the wheel mods." : "Install a wheel mod to access to this menu";
        }
    }
}
