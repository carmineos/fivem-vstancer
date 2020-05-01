using System.Text;

using CitizenFX.Core;

namespace VStancer.Client.Data
{
    public class VStancerExtra
    {
        private const float Epsilon = VStancerUtilities.Epsilon;

        private readonly int _wheelsCount;
        private readonly int _frontWheelsCount;

        private float wheelSize;
        private float wheelWidth;

        public delegate void WheelModSizePropertyEdited(string name, float value);
        public event WheelModSizePropertyEdited PropertyChanged;

        public float WheelSize
        {
            get => wheelSize;
            set
            {
                if (value == wheelSize)
                    return;

                wheelSize = value;
                PropertyChanged?.Invoke(nameof(WheelSize), value);
            }
        }

        public float WheelWidth
        {
            get => wheelWidth;
            set
            {
                if (value == wheelWidth)
                    return;

                wheelWidth = value;
                PropertyChanged?.Invoke(nameof(WheelWidth), value);
            }
        }

        public float DefaultWheelSize { get; private set; }
        public float DefaultWheelWidth { get; private set; }

        public VStancerExtraNode[] Nodes { get; set; }
        public VStancerExtraNode[] DefaultNodes { get; private set; }

        public VStancerExtra(int wheelsCount, float width, float radius,
            float frontTireColliderScaleX, float frontTireColliderScaleYZ, float frontRimColliderScaleYZ,
            float rearTireColliderScaleX, float rearTireColliderScaleYZ, float rearRimColliderScaleYZ)
        {
            _wheelsCount = wheelsCount;
            _frontWheelsCount = VStancerUtilities.CalculateFrontWheelsCount(_wheelsCount);

            DefaultWheelSize = radius;
            DefaultWheelWidth = width;

            wheelSize = radius;
            wheelWidth = width;

            DefaultNodes = new VStancerExtraNode[_wheelsCount];

            for (int i = 0; i < _frontWheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    DefaultNodes[i].TireColliderScaleX = frontTireColliderScaleX;
                    DefaultNodes[i].TireColliderScaleYZ = frontTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderScaleYZ = frontRimColliderScaleYZ;
                }
                else
                {
                    DefaultNodes[i].TireColliderScaleX = -frontTireColliderScaleX;
                    DefaultNodes[i].TireColliderScaleYZ = -frontTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderScaleYZ = -frontRimColliderScaleYZ;
                }
            }

            for (int i = _frontWheelsCount; i < _wheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    DefaultNodes[i].TireColliderScaleX = rearTireColliderScaleX;
                    DefaultNodes[i].TireColliderScaleYZ = rearTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderScaleYZ = rearRimColliderScaleYZ;
                }
                else
                {
                    DefaultNodes[i].TireColliderScaleX = -rearTireColliderScaleX;
                    DefaultNodes[i].TireColliderScaleYZ = -rearTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderScaleYZ = -rearRimColliderScaleYZ;
                }
            }

