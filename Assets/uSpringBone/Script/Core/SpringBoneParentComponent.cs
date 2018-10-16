using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;

namespace Es.uSpringBone
{
    [System.Serializable]
    public struct SpringBoneParentData : IComponentData { }

    [RequireComponent(typeof(CopyTransformFromGameObjectComponent))]
    public class SpringBoneParentComponent : ComponentDataWrapper<SpringBoneParentData> { }
}