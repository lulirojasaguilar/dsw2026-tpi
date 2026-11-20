
namespace Dsw2026Tpi.CrossCutting.Resources {
    using System;
    
    
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "18.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class ErrorCodes {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ErrorCodes() {
        }
        
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Dsw2026Tpi.CrossCutting.Resources.ErrorCodes", typeof(ErrorCodes).Assembly);
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
        
        public static string APPOINTMENT_CONFLICT {
            get {
                return ResourceManager.GetString("APPOINTMENT_CONFLICT", resourceCulture);
            }
        }
        
        public static string APPOINTMENT_NOT_CANCELLABLE {
            get {
                return ResourceManager.GetString("APPOINTMENT_NOT_CANCELLABLE", resourceCulture);
            }
        }
        
        public static string APPOINTMENT_NOT_FOUND {
            get {
                return ResourceManager.GetString("APPOINTMENT_NOT_FOUND", resourceCulture);
            }
        }
        
        public static string APPOINTMENT_NOT_UPDATABLE {
            get {
                return ResourceManager.GetString("APPOINTMENT_NOT_UPDATABLE", resourceCulture);
            }
        }
        
        public static string AUTHENTICATION_FAILED {
            get {
                return ResourceManager.GetString("AUTHENTICATION_FAILED", resourceCulture);
            }
        }
        
        public static string AUTHORIZATION_FAILED {
            get {
                return ResourceManager.GetString("AUTHORIZATION_FAILED", resourceCulture);
            }
        }
        
        public static string AVAILABILITY_NOT_FOUND {
            get {
                return ResourceManager.GetString("AVAILABILITY_NOT_FOUND", resourceCulture);
            }
        }
        
        public static string DOCTOR_NOT_FOUND {
            get {
                return ResourceManager.GetString("DOCTOR_NOT_FOUND", resourceCulture);
            }
        }
        
        public static string DUPLICATE_SPECIALTY_NAME {
            get {
                return ResourceManager.GetString("DUPLICATE_SPECIALTY_NAME", resourceCulture);
            }
        }
        
        public static string ENTITY_NOT_FOUND {
            get {
                return ResourceManager.GetString("ENTITY_NOT_FOUND", resourceCulture);
            }
        }
        
        public static string INVALID_APPOINTMENT_STATUS {
            get {
                return ResourceManager.GetString("INVALID_APPOINTMENT_STATUS", resourceCulture);
            }
        }
        
        public static string INVALID_REQUEST_FORMAT {
            get {
                return ResourceManager.GetString("INVALID_REQUEST_FORMAT", resourceCulture);
            }
        }
        
        public static string PAST_DATETIME_NOT_ALLOWED {
            get {
                return ResourceManager.GetString("PAST_DATETIME_NOT_ALLOWED", resourceCulture);
            }
        }
        
        public static string PATIENT_MISMATCH {
            get {
                return ResourceManager.GetString("PATIENT_MISMATCH", resourceCulture);
            }
        }
        
        public static string PATIENT_NOT_FOUND {
            get {
                return ResourceManager.GetString("PATIENT_NOT_FOUND", resourceCulture);
            }
        }
        
        public static string RATE_LIMIT_EXCEEDED {
            get {
                return ResourceManager.GetString("RATE_LIMIT_EXCEEDED", resourceCulture);
            }
        }
        
        public static string REGISTER_USER_CONFLICT {
            get {
                return ResourceManager.GetString("REGISTER_USER_CONFLICT", resourceCulture);
            }
        }
        
        public static string SCHEDULE_OVERLAP {
            get {
                return ResourceManager.GetString("SCHEDULE_OVERLAP", resourceCulture);
            }
        }
        
        public static string SLOT_NOT_AVAILABLE {
            get {
                return ResourceManager.GetString("SLOT_NOT_AVAILABLE", resourceCulture);
            }
        }
        
        public static string SPECIALITY_NOT_FOUND {
            get {
                return ResourceManager.GetString("SPECIALITY_NOT_FOUND", resourceCulture);
            }
        }
        
        public static string UNHANDLED_ERROR {
            get {
                return ResourceManager.GetString("UNHANDLED_ERROR", resourceCulture);
            }
        }
        
        public static string VALIDATION_ERROR {
            get {
                return ResourceManager.GetString("VALIDATION_ERROR", resourceCulture);
            }
        }
        public static string SPECIALITY_HAS_DOCTORS {
            get {
                return ResourceManager.GetString("SPECIALITY_HAS_DOCTORS", resourceCulture);
            }
        }

        public static string DOCTOR_HAS_APPOINTMENTS {
            get {
                return ResourceManager.GetString("DOCTOR_HAS_APPOINTMENTS", resourceCulture);
            }
        }

        public static string DOCTORS_NOT_FOUND {
            get {
                return ResourceManager.GetString("DOCTORS_NOT_FOUND", resourceCulture);
            }
        }

    }
}
