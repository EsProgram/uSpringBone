using UnityEngine;
using Unity;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System;
using System.Collections;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Es.uSpringBone
{
    /// <summary>
    /// SpringBone data.
    /// </summary>
    [Serializable]
    public struct SpringBoneData : IComponentData
    {
        const int TRUE = 1;
        [HideInInspector]
        public Entity entity;
        [HideInInspector]
        public Entity parent;
        [HideInInspector]
        public float3 localPosition;
        [HideInInspector]
        public float3 grobalPosition;
        [HideInInspector]
        public float3 currentEndpoint;
        [HideInInspector]
        public float3 previousEndpoint;
        [HideInInspector]
        public quaternion localRotation;
        [HideInInspector]
        public quaternion grobalRotation;
        [HideInInspector]
        public float3 parentScale;
        [HideInInspector]
        public float3 boneAxis;
        [HideInInspector]
        public float radius;
        [HideInInspector]
        public float stiffnessForce;
        [HideInInspector]
        public float dragForce;
        [HideInInspector]
        public float3 springForce;
        [HideInInspector]
        public float springLength;
        [HideInInspector]
        public int isRootChild;

        public SpringBoneData(
            Entity entity,
            Entity parent,
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
            this.entity = entity;
            this.parent = parent;
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
        public bool IsRootBone => isRootChild == TRUE;
    }


    /// <summary>
    /// Has bone specific data.
    /// </summary>
    [RequireComponent(typeof(CopyTransformToGameObjectComponent))]
    public class SpringBoneComponent : ComponentDataWrapper<SpringBoneData>
    {
        const int TRUE = 1;
        const int FALSE = 0;

        [HideInInspector, NonSerialized]
        public Transform child;
        public Vector3 boneAxis = new Vector3(-1.0f, 0.0f, 0.0f);
        public float radius = 0.1f;
        public float stiffnessForce = 0.05f;
        public float dragForce = 2f;
        public Vector3 springForce = new Vector3(0.0f, 0.0f, 0.0f);

        [SerializeField]
        Mesh debugMesh;

        /// <summary>
        /// Initialize SpringBone data.
        /// </summary>
        public void Initialize()
        {
            // get child.
            if (child == null)
                child = GetChild();

            #region // FIXME : Entities 0.0.12-preview.17 BUG
            //? CopyTransformToGameObjectComponent causes a bug when working on the Editor.
            //? Specifically, since we always try to rewrite Transform with global coordinates, we can not move the parent object.
            //? In addition, the local coordinates of Transform are rewritten to global coordinates, and the positional relationship becomes strange between parent and child.
            //? For that reason, we do not attach PositionComponent / RotationComponent in the Editor but add it dynamically with AddComponent.
            gameObject.AddComponent<PositionComponent>();
            gameObject.AddComponent<RotationComponent>();

            var gameObjectEntity = GetComponent<GameObjectEntity>();
            var cachedTransform = transform;
            var isRootChild = transform.parent.GetComponent<SpringBoneComponent>() == null ? TRUE : FALSE;

            // setup parent component data.
            var parent = Entity.Null;
            if(isRootChild == TRUE)
            {
                var parentTransform = transform.parent;
                var parentComponent = parentTransform.gameObject.AddComponent<SpringBoneParentComponent>();
                var parentPositionComponent = parentTransform.gameObject.AddComponent<PositionComponent>();
                var parentRotationComponent = parentTransform.gameObject.AddComponent<RotationComponent>();
                parentPositionComponent.Value = new Position() { Value = parentTransform.position };
                parentRotationComponent.Value = new Rotation() { Value = parentTransform.rotation };

                parent = parentComponent.GetComponent<GameObjectEntity>().Entity;
            }
            #endregion

            // make bone data.
            Value = new SpringBoneData(
                gameObjectEntity.Entity,
                parent,
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

        /// <summary>
        /// get child transform.
        /// </summary>
        /// <returns></returns>
        private Transform GetChild()
        {
            return transform.GetChild(0);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var tmp = Gizmos.color;
            Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
            var childTransform = EditorApplication.isPlaying ? child : GetChild();
            var length = EditorApplication.isPlaying ? Value.springLength : Vector3.Distance(transform.position, childTransform.position);
            Gizmos.DrawMesh(debugMesh, transform.position, transform.rotation, Vector3.one * radius + boneAxis * length);
            Gizmos.color = tmp;
        }
#endif
    }
}