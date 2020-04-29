namespace VStancer.Client
{
    public class VStancerConfig
    {
        public bool Debug { get; set; }
        public bool ExposeCommand { get; set; }
        public bool ExposeEvent { get; set; }
        public float ScriptRange { get; set; }
        public long Timer { get; set; }
        public int ToggleMenuControl { get; set; }
        public float FloatStep { get; set; }
        public ConfigNodeLimits FrontLimits { get; set; }
        public ConfigNodeLimits RearLimits { get; set; }
        public ConfigExtra Extra { get; set; }

        public VStancerConfig()
        {
            Debug = false;
            ExposeCommand = false;
            ExposeEvent = false;
            ScriptRange = 150.0f;
            Timer = 1000;
            ToggleMenuControl = 167;
            FloatStep = 0.01f;
            FrontLimits = new ConfigNodeLimits { PositionX = 0.25f, RotationY = 0.20f };
            RearLimits = new ConfigNodeLimits { PositionX = 0.25f, RotationY = 0.20f };
            
            Extra = new ConfigExtra
            {
                EnableExtra = true,
                WheelSize = 0.2f,
                WheelWidth = 0.2f,
                FrontWheelModSizeNodeLimit = new ConfigWheelModSizeNodeLimit
                {
                    TireColliderScaleX = 0.1f,
                    TireColliderScaleYZ = 0.1f,
                    RimColliderScaleYZ = 0.1f,
                },
                RearWheelModSizeNodeLimit = new ConfigWheelModSizeNodeLimit
                {
                    TireColliderScaleX = 0.1f,
                    TireColliderScaleYZ = 0.1f,
                    RimColliderScaleYZ = 0.1f,
                },
            };
        }
    }

    public struct ConfigNodeLimits
    {
        public float PositionX { get; set; }
        public float RotationY { get; set; }
    }

    public struct ConfigWheelModSizeNodeLimit
    {
        public float TireColliderScaleX { get; set; }
        public float TireColliderScaleYZ { get; set; }
        public float RimColliderScaleYZ { get; set; }
    }

    public struct ConfigExtra
    {
        public bool EnableExtra { get; set; }
        public float WheelSize { get; set; }
        public float WheelWidth { get; set; }
        public ConfigWheelModSizeNodeLimit FrontWheelModSizeNodeLimit { get; set; }
        public ConfigWheelModSizeNodeLimit RearWheelModSizeNodeLimit { get; set; }
    }
}
