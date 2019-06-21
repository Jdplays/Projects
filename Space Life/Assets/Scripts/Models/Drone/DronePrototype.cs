using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Animation;
using UnityEngine;

public class DronePrototype : IPrototypable
{

    public string Type { get; set; }

    public string Name { get; set; }

    public SpritenameAnimation AnimationIdle { get; set; }

    public SpritenameAnimation AnimationFlying { get; set; }

    public void ReadXmlPrototype(XmlReader reader_parent)
    {
        Type = reader_parent.GetAttribute("type");

        XmlReader reader = reader_parent.ReadSubtree();

        while (reader.Read())
        {
            switch (reader.Name)
            {
                case "name":
                    reader.Read();
                    Name = reader.ReadContentAsString();
                    break;

                case "Animations":
                    XmlReader animationReader = reader.ReadSubtree();
                    ReadAnimationXml(animationReader);
                    break;
            }
        }
    }

    public Drone CreateDrone()
    {
        Drone d = new Drone
        {
            name = Name
        };

        return d;
    }

    private void ReadAnimationXml(XmlReader animationReader)
    {
        while (animationReader.Read())
        {
            if (animationReader.Name == "Animation")
            {
                string state = animationReader.GetAttribute("state");
                float fps = 1;
                float.TryParse(animationReader.GetAttribute("fps"), out fps);
                bool looping = true;
                bool.TryParse(animationReader.GetAttribute("looping"), out looping);
                bool valueBased = false;

                // read frames
                XmlReader frameReader = animationReader.ReadSubtree();
                List<string> framesSpriteNames = new List<string>();
                while (frameReader.Read())
                {
                    if (frameReader.Name == "Frame")
                    {
                        framesSpriteNames.Add(frameReader.GetAttribute("name"));
                    }
                }

                switch (state)
                {
                    case "idle":
                        AnimationIdle = new SpritenameAnimation(state, framesSpriteNames.ToArray(), fps, looping, valueBased);
                        break;
                    case "flying":
                        AnimationFlying = new SpritenameAnimation(state, framesSpriteNames.ToArray(), fps, looping, valueBased);
                        break;
                }
            }
        }
    }
}
