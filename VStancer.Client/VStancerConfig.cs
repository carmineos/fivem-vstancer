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
        public NodeLimits FrontLimits { get; set; }
        public NodeLimits RearLimits { get; set; }
        public Extra Extra { get; set; }

        public VStancerConfig()
        {
            Debug = false;
            ExposeCommand = false;
            ExposeEvent = false;
            ScriptRange = 150.0f;
            Timer = 1000;
            ToggleMenuControl = 167;
            FloatStep = 0.01f;
            FrontLimits = new NodeLimits { PositionX = 0.25f, RotationY = 0.20f };
            RearLimits = new NodeLimits { PositionX = 0.25f, RotationY = 0.20f };
            
            Extra = new Extra
            {
                EnableExtra = true,
                WheelSize = 0.2f,
                WheelWidth = 0.2f,
                FrontWheelModSizeNodeLimit = new WheelModSizeNodeLimit
                {
                    TireColliderScaleX = 0.1f,
                    TireColliderScaleYZ = 0.1f,
                    RimColliderScaleYZ = 0.1f,
                },
                RearWheelModSizeNodeLimit = new WheelModSizeNodeLimit
                {
                    TireColliderScaleX = 0.1f,
                    TireColliderScaleYZ = 0.1f,
                    RimColliderScaleYZ = 0.1f,
                },
            };
        }
    }

    public struct NodeLimits
    {
        public float PositionX { get; set; }
        public float RotationY { get; set; }
    }

    public struct WheelModSizeNodeLimit
    {
        public float TireColliderScaleX { get; set; }
        public float TireColliderScaleYZ { get; set; }
        public float RimColliderScaleYZ { get; set; }
    }

    public struct Extra
    {
        public bool EnableExtra { get; set; }
        public float WheelSize { get; set; }
        public float WheelWidth { get; set; }
        public WheelModSizeNodeLimit FrontWheelModSizeNodeLimit { get; set; }
        public WheelModSizeNodeLimit RearWheelModSizeNodeLimit { get; set; }
    }
}
