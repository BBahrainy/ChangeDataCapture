using System;
using System.Collections.Generic;
using BPL.CDC.Models;

namespace BPL.CDC.Services
{
    public static class ChangeDataCaptureExtension
    {
        public static string CDCtext(this object obj, object newEntry,
            Dictionary<string, string> fieldList = null, List<string> keyFields = null, string parentName = "", string mainKey = "", string classTitle = "")
        {
            try
            {
                ChangeDataCaptureService cdcService = new ChangeDataCaptureService();
                ChangeDataCaptureList result = cdcService.GetChanges(obj, newEntry, DateTime.Now, fieldList, keyFields, parentName, mainKey, classTitle);

                return result?.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static ChangeDataCaptureList CDC(this object obj, object newEntry,
            Dictionary<string, string> fieldList = null, List<string> keyFields = null, string parentName = "", string mainKey = "", string classTitle = "")
        {
            try
            {
                ChangeDataCaptureService cdcService = new ChangeDataCaptureService();
                return cdcService.GetChanges(obj, newEntry, DateTime.Now, fieldList, keyFields, parentName, mainKey, classTitle);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string GetCDCtext(object oldEntry, object newEntry,
            Dictionary<string, string> fieldList = null, List<string> keyField = null, string parentName = "", string mainKey = "", string classTitle = "")
        {
            try
            {
                ChangeDataCaptureService cdcService = new ChangeDataCaptureService();
                ChangeDataCaptureList result = cdcService.GetChanges(oldEntry, newEntry, DateTime.Now, fieldList, keyField, parentName, mainKey, classTitle);

                return result?.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static ChangeDataCaptureList GetCDC(object oldEntry, object newEntry,
            Dictionary<string, string> fieldList = null, List<string> keyFields = null, string parentName = "", string mainKey = "", string classTitle = "")
        {
            try
            {
                ChangeDataCaptureService cdcService = new ChangeDataCaptureService();
                return cdcService.GetChanges(oldEntry, newEntry, DateTime.Now, fieldList, keyFields, parentName, mainKey, classTitle);
            }
            catch
            {
                return null;
            }
        }
    }
}
