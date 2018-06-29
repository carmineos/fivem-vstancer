using System;
using System.Text;

namespace vstancer_client
{
    public class Preset : IEquatable<Preset>
    {
        public int wheelsCount;
        public int frontCount;

        public float[] DefaultRotationY { get; private set; }
        public float[] DefaultOffsetX { get; private set; }
        public float[] RotationY { get; set; }
        public float[] OffsetX { get; set; }

        public void SetOffsetFront(float value)
        {
            for (int index = 0; index < frontCount; index++)
                OffsetX[index] = (index % 2 == 0) ? -value : value;     
        }

        public void SetOffsetRear(float value)
        {
            for (int index = frontCount; index < wheelsCount; index++)
                OffsetX[index] = (index % 2 == 0) ? -value : value;
        }

        public void SetRotationFront(float value)
        {
            for (int index = 0; index < frontCount; index++)
                RotationY[index] = (index % 2 == 0) ? value : -value;
        }

        public void SetRotationRear(float value)
        {
            for (int index = frontCount; index < wheelsCount; index++)
                RotationY[index] = (index % 2 == 0) ? value : -value;
        }

        public bool IsEdited
        {
            get
            {
                for (int index = 0; index < wheelsCount; index++)
                {
                    if ((DefaultOffsetX[index] != OffsetX[index]) || (DefaultRotationY[index] != RotationY[index]))
                        return true;
                }
                return false;
            }
        }

        public Preset()
        {
            wheelsCount = 4;
            frontCount = 2;

            DefaultRotationY = new float[] { 0, 0, 0, 0 };
            DefaultOffsetX = new float[] { 0, 0, 0, 0 };
            RotationY = new float[] { 0, 0, 0, 0 };
            OffsetX = new float[] { 0, 0, 0, 0 };
        }

        public Preset(int count, float[] defRot, float[] defOff)
        {
            wheelsCount = count;
            frontCount = wheelsCount / 2;

            if (frontCount % 2 != 0)
                frontCount -= 1;

            DefaultRotationY = new float[wheelsCount];
            DefaultOffsetX = new float[wheelsCount];
            RotationY = new float[wheelsCount];
            OffsetX = new float[wheelsCount];

            for (int index = 0; index < wheelsCount; index++)
            {
                DefaultRotationY[index] = defRot[index];
                DefaultOffsetX[index] = defOff[index];

                RotationY[index] = DefaultRotationY[index];
                OffsetX[index] = DefaultOffsetX[index];
            }
        }

        public Preset(int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            wheelsCount = count;

            DefaultRotationY = new float[wheelsCount];
            DefaultOffsetX = new float[wheelsCount];
            RotationY = new float[wheelsCount];
            OffsetX = new float[wheelsCount];

            frontCount = wheelsCount / 2;
            if (frontCount % 2 != 0)
                frontCount -= 1;

            for (int index = 0; index < frontCount; index++)
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

            for (int index = frontCount; index < wheelsCount; index++)
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
            for (int index = 0; index < wheelsCount; index++)
            {
                RotationY[index] = DefaultRotationY[index];
                OffsetX[index] = DefaultOffsetX[index];
            }
        }

        public bool Equals(Preset other)
        {
            if (wheelsCount != other.wheelsCount)
                return false;

            for (int index = 0; index < wheelsCount; index++)
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
            s.AppendLine($"Edited:{IsEdited} Wheels count:{wheelsCount} Front count:{frontCount}");

            StringBuilder defOff = new StringBuilder(string.Format("{0,20}", "Default offset:"));
            StringBuilder defRot = new StringBuilder(string.Format("{0,20}", "Default rotation:"));
            StringBuilder curOff = new StringBuilder(string.Format("{0,20}", "Current offset:"));
            StringBuilder curRot = new StringBuilder(string.Format("{0,20}", "Current rotation:"));

            for (int i = 0; i < wheelsCount; i++)
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
