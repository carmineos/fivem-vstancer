using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vstancer_shared
{
    public class vstancerPreset
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

            if ((wheelsCount / 2) % 2 == 0)
                frontCount = wheelsCount / 2;
            else
                frontCount = (wheelsCount / 2) - 1;

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

        /*public vstancerPreset(int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            wheelsCount = count;

            _defaultWheelsRot = new float[wheelsCount];
            _defaultWheelsOffset = new float[wheelsCount];
            currentWheelsRot = new float[wheelsCount];
            currentWheelsOffset = new float[wheelsCount];

            _defaultWheelsRot[0] = defRotFront;
            _defaultWheelsRot[1] = -defRotFront;
            _defaultWheelsRot[2] = defRotRear;
            _defaultWheelsRot[3] = -defRotRear;

            _defaultWheelsOffset[0] = defOffFront;
            _defaultWheelsOffset[1] = -defOffFront;
            _defaultWheelsOffset[2] = defOffRear;
            _defaultWheelsOffset[3] = -defOffRear;

            currentWheelsRot[0] = currentRotFront;
            currentWheelsRot[1] = -currentRotFront;
            currentWheelsRot[2] = currentRotRear;
            currentWheelsRot[3] = -currentRotRear;

            currentWheelsOffset[0] = currentOffFront;
            currentWheelsOffset[1] = -currentOffFront;
            currentWheelsOffset[2] = currentOffRear;
            currentWheelsOffset[3] = -currentOffRear;
        }*/

        public vstancerPreset(int count, float currentRotFront, float currentRotRear, float currentOffFront, float currentOffRear, float defRotFront, float defRotRear, float defOffFront, float defOffRear)
        {
            wheelsCount = count;

            _defaultWheelsRot = new float[wheelsCount];
            _defaultWheelsOffset = new float[wheelsCount];
            currentWheelsRot = new float[wheelsCount];
            currentWheelsOffset = new float[wheelsCount];

            if ((wheelsCount / 2) % 2 == 0)
                frontCount = wheelsCount / 2;
            else
                frontCount = (wheelsCount / 2) - 1;

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
       
    }
}
