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
        [BurstCompile]
        struct CopyNativeArrayJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public unsafe BoneData * srcHeadPtr;
            [NativeDisableUnsafePtrRestriction]
            public unsafe BoneData * dstHeadPtr;
            public int length;

            public unsafe void Execute()
            {
                for (int i = 0; i < length; ++i)
                {
                    var srcPtr = (srcHeadPtr + i);
                    var dstPtr = (dstHeadPtr + i);
                    dstPtr -> grobalPosition = srcPtr -> grobalPosition;
                    dstPtr -> grobalRotation = srcPtr -> grobalRotation;
                }
            }
        }

        [BurstCompile]
        struct ApplyTransformJob : IJobParallelForTransform
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<BoneData> data;

            public void Execute(int index, TransformAccess transform)
            {
                transform.position = data[index].grobalPosition;
                transform.rotation = data[index].grobalRotation;
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
        private TransformAccessArray access;
        private ApplyTransformJob transformJob = new ApplyTransformJob();
        private List<JobHandle> copyJobHandle = new List<JobHandle>();

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
                chain.ScheduleJob();
            Profiler.EndSample();

            Profiler.BeginSample("<> Batch");
            JobHandle.ScheduleBatchedJobs();
            Profiler.EndSample();
        }

        private unsafe void LateUpdate()
        {
            Profiler.BeginSample("<> JobComplete");
            foreach (var chain in chains)
                chain.CompleteJob();
            Profiler.EndSample();

            // copy transform data.
            NativeArray<BoneData> data = ScheduleCopyBoneDataJob();

            Profiler.BeginSample("<> Batch");
            JobHandle.ScheduleBatchedJobs();
            Profiler.EndSample();

            foreach (var chain in chains)
            {
                chain.UpdateParentData();
                chain.UpdateColliderData();
            }


            Profiler.BeginSample("<> Complete");
            foreach (var handle in copyJobHandle)
                handle.Complete();
            Profiler.EndSample();


            Profiler.BeginSample("<> Transform Apply");
            transformJob.data = data;
            var transformJobHandle = transformJob.Schedule(access);
            JobHandle.ScheduleBatchedJobs();
            transformJobHandle.Complete();
            Profiler.EndSample();
        }

        private void OnDestroy()
        {
            access.Dispose();
        }

        private unsafe NativeArray<BoneData> ScheduleCopyBoneDataJob()
        {
            copyJobHandle.Clear();
            var data = new NativeArray<BoneData>(access.length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var copyJob = new CopyNativeArrayJob();
            int index = 0;
            for (int i = 0; i < chains.Count; ++i)
            {
                Profiler.BeginSample("<> Slice");
                var slice = data.Slice(index, chains[i].BoneData.Length);
                Profiler.EndSample();
                Profiler.BeginSample("<> Create Job");
                copyJob.srcHeadPtr = (BoneData*)chains[i].BoneData.GetUnsafeReadOnlyPtr();
                copyJob.dstHeadPtr = (BoneData*)slice.GetUnsafePtr();
                copyJob.length = chains[i].BoneData.Length;
                Profiler.EndSample();
                index += chains[i].BoneData.Length;

                Profiler.BeginSample("<> Schedule");
                copyJobHandle.Add(copyJob.Schedule());
                Profiler.EndSample();
            }

            return data;
        }

        /// <summary>
        /// Register the chain of SpringBone.
        /// </summary>
        /// <param name="chain">Chain of SpringBone.</param>
        public void Register(SpringBoneChain chain)
        {
            if (chain == null)
                return;

            if (chains.Contains(chain))
                return;

            chains.Add(chain);

            if (!access.isCreated)
                access = new TransformAccessArray(5000);

            foreach (var bone in chain.Bones)
                access.Add(bone.cachedTransform);
        }
    }
}