            Nodes = new VStancerExtraNode[_wheelsCount];
            for (int i = 0; i < _wheelsCount; i++)
                Nodes[i] = DefaultNodes[i];
        }

        
        public float FrontTireColliderWidth
        {
            get => Nodes[0].TireColliderScaleX;
            set
            {
                for (int index = 0; index < _frontWheelsCount; index++)
                    Nodes[index].TireColliderScaleX = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(FrontTireColliderWidth), value);
            }
        }

        
        public float FrontTireColliderSize
        {
            get => Nodes[0].TireColliderScaleYZ;
            set
            {
                for (int index = 0; index < _frontWheelsCount; index++)
                    Nodes[index].TireColliderScaleYZ = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(FrontTireColliderSize), value);
            }
        }

        
        public float FrontRimColliderSize
        {
            get => Nodes[0].RimColliderScaleYZ;
            set
            {
                for (int index = 0; index < _frontWheelsCount; index++)
                    Nodes[index].RimColliderScaleYZ = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(FrontRimColliderSize), value);
            }
        }

        
        public float RearTireColliderWidth
        {
            get => Nodes[_frontWheelsCount].TireColliderScaleX;
            set
            {
                for (int index = _frontWheelsCount; index < _wheelsCount; index++)
                    Nodes[index].TireColliderScaleX = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(RearTireColliderWidth), value);
            }
        }

        
        public float RearTireColliderSize
        {
            get => Nodes[_frontWheelsCount].TireColliderScaleYZ;
            set
            {
                for (int index = _frontWheelsCount; index < _wheelsCount; index++)
                    Nodes[index].TireColliderScaleYZ = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(RearTireColliderSize), value);
            }
        }

        public float RearRimColliderSize
        {
            get => Nodes[_frontWheelsCount].RimColliderScaleYZ;
            set
            {
                for (int index = _frontWheelsCount; index < _wheelsCount; index++)
                    Nodes[index].RimColliderScaleYZ = (index % 2 == 0) ? value : -value;

                PropertyChanged?.Invoke(nameof(RearRimColliderSize), value);
            }
        }

        public float DefaultFrontTireColliderWidth => DefaultNodes[0].TireColliderScaleX;
        public float DefaultFrontTireColliderSize => DefaultNodes[0].TireColliderScaleYZ;
        public float DefaultFrontRimColliderSize => DefaultNodes[0].RimColliderScaleYZ;

        public float DefaultRearTireColliderWidth => DefaultNodes[_frontWheelsCount].TireColliderScaleX;
        public float DefaultRearTireColliderSize => DefaultNodes[_frontWheelsCount].TireColliderScaleYZ;
        public float DefaultRearRimColliderSize => DefaultNodes[_frontWheelsCount].RimColliderScaleYZ;

        public void Reset()
        {
            WheelSize = DefaultWheelSize;
            WheelWidth = DefaultWheelWidth;

            for (int i = 0; i < _wheelsCount; i++)
                Nodes[i] = DefaultNodes[i];

            PropertyChanged?.Invoke(nameof(Reset), 0f);
        }

        public bool IsEdited
        {
            get
            {
                if (!MathUtil.WithinEpsilon(DefaultWheelSize, WheelSize, Epsilon) ||
                    !MathUtil.WithinEpsilon(DefaultWheelWidth, WheelWidth, Epsilon))
                    return true;

                for (int i = 0; i < _wheelsCount; i++)
                {
                    if (!MathUtil.WithinEpsilon(DefaultNodes[i].TireColliderScaleX, Nodes[i].TireColliderScaleX, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[i].TireColliderScaleYZ, Nodes[i].TireColliderScaleYZ, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[i].RimColliderScaleYZ, Nodes[i].RimColliderScaleYZ, Epsilon))
                        return true;
                }
                return false;
            }
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine($"{nameof(VStancerExtra)}, Edited:{IsEdited}");
            s.AppendLine($"{nameof(WheelSize)}: {WheelSize} ({DefaultWheelSize})");
            s.AppendLine($"{nameof(WheelWidth)}: {WheelWidth} ({DefaultWheelWidth})");

            for (int i = 0; i < _wheelsCount; i++)
            {
                var defNode = DefaultNodes[i];
                var node = Nodes[i];
                s.Append($"Wheel {i}: {nameof(VStancerExtraNode.TireColliderScaleX)}: {node.TireColliderScaleX} ({defNode.TireColliderScaleX})");
                s.Append($" {nameof(VStancerExtraNode.TireColliderScaleYZ)}: {node.TireColliderScaleYZ} ({defNode.TireColliderScaleYZ})");
                s.AppendLine($" {nameof(VStancerExtraNode.RimColliderScaleYZ)}: {node.RimColliderScaleYZ} ({defNode.RimColliderScaleYZ})");
            }
            return s.ToString();
        }
    }

    public struct VStancerExtraNode
    {
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
}
