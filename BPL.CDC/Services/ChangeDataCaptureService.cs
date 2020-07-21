using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BPL.CDC.Attributes;
using BPL.CDC.Models;

namespace BPL.CDC.Services
{
    public class ChangeDataCaptureService
    {
        private bool IsSimple(Type type)
        {
            bool result = false;

            Type utype = Nullable.GetUnderlyingType(type);

            result = type.IsPrimitive
              || type.Equals(typeof(string))
              || type.Equals(typeof(DateTime))
              || 
              ((utype != null) && (utype.IsPrimitive
                || utype.Equals(typeof(string))
                || utype.Equals(typeof(DateTime))));

            return result;
        }

        private bool IsEnum(Type type)
        {
            bool? nullableEnum = Nullable.GetUnderlyingType(type)?.IsEnum;
            return type.IsEnum
              || (nullableEnum != null ? (bool)nullableEnum : false);
        }

        private bool IsCollection(object entry)
        {
            if (entry == null)
                return false;

            return entry.GetType().GetInterfaces().Contains(typeof(System.Collections.ICollection));
        }

        private bool IsCollection(PropertyInfo property)
        {
            if (property == null)
                return false;

            return property.PropertyType.GetInterfaces().Contains(typeof(System.Collections.ICollection));
        }

        private string GetKeyFieldsValue(object entry, List<string> keyFields)
        {
            string result = string.Empty;
            IEnumerable<PropertyInfo> keyProps = null;
            if (keyFields?.Count > 0)
                keyProps = entry.GetType().GetProperties().Where(p => keyFields.Contains(p.Name));
            else
                keyProps = entry.GetType().GetProperties().Where(p => Attribute.IsDefined(p, typeof(CDCPrimaryKeyAttribute)));

            if (keyProps != null)
                foreach (PropertyInfo pi in keyProps)
                    result += pi.GetValue(entry).ToString() + "_";

            if (!string.IsNullOrWhiteSpace(result))
                result = result.Remove(result.Length - 1); // remove last _ character

            return result;
        }

        private void getEnumValues(object oldEntry, object newEntry, PropertyInfo oldProp, PropertyInfo newProp, out string oldValue, out string newValue)
        {
            oldValue = string.Empty;
            newValue = string.Empty;

            if (IsEnum(newProp.PropertyType))
            {
                newValue = newProp.GetValue(newEntry)?.ToString();

                if (IsEnum(oldProp.PropertyType))
                    oldValue = oldProp.GetValue(oldEntry)?.ToString();
                else if (oldProp.GetValue(oldEntry) != null)
                {
                    if (Nullable.GetUnderlyingType(newProp.PropertyType) == null)
                        oldValue = Enum.Parse(newProp.PropertyType, oldProp.GetValue(oldEntry)?.ToString()).ToString();
                    else
                        oldValue = Enum.Parse(Nullable.GetUnderlyingType(newProp.PropertyType), oldProp.GetValue(oldEntry)?.ToString()).ToString();
                }
            }
            else
            {
                newValue = newProp.GetValue(newEntry)?.ToString();
                oldValue = oldProp.GetValue(oldEntry)?.ToString();
            }
        }

        private void CaptureAllCollection(System.Collections.ICollection entry, ChangeType changeType, string mask,
            ChangeDataCaptureList CDCs, string parentName, string mainKey, string propertyName, DateTime dateChanged)
        {
            bool? containsSimpleType = null;

            //Test the item types of Enumerable oldEntry
            foreach (var item in entry)
            {
                containsSimpleType = IsEnum(item.GetType()) || IsSimple(item.GetType());
                break;
            }

            if (containsSimpleType == null)
                return;


            object oldEntry = null;
            object newEntry = null;

            switch (changeType)
            {
                case ChangeType.Assign:
                        newEntry = entry;
                        break;

                case ChangeType.Remove:
                    oldEntry = entry;
                    break;
            }

            if (containsSimpleType == true)
            {
                foreach (var item in entry)
                {
                    CDCs.Add(new ChangeDataCapture(mask)
                    {
                        ChangeType = changeType,
                        PrimaryKey = mainKey,
                        DateChanged = dateChanged,
                        ClassName = parentName,
                        PropertyName = propertyName,
                        OldValue = item?.ToString(),
                        NewValue = string.Empty,
                        OldEntry = oldEntry,
                        NewEntry = newEntry
                    });
                }                
            }
            else if (containsSimpleType == false)
            {
                foreach (var item in entry)
                {
                    if (item == null)
                        continue;

                    object oldV = null;
                    object newV = null;

                    switch (changeType)
                    {
                        case ChangeType.Assign:
                            newV = item;
                            break;

                        case ChangeType.Remove:
                            oldV = item;
                            break;
                    }

                    ChangeDataCaptureList tempCDC = GetChanges(oldV, newV, dateChanged, null, null, parentName, mainKey);
                    if (tempCDC != null)
                        CDCs.AddRange(tempCDC);
                }
            }
        }

