﻿using System;
using System.Collections.Generic;

namespace EngineNS.Rtti
{
    public class UDummyAttribute : Attribute
    {
    }
    public class MetaParameterAttribute : Attribute
    {
        [Flags]
        public enum EArgumentFilter
        {
            A1 = 1,
            A2 = 1 << 1,
            A3 = 1 << 2,
            A4 = 1 << 3,
            A5 = 1 << 4,
            A6 = 1 << 5,
            A7 = 1 << 6,
            A8 = 1 << 7,
            R = 1 << 23
        }
        public System.Type FilterType;//参数类型为Type的时候，macross用来筛选可选择类型
        public System.Type[] TypeList;
        public EArgumentFilter ConvertOutArguments;//最高位是控制返回值，其他是参数的顺序 
        
    }
    public class GenMetaClassAttribute : Attribute
    {
        public bool IsOverrideBitset = true;
    }
    public class GenMetaAttribute : Attribute
    {//使用在Field上，系统自动产生Meta标志过的Property
        public MetaAttribute.EMetaFlags Flags = 0;
    }
    public class UStructAttrubte : Attribute
    {
        public int ReadSize;
    }
    public class MetaAttribute : Attribute
    {
        [Flags]
        public enum EMetaFlags : uint
        {
            NoMacrossUseable = 1,//可使用在类型，成员变量，成员属性，函数上，不允许Macross使用
            NoMacrossCreate = (1 << 1),//只可使用在类型上，不允许Macross申明，也就是不能在macross中CreateInstance
            NoMacrossInherit = (1 << 2),//只可使用在类型上，不允许Macross派生
            NoMacrossOverride = (1 << 3),//只可使用在虚函数上，不允许Macross重载

            MacrossReadOnly = (1 << 4),//只可使用在成员变量，成员属性上，该属性对Macross只读
            MacrossDeclareable = (1<<5),//可在Macross中申明实例

            DiscardWhenCooked = (1 << 6),//在cook资源中不序列化
            DiscardWhenRPC = (1 << 7),//在做RPC的时候不序列化

