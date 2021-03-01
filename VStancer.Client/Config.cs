﻿namespace VStancer.Client
{
    public class Config
    {
        public bool Debug { get; set; }
        public bool DisableMenu { get; set; }
        public bool ExposeCommand { get; set; }
        public bool ExposeEvent { get; set; }
        public float ScriptRange { get; set; }
        public long Timer { get; set; }
        public int ToggleMenuControl { get; set; }
        public float FloatStep { get; set; }
        public bool EnableWheelMod { get; set; }
        public bool EnableSuspension { get; set; }
        public bool EnableClientPresets { get; set; }
        public bool EnableClientSettings { get; set; }
        public WheelLimits WheelLimits { get; set; }
        public WheelModLimits WheelModLimits { get; set; }
        public SuspensionLimits SuspensionLimits { get; set; }

        public Config()
        {
            Debug = false;
            DisableMenu = false;
            ExposeCommand = false;
            ExposeEvent = false;
            ScriptRange = 150.0f;
            Timer = 1000;
            ToggleMenuControl = 167;
            FloatStep = 0.01f;
            EnableWheelMod = true;
            EnableSuspension = true;
            EnableClientPresets = true;
            EnableClientSettings = true;
            
            WheelLimits = new WheelLimits 
            { 
                FrontTrackWidth= 0.25f,
                RearTrackWidth = 0.25f,
                FrontCamber = 0.20f, 
                RearCamber = 0.20f,
            };

            WheelModLimits = new WheelModLimits
            {
                WheelSize = 0.2f,
                WheelWidth = 0.2f,
                FrontTireColliderWidth = 0.1f,
                FrontTireColliderSize = 0.1f,
                FrontRimColliderSize = 0.1f,
                RearTireColliderWidth = 0.1f,
                RearTireColliderSize = 0.1f,
                RearRimColliderSize = 0.1f,
            };

            SuspensionLimits = new SuspensionLimits
            {
                SuspensionVisualHeight = 0.10f,
            };
        }
    }

    public struct WheelLimits
    {
        public float FrontTrackWidth { get; set; }
        public float RearTrackWidth { get; set; }
        public float FrontCamber { get; set; }
        public float RearCamber { get; set; }
    }

    public struct WheelModLimits
    {
        public float WheelSize { get; set; }
        public float WheelWidth { get; set; }
        public float FrontTireColliderWidth { get; set; }
        public float FrontTireColliderSize { get; set; }
        public float FrontRimColliderSize { get; set; }
        public float RearTireColliderWidth { get; set; }
        public float RearTireColliderSize { get; set; }
        public float RearRimColliderSize { get; set; }
    }

    public struct SuspensionLimits
    {
        public float SuspensionVisualHeight { get; set; }
    }
}