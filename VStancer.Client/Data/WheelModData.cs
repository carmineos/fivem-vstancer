using System.Text;

using CitizenFX.Core;

namespace VStancer.Client.Data
{
    public class WheelModData
    {
        private const float Epsilon = VStancerUtilities.Epsilon;

        private readonly WheelModNode[] _nodes;
        private readonly WheelModNode[] _defaultNodes;
        private float _wheelSize;
        private float _wheelWidth;

        public int WheelsCount { get; private set; }
        public int FrontWheelsCount { get; private set; }

        public delegate void WheelModPropertyEdited(string name, float value);
        public event WheelModPropertyEdited PropertyChanged;

        public float WheelSize
        {
            get => _wheelSize;
            set
            {
                if (value == _wheelSize)
                    return;

                _wheelSize = value;
                PropertyChanged?.Invoke(nameof(WheelSize), value);
            }
        }

        public float WheelWidth
        {
            get => _wheelWidth;
            set
            {
                if (value == _wheelWidth)
                    return;

                _wheelWidth = value;
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



        public WheelModData(int wheelsCount, float width, float radius,
            float frontTireColliderWidth, float frontTireColliderSize, float frontRimColliderSize,
            float rearTireColliderWidth, float rearTireColliderSize, float rearRimColliderSize)
        {
            WheelsCount = wheelsCount;
            FrontWheelsCount = VStancerUtilities.CalculateFrontWheelsCount(WheelsCount);

            DefaultWheelSize = radius;
            DefaultWheelWidth = width;

            _wheelSize = radius;
            _wheelWidth = width;

            _defaultNodes = new WheelModNode[WheelsCount];

            for (int i = 0; i < FrontWheelsCount; i++)
            {
                _defaultNodes[i].TireColliderWidth = frontTireColliderWidth;
                _defaultNodes[i].TireColliderSize = frontTireColliderSize;
                _defaultNodes[i].RimColliderSize = frontRimColliderSize;
            }

            for (int i = FrontWheelsCount; i < WheelsCount; i++)
            {
                _defaultNodes[i].TireColliderWidth = rearTireColliderWidth;
                _defaultNodes[i].TireColliderSize = rearTireColliderSize;
                _defaultNodes[i].RimColliderSize = rearRimColliderSize;
            }

            DefaultFrontTireColliderWidthRatio = DefaultWheelWidth / DefaultFrontTireColliderWidth;
            DefaultFrontTireColliderSizeRatio = DefaultWheelSize / DefaultFrontTireColliderSize;
            DefaultFrontRimColliderSizeRatio = DefaultWheelSize / DefaultFrontRimColliderSize;

            DefaultRearTireColliderWidthRatio = DefaultWheelWidth / DefaultRearTireColliderWidth;
            DefaultRearTireColliderSizeRatio = DefaultWheelSize / DefaultRearTireColliderSize;
            DefaultRearRimColliderSizeRatio = DefaultWheelSize / DefaultRearRimColliderSize;

            _nodes = new WheelModNode[WheelsCount];
            for (int i = 0; i < WheelsCount; i++)
                _nodes[i] = _defaultNodes[i];
        }

        
        public float FrontTireColliderWidth
        {
            get => _nodes[0].TireColliderWidth;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    _nodes[index].TireColliderWidth = value;

                PropertyChanged?.Invoke(nameof(FrontTireColliderWidth), value);
            }
        }

        
        public float FrontTireColliderSize
        {
            get => _nodes[0].TireColliderSize;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    _nodes[index].TireColliderSize = value;

                PropertyChanged?.Invoke(nameof(FrontTireColliderSize), value);
            }
        }

        
        public float FrontRimColliderSize
        {
            get => _nodes[0].RimColliderSize;
            set
            {
                for (int index = 0; index < FrontWheelsCount; index++)
                    _nodes[index].RimColliderSize = value;

                PropertyChanged?.Invoke(nameof(FrontRimColliderSize), value);
            }
        }

        
        public float RearTireColliderWidth
        {
            get => _nodes[FrontWheelsCount].TireColliderWidth;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    _nodes[index].TireColliderWidth = value;

                PropertyChanged?.Invoke(nameof(RearTireColliderWidth), value);
            }
        }

        
        public float RearTireColliderSize
        {
            get => _nodes[FrontWheelsCount].TireColliderSize;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    _nodes[index].TireColliderSize = value;

                PropertyChanged?.Invoke(nameof(RearTireColliderSize), value);
            }
        }

        public float RearRimColliderSize
        {
            get => _nodes[FrontWheelsCount].RimColliderSize;
            set
            {
                for (int index = FrontWheelsCount; index < WheelsCount; index++)
                    _nodes[index].RimColliderSize = value;

                PropertyChanged?.Invoke(nameof(RearRimColliderSize), value);
            }
        }

        public float DefaultFrontTireColliderWidth => _defaultNodes[0].TireColliderWidth;
        public float DefaultFrontTireColliderSize => _defaultNodes[0].TireColliderSize;
        public float DefaultFrontRimColliderSize => _defaultNodes[0].RimColliderSize;

        public float DefaultRearTireColliderWidth => _defaultNodes[FrontWheelsCount].TireColliderWidth;
        public float DefaultRearTireColliderSize => _defaultNodes[FrontWheelsCount].TireColliderSize;
        public float DefaultRearRimColliderSize => _defaultNodes[FrontWheelsCount].RimColliderSize;

        public void Reset()
        {
            WheelSize = DefaultWheelSize;
            WheelWidth = DefaultWheelWidth;

            for (int i = 0; i < WheelsCount; i++)
                _nodes[i] = _defaultNodes[i];

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
                    if (!MathUtil.WithinEpsilon(_defaultNodes[i].TireColliderWidth, _nodes[i].TireColliderWidth, Epsilon) ||
                        !MathUtil.WithinEpsilon(_defaultNodes[i].TireColliderSize, _nodes[i].TireColliderSize, Epsilon) ||
                        !MathUtil.WithinEpsilon(_defaultNodes[i].RimColliderSize, _nodes[i].RimColliderSize, Epsilon))
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
                var defNode = _defaultNodes[i];
                var node = _nodes[i];
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
