using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace GameUtils.CullingAPI
{
    public interface ICullingAPI
    {
        public void SetCamera(Camera camera);
        public void SetAnchorTransform(Transform anchorTransform);
        public void AddElement(ICullingAPIComponent element);
        public void RemoveElement(ICullingAPIComponent element);
    }

    public class CullingAPISource : MonoBehaviour, ICullingAPI
    {
        private struct CullingElementInfo
        {
            public bool isValid;
            public bool visible;
            public int lod;
            public int queryLOD;
            public bool queryVisible;
        }

        [SerializeField] private int maxSupportElement = 1024;
        [SerializeField] private float[] distanceBands = new[] { 25f, 50f };

        private BoundingSphere[] boundingSpheres;
        private int currentCount;
        private CullingGroup cullingGroupAPI;
        private int[] queryArray;
        private int bandCount;
        private bool flagElementChanged = false;
        private CullingElementInfo[] elements;

        private HashSet<ICullingAPIComponent> addedHash = new HashSet<ICullingAPIComponent>();
        private ICullingAPIComponent[] mapArray;

        private void Awake()
        {
            elements = new CullingElementInfo[maxSupportElement];
            boundingSpheres = new BoundingSphere[maxSupportElement];
            queryArray = new int[maxSupportElement];
            mapArray = new ICullingAPIComponent[maxSupportElement];
            cullingGroupAPI = new CullingGroup();
            cullingGroupAPI.SetBoundingSpheres(boundingSpheres);
            cullingGroupAPI.SetBoundingSphereCount(0);
            cullingGroupAPI.SetBoundingDistances(distanceBands);
            bandCount = distanceBands.Length;
        }

        private void LateUpdate()
        {
            UnityEngine.Profiling.Profiler.BeginSample("calculate_last_results");
            CalculateLastResults();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("add_new_elements");
            AddNewElements();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("remove_null_elements");
            RemoveNullElements();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("prepare_next_frame");
            PrepareNextFrame();
            UnityEngine.Profiling.Profiler.EndSample();

            if (flagElementChanged)
            {
                cullingGroupAPI.SetBoundingSphereCount(currentCount);
            }
        }

        private void OnDestroy()
        {
            cullingGroupAPI.Dispose();
        }

        public void SetCamera(Camera camera)
        {
            cullingGroupAPI.targetCamera = camera;
        }

        public void SetAnchorTransform(Transform anchorTransform)
        {
            cullingGroupAPI.SetDistanceReferencePoint(anchorTransform);
        }

        public void AddElement(ICullingAPIComponent element)
        {
            addedHash.Add(element);
        }
        public void RemoveElement(ICullingAPIComponent element)
        {
            if (!addedHash.Remove(element))
            {
                OnRemoveCullingBehaviour(element);
            }
        }

        private void CalculateLastResults()
        {
            for (int i = 0; i < currentCount; i++)
            {
                ref var ele = ref elements[i];
                ele.queryLOD = -1;
                ele.queryVisible = false;
            }

            int count;
            for (int i = 0; i < bandCount; i++)
            {
                int band = i;
                count = cullingGroupAPI.QueryIndices(true, band, queryArray, 0);

                for (int j = 0; j < count; j++)
                {
                    ref var ele = ref elements[queryArray[j]];
                    ele.queryLOD = band;
                    ele.queryVisible = true;
                }
            }

            for (int i = 0; i < currentCount; i++)
            {
                ref var ele = ref elements[i];
                if (!ele.isValid)
                    continue;

                if (ele.queryVisible ^ ele.visible)
                {
                    ele.visible = ele.queryVisible;
                    var behaviour = mapArray[i];
                    ref var property = ref behaviour.GetPropertyRef();
                    property.visible = ele.lod;
                    property.HasVisibleChanged = true;
                }

                if (ele.queryLOD != ele.lod)
                {
                    ele.lod = ele.queryLOD;
                    var behaviour = mapArray[i];
                    ref var property = ref behaviour.GetPropertyRef();
                    property.visible = ele.lod;
                    property.HasLODChanged = true;
                }
            }
        }
        private void AddNewElements()
        {
            if (addedHash.Count == 0)
                return;

            foreach (var behaviour in addedHash)
            {
                if (behaviour == null)
                    continue;

                if (currentCount >= maxSupportElement)
                {
                    Debug.LogError("Max supported element reaches, ignore new members!");
                    break;
                }
                int id = currentCount++;
                elements[id] = new CullingElementInfo
                {
                    visible = false,
                    isValid = true,
                    lod = -1
                };
                ref var prop = ref behaviour.GetPropertyRef();
                prop.flags = 0;
                prop.visible = -1;

                behaviour.GetCullInfo(out Vector3 pos, out float radius);

                boundingSpheres[id] = new BoundingSphere
                {
                    position = pos,
                    radius = radius
                };

                behaviour.OnRemove = OnRemoveCullingBehaviour;
                mapArray[id] = behaviour;
                prop.id = id;                
            }

            addedHash.Clear();
            flagElementChanged = true;
        }
        private void RemoveNullElements()
        {
            int step = 0;
            for (int i = 0; i < currentCount; i++)
            {
                ref var ele = ref elements[i];
                if (!ele.isValid)
                {
                    step++;
                    continue;
                }
                if (step == 0)
                    continue;

                elements[i - step] = elements[i];
                boundingSpheres[i - step] = boundingSpheres[i];
            }
            currentCount -= step;
            if (step > 0)
            {
                flagElementChanged = true;
            }
        }
        private void PrepareNextFrame()
        {
            Vector3 pos;
            float radius;
            for (int i = 0; i < currentCount; i++)
            {
                ref var bs = ref boundingSpheres[i];
                ref var ele = ref elements[i];
                if (!ele.isValid)
                    continue;

                var behaviour = mapArray[i];

                if (!behaviour.CheckTransformChanged(out pos, out radius))
                    continue;

                bs.position = pos;
                bs.radius = radius;
            }

        }

        private void OnRemoveCullingBehaviour(ICullingAPIComponent behaviour)
        {
            ref var prop = ref behaviour.GetPropertyRef();

            if (prop.id < 0 || prop.id >= currentCount)
                return;

            if (!ReferenceEquals(mapArray[prop.id], behaviour))
                return;

            mapArray[prop.id] = null;
            ref var ele = ref elements[prop.id];
            ele.isValid = false;

            
        }
    }
}