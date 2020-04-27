using CitizenFX.Core;
using System;
using System.Text;

namespace VStancer.Client
{
    public class VStancerPreset : IEquatable<VStancerPreset>
    {
        private const float Epsilon = 0.001f;

        public event EventHandler PresetEdited;

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

                PresetEdited?.Invoke(this, EventArgs.Empty);
            }
        }

        public float RearPositionX
        {
            get => Nodes[FrontWheelsCount].PositionX;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    Nodes[index].PositionX = (index % 2 == 0) ? value : -value;

                PresetEdited?.Invoke(this, EventArgs.Empty);
            }
        }

        public float FrontRotationY
        {
            get => Nodes[0].RotationY;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    Nodes[index].RotationY = (index % 2 == 0) ? value : -value;

                PresetEdited?.Invoke(this, EventArgs.Empty);
            }
        }

        public float RearRotationY
        {
            get => Nodes[FrontWheelsCount].RotationY;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    Nodes[index].RotationY = (index % 2 == 0) ? value : -value;

                PresetEdited?.Invoke(this, EventArgs.Empty);
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
                for (int index = 0; index < WheelsCount; index++)
                {
                    if (!MathUtil.WithinEpsilon(DefaultNodes[index].PositionX, Nodes[index].PositionX, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[index].RotationY, Nodes[index].RotationY, Epsilon))
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

            FrontWheelsCount = CalculateFrontWheelsCount(WheelsCount);

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
                    Nodes[index].PositionX = - frontOffset;
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
            for (int index = 0; index < WheelsCount; index++)
            {
                Nodes[index] = DefaultNodes[index];
            }

            PresetEdited?.Invoke(this, EventArgs.Empty);
        }

        public bool Equals(VStancerPreset other)
        {
            if (WheelsCount != other.WheelsCount)
                return false;

            for (int index = 0; index < WheelsCount; index++)
            {
                if (!MathUtil.WithinEpsilon(DefaultNodes[index].PositionX, other.DefaultNodes[index].PositionX, Epsilon) ||
                    !MathUtil.WithinEpsilon(DefaultNodes[index].RotationY, other.DefaultNodes[index].RotationY, Epsilon) ||
                    !MathUtil.WithinEpsilon(Nodes[index].PositionX, other.Nodes[index].PositionX, Epsilon) ||
                    !MathUtil.WithinEpsilon(Nodes[index].RotationY, other.Nodes[index].RotationY, Epsilon))
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
    }


    // TODO: Edit Preset to use nodes structs
    public struct VStancerNode
    {
        //public Vector3 Position { get; set; }
        //public Vector3 Rotation { get; set; }
        //public Vector3 Scale { get; set; }
        public float PositionX { get; set; }
        public float RotationY { get; set; }
    }
}
