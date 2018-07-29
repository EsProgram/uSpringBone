using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using UnityEngine.Profiling;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity;
using Unity.Collections.LowLevel.Unsafe;

using BoneData = Es.uSpringBone.SpringBone.Data;

namespace Es.uSpringBone
{
    /// <summary>
    /// Manages the scheduling of Job required by SpringBone.
    /// </summary>
    [ScriptExecutionOrder(-32000)]
    public class SpringBoneJobScheduler : MonoBehaviour
    {
        /// <summary>
        /// Apply the calculated Transform.
        /// It is expensive processing on UnityEditor, but it is relatively inexpensive after compilation.
        /// </summary>
        [BurstCompile]
        struct ApplyTransformJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeArray<IntPtr> boneHeadPtrArray;
            [ReadOnly]
            public NativeArray<int> boneLengthArray;

            public unsafe void Execute(int index, TransformAccess transform)
            {
                var ptr = GetDataPtr(index);

                transform.position = ptr -> grobalPosition;
                transform.rotation = ptr -> grobalRotation;
            }

            private unsafe BoneData* GetDataPtr(int currentIndex)
            {
                var headPtrIndex = 0;
                var elemPtrIndex = currentIndex;

                for(int i = 0; i < boneLengthArray.Length; ++i)
                {
                    headPtrIndex = i;
                    elemPtrIndex = currentIndex;
                    currentIndex = currentIndex - boneLengthArray[i];
                    if(currentIndex < 0)
                        break;
                }

                var head = (BoneData*)boneHeadPtrArray[headPtrIndex];
                var elem = (BoneData*)(head + elemPtrIndex);
                return elem;
            }
        }

        /// <summary>
        /// Returns the instance of the scheduler.
        /// If not, it will be generated.
        /// </summary>
        /// <returns>Instance of the scheduler.</returns>
        public static SpringBoneJobScheduler Instance
        {
            get
            {
                if (instance == null)
                {
                    var obj = new GameObject();
                    obj.name = DefaultObjectName;
                    obj.hideFlags = HideFlags.HideInHierarchy;
                    instance = obj.AddComponent<SpringBoneJobScheduler>();
                }
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        private const string DefaultObjectName = "SpringBoneManager";
        private static SpringBoneJobScheduler instance;
        private List<SpringBoneChain> chains = new List<SpringBoneChain>();
        private NativeList<int> boneLengthArray;
        private NativeList<IntPtr> boneHeadPtrArray;
        private TransformAccessArray access;
        private ApplyTransformJob applyJob;

        private void Awake()
        {
            if (instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            Profiler.BeginSample("<> Check Chanes List");
            chains.RemoveAll(c => c == null);
            Profiler.EndSample();

            Profiler.BeginSample("<> Schedule");
            foreach (var chain in chains)
                chain.ScheduleCalculateJob();
            Profiler.EndSample();

            Profiler.BeginSample("<> Batch");
            JobHandle.ScheduleBatchedJobs();
            Profiler.EndSample();
        }

        private unsafe void LateUpdate()
        {
            Profiler.BeginSample("<> JobComplete");
            foreach (var chain in chains)
                chain.CompleteCalculateJob();
            Profiler.EndSample();

            Profiler.BeginSample("<>Schedule");
            var applyJobHandle = applyJob.Schedule(access);
            JobHandle.ScheduleBatchedJobs();
            Profiler.EndSample();

            Profiler.BeginSample("<> Update Data");
            foreach (var chain in chains)
            {
                chain.UpdateParentData();
                chain.UpdateColliderData();
            }
            Profiler.EndSample();

            Profiler.BeginSample("<> JobComplete");
            // TODO: delay.
            applyJobHandle.Complete();
            Profiler.EndSample();
        }

        void OnDestroy()
        {
            access.Dispose();
            boneLengthArray.Dispose();
            boneHeadPtrArray.Dispose();
        }

        /// <summary>
        /// Register the chain of SpringBone.
        /// </summary>
        /// <param name="chain">Chain of SpringBone.</param>
        public unsafe void Register(SpringBoneChain chain)
        {
            if (chain == null)
                return;

            if (chains.Contains(chain))
                return;

            chains.Add(chain);

            if (!access.isCreated)
                access = new TransformAccessArray(9999);
            foreach (var bone in chain.Bones)
                access.Add(bone.cachedTransform);

            if(!boneLengthArray.IsCreated)
                boneLengthArray = new NativeList<int>(Allocator.Persistent);
            if(!boneHeadPtrArray.IsCreated)
                boneHeadPtrArray = new NativeList<IntPtr>(Allocator.Persistent);

            boneLengthArray.Add(chain.Bones.Length);
            boneHeadPtrArray.Add((IntPtr)chain.BoneData.GetUnsafeReadOnlyPtr());

            applyJob = new ApplyTransformJob()
            {
                boneLengthArray = boneLengthArray,
                boneHeadPtrArray = boneHeadPtrArray
            };
        }
    }
}