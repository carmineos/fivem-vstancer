using System;

namespace vstancer_shared
{
    public class vstancerPreset : IEquatable<vstancerPreset>
    {
        public int wheelsCount;
        public int frontCount;

        private float[] _defaultWheelsRot;
        private float[] _defaultWheelsOffset;

        public float[] defaultWheelsRot { get { return _defaultWheelsRot; } }
        public float[] defaultWheelsOffset { get { return _defaultWheelsOffset; } }

        public float[] currentWheelsRot;
        public float[] currentWheelsOffset;

        public void SetFrontOffset(float amount)
        {
            for (int index = 0; index < frontCount; index++)
            {
                if (index % 2 == 0)
                    currentWheelsOffset[index] = -amount;
                else
                    currentWheelsOffset[index] = amount;
            }
        }

        public void SetRearOffset(float amount)
        {
            for (int index = frontCount; index < wheelsCount; index++)
            {
                if (index % 2 == 0)
                    currentWheelsOffset[index] = -amount;
                else
                    currentWheelsOffset[index] = amount;
            }
        }

        public void SetFrontRotation(float amount)
        {
            for (int index = 0; index < frontCount; index++)
            {
                if (index % 2 == 0)
                    currentWheelsRot[index] = amount;
                else
                    currentWheelsRot[index] = -amount;
            }
        }

        public void SetRearRotation(float amount)
        {
            for (int index = frontCount; index < wheelsCount; index++)
            {
                if (index % 2 == 0)
                    currentWheelsRot[index] = amount;
                else
                    currentWheelsRot[index] = -amount;
            }
        }

        public bool HasBeenEdited
        {
            get
            {
                for (int index = 0; index < wheelsCount; index++)
                {
                    if ((_defaultWheelsOffset[index] != currentWheelsOffset[index]) || (_defaultWheelsRot[index] != currentWheelsRot[index]))
                        return true;
                }
                return false;
            }
        }

        public vstancerPreset(int count, float[] defRot, float[] defOff)
        {
            wheelsCount = count;
            frontCount = wheelsCount / 2;

            if (frontCount % 2 != 0)
                frontCount -= 1;

            _defaultWheelsRot = new float[wheelsCount];
            _defaultWheelsOffset = new float[wheelsCount];
            currentWheelsRot = new float[wheelsCount];
            currentWheelsOffset = new float[wheelsCount];

            for (int index = 0; index < wheelsCount; index++)
            {
                _defaultWheelsRot[index] = defRot[index];
                _defaultWheelsOffset[index] = defOff[index];

                currentWheelsRot[index] = _defaultWheelsRot[index];
                currentWheelsOffset[index] = _defaultWheelsOffset[index];
            }
        }

        public vstancerPreset(int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            wheelsCount = count;

            _defaultWheelsRot = new float[wheelsCount];
            _defaultWheelsOffset = new float[wheelsCount];
            currentWheelsRot = new float[wheelsCount];
            currentWheelsOffset = new float[wheelsCount];

            frontCount = wheelsCount / 2;
            if (frontCount % 2 != 0)
                frontCount -= 1;

            for (int index = 0; index < frontCount; index++)
            {
                if (index % 2 == 0)
                {
                    _defaultWheelsRot[index] = defRotFront;
                    _defaultWheelsOffset[index] = defOffFront;
                    currentWheelsRot[index] = currentRotFront;
                    currentWheelsOffset[index] = currentOffFront;
                }
                else
                {
                    _defaultWheelsRot[index] = -defRotFront;
                    _defaultWheelsOffset[index] = -defOffFront;
                    currentWheelsRot[index] = -currentRotFront;
                    currentWheelsOffset[index] = -currentOffFront;
                }
            }

            for (int index = frontCount; index < wheelsCount; index++)
            {
                if (index % 2 == 0)
                {
                    _defaultWheelsRot[index] = defRotRear;
                    _defaultWheelsOffset[index] = defOffRear;
                    currentWheelsRot[index] = currentRotRear;
                    currentWheelsOffset[index] = currentOffRear;
                }
                else
                {
                    _defaultWheelsRot[index] = -defRotRear;
                    _defaultWheelsOffset[index] = -defOffRear;
                    currentWheelsRot[index] = -currentRotRear;
                    currentWheelsOffset[index] = -currentOffRear;
                }
            }
        }

        public void ResetDefault()
        {
            for (int index = 0; index < wheelsCount; index++)
            {
                currentWheelsRot[index] = _defaultWheelsRot[index];
                currentWheelsOffset[index] = _defaultWheelsOffset[index];
            }
        }

        public bool Equals(vstancerPreset other)
        {
            if (wheelsCount != other.wheelsCount)
                return false;

            for (int index = 0; index < wheelsCount; index++)
            {
                if ((Math.Round(_defaultWheelsOffset[index],3) != Math.Round(other._defaultWheelsOffset[index],3))
                || (Math.Round(_defaultWheelsRot[index], 3) != Math.Round(other._defaultWheelsRot[index], 3))
                || (Math.Round(currentWheelsOffset[index], 3) != Math.Round(other.currentWheelsOffset[index], 3))
                || (Math.Round(currentWheelsRot[index], 3) != Math.Round(other.currentWheelsRot[index], 3))
                )
                    return false;
            }
            return true;
        }

    }
}
