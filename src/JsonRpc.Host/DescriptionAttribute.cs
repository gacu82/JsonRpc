using System;

namespace JsonRpc.Host
{
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            this.Description = description;
        }

        public string Description { get; }
    }
}
