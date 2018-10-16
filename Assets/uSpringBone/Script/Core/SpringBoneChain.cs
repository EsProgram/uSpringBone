using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Jobs;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

using static Unity.Mathematics.math;
using BoneData = Es.uSpringBone.SpringBoneData;
using ParentData = Es.uSpringBone.SpringBoneParentData;
using ColliderData = Es.uSpringBone.SphereColliderData;

namespace Es.uSpringBone
{
    /// <summary>
    /// One chain hierarchy of SpringBone.
    /// </summary>
    [ScriptExecutionOrder(0)]
    public class SpringBoneChain : MonoBehaviour
    {
        /// <summary>
        /// The main logic of SpringBone.
        /// Calculate the position of the child in the next frame and get rotation in that direction.
        /// </summary>
        [BurstCompile]
        public struct SpringBoneJob : IJob
        {
            public NativeArray<BoneData> boneData;
            [ReadOnly]
            public NativeArray<ColliderData> colliderData;
            public NativeList<ColliderData> selectedColliderList;
            public float dt;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<Position> position;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<Rotation> rotation;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<ParentData> parentData;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public ComponentDataFromEntity<ColliderData> collider;

            public unsafe void Execute()
            {
                float3 parentPosition = Vector3.zero;
                quaternion parentRotation = Quaternion.identity;
                var sqrDt = dt * dt;
                var parentIndex = 0;

                // create collider list.
                selectedColliderList.Clear();
                for(int i = 0; i < colliderData.Length; ++i)
                    selectedColliderList.Add(collider[colliderData[i].entity]);

                // spring.
                for (int i = 0; i < boneData.Length; ++i)
                {
                    var bone = boneData[i];

                    // set root parent data.
                    if (bone.IsRootBone)
                    {
                        parentPosition = position[bone.parent].Value;
                        parentRotation = rotation[bone.parent].Value;
                        ++parentIndex;
                    }

                    // get local and grobal position.
                    var localPosition = bone.localPosition * bone.parentScale;
                    var localRotation = bone.localRotation;
                    var grobalPosition = parentPosition + mul(parentRotation, localPosition);
                    var grobalRotation = mul(parentRotation, localRotation);

                    // calculate force.
                    float3 force = mul(grobalRotation, (bone.boneAxis * bone.stiffnessForce)) / sqrDt;
                    force += (bone.previousEndpoint - bone.currentEndpoint) * bone.dragForce / sqrDt;
                    force += bone.springForce / sqrDt;

                    float3 temp = bone.currentEndpoint;
                    var dataTemp = boneData[i];

                    // calculate next endpoint position.
                    dataTemp.currentEndpoint = (dataTemp.currentEndpoint - dataTemp.previousEndpoint) + dataTemp.currentEndpoint + (force * sqrDt);
                    dataTemp.currentEndpoint = (normalize(dataTemp.currentEndpoint - grobalPosition) * dataTemp.springLength) + grobalPosition;

                    // collision.
                    for (int j = 0; j < selectedColliderList.Length; ++j)
                    {
                        var collider = colliderData[j];
                        var colliderPosition = position[selectedColliderList[j].entity].Value;
                        if (distance(dataTemp.currentEndpoint, colliderPosition) <= (bone.radius + collider.radius))
                        {
                            float3 normal = normalize(dataTemp.currentEndpoint - colliderPosition);
                            dataTemp.currentEndpoint = colliderPosition + (normal * (bone.radius + collider.radius));
                            dataTemp.currentEndpoint = (normalize(dataTemp.currentEndpoint - grobalPosition) * dataTemp.springLength) + grobalPosition;
                        }
                    }

                    dataTemp.previousEndpoint = temp;

                    // calculate next rotation.
                    float3 from = mul(parentRotation, bone.boneAxis);
                    float3 to = dataTemp.currentEndpoint - grobalPosition;
                    float diff = length(from - to);
                    quaternion targetRotation = Quaternion.identity;
                    if(float.MinValue < diff && diff < float.MaxValue)
                        targetRotation = Quaternion.FromToRotation(from, to);

                    // set calculated data.
                    dataTemp.grobalPosition = parentPosition + mul(parentRotation, localPosition);
                    dataTemp.grobalRotation = Quaternion.Lerp(dataTemp.grobalRotation, mul(targetRotation, parentRotation), 1f); // TODO: lerp parameter
                    parentPosition = dataTemp.grobalPosition;
                    parentRotation = dataTemp.grobalRotation;

                    // update data.
                    boneData[i] = dataTemp;

                    // update entity.
                    position[dataTemp.entity] = new Position() { Value = dataTemp.grobalPosition };
                    rotation[dataTemp.entity] = new Rotation() { Value = dataTemp.grobalRotation };
                }
            }
        }

