using CitizenFX.Core;
using System.Text;
using static VStancer.Client.Utilities;

namespace VStancer.Client.Data
{
    public class SuspensionData
    {
        private const float Epsilon = Utilities.Epsilon;

        private float _visualHeight;

        public event PropertyChanged<float> PropertyChanged;

        public float VisualHeight
        {
            get => _visualHeight;
            set
            {
                if (value == _visualHeight)
                    return;

                _visualHeight = value;
                PropertyChanged?.Invoke(nameof(VisualHeight), value);
            }
        }

        public float DefaultVisualHeight { get; private set; }

        public SuspensionData(float suspensionHeight)
        {
            DefaultVisualHeight = suspensionHeight;
            _visualHeight = suspensionHeight;
        }

        public void Reset()
        {
            VisualHeight = DefaultVisualHeight;
            PropertyChanged?.Invoke(nameof(Reset), default);
        }

        public bool IsEdited
        {
            get
            {
                if (!MathUtil.WithinEpsilon(DefaultVisualHeight, VisualHeight, Epsilon))
                    return true;

                return false;
            }
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine($"{nameof(SuspensionData)}, Edited:{IsEdited}");
            s.AppendLine($"{nameof(VisualHeight)}: {VisualHeight} ({DefaultVisualHeight})");
            return s.ToString();
        }
    }
}