        private void CaptureAllPoperties(object entry, ChangeType changeType, ChangeDataCaptureList CDCs,
            Dictionary<String,String> fieldList, List<string> keyFields, string parentName, string mainKey, string classTitle, DateTime dateChanged)
        {
            if (entry == null || CDCs == null)
                return;

            var _type = entry.GetType();
            //var _properties = _type.GetProperties();
            var _properties = _type.GetProperties().Where(pr => pr.GetCustomAttributes(typeof(CDCFieldAttribute), false).Any() 
                                                            || ((fieldList != null) && fieldList.ContainsKey(pr.Name)));
            //var dateChanged = DateTime.Now;

            //var key = _type.GetProperties().Where(x => Attribute.IsDefined(x, typeof(CDCPrimaryKeyAttribute)) || (x.Name == keyField)).FirstOrDefault()?.GetValue(entry)?.ToString();
            var key = GetKeyFieldsValue(entry, keyFields);
            var primaryKey = (string.IsNullOrWhiteSpace(mainKey) ? string.Empty : mainKey) +
                             (string.IsNullOrWhiteSpace(mainKey) || string.IsNullOrWhiteSpace(key) ? string.Empty : ",") +
                             (string.IsNullOrWhiteSpace(key) ? string.Empty : key);

            //var className = (string.IsNullOrWhiteSpace(parentName) ? string.Empty : parentName + ".") + entry.GetType().Name;
            string _classTitle = string.IsNullOrWhiteSpace(classTitle) ? (entry.GetType().GetCustomAttribute(typeof(CDCClassTitleAttribute)) as CDCClassTitleAttribute)?.Title : classTitle;
            var className = string.IsNullOrWhiteSpace(parentName) ? (string.IsNullOrWhiteSpace(_classTitle) ? entry.GetType().Name : _classTitle) : parentName;

            //loop all properties
            foreach (var _property in _properties)
            {
                string mask = (_property.GetCustomAttribute(typeof(CDCMaskValueAttribute)) as CDCMaskValueAttribute)?.Mask;

                if (IsCollection(_property))
                {
                    string pName = Attribute.IsDefined(_property, typeof(CDCFieldAttribute)) ?
                                      (_property.GetCustomAttribute(typeof(CDCFieldAttribute)) as CDCFieldAttribute)?.Title :
                                      ((fieldList != null) && fieldList.ContainsKey(_property.Name)) ? fieldList[_property.Name] : _property.Name;

                    CaptureAllCollection((System.Collections.ICollection)_property.GetValue(entry, null), changeType, mask, CDCs, className, primaryKey, pName, dateChanged);
                    continue;
                }

                //if the property is a complex object so go for recursive process
                if ((!IsEnum(_property.PropertyType)) &&
                    (!IsSimple(_property.PropertyType)))
                {
                    CaptureAllPoperties(_property.GetValue(entry), changeType, CDCs, fieldList, keyFields, className, primaryKey, classTitle, dateChanged);
                    continue;
                }

                var oldV = string.Empty;
                var newV = string.Empty;
                object oldEntry = null;
                object newEntry = null;
                switch (changeType)
                {
                    case ChangeType.Assign:
                        newEntry = entry;
                        newV = _property.GetValue(entry)?.ToString();
                        break;

                    case ChangeType.Remove:
                        oldV = _property.GetValue(entry)?.ToString();
                        oldEntry = entry;
                        break;
                }

                string propName = Attribute.IsDefined(_property, typeof(CDCFieldAttribute)) ?
                                  (_property.GetCustomAttribute(typeof(CDCFieldAttribute)) as CDCFieldAttribute)?.Title :
                                  ((fieldList != null) && fieldList.ContainsKey(_property.Name)) ? fieldList[_property.Name] : _property.Name; 

                CDCs.Add(new ChangeDataCapture(mask)
                {
                    ChangeType = changeType,
                    PrimaryKey = primaryKey,
                    DateChanged = dateChanged,
                    ClassName = className,
                    PropertyName = propName,
                    OldValue = oldV,
                    NewValue = newV,
                    OldEntry = oldEntry,
                    NewEntry = newEntry
                });
            }
        }