        [SerializeField]
        private SphereColliderComponent[] colliders;
        [SerializeField]
        private bool optimizeGameObject = true;

        private SpringBoneComponent[] bones;
        private Transform[] rootBoneParents;
        private NativeArray<BoneData> boneData;
        private NativeArray<ColliderData> colliderData;
        private NativeList<ColliderData> selectedColliderList;
        private BoneData[] boneDataTemp;
        private ColliderData[] colliderDataTemp;
        private SpringBoneJob calculateJob;
        private JobHandle calculateJobHandle;
        private ComponentDataFromEntity<Position> position;
        private ComponentDataFromEntity<Rotation> rotation;
        private ComponentDataFromEntity<ParentData> parent;
        private ComponentDataFromEntity<ColliderData> sphereCollider;

        public SpringBoneComponent[] Bones { get { return bones; } }
        public NativeArray<BoneData> BoneData { get { return boneData; } }

        void Start()
        {
            bones = GetComponentsInChildren<SpringBoneComponent>();

            // Initialization of SpringBone.
            foreach (var bone in bones)
                bone.Initialize();

            // Initialization of SphereCollider
            foreach (var col in colliders)
                col.Initialize();

            // Cache parent's transform.
            rootBoneParents = bones.Where(t => t.Value.IsRootBone)
                .Select(t => t.transform.parent)
                .ToArray();

            // Cancel parentage relationship.
            if(optimizeGameObject)
                foreach (var bone in bones)
                    bone.transform.SetParent(null);

            // Memory allocation.
            boneDataTemp = new BoneData[bones.Length];
            colliderDataTemp = new ColliderData[colliders.Length];
            boneData = new NativeArray<BoneData>(bones.Length, Allocator.Persistent);
            colliderData = new NativeArray<ColliderData>(colliders.Length, Allocator.Persistent);
            selectedColliderList = new NativeList<ColliderData>(bones.Length, Allocator.Persistent);

            // Set data.
            SetBoneData();
            UpdateColliderData();
            UpdateComponentData();

            // Register this component.
            SpringBoneJobScheduler.Instance.Register(this);

            // Create a job.
            calculateJob = new SpringBoneJob()
            {
                boneData = boneData,
                colliderData = colliderData,
                selectedColliderList = selectedColliderList,
                position = position,
                rotation = rotation,
                parentData = parent,
                collider = sphereCollider
            };
        }

        void OnDestroy()
        {
            boneData.Dispose();
            colliderData.Dispose();
            selectedColliderList.Dispose();
        }

        /// <summary>
        /// Schedule transform calculate Job.
        /// </summary>
        public void ScheduleCalculateJob()
        {
            calculateJob = new SpringBoneJob()
            {
                boneData = boneData,
                colliderData = colliderData,
                selectedColliderList = selectedColliderList,
                position = position,
                rotation = rotation,
                parentData = parent,
                collider = sphereCollider
            };
            calculateJob.dt = Time.deltaTime;
            calculateJobHandle = calculateJob.Schedule();
        }

        /// <summary>
        /// Wait for completion of calculate Job.
        /// </summary>
        public void CompleteCalculateJob()
        {
            calculateJobHandle.Complete();
        }

        /// <summary>
        /// Update collider's information.
        /// </summary>
        public void UpdateColliderData()
        {
            for (int i = 0; i < colliders.Length; ++i)
                colliderDataTemp[i] = colliders[i].Value;
            colliderData.CopyFrom(colliderDataTemp);
        }

        /// <summary>
        /// Update component data.
        /// </summary>
        public void UpdateComponentData()
        {
            var boneEntityManager = bones[0].GetComponent<GameObjectEntity>().EntityManager;// TODO: cahce
            position = boneEntityManager.GetComponentDataFromEntity<Position>();
            rotation = boneEntityManager.GetComponentDataFromEntity<Rotation>();
            parent = boneEntityManager.GetComponentDataFromEntity<ParentData>();
            sphereCollider = boneEntityManager.GetComponentDataFromEntity<SphereColliderData>();
        }

        /// <summary>
        /// Set information on Bone.
        /// </summary>
        private void SetBoneData()
        {
            for (int i = 0; i < bones.Length; ++i)
                boneDataTemp[i] = bones[i].Value;
            boneData.CopyFrom(boneDataTemp);
        }
    }
}