using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Vstancer.Client
{
    public class VStancerConfig
    {
        public bool Debug { get; set; }
        public bool ExposeCommand { get; set; }
        public bool ExposeEvent { get; set; }
        public float ScriptRange { get; set; }
        public long Timer { get; set; }
        public int ToggleMenuControl { get; set; }
        public float FloatStep { get; set; }
        public NodeLimits FrontLimits { get; set; }
        public NodeLimits RearLimits { get; set; }

        public VStancerConfig()
        {
            Debug = false;
            ExposeCommand = false;
            ExposeEvent = false;
            ScriptRange = 150.0f;
            Timer = 1000;
            ToggleMenuControl = 167;
            FloatStep = 0.01f;
            FrontLimits = new NodeLimits { PositionX = 0.25f, RotationY = 0.20f };
            RearLimits = new NodeLimits { PositionX = 0.25f, RotationY = 0.20f };
        }
    }

    public struct NodeLimits
    {
        public float PositionX { get; set; }
        public float RotationY { get; set; }
    }
}
