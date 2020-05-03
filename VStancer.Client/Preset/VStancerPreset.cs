using VStancer.Client.Data;

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
        public float FrontCamber { get; set; }
        public float RearTrackWidth { get; set; }
        public float RearCamber { get; set; }

        public WheelPreset()
        {

        }

        public WheelPreset(float frontTrackWidth, float frontCamber, float rearTrackWidth, float rearCamber)
        {
            FrontTrackWidth = frontTrackWidth;
            FrontCamber = frontCamber;
            RearTrackWidth = rearTrackWidth;
            RearCamber = rearCamber;
        }

        public WheelPreset(WheelData data)
        {
            if (data == null)
                return;

            FrontTrackWidth = data.FrontTrackWidth;
            FrontCamber = data.FrontCamber;
            RearTrackWidth = data.RearTrackWidth;
            RearCamber = data.RearCamber;
        }

        public float[] ToArray() { return new float[] { FrontTrackWidth, FrontCamber, RearTrackWidth, RearCamber }; }
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

        public WheelModPreset()
        {

        }

        public WheelModPreset(WheelModData data)
        {
            if (data == null)
                return;

            WheelSize = data.WheelSize;
            WheelWidth = data.WheelWidth;

            FrontTireColliderWidth = data.FrontTireColliderWidth;
            FrontTireColliderSize = data.FrontTireColliderSize;
            FrontRimColliderSize = data.FrontRimColliderSize;

            RearTireColliderWidth = data.RearTireColliderWidth;
            RearTireColliderSize = data.FrontTireColliderSize;
            RearRimColliderSize = data.RearRimColliderSize;
        }

    public float[] ToArray() { return new float[] { WheelSize, WheelWidth, FrontTireColliderWidth, FrontTireColliderSize, FrontRimColliderSize, RearTireColliderWidth, RearTireColliderSize, RearRimColliderSize }; }
    }
}
