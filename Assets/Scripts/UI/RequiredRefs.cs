using UnityEngine;

namespace Needle.UI
{
    public static class RequiredRefs
    {
        public static bool Assign<T>(T value, string fieldName, Object context) where T : class
        {
            if (value != null)
                return true;

            string owner = context != null ? context.GetType().Name : "Unknown";
            Debug.LogError($"{owner}: serialized field '{fieldName}' is not assigned.", context);
            return false;
        }

        public static bool AssignString(string value, string fieldName, Object context)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return true;

            string owner = context != null ? context.GetType().Name : "Unknown";
            Debug.LogError($"{owner}: serialized field '{fieldName}' is empty.", context);
            return false;
        }

        public static bool CanAct(bool isValid, string action, Object context)
        {
            if (isValid)
                return true;

            string owner = context != null ? context.GetType().Name : "Unknown";
            Debug.LogError($"{owner}: cannot {action} because required serialized fields are missing.", context);
            return false;
        }
    }
}