            ManualMarshal = (1 << 8),
        }
        public int Order = 0;
        public EMetaFlags Flags = 0;
        public bool IsReadOnly
        {
            get
            {
                return (Flags & EMetaFlags.MacrossReadOnly) != 0;
            }
        }
        public bool IsMacrossDeclareable
        {
            get
            {
                return (Flags & EMetaFlags.MacrossDeclareable) != 0;
            }
        }
    }
    public class UClassMeta
    {
        public MetaAttribute MetaAttribute { get; private set; }
        private List<UClassMeta> mSubClasses = null;
        public void CopyObjectMetaField(object tar, object src)
        {
            if(tar is System.Collections.IList && src.GetType() == tar.GetType())
            {
                var Tarlst = tar as System.Collections.IList;
                var Srclst = src as System.Collections.IList;
                Tarlst.Clear();
                for (int i = 0; i < Srclst.Count; i++)
                {
                    Tarlst.Add(Srclst[i]);
                }
                return;
            }
            else if (tar is System.Collections.IDictionary && src.GetType() == tar.GetType())
            {
                var Tarlst = tar as System.Collections.IDictionary;
                var Srclst = src as System.Collections.IDictionary;
                Tarlst.Clear();
                var i = Srclst.GetEnumerator();
                while(i.MoveNext())
                {
                    Tarlst.Add(i.Key, i.Value);
                }
                return;
            }

            var tarType = tar.GetType();
            var srcType = src.GetType();
            foreach (var i in CurrentVersion.Propertys)
            {
                var tarProp = tarType.GetProperty(i.PropertyName);
                if (tarProp == null)
                    continue;
                var srcProp = srcType.GetProperty(i.PropertyName);
                if (srcProp == null)
                    continue;
                if (tarProp.PropertyType == srcProp.PropertyType && tarProp.CanWrite)
                {
                    var v = srcProp.GetValue(src);
                    tarProp.SetValue(tar, v);
                }
            }
        }
        public struct UMetaFieldValue
        {
            public string Name;
            public Rtti.UTypeDesc FieldType;
            public object Value;
        }
        public void CopyObjectMetaField(List<UMetaFieldValue> tar, object src)
        {
            var srcType = src.GetType();
            foreach (var i in CurrentVersion.Propertys)
            {
                var srcProp = srcType.GetProperty(i.PropertyName);
                if (srcProp == null)
                    continue;

                var fld = new UMetaFieldValue();
                fld.Name = i.PropertyName;
                fld.FieldType = Rtti.UTypeDesc.TypeOf(srcProp.PropertyType);
                fld.Value = srcProp.GetValue(src);
                var lst = fld.Value as System.Collections.IList;
                var dict = fld.Value as System.Collections.IDictionary;
                if (lst != null)
                {
                    var tarlst = Rtti.UTypeDescManager.CreateInstance(fld.FieldType) as System.Collections.IList;
                    for (int j = 0; j < lst.Count; j++)
                    {
                        tarlst.Add(lst[j]);
                    }
                    fld.Value = tarlst;
                }
                if (dict != null)
                {
                    var tardict = Rtti.UTypeDescManager.CreateInstance(fld.FieldType) as System.Collections.IDictionary;
                    var j = dict.GetEnumerator();
                    while (j.MoveNext())
                    {
                        tardict.Add(j.Key, j.Value);
                    }
                    fld.Value = tardict;
                }
            }
        }
        public void SetObjectMetaField(object tar, List<UMetaFieldValue> fldValues)
        {
            var srcType = tar.GetType();
            foreach(var i in fldValues)
            {
                var srcProp = srcType.GetProperty(i.Name);
                if (srcProp == null)
                    continue;

                if (srcProp.PropertyType != i.FieldType.SystemType)
                    continue;

                var old = srcProp.GetValue(tar);
                if (old == i.Value)
                    continue;

                srcProp.SetValue(tar, i.Value);
            }
        }
        public List<UClassMeta> SubClasses
        {
            get
            {
                if (mSubClasses != null)
                    return mSubClasses;

                mSubClasses = new List<UClassMeta>();
                foreach (var i in Rtti.UTypeDescManager.Instance.Services)
                {
                    foreach (var j in i.Value.Types)
                    {
                        if(j.Value.IsSubclassOf(this.ClassType))
                        {
                            var klsMeta = Rtti.UClassMetaManager.Instance.GetMeta(j.Value);
                            if (klsMeta == null)
                                continue;
                            if (mSubClasses.Contains(klsMeta) == false)
                                mSubClasses.Add(klsMeta);
                        }
                    }
                }
                return mSubClasses;
            }
        }
        public bool CanConvertTo(UClassMeta tarKls)
        {
            if (tarKls == null)
                return false;
            if (ClassType == tarKls.ClassType)
                return true;
            return ClassType.IsSubclassOf(tarKls.ClassType);
        }
        public bool CanConvertFrom(UClassMeta srcKls)
        {
            if (srcKls == null)
                return false;
            if (ClassType == srcKls.ClassType)
                return true;
            return srcKls.ClassType.IsSubclassOf(ClassType);
        }
        public UClassMeta(UTypeDesc t)
        {
            ClassType = t;
        }
        public UTypeDesc ClassType
        {
            get;
            set;
        }
        public string ClassMetaName
        {
            get
            {
                return UTypeDesc.TypeStr(ClassType);
            }
        }
        public string MetaDirectoryName
        {
            get
            {
                return Hash160.CreateHash160(ClassMetaName).ToString();
            }
        }
        public Dictionary<uint, UMetaVersion> MetaVersions
        {
            get;
        } = new Dictionary<uint, UMetaVersion>();
        UMetaVersion mCurrentVersion;
        public UMetaVersion CurrentVersion
        {
            get { return mCurrentVersion; }
        }
        public UMetaVersion GetMetaVersion(uint hash)
        {
            UMetaVersion result;
            if (MetaVersions.TryGetValue(hash, out result))
            {
                return result;
            }

            //var myXmlDoc = new System.Xml.XmlDocument();
            //var file = Path + "\\" + hash.ToString() + ".metadata";
            //file = file.Replace("/", "\\");
            //myXmlDoc.Load(file);
            //var ver = new UMetaVersion(this);
            //ver.LoadVersion(hash, myXmlDoc.LastChild);
            //MetaVersions[ver.MetaHash] = ver;
            return null;
        }
        internal string Path { get; set; }
        public bool LoadClass(string path)
        {
            try
            {
                Path = path;
                var versions = EngineNS.IO.FileManager.GetFiles(path, "*.metadata", false);
                foreach (var i in versions)
                {
                    var filename = EngineNS.IO.FileManager.GetPureName(i);
                    var myXmlDoc = new System.Xml.XmlDocument();
                    myXmlDoc.Load(i);
                    var ver = new UMetaVersion(this);
                    ver.LoadVersion(System.Convert.ToUInt32(filename), myXmlDoc.LastChild);
                    MetaVersions[ver.MetaHash] = ver;
                }
            }
            catch (System.Exception)
            {
                Profiler.Log.WriteLine(Profiler.ELogTag.Fatal, "metadata", "meta load field in " + path);
                return false;
            }
            return true;
        }
        public void SaveClass()
        {
            //var typeStr = ClassType.TypeString;
            //var typeDesc = UTypeDescManager.Instance.GetTypeDescFromString(typeStr);
            //var typeDesc = ClassType;
            //var rootDir = IO.FileManager.ERootDir.Engine;
            //if (typeDesc.Assembly.IsGameModule)
            //{
            //    rootDir = IO.FileManager.ERootDir.Game;
            //}
            //var metaRoot = UEngine.Instance.FileManager.GetPath(rootDir, IO.FileManager.ESystemDir.MetaData);
            //var path = EngineNS.IO.FileManager.CombinePath(metaRoot, typeDesc.Assembly.Service);
            //path = EngineNS.IO.FileManager.CombinePath(path, typeDesc.Assembly.Name);
            var tmpPath = Path;// EngineNS.IO.FileManager.CombinePath(Path, $"{MetaDirectoryName}");
            if (!EngineNS.IO.FileManager.DirectoryExists(tmpPath))
            {
                EngineNS.IO.FileManager.CreateDirectory(tmpPath);
            }
            var txtFilepath = EngineNS.IO.FileManager.CombinePath(tmpPath, $"typedesc.txt");
            EngineNS.IO.FileManager.WriteAllText(txtFilepath, ClassType.TypeString);
        }
        public UMetaVersion BuildCurrentVersion()
        {
            var type = ClassType;
            {
                var attrs = type.GetCustomAttributes(typeof(MetaAttribute), true);
                if (attrs.Length == 1)
                {
                    MetaAttribute = attrs[0] as MetaAttribute;
                }
            }
            var result = new UMetaVersion(this);
            var props = type.SystemType.GetProperties();
            foreach(var i in props)
            {
                var attrs = i.GetCustomAttributes(typeof(MetaAttribute), true);
                if (attrs.Length == 0)
                    continue;
                var meta = attrs[0] as MetaAttribute;
                var fd = new PropertyMeta();
                fd.Meta = meta;
                fd.PropInfo = i;
                fd.Order = meta.Order;
                fd.FieldType = Rtti.UTypeDesc.TypeOf(i.PropertyType);
                fd.PropertyName = i.Name;
                fd.FieldTypeStr = UTypeDescManager.Instance.GetTypeStringFromType(i.PropertyType);
                fd.Build();
                result.Propertys.Add(fd);
            }
            result.Propertys.Sort();
            string hashString = "";
            foreach(var i in result.Propertys)
            {
                hashString += i.ToString();
            }
            result.MetaHash = EngineNS.UniHash.APHash(hashString);
            if (MetaAttribute != null || type.GetInterface(nameof(IO.ISerializer)) != null)
            {
                if (MetaVersions.TryGetValue(result.MetaHash, out mCurrentVersion) == false)
                {
                    result.SaveVersion();
                }
                MetaVersions[result.MetaHash] = result;
            }
            mCurrentVersion = result;

            return result;
        }
        #region MacrossMethod
        public class MethodMeta
        {
            public class ParamMeta
            {
                public MetaParameterAttribute Meta;
                private System.Reflection.ParameterInfo ParamInfo;
                public System.Reflection.ParameterInfo GetParamInfo()
                {
                    return ParamInfo;
                }
                public Rtti.UTypeDesc ParameterType;
                public bool IsParamArray = false;
                public bool IsDelegate = false;
                public string Name;
                public Bricks.CodeBuilder.EMethodArgumentAttribute ArgumentAttribute = Bricks.CodeBuilder.EMethodArgumentAttribute.Default;
                public object DefaultValue = null;