        private void CaptureNewAssignment(object entry, ChangeDataCaptureList CDCs, 
            Dictionary<String,String> fieldList, List<string> keyFields, string parentName, string mainKey, string classTitle,  DateTime dateChanged)
        {
            CaptureAllPoperties(entry, ChangeType.Assign, CDCs, fieldList, keyFields, parentName, mainKey, classTitle, dateChanged);
        }

        private void CaptureNewDeletion(object entry, ChangeDataCaptureList CDCs,
            Dictionary<String,String> fieldList, List<string> keyFields, string parentName, string mainKey, string classTitle, DateTime dateChanged)
        {
            CaptureAllPoperties(entry, ChangeType.Remove, CDCs, fieldList, keyFields, parentName, mainKey, classTitle, dateChanged);
        }

        private void CDCforSimpleTypeCollection(ICollection oldEntry, ICollection newEntry, 
                                ChangeDataCaptureList CDCs, string parentName, string mainKey, string propertyName, DateTime dateChanged)
        {
            foreach (var itemO in oldEntry)
            {
                bool found = false;
                foreach (var itemN in newEntry)
                    if ((itemO != null) && (itemN != null) && (itemO.ToString().Equals(itemN.ToString())))
                    {
                        found = true;
                        break;
                    }
                if (!found)
                {
                    string mask = (newEntry.GetType().GetCustomAttribute(typeof(CDCMaskValueAttribute)) as CDCMaskValueAttribute)?.Mask;

                    CDCs.Add(new ChangeDataCapture(mask)
                    {
                        ChangeType = ChangeType.Remove,
                        PrimaryKey = mainKey,
                        DateChanged = dateChanged,
                        ClassName = parentName,
                        PropertyName = propertyName,
                        OldValue = itemO?.ToString(),
                        NewValue = string.Empty,
                        OldEntry = oldEntry,
                        NewEntry = newEntry
                    });
                }
            }

            foreach (var itemN in newEntry)
            {
                bool found = false;
                foreach (var itemO in oldEntry)
                    if ((itemO != null) && (itemN != null) && (itemO.ToString().Equals(itemN.ToString())))
                    {
                        found = true;
                        break;
                    }
                if (!found)
                {

                    string mask = (newEntry.GetType().GetCustomAttribute(typeof(CDCMaskValueAttribute)) as CDCMaskValueAttribute)?.Mask;
                    
                    CDCs.Add(new ChangeDataCapture(mask)
                    {
                        ChangeType = ChangeType.Assign,
                        PrimaryKey = mainKey,
                        DateChanged = dateChanged,
                        ClassName = parentName,
                        PropertyName = propertyName,
                        OldValue = string.Empty,
                        NewValue = itemN?.ToString(),
                        OldEntry = oldEntry,
                        NewEntry = newEntry
                    });
                }
            }
        }

