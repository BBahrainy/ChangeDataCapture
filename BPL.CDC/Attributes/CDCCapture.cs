using System;

namespace BPL.CDC.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CDCFieldAttribute : System.Attribute
    {
        public string FieldName { get; }
        public string Title { get; }
        public CDCFieldAttribute(string fieldName, string title)
        {
            FieldName = fieldName;
            Title = title;
        }
    }
}
