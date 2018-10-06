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

using BoneData = Es.uSpringBone.SpringBoneData;

namespace Es.uSpringBone
{
    /// <summary>
    /// Manages the scheduling of Job required by SpringBone.
    /// </summary>
    [ScriptExecutionOrder(-32000)]
    public class SpringBoneJobScheduler : MonoBehaviour
    {
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

            Profiler.BeginSample("<> Update Data");
            foreach (var chain in chains)
                chain.UpdateParentData();
            Profiler.EndSample();
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
        }
    }
}