        private void CDCforComplexTypeCollection(ICollection oldEntry, ICollection newEntry, ChangeDataCaptureList CDCs,
            Dictionary<String,String> fieldList, List<string> keyFields, string parentName, string mainKey, string classTitle, DateTime dateChanged)
        {
            //do not comare more than 10 entries
           // if ((oldEntry.Count > 10) || (newEntry.Count > 10))
             //   return;

            //loop through oldEntry collection to find out updates and deletions
            foreach (var itemO in oldEntry)
            {
                if (itemO == null)
                    continue;

                //extract key value
                //var oldProperties = itemO.GetType().GetProperties();
                //var oldKey = oldProperties.Where(x => Attribute.IsDefined(x, typeof(CDCPrimaryKeyAttribute)) || (x.Name == keyField)).FirstOrDefault()?.GetValue(itemO)?.ToString();
                var oldKey = GetKeyFieldsValue(itemO, keyFields);
                //key value is necessary for compare process, so compare process should be terminated if there is no key defiend for objects in collection 
                if (string.IsNullOrWhiteSpace(oldKey))
                    return;

                object newData = null;
                //loop through newEntry items to find the item with the same key of item from oldEntry
                foreach (var itemN in newEntry)
                {
                    if (itemN == null)
                        continue;

                    //extract key value
                    //var newProperties = itemN.GetType().GetProperties();
                    //var newKey = newProperties.Where(x => Attribute.IsDefined(x, typeof(CDCPrimaryKeyAttribute)) || (x.Name == keyField)).FirstOrDefault()?.GetValue(itemN)?.ToString();
                    var newKey = GetKeyFieldsValue(itemN, keyFields);
                    //key value is necessary for compare process, so compare process should be terminated if there is no key defiend for objects in collection 
                    if (string.IsNullOrWhiteSpace(newKey))
                        return;

                    //if the item exists in newEntry collection
                    if (oldKey == newKey)
                    {
                        newData = itemN;
                        break;
                    }
                }

                //compare items for changes
                ChangeDataCaptureList tempCDC = GetChanges(itemO, newData, dateChanged, fieldList, keyFields, parentName, mainKey, classTitle);
                if (tempCDC != null)
                    CDCs.AddRange(tempCDC);
            }

            //loop through newEntry collection to find out new Assignments
            foreach (var itemN in newEntry)
            {
                if (itemN == null)
                    continue;

                //var newProperties = itemN.GetType().GetProperties();
                //var newKey = newProperties.Where(x => Attribute.IsDefined(x, typeof(CDCPrimaryKeyAttribute)) || (x.Name == keyField)).FirstOrDefault()?.GetValue(itemN)?.ToString();
                var newKey = GetKeyFieldsValue(itemN, keyFields);
                if (string.IsNullOrWhiteSpace(newKey))
                    return;

                object oldData = null;
                bool found = false;
                foreach (var itemO in oldEntry)
                {
                    if (itemO == null)
                        continue;

                    //var oldProperties = itemO.GetType().GetProperties();
                    //var oldKey = oldProperties.Where(x => Attribute.IsDefined(x, typeof(CDCPrimaryKeyAttribute)) || (x.Name == keyField)).FirstOrDefault()?.GetValue(itemO)?.ToString();
                    var oldKey = GetKeyFieldsValue(itemO, keyFields);
                    if (string.IsNullOrWhiteSpace(oldKey))
                        return;

                    if (oldKey == newKey)
                    {
                        found = true;
                        break;
                    }
                }

                //if item not found in old list so this will be assumed as a new assignment
                if (!found)
                {
                    ChangeDataCaptureList tempCDC = GetChanges(oldData, itemN, dateChanged, fieldList, keyFields, parentName, mainKey, classTitle);
                    if (tempCDC != null)
                        CDCs.AddRange(tempCDC);
                }
            }
        }
        
