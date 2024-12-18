﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows.Forms;

namespace WmiExplorer.Classes
{
    internal class WmiClass
    {
        public List<ListViewItem> Instances = new List<ListViewItem>();
        private string _description;
        private string _displayName;
        private string _enumerationStatus;
        private DateTime _enumTime;
        private TimeSpan _enumTimeElapsed;
        private bool _hasLazyProperties;
        private string _instanceFilterQuick;
        private string _namespacePath;
        private string _path;
        private string _relativePath;

        public WmiClass(ManagementClass actualClass)
        {
            Class = actualClass;
        }

        public ManagementClass Class { get; set; }

        public string Description
        {
            get
            {
                try
                {
                    foreach (QualifierData q in from QualifierData q in Class.Qualifiers where q.Name.Equals("Description", StringComparison.CurrentCultureIgnoreCase) select q)
                    {
                        _description = Class.GetQualifierValue("Description").ToString();
                    }
                }
                catch (ManagementException ex)
                {
                    if ((ex.ErrorCode).ToString() == "NotFound")
                        _description = String.Empty;
                    else
                        _description = "Error getting Class Description";
                }

                return _description;
            }
        }

        public string DisplayName
        {
            get
            {
                if (_displayName == null)
                    _displayName = Class.ClassPath.ClassName;

                return _displayName;
            }
            set { _displayName = value; }
        }

        public string EnumerationStatus
        {
            get
            {
                if (String.IsNullOrEmpty(_enumerationStatus))
                    _enumerationStatus = "NoError";

                return _enumerationStatus;
            }
            set { _enumerationStatus = value; }
        }

        public DateTime EnumTime
        {
            get { return _enumTime; }
            set { _enumTime = value; }
        }

        public TimeSpan EnumTimeElapsed
        {
            get { return _enumTimeElapsed; }
            set { _enumTimeElapsed = value; }
        }

        public bool HasLazyProperties
        {
            get
            {
                foreach (PropertyData pd in Class.Properties)
                {
                    foreach (QualifierData qd in pd.Qualifiers)
                    {
                        if (qd.Name.Equals("lazy", StringComparison.CurrentCultureIgnoreCase))
                        {
                            _hasLazyProperties = true;
                            return _hasLazyProperties;
                        }
                    }
                }

                return _hasLazyProperties;
            }
        }

        public int InstanceCount { get; set; }

        public string InstanceFilterQuick
        {
            get
            {
                if (_instanceFilterQuick == null)
                    _instanceFilterQuick = String.Empty;

                return _instanceFilterQuick;
            }
            set { _instanceFilterQuick = value; }
        }

        // Indicates if class has instances enumerated
        public bool IsEnumerated { get; set; }

        // Indicates if class is currently being enumerated
        public bool IsEnumerating { get; set; }

        // Indicates if cancellation is requested for this class.
        public bool IsEnumerationCancelled { get; set; }

        // Indicates if class has instances partially enumerated. This can occur if user cancels operation.
        public bool IsPartiallyEnumerated { get; set; }

        public string NamespacePath
        {
            get
            {
                if (_namespacePath == null)
                    _namespacePath = Class.Scope.Path.Path;

                return _namespacePath;
            }
        }

        public string Path
        {
            get
            {
                if (_path == null)
                    _path = Class.Path.Path;
                return _path;
            }
        }

        public string RelativePath
        {
            get
            {
                if (_relativePath == null)
                    _relativePath = Class.Path.RelativePath;

                return _relativePath;
            }
        }

        public void AddInstance(ListViewItem listItemInstance)
        {
            Instances.Add(listItemInstance);
        }

        public string GetClassMof(bool bAmended = false)
        {
            Class.Options.UseAmendedQualifiers = bAmended;
            Class.Get();
            return Class.GetText(TextFormat.Mof).Replace("\n", "\r\n");
        }

        public string GetClassORMi()
        {
            Class.Get();

            var classPath = Class.ClassPath;

            var sb = new StringBuilder();
            sb.AppendLine($"[WMIClass(Name = \"{classPath.ClassName}\", Namespace = @\"{classPath.NamespacePath}\")]");
            sb.AppendLine($"public class {classPath.ClassName.Replace("Win32_", string.Empty)} : WMIInstance");
            sb.AppendLine("{");
            foreach (var property in Class.Properties)
            {
                var type = ManagementBaseObjectW.GetTypeFor(property.Type, false);
                sb.AppendLine($"\tpublic {ConvertType(type, property.IsArray)} {property.Name} {{ get; set; }}");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static readonly IDictionary<string, string> TypeKeywords = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "System.Object", "object" },
            { "System.String", "string" },
            { "System.Boolean", "bool" },
            { "System.UInt16", "ushort" },
            { "System.UInt32", "uint" },
            { "System.UInt64", "ulong" },
            { "System.Int16", "short" },
            { "System.Int32", "int" },
            { "System.Int64", "long" },
            { "System.Single", "float"},
            { "System.Double", "double"},
            { "System.Decimal", "decimal"},
            { "System.Byte", "byte"},
            { "System.Char", "char"}
        };

        private static string ConvertType(Type type, bool isArray)
        {
            var result = type.ToString(); 
            return (TypeKeywords.TryGetValue(result, out var keyword) ? keyword : result.Replace("System.", string.Empty)) 
                    + (isArray ? "[]" : string.Empty);
        }

        public void ResetInstances()
        {
            InstanceCount = Instances.Count;
            Instances = new List<ListViewItem>();
        }
    }
}