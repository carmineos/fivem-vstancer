using System.Text;

using CitizenFX.Core;

namespace VStancer.Client.Data
{
    public class WheelModData
    {
        private const float Epsilon = VStancerUtilities.Epsilon;

        public int WheelsCount { get; private set; }
        public int FrontWheelsCount { get; private set; }

        private float wheelSize;
        private float wheelWidth;

        public delegate void WheelModPropertyEdited(string name, float value);
        public event WheelModPropertyEdited PropertyChanged;

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

        public float DefaultFrontTireColliderWidthRatio { get; private set; }
        public float DefaultFrontTireColliderSizeRatio { get; private set; }
        public float DefaultFrontRimColliderSizeRatio { get; private set; }
        public float DefaultRearTireColliderWidthRatio { get; private set; }
        public float DefaultRearTireColliderSizeRatio { get; private set; }
        public float DefaultRearRimColliderSizeRatio { get; private set; }

        public WheelModNode[] Nodes { get; set; }
        public WheelModNode[] DefaultNodes { get; private set; }

        public WheelModData(int wheelsCount, float width, float radius,
            float frontTireColliderScaleX, float frontTireColliderScaleYZ, float frontRimColliderScaleYZ,
            float rearTireColliderScaleX, float rearTireColliderScaleYZ, float rearRimColliderScaleYZ)
        {
            WheelsCount = wheelsCount;
            FrontWheelsCount = VStancerUtilities.CalculateFrontWheelsCount(WheelsCount);

            DefaultWheelSize = radius;
            DefaultWheelWidth = width;

            wheelSize = radius;
            wheelWidth = width;

            DefaultNodes = new WheelModNode[WheelsCount];

            for (int i = 0; i < FrontWheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    DefaultNodes[i].TireColliderWidth = frontTireColliderScaleX;
                    DefaultNodes[i].TireColliderSize = frontTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderSize = frontRimColliderScaleYZ;
                }
                else
                {
                    DefaultNodes[i].TireColliderWidth = -frontTireColliderScaleX;
                    DefaultNodes[i].TireColliderSize = -frontTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderSize = -frontRimColliderScaleYZ;
                }
            }

            for (int i = FrontWheelsCount; i < WheelsCount; i++)
            {
                if (i % 2 == 0)
                {
                    DefaultNodes[i].TireColliderWidth = rearTireColliderScaleX;
                    DefaultNodes[i].TireColliderSize = rearTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderSize = rearRimColliderScaleYZ;
                }
                else
                {
                    DefaultNodes[i].TireColliderWidth = -rearTireColliderScaleX;
                    DefaultNodes[i].TireColliderSize = -rearTireColliderScaleYZ;
                    DefaultNodes[i].RimColliderSize = -rearRimColliderScaleYZ;
                }
            }

            DefaultFrontTireColliderWidthRatio = DefaultWheelWidth / DefaultFrontTireColliderWidth;
            DefaultFrontTireColliderSizeRatio = DefaultWheelSize / DefaultFrontTireColliderSize;
            DefaultFrontRimColliderSizeRatio = DefaultWheelSize / DefaultFrontRimColliderSize;

            DefaultRearTireColliderWidthRatio = DefaultWheelWidth / DefaultRearTireColliderWidth;
            DefaultRearTireColliderSizeRatio = DefaultWheelSize / DefaultRearTireColliderSize;
            DefaultRearRimColliderSizeRatio = DefaultWheelSize / DefaultRearRimColliderSize;

