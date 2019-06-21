using System;

namespace SpaceLife.Buildable.Components
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BuildableComponentNameAttribute : Attribute
    {
        public readonly string ComponentName;
        
        public BuildableComponentNameAttribute(string componentName)  
        {
            this.ComponentName = componentName;
        }
    }
}
