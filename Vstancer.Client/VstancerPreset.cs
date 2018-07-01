using System;
using System.Text;

namespace Vstancer.Client
{
    public class VstancerPreset : IEquatable<VstancerPreset>
    {
        public int WheelsCount { get; private set; }
        public int FrontWheelsCount { get; private set; }

        public float[] DefaultRotationY { get; private set; }
        public float[] DefaultOffsetX { get; private set; }
        public float[] RotationY { get; set; }
        public float[] OffsetX { get; set; }

        public void SetOffsetFront(float value)
        {
            for (int index = 0; index < FrontWheelsCount; index++)
                OffsetX[index] = (index % 2 == 0) ? -value : value;     
        }

        public void SetOffsetRear(float value)
        {
            for (int index = FrontWheelsCount; index < WheelsCount; index++)
                OffsetX[index] = (index % 2 == 0) ? -value : value;
        }

        public void SetRotationFront(float value)
        {
            for (int index = 0; index < FrontWheelsCount; index++)
                RotationY[index] = (index % 2 == 0) ? value : -value;
        }

        public void SetRotationRear(float value)
        {
            for (int index = FrontWheelsCount; index < WheelsCount; index++)
                RotationY[index] = (index % 2 == 0) ? value : -value;
        }

        public bool IsEdited
        {
            get
            {
                for (int index = 0; index < WheelsCount; index++)
                {
                    if ((DefaultOffsetX[index] != OffsetX[index]) || (DefaultRotationY[index] != RotationY[index]))
                        return true;
                }
                return false;
            }
        }

        public VstancerPreset()
        {
            WheelsCount = 4;
            FrontWheelsCount = 2;

            DefaultRotationY = new float[] { 0, 0, 0, 0 };
            DefaultOffsetX = new float[] { 0, 0, 0, 0 };
            RotationY = new float[] { 0, 0, 0, 0 };
            OffsetX = new float[] { 0, 0, 0, 0 };
        }

        public VstancerPreset(int count, float[] defRot, float[] defOff)
        {
            WheelsCount = count;
            FrontWheelsCount = WheelsCount / 2;

            if (FrontWheelsCount % 2 != 0)
                FrontWheelsCount -= 1;

            DefaultRotationY = new float[WheelsCount];
            DefaultOffsetX = new float[WheelsCount];
            RotationY = new float[WheelsCount];
            OffsetX = new float[WheelsCount];

            for (int index = 0; index < WheelsCount; index++)
            {
                DefaultRotationY[index] = defRot[index];
                DefaultOffsetX[index] = defOff[index];

                RotationY[index] = DefaultRotationY[index];
                OffsetX[index] = DefaultOffsetX[index];
            }
        }

        public VstancerPreset(int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            WheelsCount = count;

            DefaultRotationY = new float[WheelsCount];
            DefaultOffsetX = new float[WheelsCount];
            RotationY = new float[WheelsCount];
            OffsetX = new float[WheelsCount];

            FrontWheelsCount = WheelsCount / 2;
            if (FrontWheelsCount % 2 != 0)
                FrontWheelsCount -= 1;

            for (int index = 0; index < FrontWheelsCount; index++)
            {
                if (index % 2 == 0)
                {
                    DefaultRotationY[index] = defRotFront;
                    DefaultOffsetX[index] = defOffFront;
                    RotationY[index] = currentRotFront;
                    OffsetX[index] = currentOffFront;
                }
                else
                {
                    DefaultRotationY[index] = -defRotFront;
                    DefaultOffsetX[index] = -defOffFront;
                    RotationY[index] = -currentRotFront;
                    OffsetX[index] = -currentOffFront;
                }
            }

            for (int index = FrontWheelsCount; index < WheelsCount; index++)
            {
                if (index % 2 == 0)
                {
                    DefaultRotationY[index] = defRotRear;
                    DefaultOffsetX[index] = defOffRear;
                    RotationY[index] = currentRotRear;
                    OffsetX[index] = currentOffRear;
                }
                else
                {
                    DefaultRotationY[index] = -defRotRear;
                    DefaultOffsetX[index] = -defOffRear;
                    RotationY[index] = -currentRotRear;
                    OffsetX[index] = -currentOffRear;
                }
            }
        }

        public void Reset()
        {
            for (int index = 0; index < WheelsCount; index++)
            {
                RotationY[index] = DefaultRotationY[index];
                OffsetX[index] = DefaultOffsetX[index];
            }
        }

        public bool Equals(VstancerPreset other)
        {
            if (WheelsCount != other.WheelsCount)
                return false;

            for (int index = 0; index < WheelsCount; index++)
            {
                if (Math.Abs(DefaultOffsetX[index] - other.DefaultOffsetX[index]) > 0.001f
                    || Math.Abs(DefaultRotationY[index] - other.DefaultRotationY[index]) > 0.001f
                    || Math.Abs(OffsetX[index] - other.OffsetX[index]) > 0.001f
                    || Math.Abs(RotationY[index] - other.RotationY[index]) > 0.001f)
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
                defOff.Append(string.Format("{0,15}", DefaultOffsetX[i]));
                defRot.Append(string.Format("{0,15}", DefaultRotationY[i]));
                curOff.Append(string.Format("{0,15}", OffsetX[i]));
                curRot.Append(string.Format("{0,15}", RotationY[i]));
            }

            s.AppendLine(curOff.ToString());
            s.AppendLine(defOff.ToString());
            s.AppendLine(curRot.ToString());
            s.AppendLine(defRot.ToString());

            return s.ToString();
        }

    }
}