                public bool HasDefaultValue
                {
                    get => DefaultValue != null;
                }

                public void Init(System.Reflection.ParameterInfo info)
                {
                    ParamInfo = info;
                    Name = info.Name;
                    var att = info.GetCustomAttributes(typeof(System.ParamArrayAttribute), true);
                    IsParamArray = (att.Length > 0);
                    IsDelegate = typeof(Delegate).IsAssignableFrom(info.ParameterType);
                    if (info.IsOut)
                    {
                        ArgumentAttribute = Bricks.CodeBuilder.EMethodArgumentAttribute.Out;
                        ParameterType = Rtti.UTypeDesc.TypeOf(info.ParameterType.GetElementType());
                    }
                    else if (info.IsIn)
                    {
                        ArgumentAttribute = Bricks.CodeBuilder.EMethodArgumentAttribute.In;
                        ParameterType = Rtti.UTypeDesc.TypeOf(info.ParameterType.GetElementType());
                    }
                    else if (info.ParameterType.IsByRef)
                    {
                        ArgumentAttribute = Bricks.CodeBuilder.EMethodArgumentAttribute.Ref;
                        ParameterType = Rtti.UTypeDesc.TypeOf(info.ParameterType.GetElementType());
                    }
                    else
                        ParameterType = Rtti.UTypeDesc.TypeOf(info.ParameterType);
                    DefaultValue = info.DefaultValue;
                }
                public bool IsOut
                {
                    get => (ArgumentAttribute == Bricks.CodeBuilder.EMethodArgumentAttribute.Out);
                }
                public bool IsIn
                {
                    get => (ArgumentAttribute == Bricks.CodeBuilder.EMethodArgumentAttribute.In);
                }
                public bool IsRef
                {
                    get => (ArgumentAttribute == Bricks.CodeBuilder.EMethodArgumentAttribute.Ref);
                }
            }
            public MetaAttribute Meta;
            private System.Reflection.MethodInfo Method;
            public ParamMeta[] Parameters;
            public string MethodName;
            public Rtti.UTypeDesc ReturnType;
            public Rtti.UTypeDesc DeclaringType;
            public bool IsStatic = false;
            public bool IsVirtual = false;
            public List<object> CustomAttributes;
            public List<object> InheritCustomAttributes;

