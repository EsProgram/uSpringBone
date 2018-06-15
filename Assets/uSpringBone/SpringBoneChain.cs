using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity;
using Unity.Jobs;
using Unity.Burst;

using BoneData = Es.uSpringBone.SpringBone.Data;
using ColliderData = Es.uSpringBone.SpringBoneCollider.Data;

namespace Es.uSpringBone
{
    /// <summary>
    /// One chain hierarchy of SpringBone.
    /// </summary>
    [ScriptExecutionOrder(0)]
    public class SpringBoneChain : MonoBehaviour
    {
        struct ParentData
        {
            public Vector3 grobalPosition;
            public Quaternion grobalRotation;
        }

        [BurstCompile]
        struct SpringBoneJob : IJob
        {
            public NativeArray<BoneData> boneData;
            [ReadOnly]
            public NativeArray<ParentData> parentData;
            [ReadOnly]
            public NativeArray<ColliderData> colliderData;
            public float dt;

            public void Execute()
            {
                Vector3 parentPosition = Vector3.zero;
                Quaternion parentRotation = Quaternion.identity;
                var sqrDt = dt * dt;
                var parentIndex = 0;

                for (int i = 0; i < boneData.Length; ++i)
                {
                    if (boneData[i].IsRootBone)
                    {
                        parentPosition = parentData[parentIndex].grobalPosition;
                        parentRotation = parentData[parentIndex].grobalRotation;
                        ++parentIndex;
                    }

                    var localPosition = boneData[i].localPosition;
                    var localRotation = Quaternion.identity * boneData[i].localRotation;
                    var grobalPosition = parentPosition + parentRotation * localPosition;
                    var grobalRotation = parentRotation * localRotation;

                    // calculate force
                    Vector3 force = grobalRotation * (boneData[i].boneAxis * boneData[i].stiffnessForce) / sqrDt;
                    force += (boneData[i].previousEndpoint - boneData[i].currentEndpoint) * boneData[i].dragForce / sqrDt;
                    force += boneData[i].springForce / sqrDt;

                    Vector3 temp = boneData[i].currentEndpoint;
                    var dataTemp = boneData[i];

                    // calculate next endpoint position
                    dataTemp.currentEndpoint = (dataTemp.currentEndpoint - dataTemp.previousEndpoint) + dataTemp.currentEndpoint + (force * sqrDt);
                    dataTemp.currentEndpoint = ((dataTemp.currentEndpoint - grobalPosition).normalized * dataTemp.springLength) + grobalPosition;

                    // collision
                    for (int j = 0; j < colliderData.Length; j++)
                        if (Vector3.Distance(dataTemp.currentEndpoint, colliderData[j].grobalPosition) <= (boneData[i].radius + colliderData[j].radius))
                        {
                            Vector3 normal = (dataTemp.currentEndpoint - colliderData[j].grobalPosition).normalized;
                            dataTemp.currentEndpoint = colliderData[j].grobalPosition + (normal * (boneData[i].radius + colliderData[j].radius));
                            dataTemp.currentEndpoint = ((dataTemp.currentEndpoint - grobalPosition).normalized * dataTemp.springLength) + grobalPosition;
                        }

                    dataTemp.previousEndpoint = temp;

                    // calculate next rotation
                    Vector3 currentDirection = parentRotation * boneData[i].boneAxis;
                    Quaternion targetRotation = Quaternion.FromToRotation(currentDirection, dataTemp.currentEndpoint - grobalPosition);

                    dataTemp.grobalPosition = parentPosition + parentRotation * localPosition;
                    dataTemp.grobalRotation = targetRotation * parentRotation;
                    parentPosition = dataTemp.grobalPosition;
                    parentRotation = dataTemp.grobalRotation;

                    boneData[i] = dataTemp;
                }
            }
        }

        public SpringBoneCollider[] colliders;

        SpringBone[] bones;
        Transform[] rootBoneParents;
        Transform cachedTransform;
        NativeArray<BoneData> boneData;
        NativeArray<ParentData> parentData;
        NativeArray<ColliderData> colliderData;
        BoneData[] boneDataTemp;
        ParentData[] parentDataTemp;
        ColliderData[] colliderDataTemp;
        SpringBoneJob job;
        JobHandle jobHandle;

        void Start()
        {
            SpringBoneJobScheduler.Instance.Register(this);

            cachedTransform = transform;
            bones = GetComponentsInChildren<SpringBone>();

            // Initialization of SpringBone.
            foreach (var bone in bones)
                bone.Initialize(this);

            // Cache parent's transform.
            rootBoneParents = bones.Where(t => t.data.IsRootBone)
                .Select(t => t.transform.parent)
                .ToArray();

            // Cancel parentage relationship.
            foreach (var bone in bones)
                bone.transform.SetParent(transform);

            // Memory allocation.
            boneDataTemp = new BoneData[bones.Length];
            parentDataTemp = new ParentData[rootBoneParents.Length];
            colliderDataTemp = new ColliderData[colliders.Length];
            boneData = new NativeArray<BoneData>(bones.Length, Allocator.Persistent);
            parentData = new NativeArray<ParentData>(rootBoneParents.Length, Allocator.Persistent);
            colliderData = new NativeArray<ColliderData>(colliders.Length, Allocator.Persistent);

            // Set data.
            SetBoneData();
            SetParentData();
            SetColliderData();

            // Create a job.
            job = new SpringBoneJob()
            {
                boneData = boneData,
                parentData = parentData,
                colliderData = colliderData
            };
        }

        void LateUpdate()
        {
            Profiler.BeginSample("<> JobComplete");
            jobHandle.Complete();
            Profiler.EndSample();

            Profiler.BeginSample("<> Copy NativeArray");
            boneData.CopyTo(boneDataTemp);
            Profiler.EndSample();

            Profiler.BeginSample("<> Apply Transform");
            for (int i = 0; i < bones.Length; ++i)
                bones[i].cachedTransform.SetPositionAndRotation(boneDataTemp[i].grobalPosition, boneDataTemp[i].grobalRotation);
            Profiler.EndSample();

            Profiler.BeginSample("<> Update Parent Data");
            SetParentData();
            Profiler.EndSample();

            Profiler.BeginSample("<> Update Colliders");
            SetColliderData();
            Profiler.EndSample();
        }

        void OnDestroy()
        {
            boneData.Dispose();
            colliderData.Dispose();
            parentData.Dispose();
        }

        /// <summary>
        /// Schedule the Job.
        /// </summary>
        public void ScheduleJob()
        {
            job.dt = Time.deltaTime;
            jobHandle = job.Schedule();
        }

        /// <summary>
        /// Set information on Bone.
        /// </summary>
        private void SetBoneData()
        {
            for (int i = 0; i < bones.Length; ++i)
                boneDataTemp[i] = bones[i].data;
            boneData.CopyFrom(boneDataTemp);
        }

        /// <summary>
        /// Set the parent's information of RootBone.
        /// </summary>
        private void SetParentData()
        {
            for (int i = 0; i < rootBoneParents.Length; ++i)
            {
                parentDataTemp[i].grobalPosition = rootBoneParents[i].position;
                parentDataTemp[i].grobalRotation = rootBoneParents[i].rotation;
            }
            parentData.CopyFrom(parentDataTemp);
        }

        /// <summary>
        /// Set collider's information.
        /// </summary>
        private void SetColliderData()
        {
            for (int i = 0; i < colliders.Length; ++i)
                colliderDataTemp[i] = colliders[i].data;
            colliderData.CopyFrom(colliderDataTemp);
        }
    }
}