            Nodes = new WheelModNode[WheelsCount];
            for (int i = 0; i < WheelsCount; i++)
                Nodes[i] = DefaultNodes[i];
        }

        
        public float FrontTireColliderWidth
        {
            get => Nodes[0].TireColliderWidth;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    Nodes[index].TireColliderWidth = value;

                PropertyChanged?.Invoke(nameof(FrontTireColliderWidth), value);
            }
        }

        
        public float FrontTireColliderSize
        {
            get => Nodes[0].TireColliderSize;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    Nodes[index].TireColliderSize = value;

                PropertyChanged?.Invoke(nameof(FrontTireColliderSize), value);
            }
        }

        
        public float FrontRimColliderSize
        {
            get => Nodes[0].RimColliderSize;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    Nodes[index].RimColliderSize = value;

                PropertyChanged?.Invoke(nameof(FrontRimColliderSize), value);
            }
        }

        
        public float RearTireColliderWidth
        {
            get => Nodes[FrontWheelsCount].TireColliderWidth;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    Nodes[index].TireColliderWidth = value;

                PropertyChanged?.Invoke(nameof(RearTireColliderWidth), value);
            }
        }

        
        public float RearTireColliderSize
        {
            get => Nodes[FrontWheelsCount].TireColliderSize;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    Nodes[index].TireColliderSize = value;

                PropertyChanged?.Invoke(nameof(RearTireColliderSize), value);
            }
        }

        public float RearRimColliderSize
        {
            get => Nodes[FrontWheelsCount].RimColliderSize;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    Nodes[index].RimColliderSize = value;

                PropertyChanged?.Invoke(nameof(RearRimColliderSize), value);
            }
        }

        public float DefaultFrontTireColliderWidth => DefaultNodes[0].TireColliderWidth;
        public float DefaultFrontTireColliderSize => DefaultNodes[0].TireColliderSize;
        public float DefaultFrontRimColliderSize => DefaultNodes[0].RimColliderSize;

        public float DefaultRearTireColliderWidth => DefaultNodes[FrontWheelsCount].TireColliderWidth;
        public float DefaultRearTireColliderSize => DefaultNodes[FrontWheelsCount].TireColliderSize;
        public float DefaultRearRimColliderSize => DefaultNodes[FrontWheelsCount].RimColliderSize;

        public void Reset()
        {
            WheelSize = DefaultWheelSize;
            WheelWidth = DefaultWheelWidth;

            for (int i = 0; i < WheelsCount; i++)
                Nodes[i] = DefaultNodes[i];

            PropertyChanged?.Invoke(nameof(Reset), default);
        }

        public bool IsEdited
        {
            get
            {
                if (!MathUtil.WithinEpsilon(DefaultWheelSize, WheelSize, Epsilon) ||
                    !MathUtil.WithinEpsilon(DefaultWheelWidth, WheelWidth, Epsilon))
                    return true;

                for (int i = 0; i < WheelsCount; i++)
                {
                    if (!MathUtil.WithinEpsilon(DefaultNodes[i].TireColliderWidth, Nodes[i].TireColliderWidth, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[i].TireColliderSize, Nodes[i].TireColliderSize, Epsilon) ||
                        !MathUtil.WithinEpsilon(DefaultNodes[i].RimColliderSize, Nodes[i].RimColliderSize, Epsilon))
                        return true;
                }
                return false;
            }
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine($"{nameof(WheelModData)}, Edited:{IsEdited}");
            s.AppendLine($"{nameof(WheelSize)}: {WheelSize} ({DefaultWheelSize})");
            s.AppendLine($"{nameof(WheelWidth)}: {WheelWidth} ({DefaultWheelWidth})");

            for (int i = 0; i < WheelsCount; i++)
            {
                var defNode = DefaultNodes[i];
                var node = Nodes[i];
                s.Append($"Wheel {i}: {nameof(WheelModNode.TireColliderWidth)}: {node.TireColliderWidth} ({defNode.TireColliderWidth})");
                s.Append($" {nameof(WheelModNode.TireColliderSize)}: {node.TireColliderSize} ({defNode.TireColliderSize})");
                s.AppendLine($" {nameof(WheelModNode.RimColliderSize)}: {node.RimColliderSize} ({defNode.RimColliderSize})");
            }
            return s.ToString();
        }
    }

    public struct WheelModNode
    {
        /// <summary>
        /// The collider wheel thread size
        /// For vanilla wheel mod this is always 50% of visual width
        /// </summary>
        public float TireColliderWidth { get; set; }

        /// <summary>
        /// The collider wheel radius
        /// For vanilla wheel mod this is always 50% of visual size
        /// </summary>
        public float TireColliderSize { get; set; }

        /// <summary>
        /// The collider wheel radius
        /// Defined as rimRadius in carcols for wheels (CVehicleModelInfoVarGlobal)
        /// </summary>
        public float RimColliderSize { get; set; }
    }
}
