namespace VStancer.Client.Preset
{
    public class VStancerPreset
    {
        public WheelPreset WheelPreset { get; set; }
        public WheelModPreset WheelModPreset { get; set; }
    }

    public class WheelPreset
    {
        public float FrontTrackWidth { get; set; }
        public float RearTrackWidth { get; set; }
        public float FrontCamber { get; set; }
        public float RearCamber { get; set; }
    }

    public class WheelModPreset
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
}
