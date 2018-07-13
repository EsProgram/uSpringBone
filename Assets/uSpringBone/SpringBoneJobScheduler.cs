using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Profiling;

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
                chain.ScheduleJob();
            Profiler.EndSample();

            Profiler.BeginSample("<> Batch");
            JobHandle.ScheduleBatchedJobs();
            Profiler.EndSample();
        }

        /// <summary>
        /// Register the chain of SpringBone.
        /// </summary>
        /// <param name="chain">Chain of SpringBone.</param>
        public void Register(SpringBoneChain chain)
        {
            if(chain == null)
                return;

            if(!chains.Contains(chain))
                chains.Add(chain);
        }

    }
}