            private string DeclarName;
            public System.Reflection.MethodInfo GetMethod()
            {
                return Method;
            }
            public void Init(System.Reflection.MethodInfo method)
            {
                Method = method;
                MethodName = Method.Name;
                IsStatic = Method.IsStatic;
                IsVirtual = Method.IsVirtual;
                ReturnType = Rtti.UTypeDesc.TypeOf(Method.ReturnType);
                DeclaringType = Rtti.UTypeDesc.TypeOf(Method.DeclaringType);

                CustomAttributes = new List<object>(method.CustomAttributes);
                var atts = method.GetCustomAttributes(true);
                if(atts.Length > 0)
                {
                    InheritCustomAttributes = new List<object>(atts.Length - CustomAttributes.Count);
                    for(int i=0; i<atts.Length; i++)
                    {
                        if (CustomAttributes.Contains(atts[i]))
                            continue;
                        InheritCustomAttributes.Add(atts[i]);
                    }
                }

                var parameters = Method.GetParameters();
                Parameters = new ParamMeta[parameters.Length];
                DeclarName = $"{Method.ReturnType.FullName} {Method.DeclaringType.FullName}.{Method.Name}(";                
                for (int i = 0; i < parameters.Length; i++)
                {
                    Parameters[i] = new ParamMeta();
                    Parameters[i].Init(parameters[i]);
                    Parameters[i].Meta = GetParameterMeta(i);
                    DeclarName += $"{parameters[i].ParameterType.FullName} {parameters[i].Name}";
                    if (i < parameters.Length - 1)
                        DeclarName += ",";
                }
                DeclarName += ')';
            }
            public ParamMeta[] GetParameters()
            {
                return Parameters;
            }
            public ParamMeta GetParameter(int index)
            {
                return Parameters[index];
            }
            public ParamMeta FindParameter(string name)
            {
                foreach(var i in Parameters)
                {
                    if (i.Name == name)
                        return i;
                }
                return null;
            }
            private MetaParameterAttribute GetParameterMeta(int index)
            {
                var param = Method.GetParameters()[index];
                var attrs = param.GetCustomAttributes(typeof(MetaParameterAttribute), false);
                if (attrs.Length == 0)
                    return null;
                //return attrs[index] as MetaParameterAttribute;
                return attrs[0] as MetaParameterAttribute;
            }
            public override string ToString()
            {
                return DeclarName;
            }
            public string GetMethodDeclareString()
            {
                var result = $"{Method.ReturnType.FullName} {Method.Name}(";
                var parameters = Method.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    result += $"{parameters[i].ParameterType.FullName}";
                    if (i < parameters.Length - 1)
                        result += ",";
                }
                result += ')';
                return result;
            }
            public object[] GetCustomAttributes(Type type, bool inherit)
            {
                List<object> retVal;
                if (inherit)
                {
                    retVal = new List<object>(CustomAttributes.Count + InheritCustomAttributes.Count);
                    for(int i=0; i<CustomAttributes.Count; i++)
                    {
                        //var data = (CustomAttributes[i] as System.Reflection.CustomAttributeData);
                        //if (data != null)
                        //{
                        //    if (data.AttributeType == type)
                        //        retVal.Add(CustomAttributes[i]);
                        //}
                        //else
                        //{

                        //}
                        if (CustomAttributes[i].GetType() == type)
                            retVal.Add(CustomAttributes[i]);
                    }
                    for(int i=0; i<InheritCustomAttributes.Count; i++)
                    {
                        //var data = (InheritCustomAttributes[i] as System.Reflection.CustomAttributeData);
                        //if (data != null)
                        //{
                        //    if (data.AttributeType == type)
                        //        retVal.Add(InheritCustomAttributes[i]);
                        //}
                        //else
                        //{
                        //    if (InheritCustomAttributes[i].GetType() == type)
                        //        retVal.Add(InheritCustomAttributes[i]);
                        //}
                        if (InheritCustomAttributes[i].GetType() == type)
                            retVal.Add(InheritCustomAttributes[i]);
                    }
                }
                else
                {
                    retVal = new List<object>(CustomAttributes.Count);
                    for(int i=0; i<CustomAttributes.Count; i++)
                    {
                        //var data = (CustomAttributes[i] as System.Reflection.CustomAttributeData);
                        //if (data != null)
                        //{
                        //    if (data.AttributeType == type)
                        //        retVal.Add(CustomAttributes[i]);
                        //}
                        //else
                        //{
                        //    if (CustomAttributes[i].GetType() == type)
                        //        retVal.Add(CustomAttributes[i]);
                        //}
                        if (CustomAttributes[i].GetType() == type)
                            retVal.Add(CustomAttributes[i]);
                    }
                }
                return retVal.ToArray();
            }

            public bool HasReturnValue()
            {
                if (ReturnType == null)
                    return false;
                if (ReturnType.IsEqual(typeof(void)) || ReturnType.IsEqual(typeof(System.Threading.Tasks.Task)))
                    return false;
                return true;
            }
            public bool IsAsync()
            {
                return (ReturnType.IsEqual(typeof(System.Threading.Tasks.Task)) || ReturnType.IsSubclassOf(typeof(System.Threading.Tasks.Task)));
            }
        }
        public List<MethodMeta> Methods { get; } = new List<MethodMeta>();
        public MethodMeta GetMethod(string declString)
        {
            foreach(var i in Methods)
            {
                if (i.GetMethodDeclareString() == declString)
                    return i;
            }
            return null;
        }
        public void BuildMethods()
        {
            Methods.Clear();
            if (ClassType.IsRemoved)
                return;
            var methods = ClassType.SystemType.GetMethods();
            foreach (var i in methods)
            {
                var attrs = i.GetCustomAttributes(typeof(MetaAttribute), true);
                if (attrs.Length == 0)
                    continue;

                var mth = new MethodMeta();
                mth.Meta = attrs[0] as MetaAttribute;
                //mth.Method = i;
                mth.Init(i);
                Methods.Add(mth);
            }
        }
        #endregion

