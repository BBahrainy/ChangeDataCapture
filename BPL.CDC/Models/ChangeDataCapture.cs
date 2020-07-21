using System;
//
namespace BPL.CDC.Models
{     
    public class ChangeDataCapture
    {
        public ChangeDataCapture(string mask)
        {
            Mask = mask;
        }
        public string Mask { get; set; }
        public ChangeType ChangeType { get; set; }
        public string ClassName { get; set; }
        public string PropertyName { get; set; }
        private string _primaryKey = string.Empty;
        public string PrimaryKey
        {
            get
            {
                return string.IsNullOrWhiteSpace(Mask) ? _primaryKey : Mask;
            }
            set
            {
                _primaryKey = string.IsNullOrWhiteSpace(Mask) ? value : Mask;
            }
        }

        private string _oldValue = string.Empty;
        public string OldValue
        {
            get
            {
                return string.IsNullOrWhiteSpace(Mask) ? _oldValue : Mask;
            }
            set
            {
                _oldValue = string.IsNullOrWhiteSpace(Mask) ? value : Mask;
            }
        }
        private string _newValue = string.Empty;
        public string NewValue
        {
            get
            {
                return string.IsNullOrWhiteSpace(Mask) ? _newValue : Mask;
            }
            set
            {
                _newValue = string.IsNullOrWhiteSpace(Mask) ? value : Mask;
            }
        }

        public object OldEntry { get; set; }

        public object NewEntry { get; set; }

        public DateTime DateChanged { get; set; }
    }
}
