using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;

namespace Es.uSpringBone
{
    /// <summary>
    /// Sphere collider data.
    /// </summary>
    [System.Serializable]
    public struct SphereColliderData : IComponentData
    {
        [HideInInspector]
        public Entity entity;
        [HideInInspector]
        public float radius;

        public SphereColliderData(Entity entity, float radius)
        {
            this.entity = entity;
            this.radius = radius;
        }
    }

    /// <summary>
    /// Collision determination component.
    /// </summary>
    [ScriptExecutionOrder(-30000)]
    [RequireComponent(typeof(PositionComponent), typeof(CopyTransformFromGameObjectComponent))]
    public class SphereColliderComponent : ComponentDataWrapper<SphereColliderData>
    {
        public float radius;

        public void Initialize()
        {
            var gameObjectEntity = GetComponent<GameObjectEntity>();
            Value = new SphereColliderData(gameObjectEntity.Entity, radius);
        }

        // TODO: radiusが変わった時のみentityをupdateするように
        // void Update()
        // {
        //     var tmp = Value;
        //     tmp.radius = radius;
        //     Value = tmp;
        // }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var tmp = Gizmos.color;
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.8f);
            Gizmos.DrawSphere(transform.position, radius);
            Gizmos.color = tmp;
        }
#endif
    }
}