        #region MacrossField
        public class FieldMeta
        {
            public MetaAttribute Meta;
            public System.Reflection.FieldInfo Field;
            private string Name;
            public void Init()
            {
                Name = $"{Field.FieldType.FullName} {Field.Name}";
            }
            public override string ToString()
            {
                return Name;
            }
        }
        public List<FieldMeta> Fields { get; } = new List<FieldMeta>();
        public void BuildFields()
        {
            Fields.Clear();
            var flds = ClassType.SystemType.GetFields();
            foreach (var i in flds)
            {
                var attrs = i.GetCustomAttributes(typeof(MetaAttribute), true);
                if (attrs.Length == 0)
                    continue;

                var mth = new FieldMeta();
                mth.Meta = attrs[0] as MetaAttribute;
                mth.Field = i;
                mth.Init();
                Fields.Add(mth);
            }
        }
        public FieldMeta GetField(string declString)
        {
            for (int i = 0; i < Fields.Count; i++)
            {
                if (Fields[i].Field.Name == declString)
                    return Fields[i];
            }
            return null;
        }
        #endregion

        #region MacrossProperty
        public class PropertyMeta : IComparable<PropertyMeta>
        {
            public MetaAttribute Meta
            {
                get;
                set;
            }
            public System.Reflection.PropertyInfo PropInfo
            {
                get;
                set;
            }
            public void Build()
            {
                if (PropInfo == null)
                    return;
                var attrs = PropInfo.GetCustomAttributes(typeof(IO.UCustomSerializerAttribute), true);
                if (attrs.Length == 0)
                    return;
                CustumSerializer = attrs[0] as IO.UCustomSerializerAttribute;
            }
            public IO.UCustomSerializerAttribute CustumSerializer;
            public Rtti.UTypeDesc FieldType
            {
                get;
                set;
            }
            public int Order { get; set; } = 0;
            private string mFieldTypeStr;
            public string FieldTypeStr
            {
                get => mFieldTypeStr;
                set
                {
                    mFieldTypeStr = value;
                }
            }
            public string PropertyName
            {
                get;
                set;
            }
            public int CompareTo(PropertyMeta other)
            {
                var cmp = Order.CompareTo(other.Order);
                if (cmp > 0)
                    return 1;
                else if (cmp < 0)
                    return -1;
                else
                {
                    cmp = PropertyName.CompareTo(other.PropertyName);
                    if (cmp > 0)
                        return 1;
                    else if (cmp < 0)
                        return -1;
                    else
                    {
                        return FieldTypeStr.CompareTo(other.FieldTypeStr);
                    }
                }
            }
            public override string ToString()
            {
                return FieldTypeStr + PropertyName;
            }
        }
        #endregion

