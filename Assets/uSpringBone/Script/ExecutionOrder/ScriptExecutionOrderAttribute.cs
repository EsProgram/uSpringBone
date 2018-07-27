using System;

namespace Es.uSpringBone
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ScriptExecutionOrderAttribute : Attribute
    {
        private int order = 0;

        public ScriptExecutionOrderAttribute(int order)
        {
            this.order = order;
        }

        public int GetOrder()
        {
            return order;
        }
    }
}