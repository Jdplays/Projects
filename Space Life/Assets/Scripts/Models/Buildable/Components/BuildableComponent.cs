using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SpaceLife.Buildable.Components
{
    [Serializable]
    public abstract class BuildableComponent
    {
        protected static readonly string ComponentLogChannel = "NestedObjectComponents";

        private static Dictionary<string, Type> componentTypes;
        
        public BuildableComponent()
        {
            // need to set it, for some reason GetHashCode is called during serialization (when Name is still null)
            Type = string.Empty;
        }

        [XmlIgnore]
        public string Type { get; set; }

        [XmlIgnore]
        protected NestedObject ParentNestedObject { get; set; }

        [XmlIgnore]
        protected Parameter NestedObjectParams
        {
            get { return ParentNestedObject.Parameters; }
        }
        
        public static BuildableComponent Deserialize(XmlReader xmlReader)
        {
            if (componentTypes == null)
            {
                componentTypes = FindComponentsInAssembly();
            }

            string componentTypeName = xmlReader.GetAttribute("type");
            if (componentTypes.ContainsKey(componentTypeName))
            {
                xmlReader = xmlReader.ReadSubtree();
                Type t = componentTypes[componentTypeName];
                XmlSerializer serializer = new XmlSerializer(t);
                var cmp = (BuildableComponent)serializer.Deserialize(xmlReader);
                //// need to set name explicitly (not part of deserialization as it's passed in)
                cmp.Type = componentTypeName;
                return cmp;
            }
            else
            {
                Debug.ULogErrorChannel(ComponentLogChannel, "There is no deserializer for component '{0}'", componentTypeName);
                return null;
            }
        }

        public void Initialize(NestedObject parentNestedObject)
        {
            ParentNestedObject = parentNestedObject;
            Initialize();
        }

        public virtual bool CanFunction()
        {
            return true;
        }

        public virtual void FixedFrequencyUpdate(float deltaTime)
        {
        }

        public virtual void EveryFrameUpdate(float deltaTime)
        {
        }

        public virtual List<ContextMenuAction> GetContextMenu()
        {
            return null;
        }

        public virtual string GetDescription()
        { 
            return null;
        }
        
        public override string ToString()
        {
            return Type;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Type.Equals(obj);
        }

        protected abstract void Initialize();

        protected ContextMenuAction CreateComponentContextMenuItem(ComponentContextMenu componentContextMenuAction)
        {
            return new ContextMenuAction
            {
                Text = componentContextMenuAction.Name, // TODO: localization here
                RequireCharacterSelected = false,
                Action = (cma, c) => InvokeContextMenuAction(componentContextMenuAction.Function, componentContextMenuAction.Name)
            };
        }

        protected void InvokeContextMenuAction(Action<NestedObject, string> function, string arg)
        {
            function(ParentNestedObject, arg);
        }

        private static Dictionary<string, System.Type> FindComponentsInAssembly()
        {
            componentTypes = new Dictionary<string, System.Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    var attribs = type.GetCustomAttributes(typeof(BuildableComponentNameAttribute), false);
                    if (attribs != null && attribs.Length > 0)
                    {
                        foreach (BuildableComponentNameAttribute compNameAttr in attribs)
                        {
                            componentTypes.Add(compNameAttr.ComponentName, type);
                            Debug.ULogChannel(ComponentLogChannel, "Found component in assembly: {0}", compNameAttr.ComponentName);
                        }
                    }
                }
            }

            return componentTypes;
        }

        [Serializable]
        public class UsedAnimations
        {
            [XmlAttribute("idle")]
            public string Idle { get; set; }

            [XmlAttribute("running")]
            public string Running { get; set; }
        }
    }    
}