        private void CDCforCollection(System.Collections.ICollection oldEntry, System.Collections.ICollection newEntry, ChangeDataCaptureList CDCs,
                                Dictionary<String, String> fieldList, List<string> keyFields, string propertyName, string parentName, string mainKey, string classTitle, DateTime dateChanged)
        {
            if ((oldEntry == null) || (newEntry == null))
                return;

            bool? containsSimpleType = null;

            //Test the item types of Enumerable oldEntry
            foreach (var item in oldEntry)
            {
                containsSimpleType = IsEnum(item.GetType()) || IsSimple(item.GetType());
                break;
            }

            //Test the item types of Enumerable newEntry
            foreach (var item in newEntry)
            {
                containsSimpleType = IsEnum(item.GetType()) || IsSimple(item.GetType());
                break;
            }

            if (containsSimpleType == null)
                return;

            if (containsSimpleType == true)
                CDCforSimpleTypeCollection(oldEntry, newEntry, CDCs, parentName, mainKey,
                    (string.IsNullOrWhiteSpace(propertyName) ? classTitle : propertyName), dateChanged);
            else
                CDCforComplexTypeCollection(oldEntry, newEntry, CDCs, fieldList, keyFields, 
                    parentName + "." + (string.IsNullOrWhiteSpace(propertyName) ? classTitle : propertyName), mainKey, classTitle, dateChanged);
        }

