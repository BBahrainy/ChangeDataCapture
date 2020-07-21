using System;

namespace BPL.CDC.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CDCMaskValueAttribute : System.Attribute
    {
        private string _mask;

        public string Mask
        {
            get
            {
                return this._mask;
            }
        }
        public CDCMaskValueAttribute(string mask)
        {
            this._mask = mask;
        }
    }
}
