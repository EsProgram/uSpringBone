using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Es.uSpringBone
{
    /// <summary>
    /// Collision determination component.
    /// </summary>
    [ScriptExecutionOrder(-30000)]
    public class SpringBoneCollider : MonoBehaviour
    {
        public float radius;
        public Data data;

        Transform cachedTransform;

        /// <summary>
        /// Collision data.
        /// </summary>
        public struct Data
        {
            public float radius;
            public Vector3 grobalPosition;

            public Data(float radius, Vector3 grobalPosition)
            {
                this.radius = radius;
                this.grobalPosition = grobalPosition;
            }
        }

        void Start()
        {
            cachedTransform = transform;
            data = new Data(radius, transform.position);
        }

        void Update()
        {
            data.radius = radius;
            data.grobalPosition = cachedTransform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}