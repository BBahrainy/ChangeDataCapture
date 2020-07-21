using System;

namespace BPL.CDC.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CDCClassTitleAttribute : System.Attribute
    {
        private string _title;

        public string Title
        {
            get
            {
                return this._title;
            }
        }
        public CDCClassTitleAttribute(string title)
        {
            this._title = title;
        }
    }
}
