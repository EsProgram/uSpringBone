using UnityEngine;
using Unity;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System.Linq;

namespace Es.uSpringBone
{
    /// <summary>
    /// Has bone specific data.
    /// </summary>
    public class SpringBone : MonoBehaviour
    {
        /// <summary>
        /// SpringBone data.
        /// </summary>
        public struct Data
        {
            public float3 localPosition;
            public float3 grobalPosition;
            public float3 currentEndpoint;
            public float3 previousEndpoint;
            public quaternion localRotation;
            public quaternion grobalRotation;
            public float3 parentScale;
            public float3 boneAxis;
            public float radius;
            public float stiffnessForce;
            public float dragForce;
            public float3 springForce;
            public float springLength;
            public int isRootChild;

            public Data(
                Vector3 localPosition,
                Vector3 grobalPosition,
                Vector3 currentEndpoint,
                Vector3 previousEndpoint,
                Quaternion localRotation,
                Quaternion grobalRotation,
                Vector3 parentScale,
                Vector3 boneAxis,
                float radius,
                float stiffnessForce,
                float dragForce,
                Vector3 springForce,
                float springLength,
                int isRootChild
            )
            {
                this.localPosition = localPosition;
                this.grobalPosition = grobalPosition;
                this.currentEndpoint = currentEndpoint;
                this.previousEndpoint = previousEndpoint;
                this.localRotation = localRotation;
                this.grobalRotation = grobalRotation;
                this.parentScale = parentScale;
                this.boneAxis = boneAxis;
                this.radius = radius;
                this.stiffnessForce = stiffnessForce;
                this.dragForce = dragForce;
                this.springForce = springForce;
                this.springLength = springLength;
                this.isRootChild = isRootChild;
            }

            /// <summary>
            /// Whether it is the bone which becomes the root of the hierarchy.
            /// </summary>
            /// <returns>If true, it indicates that it is the root SpringBone.</returns>
            public bool IsRootBone
            {
                get
                {
                    return isRootChild == TRUE;
                }
            }
        }

        const int TRUE = 1;
        const int FALSE = 0;

        [HideInInspector]
        public Transform cachedTransform;
        [HideInInspector]
        public Transform child;
        public Vector3 boneAxis = new Vector3(-1.0f, 0.0f, 0.0f);
        public float radius = 0.1f;
        public float stiffnessForce = 0.05f;
        public float dragForce = 2f;
        public Vector3 springForce = new Vector3(0.0f, 0.0f, 0.0f);
        public Data data;

        [SerializeField]
        bool hideOnPlayMode = false;

        /// <summary>
        /// Initialize SpringBone data.
        /// </summary>
        /// <param name="root">Root of the chain of SpringBone.</param>
        public void Initialize(SpringBoneChain root)
        {
            // get child.
            if (child == null)
                child = transform.GetChild(0);

            // cache transform.
            cachedTransform = transform;

            // whether or not the root bone.
            var isRootChild = transform.parent.GetComponent<SpringBone>() == null ? TRUE : FALSE;

            // make bone data.
            data = new Data(
                cachedTransform.localPosition,
                cachedTransform.position,
                child.position,
                child.position,
                cachedTransform.localRotation,
                cachedTransform.rotation,
                cachedTransform.parent.lossyScale,
                boneAxis,
                radius,
                stiffnessForce,
                dragForce,
                springForce,
                Vector3.Distance(cachedTransform.position, child.position),
                isRootChild
            );
        }

        private void Start()
        {
            // hide in hierarchy.
            if(hideOnPlayMode)
                gameObject.hideFlags |= HideFlags.HideInHierarchy;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}