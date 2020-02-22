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
        public WheelLimits FrontLimits { get; set; }
        public WheelLimits RearLimits { get; set; }

        public VStancerConfig()
        {
            Debug = false;
            ExposeCommand = false;
            ExposeEvent = false;
            ScriptRange = 150.0f;
            Timer = 1000;
            ToggleMenuControl = 167;
            FloatStep = 0.01f;
            FrontLimits = new WheelLimits { PositionX = 0.25f, RotationY = 0.20f };
            RearLimits = new WheelLimits { PositionX = 0.25f, RotationY = 0.20f };
        }

        public void LoadXml(string xml)
        {
            string txt = Helpers.RemoveByteOrderMarks(xml);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(txt);

            var rootNode = doc[nameof(VStancerConfig)];

            foreach (XmlNode node in rootNode?.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;

                if (node.Name == nameof(Debug) && bool.TryParse(node.InnerText, out bool debugValue))
                    Debug = debugValue;

                else if (node.Name == nameof(ExposeCommand) && bool.TryParse(node.InnerText, out bool exposeCommandValue))
                    ExposeCommand = exposeCommandValue;

                else if (node.Name == nameof(ExposeEvent) && bool.TryParse(node.InnerText, out bool exposeEventValue))
                    ExposeEvent = exposeEventValue;

                else if (node.Name == nameof(ScriptRange) && float.TryParse(node.InnerText, out float scriptRangeValue))
                    ScriptRange = scriptRangeValue;

                else if (node.Name == nameof(Timer) && long.TryParse(node.InnerText, out long timerValue))
                    Timer = timerValue;

                else if (node.Name == nameof(ToggleMenuControl) && int.TryParse(node.InnerText, out int toggleMenuControlValue))
                    ToggleMenuControl = toggleMenuControlValue;

                else if (node.Name == nameof(FloatStep) && float.TryParse(node.InnerText, out float floatStepValue))
                    FloatStep = floatStepValue;

                else if (node.Name == nameof(FrontLimits))
                {
                    var frontLimits = new WheelLimits();

                    foreach (XmlNode child in node.ChildNodes)
                    {
                        if (node.NodeType != XmlNodeType.Element)
                            continue;

                        if (child.Name == nameof(WheelLimits.PositionX) && float.TryParse(child.InnerText, out float positionXValue))
                            frontLimits.PositionX = positionXValue;

                        if (child.Name == nameof(WheelLimits.RotationY) && float.TryParse(child.InnerText, out float rotationYValue))
                            frontLimits.RotationY = rotationYValue;

                        FrontLimits = frontLimits; 
                    }
                }

                else if (node.Name == nameof(RearLimits))
                {
                    var rearLimits = new WheelLimits();

                    foreach (XmlNode child in node.ChildNodes)
                    {
                        if (node.NodeType == XmlNodeType.Comment)
                            continue;

                        if (child.Name == nameof(WheelLimits.PositionX) && float.TryParse(child.InnerText, out float positionXValue))
                            rearLimits.PositionX = positionXValue;

                        if (child.Name == nameof(WheelLimits.RotationY) && float.TryParse(child.InnerText, out float rotationYValue))
                            rearLimits.RotationY = rotationYValue;

                        RearLimits = rearLimits;
                    }
                }

                else continue;
            }
        }
    }

    public struct WheelLimits
    {
        public float PositionX { get; set; }
        public float RotationY { get; set; }
    }
}
