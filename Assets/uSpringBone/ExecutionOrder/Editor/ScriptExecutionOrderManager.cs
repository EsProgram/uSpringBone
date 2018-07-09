using System;
using UnityEditor;

namespace Es.uSpringBone
{
    [InitializeOnLoad]
    public class ScriptExecutionOrderManager : Editor
    {
        static ScriptExecutionOrderManager()
        {
            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                Type type = monoScript.GetClass();
                if (type == null)
                    continue;

                object[] attributes = type.GetCustomAttributes(typeof(ScriptExecutionOrderAttribute), true);

                if (attributes.Length == 0)
                    continue;

                ScriptExecutionOrderAttribute attribute = (ScriptExecutionOrderAttribute) attributes[0];
                if (MonoImporter.GetExecutionOrder(monoScript) != attribute.GetOrder())
                    MonoImporter.SetExecutionOrder(monoScript, attribute.GetOrder());
            }
        }
    }
}