        public void CheckMetaField()
        {
            foreach (var i in MetaVersions)
            {
                i.Value.CheckMetaProperty();
            }
            BuildMethods();
        }
    }

    public class UMetaVersionMeta : IO.IAssetMeta
    {
        public override string GetAssetExtType()
        {
            return ".metadata";
        }
        public override string GetAssetTypeName()
        {
            return "Metadata";
        }
        public override async System.Threading.Tasks.Task<IO.IAsset> LoadAsset()
        {
            return null;
        }
        //public unsafe override void OnDraw(in ImDrawList cmdlist, in Vector2 sz, EGui.Controls.UContentBrowser ContentBrowser)
        //{
        //    var start = ImGuiAPI.GetItemRectMin();
        //    var end = start + sz;

        //    var name = IO.FileManager.GetPureName(GetAssetName().Name);
        //    var tsz = ImGuiAPI.CalcTextSize(name, false, -1);
        //    Vector2 tpos;
        //    tpos.Y = start.Y + sz.Y - tsz.Y;
        //    tpos.X = start.X + (sz.X - tsz.X) * 0.5f;
        //    ImGuiAPI.PushClipRect(in start, in end, true);

        //    end.Y -= tsz.Y;
        //    OnDrawSnapshot(in cmdlist, ref start, ref end);
        //    cmdlist.AddRect(in start, in end, (uint)EGui.UCoreStyles.Instance.SnapBorderColor.ToArgb(),
        //        EGui.UCoreStyles.Instance.SnapRounding, ImDrawFlags_.ImDrawFlags_RoundCornersAll, EGui.UCoreStyles.Instance.SnapThinkness);

        //    cmdlist.AddText(in tpos, 0xFFFF00FF, name, null);
        //    ImGuiAPI.PopClipRect();
        //}
    }
    [Editor.UAssetEditor(EditorType = typeof(Editor.UMetaVersionViewer))]
    public class UMetaVersion
    {
        public UMetaVersion(UClassMeta kls)
        {
            HostClass = kls;
        }
        public UClassMeta HostClass
        {
            get;
            private set;
        }
        public uint MetaHash
        {
            get;
            internal set;
        } = 0;
        public List<UClassMeta.PropertyMeta> Propertys
        {
            get;
        } = new List<UClassMeta.PropertyMeta>();
        public UClassMeta.PropertyMeta GetProperty(string declString)
        {
            for (int i = 0; i < Propertys.Count; i++)
            {
                if (Propertys[i].PropertyName == declString)
                    return Propertys[i];
            }
            return null;
        }
        public void CheckMetaProperty()
        {
            if (HostClass.ClassType.IsRemoved)
            {
                foreach (var i in Propertys)
                {
                    i.PropInfo = null;
                }
                return;
            }
            foreach (var i in Propertys)
            {
                i.PropInfo = HostClass.ClassType.SystemType.GetProperty(i.PropertyName);
            }
        }
        public bool LoadVersion(uint hash, System.Xml.XmlNode node)
        {
            MetaHash = hash;
            foreach (System.Xml.XmlNode i in node.ChildNodes)
            {
                var fd = new UClassMeta.PropertyMeta();
                fd.PropertyName = i.Name;
                foreach (System.Xml.XmlAttribute j in i.Attributes)
                {
                    if (j.Name == "Type")
                    {
                        fd.FieldTypeStr = j.Value;
                    }
                    else if (j.Name == "Order")
                    {
                        fd.Order = System.Convert.ToInt32(j.Value);
                    }
                }
                fd.FieldType = Rtti.UTypeDesc.TypeOf(fd.FieldTypeStr);
                fd.PropInfo = HostClass.ClassType.SystemType.GetProperty(fd.PropertyName);
                fd.Build();
                if (fd.FieldType == null)
                {
                    fd.FieldType = UMissingTypeManager.Instance.GetConvertType(fd.FieldTypeStr);
                    if (fd.PropInfo != null && fd.FieldType == null)
                    {
                        Profiler.Log.WriteLine(Profiler.ELogTag.Warning, "Meta", $"Property lost: Name = {fd.PropertyName}; Type = {fd.FieldTypeStr}");
                    }
                }
                Propertys.Add(fd);
            }
            return true;
        }
        public void SaveVersion()
        {
            var xml = new System.Xml.XmlDocument();
            var root = xml.CreateElement($"Hash_{MetaHash}", xml.NamespaceURI);
            var fdType = xml.CreateAttribute("Type");
            fdType.Value = HostClass.ClassMetaName;
            root.Attributes.Append(fdType);
            xml.AppendChild(root);
            foreach (var i in Propertys)
            {
                var fd = xml.CreateElement($"{i.PropertyName}", root.NamespaceURI);
                fdType = xml.CreateAttribute("Type");
                fdType.Value = i.FieldTypeStr;
                fd.Attributes.Append(fdType);
                fdType = xml.CreateAttribute("Order");
                fdType.Value = i.Order.ToString();
                fd.Attributes.Append(fdType);
                root.AppendChild(fd);
            }
            var xmlText = EngineNS.IO.FileManager.GetXmlText(xml);

            //var typeStr = UTypeDescManager.Instance.GetTypeStringFromType(HostClass.ClassType.SystemType);
            //var typeDesc = UTypeDescManager.Instance.GetTypeDescFromString(typeStr);
            //var typeStr = HostClass.ClassType.TypeString;
            //var typeDesc = HostClass.ClassType;
            //EngineNS.IO.FileManager.ERootDir rootDirType = IO.FileManager.ERootDir.Engine;
            //if (typeDesc.Assembly.IsGameModule)
            //{
            //    rootDirType = IO.FileManager.ERootDir.Game;
            //}
            //var assmemblyDir = typeDesc.Assembly.Service + "/" + typeDesc.Assembly.Name + "/";
            //var rootDir = UEngine.Instance.FileManager.GetPath(rootDirType, IO.FileManager.ESystemDir.MetaData);
            //rootDir += assmemblyDir;
            //var tmpPath = EngineNS.IO.FileManager.CombinePath(rootDir, $"{HostClass.MetaDirectoryName}");
            var tmpPath = HostClass.Path;
            if (!EngineNS.IO.FileManager.DirectoryExists(tmpPath))
            {
                EngineNS.IO.FileManager.CreateDirectory(tmpPath);
            }
            var xmlFilepath = EngineNS.IO.FileManager.CombinePath(tmpPath, $"{MetaHash}.metadata");
            EngineNS.IO.FileManager.WriteAllText(xmlFilepath, xmlText);
        }
    }

    public class UClassMetaManager
    {
        public static UClassMetaManager Instance { get; } = new UClassMetaManager();
        Dictionary<string, UClassMeta> mMetas = new Dictionary<string, UClassMeta>();
        public Dictionary<string, UClassMeta> Metas
        {
            get => mMetas;
        }
        Dictionary<Hash64, UClassMeta> mHashMetas = new Dictionary<Hash64, UClassMeta>();
        public TypeTreeManager TreeManager = new TypeTreeManager();
        public string MetaRoot;
        public void LoadMetas1()
        {
            var rootTypes = new IO.FileManager.ERootDir[2] { IO.FileManager.ERootDir.Engine, IO.FileManager.ERootDir.Game };
            foreach(var r in rootTypes)
            {
                var metaRoot = UEngine.Instance.FileManager.GetPath(r, IO.FileManager.ESystemDir.MetaData);
                if (EngineNS.IO.FileManager.DirectoryExists(metaRoot) == false)
                {
                    continue;
                }
                var services = EngineNS.IO.FileManager.GetDirectories(metaRoot, "*.*", false);
                foreach (var i in services)
                {
                    var assemblies = EngineNS.IO.FileManager.GetDirectories(i, "*.*", false);
                    foreach (var j in assemblies)
                    {
                        var kls = EngineNS.IO.FileManager.GetDirectories(j, "*.*", false);
                        foreach (var k in kls)
                        {
                            var tmpPath = EngineNS.IO.FileManager.CombinePath(k, $"typedesc.txt");
                            var strName = EngineNS.IO.FileManager.ReadAllText(tmpPath);
                            if (strName == null)
                                continue;
                            var type = UTypeDesc.TypeOf(strName);// EngineNS.Rtti.UTypeDescManager.Instance.GetTypeDescFromString(strName);
                            if (type != null)
                            {
                                var meta = new UClassMeta(type);
                                meta.LoadClass(k);

                                mMetas.Add(meta.ClassMetaName, meta);
                            }
                        }
                    }
                }
            }

            //编辑器状态，程序修改过执行
            BuildMeta();

            foreach(var i in mMetas)
            {
                var hashCode = Hash64.FromString(i.Key);
                UClassMeta meta;
                if (mHashMetas.TryGetValue(hashCode, out meta))
                {
                    Profiler.Log.WriteLine(Profiler.ELogTag.Fatal, "Meta", $"Same Hash:{i.Value.ClassMetaName} == {meta.ClassMetaName}");
                    System.Diagnostics.Debug.Assert(false);
                    continue;
                }
                mHashMetas[hashCode] = i.Value;

                TreeManager.RegType(i.Value);
                i.Value.BuildMethods();
                i.Value.BuildFields();
            }

            ForceSaveAll();
        }
        public void LoadMetas()
        {
            var rootTypes = new IO.FileManager.ERootDir[2] { IO.FileManager.ERootDir.Engine, IO.FileManager.ERootDir.Game };
            foreach (var r in rootTypes)
            {
                var metaRoot = UEngine.Instance.FileManager.GetPath(r, IO.FileManager.ESystemDir.MetaData);
                if (EngineNS.IO.FileManager.DirectoryExists(metaRoot) == false)
                {
                    continue;
                }
                var services = EngineNS.IO.FileManager.GetDirectories(metaRoot, "*.*", false);
                foreach (var i in services)
                {
                    var assemblies = EngineNS.IO.FileManager.GetDirectories(i, "*.*", true);
                    foreach (var j in assemblies)
                    {
                        var kls = EngineNS.IO.FileManager.GetDirectories(j, "*.*", false);
                        foreach (var k in kls)
                        {
                            var tmpPath = EngineNS.IO.FileManager.CombinePath(k, $"typedesc.txt");
                            var strName = EngineNS.IO.FileManager.ReadAllText(tmpPath);
                            if (strName == null)
                                continue;
                            var type = UTypeDesc.TypeOf(strName);// EngineNS.Rtti.UTypeDescManager.Instance.GetTypeDescFromString(strName);
                            if (type != null)
                            {
                                var meta = new UClassMeta(type);
                                meta.LoadClass(k);

                                //mMetas.Add(meta.ClassMetaName, meta);
                                mMetas[meta.ClassMetaName] = meta;
                            }
                        }
                    }
                }
            }

            //编辑器状态，程序修改过执行
            BuildMeta();

            foreach (var i in mMetas)
            {
                var hashCode = Hash64.FromString(i.Key);
                UClassMeta meta;
                if (mHashMetas.TryGetValue(hashCode, out meta))
                {
                    Profiler.Log.WriteLine(Profiler.ELogTag.Fatal, "Meta", $"Same Hash:{i.Value.ClassMetaName} == {meta.ClassMetaName}");
                    System.Diagnostics.Debug.Assert(false);
                    continue;
                }
                mHashMetas[hashCode] = i.Value;

                TreeManager.RegType(i.Value);
                i.Value.BuildMethods();
                i.Value.BuildFields();
            }

            ForceSaveAll();

            GetMeta(UTypeDesc.TypeOf(typeof(void)));
            GetMeta(UTypeDesc.TypeOf(typeof(char)));
            GetMeta(UTypeDesc.TypeOf(typeof(byte)));
            GetMeta(UTypeDesc.TypeOf(typeof(Int16)));
            GetMeta(UTypeDesc.TypeOf(typeof(UInt16)));
            GetMeta(UTypeDesc.TypeOf(typeof(Int32)));
            GetMeta(UTypeDesc.TypeOf(typeof(UInt32)));
            GetMeta(UTypeDesc.TypeOf(typeof(Int64)));
            GetMeta(UTypeDesc.TypeOf(typeof(UInt64)));
            GetMeta(UTypeDesc.TypeOf(typeof(float)));
            GetMeta(UTypeDesc.TypeOf(typeof(double)));
            GetMeta(UTypeDesc.TypeOf(typeof(string)));
            GetMeta(UTypeDesc.TypeOf(typeof(Vector2)));
            GetMeta(UTypeDesc.TypeOf(typeof(Vector3)));
            GetMeta(UTypeDesc.TypeOf(typeof(Vector4)));
            GetMeta(UTypeDesc.TypeOf(typeof(Quaternion)));
        }
        public void BuildMeta()
        {
            foreach (var i in UTypeDescManager.Instance.Services)
            {
                var klsColloector = new List<UClassMeta>();
                foreach(var j in i.Value.Types)
                {
                    MetaAttribute meta;
                    if (j.Value.GetInterface(nameof(EngineNS.IO.ISerializer)) == null)
                    {
                        var attrs = j.Value.GetCustomAttributes(typeof(MetaAttribute), true);
                        if (attrs.Length == 0)
                            continue;
                        meta = attrs[0] as MetaAttribute;
                    }

                    UClassMeta kls = null;
                    var metaName = j.Value.TypeString;// Rtti.UTypeDescManager.Instance.GetTypeStringFromType(j.Value.SystemType);
                    if (mMetas.TryGetValue(metaName, out kls) == false)
                    {
                        kls = new UClassMeta(j.Value);
                        kls.Path = GetPath(kls);                        
                        var ver = kls.BuildCurrentVersion();
                        mMetas.Add(metaName, kls);
                        kls.SaveClass();
                    }
                    else
                    {
                        klsColloector.Add(kls);
                        //var ver = kls.BuildCurrentVersion();
                    }
                }
                foreach(var j in klsColloector)
                {
                    var ver = j.BuildCurrentVersion();
                }
            }
        }

        public void ForceSaveAll()
        {
            foreach (var i in mMetas)
            {
                i.Value.Path = GetPath(i.Value);
                IO.FileManager.SureDirectory(i.Value.Path);
                foreach (var j in i.Value.MetaVersions)
                {
                    j.Value.SaveVersion();
                }

                var txtFilepath = EngineNS.IO.FileManager.CombinePath(i.Value.Path, $"typedesc.txt");
                EngineNS.IO.FileManager.WriteAllText(txtFilepath, i.Value.ClassType.TypeString);
            }
        }
        string GetPath(UClassMeta classMeta)
        {
            string root = "";
            if (classMeta.ClassType.Assembly.IsGameModule)
                root = UEngine.Instance.FileManager.GetPath(IO.FileManager.ERootDir.Game, IO.FileManager.ESystemDir.MetaData);
            else
                root = UEngine.Instance.FileManager.GetPath(IO.FileManager.ERootDir.Engine, IO.FileManager.ESystemDir.MetaData);

            var dir = classMeta.ClassType.Assembly.Service;
            dir += "." + classMeta.ClassType.Assembly.Name;
            if(!string.IsNullOrEmpty(classMeta.ClassType.Namespace))
                dir += "." + classMeta.ClassType.Namespace;

            dir = dir.Replace('.', '/');
            dir = root + dir;

            string name;
            if (string.IsNullOrEmpty(classMeta.ClassType.Namespace))
                name = classMeta.ClassType.FullName;
            else
                name = classMeta.ClassType.FullName.Substring(classMeta.ClassType.Namespace.Length);
            if (name.StartsWith('.'))
            {
                name = name.Substring(1);
            }
            if (name.Length <250)
            {
                return dir + "/a0." + name;
            }
            else
            {
                return dir + "/a0." + classMeta.MetaDirectoryName;
            }
        }
        public UClassMeta GetMeta(string name, bool bTryBuild = true)
        {
            UClassMeta meta;
            if (mMetas.TryGetValue(name, out meta))
                return meta;
            if (bTryBuild == false)
                return null;
            lock (this)
            {
                var type = Rtti.UTypeDesc.TypeOf(name);
                if (type == null)
                    return null;
                UClassMeta result;
                if (TypeMetas.TryGetValue(type, out result))
                {
                    return result;
                }
                else
                {
                    result = new UClassMeta(type);
                    result.Path = GetPath(result);
                    result.BuildMethods();
                    result.BuildFields();
                    result.BuildCurrentVersion();
                    TypeMetas.Add(type, result);
                    return result;
                }
            }
        }
        public Dictionary<UTypeDesc, UClassMeta> TypeMetas { get; } = new Dictionary<UTypeDesc, UClassMeta>();
        public UClassMeta GetMetaFromFullName(string fname)
        {
            var desc = Rtti.UTypeDescManager.Instance.GetTypeDescFromFullName(fname);
            if (desc == null)
                return null;
            return GetMeta(desc);
        }
        public UClassMeta GetMeta(UTypeDesc type)
        {
            if (type == null)
                return null;
            lock (this)
            {
                UClassMeta result;
                if (TypeMetas.TryGetValue(type, out result))
                {
                    return result;
                }
                var typeStr = type.TypeString; //Rtti.UTypeDesc.TypeStr(type.SystemType);
                return GetMeta(typeStr);
            }
        }
        public UClassMeta GetMeta(in Hash64 hash)
        {
            UClassMeta meta;
            if (mHashMetas.TryGetValue(hash, out meta))
                return meta;
            return null;
        }
    }

    
}
