namespace GuardianOnline.Resources {
    using System;
    
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Layout {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Layout() {
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GuardianOnline.Resources.Layout", typeof(Layout).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        public static string Brand {
            get {
                return ResourceManager.GetString("Brand", resourceCulture);
            }
        }
        
        public static string Menu {
            get {
                return ResourceManager.GetString("Menu", resourceCulture);
            }
        }
        
        public static string Account {
            get {
                return ResourceManager.GetString("Account", resourceCulture);
            }
        }
        
        public static string SignOut {
            get {
                return ResourceManager.GetString("SignOut", resourceCulture);
            }
        }
        
        public static string Language {
            get {
                return ResourceManager.GetString("Language", resourceCulture);
            }
        }
    }
}