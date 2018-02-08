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
        private float[] _defaultWheelsRot;
        private float[] _defaultWheelsOffset;

        public float[] defaultWheelsRot { get { return _defaultWheelsRot; } }
        public float[] defaultWheelsOffset { get { return _defaultWheelsOffset; } }

        public float[] currentWheelsRot;
        public float[] currentWheelsOffset;

        public vstancerPreset(int count, float[] defRot, float[] defOff)
        {
            wheelsCount = count;

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
        }

        public void ResetDefault()
        {
            for (int index = 0; index < wheelsCount; index++)
            {
                currentWheelsRot[index] = _defaultWheelsRot[index];
                currentWheelsOffset[index] = _defaultWheelsOffset[index];
            }
        }

        public bool HasBeenEdited 
        {
            get
            {
                return ((_defaultWheelsOffset[0] != currentWheelsOffset[0]) ||
                (_defaultWheelsOffset[1] != currentWheelsOffset[1]) ||
                (_defaultWheelsOffset[2] != currentWheelsOffset[2]) ||
                (_defaultWheelsOffset[3] != currentWheelsOffset[3]) ||
                (_defaultWheelsRot[0] != currentWheelsRot[0]) ||
                (_defaultWheelsRot[1] != currentWheelsRot[1]) ||
                (_defaultWheelsRot[2] != currentWheelsRot[2]) ||
                (_defaultWheelsRot[3] != currentWheelsRot[3])
                );
            }
        }
    }
}
