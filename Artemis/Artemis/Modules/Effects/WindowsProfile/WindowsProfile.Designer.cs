﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Artemis.Modules.Effects.WindowsProfile {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class WindowsProfile : global::System.Configuration.ApplicationSettingsBase {
        
        private static WindowsProfile defaultInstance = ((WindowsProfile)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new WindowsProfile())));
        
        public static WindowsProfile Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Demo (Duplicate to keep changes)")]
        public string LastProfile {
            get {
                return ((string)(this["LastProfile"]));
            }
            set {
                this["LastProfile"] = value;
            }
        }
    }
}
