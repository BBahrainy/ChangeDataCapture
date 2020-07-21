using System;
using System.Collections.Generic;

namespace BPL.CDC.Models
{
    public class ChangeDataCaptureList : List<ChangeDataCapture>
    {
        public EventHandler BeforeClearCDCs { get; set; }
        public EventHandler AfterClearCDCs { get; set; }

        protected virtual void DoBeforeClearCDCs()
        {
            if (BeforeClearCDCs != null)
                BeforeClearCDCs(this, new EventArgs());
        }

        protected virtual void DoAfterClearCDCs()
        {
            if (AfterClearCDCs != null)
                AfterClearCDCs(this, new EventArgs());
        }

        public string HeaderTitle { get; set; } = string.Empty;
        public CDCOutputFormat OutputFormat { get; set; } = new CDCOutputFormat();
        public override string ToString()
        {
            string result = string.Empty;

            if (OutputFormat == null)
                return result;

            //"{ChangeType}{Space}{ClassName}{Space}{PrimaryKey}{Comma}{Space}{PropertyName}"
            //0" ",1",",2System.Environment.NewLine,3ChangeType,4ClassName,5PrimaryKey,6PropertyName,7OldValue,8NewValue,9DateChanged

            //create CDC Header from first row of cdc collection
            foreach (ChangeDataCapture cdc in this)
            {
                string mask = getOutputMask(cdc.ChangeType, MaskType.Header);
                mask = string.IsNullOrWhiteSpace(cdc.PrimaryKey) ? mask.Replace("({5})", "") : mask;

                string changeType = cdc.ClassName.Contains(".") ? "*" : getChageTypeStr(cdc.ChangeType);

                result = string.Format(mask,
                    " ", ",", System.Environment.NewLine, changeType,
                     cdc.ClassName, cdc.PrimaryKey, cdc.PropertyName, cdc.OldValue, cdc.NewValue, cdc.DateChanged.ToString(), HeaderTitle);
                break;
            }

            foreach (ChangeDataCapture cdc in this)
            {
                if ((cdc.ChangeType == ChangeType.Assign) && OutputFormat.IgnoreEmptyFieldOnAssign && string.IsNullOrWhiteSpace(cdc.NewValue))
                    continue;

                if ((cdc.ChangeType == ChangeType.Remove) && OutputFormat.IgnoreEmptyFieldOnAssign && string.IsNullOrWhiteSpace(cdc.OldValue))
                    continue;

                string className = getClassNameForDetail(cdc.ClassName);
                string primaryKey = getPrimaryKeyForDetail(cdc.PrimaryKey);
                string mask = getOutputMask(cdc.ChangeType, MaskType.Detail);
                mask = string.IsNullOrWhiteSpace(className) ? mask.Replace("{0}{4}", "") : mask;
                mask = string.IsNullOrWhiteSpace(primaryKey) ? mask.Replace("{0}({5})", "") : mask;
                string changeType = getChageTypeStr(cdc.ChangeType);

                result += string.Format(mask,
                    " ", ",", System.Environment.NewLine, changeType,
                     className, primaryKey, cdc.PropertyName, cdc.OldValue, cdc.NewValue, cdc.DateChanged.ToString(), HeaderTitle);
            }

            return result;
        }

        public virtual void ClearCDCs()
        {
            DoBeforeClearCDCs();
            this.Clear();
            DoAfterClearCDCs();
        }

        public virtual string ToStringAndClear(string headerTitle = "")
        {
            this.HeaderTitle = headerTitle;
            string result = ToString();
            ClearCDCs();
            return result;
        }

        private string getOutputMask(ChangeType changeType, MaskType maskType)
        {
            string result = string.Empty;

            switch (changeType)
            {
                case ChangeType.Assign:
                    result = (maskType == MaskType.Header) ? this.OutputFormat.AssignHeaderFormat : this.OutputFormat.AssignDetailFormat;
                    break;
                case ChangeType.Update:
                    result = (maskType == MaskType.Header) ? this.OutputFormat.UpdateHeaderFormat : this.OutputFormat.UpdateDetailFormat;
                    break;
                case ChangeType.Remove:
                    result = (maskType == MaskType.Header) ? this.OutputFormat.RemoveHeaderFormat : this.OutputFormat.RemoveDetailFormat;
                    break;
            }

            result = result.Replace("{Space}", "{0}")
                        .Replace("{Comma}", "{1}")
                        .Replace("{NewLine}", "{2}")
                        .Replace("{ChangeType}", "{3}")
                        .Replace("{ClassName}", "{4}")
                        .Replace("{PrimaryKey}", "{5}")
                        .Replace("{PropertyName}", "{6}")
                        .Replace("{OldValue}", "{7}")
                        .Replace("{NewValue}", "{8}")
                        .Replace("{DateChanged}", "{9}")
                        .Replace("{HeaderTitle}", "{10}");

            return result;
        }

        private string getChageTypeStr(ChangeType changeType)
        {
            switch (changeType)
            {
                case ChangeType.Assign:
                    return "+";
                case ChangeType.Update:
                    return "*";
                case ChangeType.Remove:
                    return "-";
                default:
                    return "";

            }
        }

        private string getClassNameForDetail(string className)
        {
            return removeFirstItem(className, '.');
        }
        private string getPrimaryKeyForDetail(string primaryKey)
        {
            return removeFirstItem(primaryKey, ',');
        }

        private string removeFirstItem(string item, char seprator)
        {
            string result = string.Empty;
            if (string.IsNullOrWhiteSpace(item))
                return result;

            string[] itemes = item.Split(seprator);
            if (itemes.Length > 1)
                for (int i = 1; i < itemes.Length; i++)
                    result += itemes[i] + seprator;
            if (!string.IsNullOrWhiteSpace(result))
                result = result.Remove(result.Length - 1);

            return result;
        }
    }
}