        /// <summary>
        /// This method is will compare two objects and returns the Change Data Capture List as result
        /// </summary>
        /// <param name="oldEntry">data model object which contains previous state of data (Could be the DAL object)</param>
        /// <param name="newEntry">data model object which contains new state of data (Could be BLL object)</param>
        /// <param name="dateChanged">Data time of change</param>
        /// <param name="fieldList">Optional fieldList to compare, also fields with CDCFieldAttribute annotation in will be compare</param>
        /// <param name="keyField">Optional key field name, also field with CDCPrimaryKeyAttribute annotation will be used as key field</param>
        /// <param name="parentName">Optional Name of parent or owner class</param>
        /// <param name="mainKey">Optional Key field value of parents or owner objects</param>
        /// <param name="classTitle">Optional preferted title for current object, also class annotation CDCClassTitleAttribute will be used to determain class title</param>
        /// <returns></returns>
        public ChangeDataCaptureList GetChanges(object oldEntry, object newEntry, DateTime dateChanged, 
            Dictionary<String,String> fieldList = null, List<string> keyFields = null,  string parentName = "", string mainKey = "", string classTitle = "")
        {
            ChangeDataCaptureList CDCs = new ChangeDataCaptureList();

            //there is nothing to compare
            if ((oldEntry == null) && (newEntry == null))
                return CDCs;

            //new assigment happend and all properties of newEntry should be captured
            if (oldEntry == null)
            {
                CaptureNewAssignment(newEntry, CDCs, fieldList, keyFields, parentName, mainKey, classTitle, dateChanged);
                return CDCs;
            }

            //deletion happend and all properties of oldEntry should be captured
            if (newEntry == null)
            {
                CaptureNewDeletion(oldEntry, CDCs, fieldList, keyFields, parentName, mainKey, classTitle, dateChanged);
                return CDCs;
            }

            if (IsCollection(oldEntry) || IsCollection(oldEntry))
            {
                CDCforCollection((System.Collections.ICollection)oldEntry, (System.Collections.ICollection)newEntry, CDCs,
                    fieldList, keyFields, "", parentName, mainKey, classTitle, dateChanged);
                return CDCs;
            }


            var oldType = oldEntry.GetType();
            var newType = newEntry.GetType();

            //if(oldType != newType)
            //{
            //    return CDCs; //Types don't match, cannot capture changes
            //}
            
            //DAL
            var oldProperties = oldType.GetProperties();
            //var newProperties = newType.GetProperties();
            //BLL
            var newProperties = newType.GetProperties().Where(pr => pr.GetCustomAttributes(typeof(CDCFieldAttribute), false).Any()
                                                                    || ((fieldList != null) && fieldList.ContainsKey(pr.Name)) );

            //extract key value
            //var key = newType.GetProperties().Where(x => Attribute.IsDefined(x, typeof(CDCPrimaryKeyAttribute)) || (x.Name == keyField)).FirstOrDefault()?.GetValue(newEntry)?.ToString();
            var key = GetKeyFieldsValue(newEntry, keyFields);

            var primaryKey = (string.IsNullOrWhiteSpace(mainKey) ? string.Empty : mainKey) +
                             (string.IsNullOrWhiteSpace(mainKey) || string.IsNullOrWhiteSpace(key) ? string.Empty : ",") +
                             (string.IsNullOrWhiteSpace(key) ? string.Empty : key);

            //var className = (string.IsNullOrWhiteSpace(parentName) ? string.Empty : parentName + ".") + oldEntry.GetType().Name;
            string _classTitle = string.IsNullOrWhiteSpace(classTitle) ? 
                                (newEntry.GetType().GetCustomAttribute(typeof(CDCClassTitleAttribute)) as CDCClassTitleAttribute)?.Title : 
                                classTitle;
            var className = string.IsNullOrWhiteSpace(parentName) ? (string.IsNullOrWhiteSpace(_classTitle) ? newEntry.GetType().Name : _classTitle) : parentName;

            //loop all old properties (DAL object)
            foreach (var oldProperty in oldProperties)
            {
                //find maching property from newEntry (BLL object)
                var matchingProperty =
                    newProperties.Where(x => ( (Attribute.IsDefined(x, typeof(CDCFieldAttribute)) &&
                                                (x.GetCustomAttributes(typeof(CDCFieldAttribute), false)[0] 
                                                                    as CDCFieldAttribute).FieldName == oldProperty.Name) ||
                                                                     ((fieldList != null) && fieldList.ContainsKey(oldProperty.Name))
                                                                    ))
                                                                //&& x.PropertyType == oldProperty.PropertyType)
                                                    .FirstOrDefault();
                //no match find so continue
                if(matchingProperty == null)
                {
                    continue;
                }

                //if (matchingProperty.PropertyType.IsGenericType)
                //{
                //    continue;
                //}

                //if (typeof(System.Collections.IEnumerable).IsAssignableFrom(matchingProperty.PropertyType))
                //{
                //    // ...
                //}

                //if the property is Collection
                if (IsCollection(matchingProperty))
                {
                    CDCforCollection((System.Collections.ICollection)oldProperty.GetValue(oldEntry, null), (System.Collections.ICollection)matchingProperty.GetValue(newEntry, null), 
                         CDCs, fieldList, keyFields, oldProperty.Name, className, primaryKey, classTitle, dateChanged);
                    continue;
                }

                //if the property is a complex object so go for recursive process
                if ((!IsEnum(matchingProperty.PropertyType)) && 
                    (!IsSimple(matchingProperty.PropertyType)))
                {
                    ChangeDataCaptureList nestedCDCs = GetChanges(oldProperty.GetValue(oldEntry), matchingProperty.GetValue(newEntry), dateChanged, 
                        fieldList, keyFields, className + "." + matchingProperty.Name, primaryKey, "");
                    CDCs.AddRange(nestedCDCs);

                    continue;
                }

                //extract old and new values
                string oldValue = string.Empty;
                string newValue = string.Empty;
                if (IsEnum(matchingProperty.PropertyType))
                    getEnumValues(oldEntry, newEntry, oldProperty, matchingProperty, out oldValue, out newValue);
                else
                {
                    oldValue = oldProperty.GetValue(oldEntry)?.ToString();
                    newValue = matchingProperty.GetValue(newEntry)?.ToString();
                }

                string mask = (matchingProperty.GetCustomAttribute(typeof(CDCMaskValueAttribute)) as CDCMaskValueAttribute)?.Mask;
                string propName = Attribute.IsDefined(matchingProperty, typeof(CDCFieldAttribute)) ?
                                 (matchingProperty.GetCustomAttribute(typeof(CDCFieldAttribute)) as CDCFieldAttribute)?.Title :
                                 ((fieldList != null) && fieldList.ContainsKey(matchingProperty.Name)) ? fieldList[matchingProperty.Name] : matchingProperty.Name;
                                
                //compare old and new values and capture as a new change if there is difference
                if (oldValue != newValue)
                {
                    CDCs.Add(new ChangeDataCapture(mask)
                    {
                        ChangeType = ChangeType.Update,
                        PrimaryKey = primaryKey,
                        DateChanged = dateChanged,
                        ClassName = className,
                        PropertyName = propName,
                        OldValue = oldValue,
                        NewValue = newValue,
                        OldEntry = oldEntry,
                        NewEntry = newEntry
                    });
                }
            }

            return CDCs;
        }
    }
}