using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

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
            public float3 grobalPosition;

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
            var tmp = Gizmos.color;
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.8f);
            Gizmos.DrawSphere(transform.position, radius);
            Gizmos.color = tmp;
        }
    }
}