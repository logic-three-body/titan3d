﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CppWeaving.Cpp2CS
{
    class UClassCodeCs : UCodeBase
    {
        public UClass mClass;
        public List<string> DelegateDeclares = new List<string>();
        public UClassCodeCs(UClass kls)
        {
            this.FullName = kls.FullName;
            mClass = kls;
        }
        public override string GetFileExt()
        {
            return ".cpp2cs.cs";
        }
        public override void OnGenCode()
        {
            AddLine($"//generated by CppWeaving");
            AddLine($"using System;");
            AddLine($"using System.Runtime.InteropServices;");

            AddLine($"#if DEBUG");
            PushTab();
            {
                AddLine($"using SuppressGC = EngineNS.Rtti.UDummyAttribute;");
            }
            PopTab();
            AddLine($"#else");
            PushTab();
            {
                AddLine($"using SuppressGC = System.Runtime.InteropServices.SuppressGCTransitionAttribute;");
            }
            PopTab();
            AddLine($"#endif");

            NewLine();

            if (!string.IsNullOrEmpty(mClass.Namespace))
            {
                AddLine($"namespace {mClass.Namespace}");
                PushBrackets();
            }

            UserAttribute();
            if (mClass.HasMeta(UProjectSettings.SV_Dispose) && mClass.GetType() != typeof(UStruct))
            {
                AddLine($"public unsafe partial struct {Name} : EngineNS.IPtrType, IDisposable");
            }
            else
            {
                AddLine($"public unsafe partial struct {Name} : EngineNS.IPtrType");
            }

            PushBrackets();
            {
                DefineLayout();

                AddLine($"#region Constructor&Cast");
                GenConstructor();
                GenCast();
                AddLine($"#endregion");

                AddLine($"#region Fields");
                GenFields();
                AddLine($"#endregion");

                AddLine($"#region Function");
                GenFunction();
                AddLine($"#endregion");

                AddLine($"#region Core SDK");
                {
                    AddLine($"const string ModuleNC = {UProjectSettings.ModuleNC};");
                    GenPInvokeConstructor();

                    GenPInvokeFields();

                    GenPInvokeFunction();

                    GenPInvokeCast();
                }
                AddLine($"#endregion");
            }
            PopBrackets();

            if (mClass.HasMeta(UProjectSettings.SV_CSImplement))
            {
                GenInheritable();
            }

            if (!string.IsNullOrEmpty(mClass.Namespace))
            {
                PopBrackets();
            }
        }
        protected void GenInheritable()
        {
            AddLine($"public unsafe abstract partial class I_{Name} : AuxPtrType<{Name}>");
            PushBrackets();
            {
                AddLine($"const string ModuleNC = {UProjectSettings.ModuleNC};");
                AddLine($"static I_{Name}()");
                PushBrackets();
                {
                    foreach (var i in mClass.Functions)
                    {
                        if (i.IsVirtual == false)
                        {
                            continue;
                        }
                        if (i.HasMeta(UProjectSettings.SV_CSImplement) == false)
                        {
                            continue;
                        }
                        AddLine($"TSDK_Set_CSImpl_{i.Name}_{i.FunctionHash}(csfn_{i.Name});");
                    }
                }
                PopBrackets();
                foreach (var i in mClass.Functions)
                {
                    if (i.IsVirtual == false)
                    {
                        continue;
                    }
                    if (i.HasMeta(UProjectSettings.SV_CSImplement) == false)
                    {
                        continue;
                    }
                    string marshalReturn = null;
                    bool pointerTypeWrapper = false;
                    if (i.ReturnType.NumOfTypePointer == 1 && i.ReturnType.PropertyType.ClassType == UTypeBase.EClassType.PointerType)
                    {
                        pointerTypeWrapper = true;
                    }
                    var retTypeStr = i.ReturnType.GetCsTypeName();
                    if (pointerTypeWrapper)
                    {
                        retTypeStr = i.ReturnType.PropertyType.ToCsName();
                    }
                    else
                    {
                        if (i.HasMeta(UProjectSettings.SV_NoStringConverter) == false)
                        {
                            if (retTypeStr == "sbyte*")
                            {
                                retTypeStr = "string";
                                marshalReturn = $"EngineNS.Rtti.UNativeCoreProvider.MarshalPtrAnsi";
                            }
                        }
                    }
                    string retStr = "return ";
                    if (retTypeStr == "void")
                        retStr = "";

                    AddLine($"private delegate {retTypeStr} CSImpl_{i.Name}(IntPtr self, {i.GetParameterDefineCs()});");
                    UTypeManager.WritePInvokeAttribute(this, i);
                    AddLine($"private extern static void TSDK_Set_CSImpl_{i.Name}_{i.FunctionHash}(CSImpl_{i.Name} fn);");
                    AddLine($"private static CSImpl_{i.Name} csfn_{i.Name} = csfn_imp_{i.Name};");
                    AddLine($"private static {retTypeStr} csfn_imp_{i.Name}(IntPtr self, {i.GetParameterDefineCs()})");
                    PushBrackets();
                    {
                        AddLine($"var handle = System.Runtime.InteropServices.GCHandle.FromIntPtr(self);");
                        AddLine($"var pThis = handle.Target as I_{Name};");
                        AddLine($"{retStr}pThis.{i.Name}({i.GetParameterCalleeCs()});");
                    }
                    PopBrackets();
                    AddLine($"{GetAccessDefine(i.Access)} abstract {retTypeStr} {i.Name}({i.GetParameterDefineCs()});");
                }
            }
            PopBrackets();
        }
        protected virtual void UserAttribute()
        {

        }
        protected virtual void DefineLayout()
        {
            AddLine($"[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = {mClass.Decl.TypeForDecl.Handle.SizeOf}, Pack = {mClass.Decl.TypeForDecl.Handle.AlignOf})]");
            AddLine($"public struct CppStructLayout");
            PushBrackets();
            {
                foreach (var i in mClass.Properties)
                {
                    var field = i as UField;
                    AddLine($"[System.Runtime.InteropServices.FieldOffset({field.Offset / 8})]");
                    if (i.IsDelegate)
                        AddLine($"public IntPtr {i.Name};");
                    else
                    {
                        var retType = i.GetCsTypeName();
                        if (i.IsTypeDef)
                        {
                            var dypeDef = USysClassManager.Instance.FindTypeDef(i.CxxName);
                            if (dypeDef != null)
                                retType = dypeDef;
                        }
                        AddLine($"public {retType} {i.Name};");
                    }
                }
            }
            PopBrackets();

            AddLine($"private void* mPointer;");
            AddLine($"public CppStructLayout* UnsafeAsLayout {{ get => (CppStructLayout*)mPointer; }}");
            AddLine($"public {mClass.Name}(void* p) {{ mPointer = p; }}");
            AddLine($"public void UnsafeSetPointer(void* p) {{ mPointer = p; }}");
            AddLine($"public IntPtr NativePointer {{ get => (IntPtr)mPointer; set => mPointer = value.ToPointer(); }}");
            AddLine($"public {mClass.Name}* CppPointer {{ get => ({mClass.Name}*)mPointer; }}");
            AddLine($"public bool IsValidPointer {{ get => mPointer != (void*)0; }}");

            AddLine($"public static implicit operator {mClass.Name}* ({mClass.Name} v)");
            PushBrackets();
            {
                AddLine($"return ({Name}*)v.mPointer;");
            }
            PopBrackets();
        }
        protected virtual void BeginInvoke(UTypeBase member)
        {

        }
        protected virtual void EndInvoke(UTypeBase member)
        {

        }
        protected virtual void GenConstructor()
        {
            foreach (var i in mClass.Constructors)
            {
                if (i.Access != EAccess.Public && mClass.IsExpProtected == false)
                    continue;

                if (i.Parameters.Count > 0)
                    AddLine($"public static {mClass.ToCsName()} CreateInstance({i.GetParameterDefineCs()})");
                else
                    AddLine($"public static {mClass.ToCsName()} CreateInstance()");
                PushBrackets();
                {
                    var sdk_fun = $"TSDK_{mClass.VisitorPInvoke}_CreateInstance_{i.FunctionHash}";
                    if (i.Parameters.Count > 0)
                        AddLine($"return new {mClass.ToCsName()}({sdk_fun}({i.GetParameterCalleeCs()}));");
                    else
                        AddLine($"return new {mClass.ToCsName()}({sdk_fun}());");
                }
                PopBrackets();
            }
            if (mClass.HasMeta(UProjectSettings.SV_Dispose))
            {
                var dispose = mClass.GetMeta(UProjectSettings.SV_Dispose);
                AddLine($"public void Dispose()");
                PushBrackets();
                {
                    var sdk_fun = $"TSDK_{mClass.VisitorPInvoke}_Dispose";
                    BeginInvoke(null);
                    AddLine($"{sdk_fun}(mPointer);");
                    EndInvoke(null);
                }
                PopBrackets();
            }
        }
        protected virtual void GenPInvokeConstructor()
        {
            AddLine($"//Constructor&Cast");
            foreach (var i in mClass.Constructors)
            {
                if (i.Access != EAccess.Public && mClass.IsExpProtected == false)
                    continue;
                UTypeManager.WritePInvokeAttribute(this, i);
                if (i.Parameters.Count > 0)
                    AddLine($"extern static {mClass.ToCsName()}* TSDK_{mClass.VisitorPInvoke}_CreateInstance_{i.FunctionHash}({i.GetParameterDefineCs()});");
                else
                    AddLine($"extern static {mClass.ToCsName()}* TSDK_{mClass.VisitorPInvoke}_CreateInstance_{i.FunctionHash}();");
            }
            if (mClass.HasMeta(UProjectSettings.SV_Dispose))
            {
                UTypeManager.WritePInvokeAttribute(this, null);
                AddLine($"extern static void TSDK_{mClass.VisitorPInvoke}_Dispose(void* self);");
            }
        }
        protected virtual void GenFields()
        {
            foreach (var i in mClass.Properties)
            {
                if (i.Access != EAccess.Public && mClass.IsExpProtected == false)
                    continue;
                var retType = i.GetCsTypeName();
                bool pointerTypeWrapper = false;
                if (i.NumOfTypePointer == 1 && i.PropertyType.ClassType == UTypeBase.EClassType.PointerType)
                {
                    pointerTypeWrapper = true;
                }

                string marshalReturn = null;
                if (i.IsDelegate)
                {
                    var dlgt = i.PropertyType as UDelegate;
                    if (dlgt != null)
                    {
                        AddLine($"public {dlgt.GetCsDelegateDefine()};");
                    }
                    AddLine($"{GetAccessDefine(i.Access)} {i.GetCsTypeName()} {i.Name}");
                }
                else
                {
                    if (pointerTypeWrapper)
                        AddLine($"{GetAccessDefine(i.Access)} {i.PropertyType.ToCsName()} {i.Name}");
                    else
                    {
                        if (i.HasMeta(UProjectSettings.SV_NoStringConverter) == false)
                        {
                            if (retType == "sbyte*")
                            {
                                retType = "string";
                                marshalReturn = $"EngineNS.Rtti.UNativeCoreProvider.MarshalPtrAnsi";
                            }
                        }
                        if (i.IsTypeDef)
                        {
                            var dypeDef = USysClassManager.Instance.FindTypeDef(i.CxxName);
                            if (dypeDef != null)
                                retType = dypeDef;
                        }
                        AddLine($"{GetAccessDefine(i.Access)} {retType} {i.Name}");
                    }
                }
                PushBrackets();
                {
                    AddLine($"get");
                    PushBrackets();
                    {
                        string pinvoke = $"TSDK_{mClass.VisitorPInvoke}_FieldGet__{i.Name}(mPointer)";
                        BeginInvoke(i);
                        if (pointerTypeWrapper)
                        {
                            AddLine($"return new {i.PropertyType.ToCsName()}({pinvoke});");
                        }
                        else
                        {
                            if (marshalReturn != null)
                            {
                                AddLine($"return {marshalReturn}((IntPtr){pinvoke});");
                            }
                            else
                            {
                                if (retType == "bool")
                                    AddLine($"return {pinvoke} == 0 ? false : true;");
                                else
                                    AddLine($"return {pinvoke};");
                            }
                        }
                        EndInvoke(i);
                    }
                    PopBrackets();

                    if (!i.HasMeta(UProjectSettings.SV_ReadOnly))
                    {
                        AddLine($"set");
                        PushBrackets();
                        {
                            BeginInvoke(i);
                            string pinvoke = $"TSDK_{mClass.VisitorPInvoke}_FieldSet__{i.Name}";
                            if (pointerTypeWrapper)
                            {
                                AddLine($"{pinvoke}(mPointer, value);");
                            }
                            else
                            {
                                AddLine($"{pinvoke}(mPointer, value);");
                            }
                            EndInvoke(i);
                        }
                        PopBrackets();
                    }
                }
                PopBrackets();
            }

        }
        protected virtual void GenPInvokeFields()
        {
            AddLine($"//Fields");
            foreach (var i in mClass.Properties)
            {
                if (i.Access != EAccess.Public && mClass.IsExpProtected == false)
                    continue;

                var retType = i.GetCsTypeName();
                if (i.IsTypeDef)
                {
                    var dypeDef = USysClassManager.Instance.FindTypeDef(i.CxxName);
                    if (dypeDef != null)
                        retType = dypeDef;
                }

                UTypeManager.WritePInvokeAttribute(this, i);
                if (retType == "bool")
                {
                    AddLine($"extern static sbyte TSDK_{mClass.VisitorPInvoke}_FieldGet__{i.Name}(void* self);");
                }
                else
                {
                    AddLine($"extern static {retType} TSDK_{mClass.VisitorPInvoke}_FieldGet__{i.Name}(void* self);");
                }

                if (!i.HasMeta(UProjectSettings.SV_ReadOnly))
                {
                    if (i.HasMeta(UProjectSettings.SV_NoStringConverter) == false)
                    {
                        if (retType == "sbyte*")
                        {
                            retType = "string";
                        }
                    }
                    UTypeManager.WritePInvokeAttribute(this, i);
                    AddLine($"extern static void TSDK_{mClass.VisitorPInvoke}_FieldSet__{i.Name}(void* self, {retType} value);");
                }
            }
        }

        protected void GenFunction()
        {
            foreach (var i in mClass.Functions)
            {
                if (i.Access != EAccess.Public && mClass.IsExpProtected == false)
                    continue;
                string marshalReturn = null;
                bool pointerTypeWrapper = false;
                if (i.ReturnType.NumOfTypePointer == 1 && i.ReturnType.PropertyType.ClassType == UTypeBase.EClassType.PointerType)
                {
                    pointerTypeWrapper = true;
                }
                var retTypeStr = i.ReturnType.GetCsTypeName();
                if (pointerTypeWrapper)
                {
                    retTypeStr = i.ReturnType.PropertyType.ToCsName();
                }
                else
                {
                    if (i.HasMeta(UProjectSettings.SV_NoStringConverter) == false)
                    {
                        if (retTypeStr == "sbyte*")
                        {
                            retTypeStr = "string";
                            marshalReturn = $"EngineNS.Rtti.UNativeCoreProvider.MarshalPtrAnsi";
                        }
                    }
                }

                string selfArg = $"{mClass.ToCppName()}* self";
                if (i.IsStatic)
                {
                    selfArg = "";
                }
                else
                {
                    if (i.Parameters.Count > 0)
                    {
                        selfArg += ",";
                    }
                }

                string retStr = "return ";
                if (retTypeStr == "void")
                    retStr = "";

                string callArg;

                i.GenCsDelegateDefine(this);
                if (i.IsStatic)
                {
                    AddLine($"{GetAccessDefine(i.Access)} static {retTypeStr} {i.Name}({i.GetParameterDefineCs()})");
                    callArg = "";
                }
                else
                {
                    AddLine($"{GetAccessDefine(i.Access)} {retTypeStr} {i.Name}({i.GetParameterDefineCs()})");
                    callArg = "mPointer";
                }
                if (i.Parameters.Count > 0)
                {
                    if (callArg == "")
                        callArg += i.GetParameterCalleeCs();
                    else
                        callArg += ", " + i.GetParameterCalleeCs();
                }
                PushBrackets();
                {
                    BeginInvoke(i);
                    var invoke = $"TSDK_{mClass.VisitorPInvoke}_{i.Name}_{i.FunctionHash}";
                    if (pointerTypeWrapper)
                    {
                        AddLine($"{retStr}new {retTypeStr}({invoke}({callArg}));");
                    }
                    else
                    {
                        if (marshalReturn == null)
                        {
                            if (retTypeStr == "bool")
                            {
                                AddLine($"return {invoke}({callArg}) == 0 ? false : true;");
                            }
                            else
                            {
                                AddLine($"{retStr}{invoke}({callArg});");
                            }
                        }
                        else
                            AddLine($"{retStr}{marshalReturn}((IntPtr){invoke}({callArg}));");
                    }
                    EndInvoke(i);
                }
                PopBrackets();

                if (i.IsRefConvert())
                {
                    if (i.IsStatic)
                        AddLine($"{GetAccessDefine(i.Access)} static {retTypeStr} {i.Name}({i.GetParameterDefineCsRefConvert()})");
                    else
                        AddLine($"{GetAccessDefine(i.Access)} {retTypeStr} {i.Name}({i.GetParameterDefineCsRefConvert()})");

                    PushBrackets();
                    {
                        i.WritePinRefConvert(this);
                        PushBrackets();
                        {
                            AddLine($"{retStr}{i.Name}({i.GetParameterCalleeCsRefConvert()});");
                        }
                        PopBrackets();
                    }
                    PopBrackets();
                }
            }
        }
        protected void GenPInvokeFunction()
        {
            AddLine($"//Functions");
            foreach (var i in mClass.Functions)
            {
                if (i.Access != EAccess.Public && mClass.IsExpProtected == false)
                    continue;

                UTypeManager.WritePInvokeAttribute(this, i);
                string callStr = "";
                if (!i.IsStatic)
                {
                    callStr = "void* Self";
                }
                if (i.Parameters.Count > 0)
                {
                    if (callStr == "")
                        callStr += i.GetParameterDefineCs();
                    else
                        callStr += ", " + i.GetParameterDefineCs();
                }
                var retTypeStr = i.ReturnType.GetCsTypeName();
                //if (i.HasMeta(UProjectSettings.SV_NoStringConverter) == false)
                //{
                //    if (retTypeStr == "sbyte*")
                //    {
                //        retTypeStr = "string";
                //    }
                //}
                if(retTypeStr=="bool")
                {
                    AddLine($"extern static sbyte TSDK_{mClass.VisitorPInvoke}_{i.Name}_{i.FunctionHash}({callStr});");
                }
                else
                {
                    AddLine($"extern static {retTypeStr} TSDK_{mClass.VisitorPInvoke}_{i.Name}_{i.FunctionHash}({callStr});");
                }   
            }
        }
        protected virtual void GenCast()
        {
            if (mClass.BaseTypes.Count == 1)
            {
                var bType = mClass.BaseTypes[0];
                AddLine($"public {bType.ToCsName()} CastSuper()");
                PushBrackets();
                {
                    var invoke = $"TSDK_{mClass.VisitorPInvoke}_CastTo_{bType.ToCppName().Replace("::", "_")}";
                    BeginInvoke(bType);
                    AddLine($"return new {bType.ToCsName()}({invoke}(mPointer));");
                    EndInvoke(bType);
                }
                PopBrackets();

                AddLine($"public {bType.ToCsName()} NativeSuper");
                PushBrackets();
                {
                    AddLine($"get {{ return CastSuper(); }}");
                }
                PopBrackets();
                return;
            }
            else
            {
                foreach (var i in mClass.BaseTypes)
                {
                    AddLine($"public {i.ToCsName()} CastTo_{i.ToCppName().Replace("::", "_")}()");
                    PushBrackets();
                    {
                        var invoke = $"TSDK_{mClass.VisitorPInvoke}_CastTo_{i.ToCppName().Replace("::", "_")}";
                        BeginInvoke(i);
                        AddLine($"return new {i.ToCsName()}({invoke}(mPointer);");
                        EndInvoke(i);
                    }
                    PopBrackets();
                }
            }
        }
        protected void GenPInvokeCast()
        {
            AddLine($"//Cast");
            foreach (var i in mClass.BaseTypes)
            {
                UTypeManager.WritePInvokeAttribute(this, i);
                AddLine($"extern static {i.ToCsName()}* TSDK_{mClass.VisitorPInvoke}_CastTo_{i.ToCppName().Replace("::", "_")}(void* self);");
            }
        }
    }
}
