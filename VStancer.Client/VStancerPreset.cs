using CitizenFX.Core;
using System;
using System.Text;

namespace VStancer.Client
{
    public class VStancerPreset : IEquatable<VStancerPreset>
    {
        private const float Epsilon = VStancerPresetUtilities.Epsilon;

        public event EventHandler<string> PresetEdited;

        public int WheelsCount { get; set; }
        public int FrontWheelsCount { get; set; }


        public VStancerNode[] Nodes { get; set; }
        public VStancerNode[] DefaultNodes { get; private set; }

        public float FrontPositionX
        {
            get => Nodes[0].PositionX;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    Nodes[index].PositionX = (index % 2 == 0) ? value : -value;

                PresetEdited?.Invoke(this, nameof(FrontPositionX));
            }
        }

        public float RearPositionX
        {
            get => Nodes[FrontWheelsCount].PositionX;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    Nodes[index].PositionX = (index % 2 == 0) ? value : -value;

                PresetEdited?.Invoke(this, nameof(RearPositionX));
            }
        }

        public float FrontRotationY
        {
            get => Nodes[0].RotationY;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    Nodes[index].RotationY = (index % 2 == 0) ? value : -value;

                PresetEdited?.Invoke(this, nameof(FrontRotationY));
            }
        }

        public float RearRotationY
        {
            get => Nodes[FrontWheelsCount].RotationY;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    Nodes[index].RotationY = (index % 2 == 0) ? value : -value;

                PresetEdited?.Invoke(this, nameof(RearRotationY));
            }
        }

        public float DefaultFrontPositionX { get => DefaultNodes[0].PositionX; }
        public float DefaultRearPositionX { get => DefaultNodes[FrontWheelsCount].PositionX; }
        public float DefaultFrontRotationY { get => DefaultNodes[0].RotationY; }
        public float DefaultRearRotationY { get => DefaultNodes[FrontWheelsCount].RotationY; }

        public bool IsEdited
        {
            get
            {
                for (int i = 0; i < WheelsCount; i++)
                {
                    if (!MathUtil.WithinEpsilon(DefaultNodes[i].PositionX, Nodes[i].PositionX, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[i].RotationY, Nodes[i].RotationY, Epsilon))
                        return true;
                }
                return false;
            }
        }

        public VStancerPreset(int count, float frontOffset, float frontRotation, float rearOffset, float rearRotation, float defaultFrontOffset, float defaultFrontRotation, float defaultRearOffset, float defaultRearRotation)
        {
            WheelsCount = count;

            DefaultNodes = new VStancerNode[WheelsCount];
            Nodes = new VStancerNode[WheelsCount];

            FrontWheelsCount = VStancerPresetUtilities.CalculateFrontWheelsCount(WheelsCount);

            for (int index = 0; index < FrontWheelsCount; index++)
            {
                if (index % 2 == 0)
                {
                    DefaultNodes[index].RotationY = defaultFrontRotation;
                    DefaultNodes[index].PositionX = defaultFrontOffset;
                    Nodes[index].RotationY = frontRotation;
                    Nodes[index].PositionX = frontOffset;
                }
                else
                {
                    DefaultNodes[index].RotationY = -defaultFrontRotation;
                    DefaultNodes[index].PositionX = -defaultFrontOffset;
                    Nodes[index].RotationY = -frontRotation;
                    Nodes[index].PositionX = -frontOffset;
                }
            }

            for (int index = FrontWheelsCount; index < WheelsCount; index++)
            {
                if (index % 2 == 0)
                {
                    DefaultNodes[index].RotationY = defaultRearRotation;
                    DefaultNodes[index].PositionX = defaultRearOffset;
                    Nodes[index].RotationY = rearRotation;
                    Nodes[index].PositionX = rearOffset;
                }
                else
                {
                    DefaultNodes[index].RotationY = -defaultRearRotation;
                    DefaultNodes[index].PositionX = -defaultRearOffset;
                    Nodes[index].RotationY = -rearRotation;
                    Nodes[index].PositionX = -rearOffset;
                }
            }
        }

        public void Reset()
        {
            for (int i = 0; i < WheelsCount; i++)
                Nodes[i] = DefaultNodes[i];

            PresetEdited?.Invoke(this, "Reset");
        }

        public bool Equals(VStancerPreset other)
        {
            if (WheelsCount != other.WheelsCount)
                return false;

            for (int i = 0; i < WheelsCount; i++)
            {
                if (!MathUtil.WithinEpsilon(DefaultNodes[i].PositionX, other.DefaultNodes[i].PositionX, Epsilon) ||
                    !MathUtil.WithinEpsilon(DefaultNodes[i].RotationY, other.DefaultNodes[i].RotationY, Epsilon) ||
                    !MathUtil.WithinEpsilon(Nodes[i].PositionX, other.Nodes[i].PositionX, Epsilon) ||
                    !MathUtil.WithinEpsilon(Nodes[i].RotationY, other.Nodes[i].RotationY, Epsilon))
                    return false;
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine($"Edited:{IsEdited} Wheels count:{WheelsCount} Front count:{FrontWheelsCount}");

            StringBuilder defOff = new StringBuilder(string.Format("{0,20}", "Default offset:"));
            StringBuilder defRot = new StringBuilder(string.Format("{0,20}", "Default rotation:"));
            StringBuilder curOff = new StringBuilder(string.Format("{0,20}", "Current offset:"));
            StringBuilder curRot = new StringBuilder(string.Format("{0,20}", "Current rotation:"));

            for (int i = 0; i < WheelsCount; i++)
            {
                defOff.Append(string.Format("{0,15}", DefaultNodes[i].PositionX));
                defRot.Append(string.Format("{0,15}", DefaultNodes[i].RotationY));
                curOff.Append(string.Format("{0,15}", Nodes[i].PositionX));
                curRot.Append(string.Format("{0,15}", Nodes[i].RotationY));
            }

            s.AppendLine(curOff.ToString());
            s.AppendLine(defOff.ToString());
            s.AppendLine(curRot.ToString());
            s.AppendLine(defRot.ToString());

            return s.ToString();
        }

        /// <summary>
        /// Returns the preset as an array of floats containing in order: 
        /// frontOffset, frontRotation, rearOffset, rearRotation, defaultFrontOffset, defaultFrontRotation, defaultRearOffset, defaultRearRotation
        /// </summary>
        /// <returns>The float array</returns>
        public float[] ToArray()
        {
            return new float[] {
                Nodes[0].PositionX,
                Nodes[0].RotationY,
                Nodes[FrontWheelsCount].PositionX,
                Nodes[FrontWheelsCount].RotationY,
                DefaultNodes[0].PositionX,
                DefaultNodes[0].RotationY,
                DefaultNodes[FrontWheelsCount].PositionX,
                DefaultNodes[FrontWheelsCount].RotationY,
            };
        }

        public void CopyFrom(VStancerPreset other)
        {
            if (other == null)
                return;

            FrontPositionX = other.FrontPositionX;
            FrontRotationY = other.FrontRotationY;
            RearPositionX = other.RearPositionX;
            RearRotationY = other.RearRotationY;
        }

        /// <summary>
        /// The size of the modded wheels in case they are installed
        /// </summary>
        public VStancerWheelModSize WheelModSize { get; set; } = null;
    }


    // TODO: Edit Preset to use nodes structs
    public struct VStancerNode
    {
        /// <summary>
        /// The track width of the wheel
        /// </summary>
        public float PositionX { get; set; }

        /// <summary>
        /// The camber of the wheel
        /// </summary>
        public float RotationY { get; set; }
    }




















    public class VStancerWheelModSize
    {
        private const float Epsilon = VStancerPresetUtilities.Epsilon;
        private readonly int _wheelsCount;
        private readonly int _frontWheelsCount;

        public event EventHandler<string> WheelModSizeEdited;

        public VStancerWheelModSizeNode[] Nodes { get; set; }
        public VStancerWheelModSizeNode[] DefaultNodes { get; private set; }

        public VStancerWheelModSize(int wheelsCount, 
            int frontScaleX, int frontScaleYZ, int frontTireColliderScaleX, int frontTireColliderScaleYZ, int frontRimColliderScaleYZ,
            int rearScaleX, int rearScaleYZ, int rearTireColliderScaleX, int rearTireColliderScaleYZ, int rearRimColliderScaleYZ)
        {
            _wheelsCount = wheelsCount;
            _frontWheelsCount = VStancerPresetUtilities.CalculateFrontWheelsCount(_wheelsCount);


            DefaultNodes = new VStancerWheelModSizeNode[_wheelsCount];

            for (int i = 0; i < _frontWheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    DefaultNodes[i].ScaleX = frontScaleX;
                    DefaultNodes[i].ScaleYZ = frontScaleYZ;
                    DefaultNodes[i].TireColliderScaleX = frontTireColliderScaleX;
                    DefaultNodes[i].TireColliderScaleYZ = frontTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderScaleYZ = frontRimColliderScaleYZ;
                }
                else
                {
                    DefaultNodes[i].ScaleX = -frontScaleX;
                    DefaultNodes[i].ScaleYZ = -frontScaleYZ;
                    DefaultNodes[i].TireColliderScaleX = -frontTireColliderScaleX;
                    DefaultNodes[i].TireColliderScaleYZ = -frontTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderScaleYZ = -frontRimColliderScaleYZ;
                }
            }

            for (int i = _frontWheelsCount; i < _wheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    DefaultNodes[i].ScaleX = rearScaleX;
                    DefaultNodes[i].ScaleYZ = rearScaleYZ;
                    DefaultNodes[i].TireColliderScaleX = rearTireColliderScaleX;
                    DefaultNodes[i].TireColliderScaleYZ = rearTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderScaleYZ = rearRimColliderScaleYZ;
                }
                else
                {
                    DefaultNodes[i].ScaleX = -rearScaleX;
                    DefaultNodes[i].ScaleYZ = -rearScaleYZ;
                    DefaultNodes[i].TireColliderScaleX = -rearTireColliderScaleX;
                    DefaultNodes[i].TireColliderScaleYZ = -rearTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderScaleYZ = -rearRimColliderScaleYZ;
                }
            }

            Nodes = new VStancerWheelModSizeNode[_wheelsCount];
            for (int i = 0; i < _wheelsCount; i++)
                Nodes[i] = DefaultNodes[i];
        }

        public float FrontScaleX
        {
            get => Nodes[0].ScaleX;
            set
            {
                for (int index = 0; index < _frontWheelsCount; index++)
                    Nodes[index].ScaleX = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(FrontScaleX));
            }
        }
        public float FrontScaleYZ
        {
            get => Nodes[0].ScaleYZ;
            set
            {
                for (int index = 0; index < _frontWheelsCount; index++)
                    Nodes[index].ScaleYZ = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(FrontScaleYZ));
            }
        }

        public float FrontTireColliderScaleX
        {
            get => Nodes[0].TireColliderScaleX;
            set
            {
                for (int index = 0; index < _frontWheelsCount; index++)
                    Nodes[index].TireColliderScaleX = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(FrontTireColliderScaleX));
            }
        }

        public float FrontTireColliderScaleYZ
        {
            get => Nodes[0].TireColliderScaleYZ;
            set
            {
                for (int index = 0; index < _frontWheelsCount; index++)
                    Nodes[index].TireColliderScaleYZ = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(FrontTireColliderScaleYZ));
            }
        }

        public float FrontRimColliderScaleYZ
        {
            get => Nodes[0].RimColliderScaleYZ;
            set
            {
                for (int index = 0; index < _frontWheelsCount; index++)
                    Nodes[index].RimColliderScaleYZ = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(FrontRimColliderScaleYZ));
            }
        }

        public float RearScaleX
        {
            get => Nodes[_frontWheelsCount].ScaleX;
            set
            {
                for (int index = _frontWheelsCount; index < _wheelsCount; index++)
                    Nodes[index].ScaleX = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(RearScaleX));
            }
        }

        public float RearScaleYZ
        {
            get => Nodes[_frontWheelsCount].ScaleYZ;
            set
            {
                for (int index = _frontWheelsCount; index < _wheelsCount; index++)
                    Nodes[index].ScaleYZ = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(RearScaleYZ));
            }
        }

        public float RearTireColliderScaleX
        {
            get => Nodes[_frontWheelsCount].TireColliderScaleX;
            set
            {
                for (int index = _frontWheelsCount; index < _wheelsCount; index++)
                    Nodes[index].TireColliderScaleX = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(RearTireColliderScaleX));
            }
        }

        public float RearTireColliderScaleYZ
        {
            get => Nodes[_frontWheelsCount].TireColliderScaleYZ;
            set
            {
                for (int index = _frontWheelsCount; index < _wheelsCount; index++)
                    Nodes[index].TireColliderScaleYZ = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(RearTireColliderScaleYZ));
            }
        }

        public float RearRimColliderScaleYZ
        {
            get => Nodes[_frontWheelsCount].RimColliderScaleYZ;
            set
            {
                for (int index = _frontWheelsCount; index < _wheelsCount; index++)
                    Nodes[index].RimColliderScaleYZ = (index % 2 == 0) ? value : -value;

                WheelModSizeEdited?.Invoke(this, nameof(RearRimColliderScaleYZ));
            }
        }

        public float DefaultFrontScaleX => DefaultNodes[0].ScaleX;
        public float DefaultFrontScaleYZ => DefaultNodes[0].ScaleYZ;
        public float DefaultFrontTireColliderScaleX => DefaultNodes[0].TireColliderScaleX;
        public float DefaultFrontTireColliderScaleYZ => DefaultNodes[0].TireColliderScaleYZ;
        public float DefaultFrontRimColliderScaleYZ => DefaultNodes[0].RimColliderScaleYZ;

        public float DefaultRearScaleX => DefaultNodes[_frontWheelsCount].ScaleX;
        public float DefaultRearScaleYZ => DefaultNodes[_frontWheelsCount].ScaleYZ;
        public float DefaultRearTireColliderScaleX => DefaultNodes[_frontWheelsCount].TireColliderScaleX;
        public float DefaultRearTireColliderScaleYZ => DefaultNodes[_frontWheelsCount].TireColliderScaleYZ;
        public float DefaultRearRimColliderScaleYZ => DefaultNodes[_frontWheelsCount].RimColliderScaleYZ;

        public void Reset()
        {
            for (int i = 0; i < _wheelsCount; i++)
                Nodes[i] = DefaultNodes[i];

            WheelModSizeEdited?.Invoke(this, "Reset");
        }

        public bool IsEdited
        {
            get
            {
                for (int i = 0; i < _wheelsCount; i++)
                {
                    if (!MathUtil.WithinEpsilon(DefaultNodes[i].ScaleX, Nodes[i].ScaleX, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[i].ScaleYZ, Nodes[i].ScaleYZ, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[i].TireColliderScaleX, Nodes[i].TireColliderScaleX, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[i].TireColliderScaleYZ, Nodes[i].TireColliderScaleYZ, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[i].RimColliderScaleYZ, Nodes[i].RimColliderScaleYZ, Epsilon))
                        return true;
                }
                return false;
            }
        }
    }








    public struct VStancerWheelModSizeNode
    {
        /// <summary>
        /// The wheel thread size
        /// </summary>
        public float ScaleX { get; set; }

        /// <summary>
        /// The wheel radius
        /// </summary>
        public float ScaleYZ { get; set; }

        /// <summary>
        /// The collider wheel thread size
        /// </summary>
        public float TireColliderScaleX { get; set; }

        /// <summary>
        /// The collider wheel radius
        /// </summary>
        public float TireColliderScaleYZ { get; set; }

        /// <summary>
        /// The collider wheel radius
        /// </summary>
        public float RimColliderScaleYZ { get; set; }
    }

    public static class VStancerPresetUtilities
    {
        public const float Epsilon = 0.001f;

        /// <summary>
        /// Calculate the number of front wheels of a vehicle, starting from the number of all the wheels
        /// </summary>
        /// <param name="wheelsCount">The number of wheels of a such vehicle</param>
        /// <returns></returns>
        public static int CalculateFrontWheelsCount(int wheelsCount)
        {
            int _frontWheelsCount = wheelsCount / 2;

            if (_frontWheelsCount % 2 != 0)
                _frontWheelsCount -= 1;

            return _frontWheelsCount;
        }
    }
}
