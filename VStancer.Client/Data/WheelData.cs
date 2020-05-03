using System;
using System.Text;

using CitizenFX.Core;

namespace VStancer.Client.Data
{
    public class WheelData : IEquatable<WheelData>
    {
        private const float Epsilon = VStancerUtilities.Epsilon;

        public event EventHandler<string> PropertyChanged;

        public int WheelsCount { get; private set; }
        public int FrontWheelsCount { get; private set; }


        public DataNode[] Nodes { get; set; }
        public DataNode[] DefaultNodes { get; private set; }

        public float FrontTrackWidth
        {
            get => Nodes[0].PositionX;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    Nodes[index].PositionX = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(this, nameof(FrontTrackWidth));
            }
        }

        public float RearTrackWidth
        {
            get => Nodes[FrontWheelsCount].PositionX;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    Nodes[index].PositionX = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(this, nameof(RearTrackWidth));
            }
        }

        public float FrontCamber
        {
            get => Nodes[0].RotationY;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    Nodes[index].RotationY = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(this, nameof(FrontCamber));
            }
        }

        public float RearCamber
        {
            get => Nodes[FrontWheelsCount].RotationY;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    Nodes[index].RotationY = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(this, nameof(RearCamber));
            }
        }

        public float DefaultFrontTrackWidth { get => DefaultNodes[0].PositionX; }
        public float DefaultRearTrackWidth { get => DefaultNodes[FrontWheelsCount].PositionX; }
        public float DefaultFrontCamber { get => DefaultNodes[0].RotationY; }
        public float DefaultRearCamber { get => DefaultNodes[FrontWheelsCount].RotationY; }

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

        public WheelData(int count, float defaultFrontOffset, float defaultFrontRotation, float defaultRearOffset, float defaultRearRotation)
        {
            WheelsCount = count;

            DefaultNodes = new DataNode[WheelsCount];

            FrontWheelsCount = VStancerUtilities.CalculateFrontWheelsCount(WheelsCount);

            for (int i = 0; i < FrontWheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    DefaultNodes[i].RotationY = defaultFrontRotation;
                    DefaultNodes[i].PositionX = defaultFrontOffset;
                }
                else
                {
                    DefaultNodes[i].RotationY = -defaultFrontRotation;
                    DefaultNodes[i].PositionX = -defaultFrontOffset;
                }
            }

            for (int i = FrontWheelsCount; i < WheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    DefaultNodes[i].RotationY = defaultRearRotation;
                    DefaultNodes[i].PositionX = defaultRearOffset;
                }
                else
                {
                    DefaultNodes[i].RotationY = -defaultRearRotation;
                    DefaultNodes[i].PositionX = -defaultRearOffset;
                }
            }

            Nodes = new DataNode[WheelsCount];
            for (int i = 0; i < WheelsCount; i++)
            {
                Nodes[i] = DefaultNodes[i];
            }
        }

        public void Reset()
        {
            for (int i = 0; i < WheelsCount; i++)
                Nodes[i] = DefaultNodes[i];

            PropertyChanged?.Invoke(this, nameof(Reset));
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine($"Edited:{IsEdited} Wheels count:{WheelsCount} Front count:{FrontWheelsCount}");

            StringBuilder defOff = new StringBuilder(string.Format("{0,20}", "Default track width:"));
            StringBuilder defRot = new StringBuilder(string.Format("{0,20}", "Default camber:"));
            StringBuilder curOff = new StringBuilder(string.Format("{0,20}", "Current track width:"));
            StringBuilder curRot = new StringBuilder(string.Format("{0,20}", "Current camber:"));

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

        public bool Equals(WheelData other)
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
    }

    public struct DataNode
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
}
