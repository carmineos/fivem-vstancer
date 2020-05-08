using System;
using System.Text;

using CitizenFX.Core;

namespace VStancer.Client.Data
{
    public class WheelData : IEquatable<WheelData>
    {
        private const float Epsilon = VStancerUtilities.Epsilon;

        public delegate void WheelDataPropertyEdited(string name, float value);
        public event WheelDataPropertyEdited PropertyChanged;

        private readonly WheelDataNode[] _nodes;
        private readonly WheelDataNode[] _defaultNodes;

        public int WheelsCount { get; private set; }
        public int FrontWheelsCount { get; private set; }

        public WheelDataNode[] GetNodes() => (WheelDataNode[])_nodes.Clone();

        public float FrontTrackWidth
        {
            get => _nodes[0].PositionX;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    _nodes[index].PositionX = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(FrontTrackWidth), value);
            }
        }

        public float RearTrackWidth
        {
            get => _nodes[FrontWheelsCount].PositionX;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    _nodes[index].PositionX = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(RearTrackWidth), value);
            }
        }

        public float FrontCamber
        {
            get => _nodes[0].RotationY;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    _nodes[index].RotationY = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(FrontCamber), value);
            }
        }

        public float RearCamber
        {
            get => _nodes[FrontWheelsCount].RotationY;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    _nodes[index].RotationY = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(RearCamber), value);
            }
        }

        public float DefaultFrontTrackWidth { get => _defaultNodes[0].PositionX; }
        public float DefaultRearTrackWidth { get => _defaultNodes[FrontWheelsCount].PositionX; }
        public float DefaultFrontCamber { get => _defaultNodes[0].RotationY; }
        public float DefaultRearCamber { get => _defaultNodes[FrontWheelsCount].RotationY; }

        public bool IsEdited
        {
            get
            {
                for (int i = 0; i < WheelsCount; i++)
                {
                    if (!MathUtil.WithinEpsilon(_defaultNodes[i].PositionX, _nodes[i].PositionX, Epsilon) ||
                        !MathUtil.WithinEpsilon(_defaultNodes[i].RotationY, _nodes[i].RotationY, Epsilon))
                        return true;
                }
                return false;
            }
        }

        public WheelData(int count, float defaultFrontOffset, float defaultFrontRotation, float defaultRearOffset, float defaultRearRotation)
        {
            WheelsCount = count;

            _defaultNodes = new WheelDataNode[WheelsCount];

            FrontWheelsCount = VStancerUtilities.CalculateFrontWheelsCount(WheelsCount);

            for (int i = 0; i < FrontWheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    _defaultNodes[i].RotationY = defaultFrontRotation;
                    _defaultNodes[i].PositionX = defaultFrontOffset;
                }
                else
                {
                    _defaultNodes[i].RotationY = -defaultFrontRotation;
                    _defaultNodes[i].PositionX = -defaultFrontOffset;
                }
            }

            for (int i = FrontWheelsCount; i < WheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    _defaultNodes[i].RotationY = defaultRearRotation;
                    _defaultNodes[i].PositionX = defaultRearOffset;
                }
                else
                {
                    _defaultNodes[i].RotationY = -defaultRearRotation;
                    _defaultNodes[i].PositionX = -defaultRearOffset;
                }
            }

            _nodes = new WheelDataNode[WheelsCount];
            for (int i = 0; i < WheelsCount; i++)
            {
                _nodes[i] = _defaultNodes[i];
            }
        }

        public void Reset()
        {
            for (int i = 0; i < WheelsCount; i++)
                _nodes[i] = _defaultNodes[i];

            PropertyChanged?.Invoke(nameof(Reset), default);
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
                defOff.Append(string.Format("{0,15}", _defaultNodes[i].PositionX));
                defRot.Append(string.Format("{0,15}", _defaultNodes[i].RotationY));
                curOff.Append(string.Format("{0,15}", _nodes[i].PositionX));
                curRot.Append(string.Format("{0,15}", _nodes[i].RotationY));
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
                if (!MathUtil.WithinEpsilon(_defaultNodes[i].PositionX, other._defaultNodes[i].PositionX, Epsilon) ||
                    !MathUtil.WithinEpsilon(_defaultNodes[i].RotationY, other._defaultNodes[i].RotationY, Epsilon) ||
                    !MathUtil.WithinEpsilon(_nodes[i].PositionX, other._nodes[i].PositionX, Epsilon) ||
                    !MathUtil.WithinEpsilon(_nodes[i].RotationY, other._nodes[i].RotationY, Epsilon))
                    return false;
            }
            return true;
        }
    }

    public struct WheelDataNode
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
