namespace BPL.CDC.Models
{
    public class CDCOutputFormat
    {
        public CDCOutputFormat()
        {

        }

        public CDCOutputFormat(string assignHeaderFormat = "", string assignDetailFormat = "",
            string updateHeaderFormat = "", string updateDetailFormat = "",
            string removeHeaderFormat = "", string removeDetailFormat = "",
            bool showHeader = true, bool showDetail = true)
        {
            _assignHeaderFormat = string.IsNullOrWhiteSpace(assignHeaderFormat) ? _assignHeaderFormat : assignHeaderFormat;
            _assignDetailFormat = string.IsNullOrWhiteSpace(assignDetailFormat) ? _assignDetailFormat : assignDetailFormat;
            _updateHeaderFormat = string.IsNullOrWhiteSpace(updateHeaderFormat) ? _updateHeaderFormat : updateHeaderFormat;
            _updateDetailFormat = string.IsNullOrWhiteSpace(updateDetailFormat) ? _updateDetailFormat : updateDetailFormat;
            _removeHeaderFormat = string.IsNullOrWhiteSpace(removeHeaderFormat) ? _removeHeaderFormat : removeHeaderFormat;
            _removeDetailFormat = string.IsNullOrWhiteSpace(removeDetailFormat) ? _removeDetailFormat : removeDetailFormat;

            ShowHeader = showHeader;
            ShowDetail = showDetail;
        }

        public bool ShowHeader { get; set; } = true;
        public bool ShowDetail { get; set; } = true;

        public bool IgnoreEmptyFieldOnAssign { get; set; } = true;

        private string _assignHeaderFormat = "{ChangeType}{Space}{ClassName}{Space}{HeaderTitle}{NewLine}";
        public string AssignHeaderFormat
        {
            get
            {
                return _assignHeaderFormat;
            }

            set
            {
                if (value != string.Empty)
                    _assignHeaderFormat = value;
            }
        }

        //private string _outputDetailFormat = "{ChangeType}{Space}{ClassName}({PrimaryKey}){Space}{PropertyName}:[{OldValue}]->[{NewValue}]{NewLine}";
        private string _assignDetailFormat = "{ChangeType}{Space}{PropertyName}{Space}[{NewValue}]{NewLine}";
        public string AssignDetailFormat
        {
            get
            {
                return _assignDetailFormat;
            }

            set
            {
                if (value != string.Empty)
                    _assignDetailFormat = value;
            }
        }

        private string _updateHeaderFormat = "{ChangeType}{Space}{ClassName}{Space}{HeaderTitle}{NewLine}";
        public string UpdateHeaderFormat
        {
            get
            {
                return _updateHeaderFormat;
            }

            set
            {
                if (value != string.Empty)
                    _updateHeaderFormat = value;
            }
        }

        //private string _outputDetailFormat = "{ChangeType}{Space}{ClassName}({PrimaryKey}){Space}{PropertyName}:[{OldValue}]->[{NewValue}]{NewLine}";
        private string _updateDetailFormat = "{ChangeType}{Space}{PropertyName}{Space}[{OldValue}]->[{NewValue}]{NewLine}";
        public string UpdateDetailFormat
        {
            get
            {
                return _updateDetailFormat;
            }

            set
            {
                if (value != string.Empty)
                    _updateDetailFormat = value;
            }
        }

        private string _removeHeaderFormat = "{ChangeType}{Space}{ClassName}{Space}{HeaderTitle}{NewLine}";
        public string RemoveHeaderFormat
        {
            get
            {
                return _removeHeaderFormat;
            }

            set
            {
                if (value != string.Empty)
                    _removeHeaderFormat = value;
            }
        }

        //private string _outputDetailFormat = "{ChangeType}{Space}{ClassName}({PrimaryKey}){Space}{PropertyName}:[{OldValue}]->[{NewValue}]{NewLine}";
        private string _removeDetailFormat = "{ChangeType}{Space}{PropertyName}{Space}[{OldValue}]{NewLine}";
        public string RemoveDetailFormat
        {
            get
            {
                return _removeDetailFormat;
            }

            set
            {
                if (value != string.Empty)
                    _removeDetailFormat = value;
            }
        }
    }
}
