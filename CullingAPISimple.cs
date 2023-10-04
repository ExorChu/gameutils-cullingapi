using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace GameUtils.CullingAPI.Simple
{    
    public struct CullingElement
    {
        internal const int VISIBLE_CHANGED = 1 << 0;
        internal const int LOD_CHANGED = 1 << 1;

        public int id;
        internal int visiblity;
        internal int flags;
        internal int queryVisibility;

        public bool IsVisible => visiblity > -1;
        public int LOD => visiblity;

        public bool HasVisibleChanged
        {
            get => (flags & VISIBLE_CHANGED) == VISIBLE_CHANGED;
            internal set
            {
                if (value)
                    flags |= VISIBLE_CHANGED;
                else
                    flags &= ~VISIBLE_CHANGED;
            }
        }

        public bool HasLODChanged
        {
            get => (flags & LOD_CHANGED) == LOD_CHANGED;
            internal set
            {
                if (value)
                    flags |= LOD_CHANGED;
                else
                    flags &= ~LOD_CHANGED;
            }
        }

        public bool HasAnyChanged => (flags & (VISIBLE_CHANGED | LOD_CHANGED)) > 0;

        public void ClearFlags()
        {
            flags = 0;
        }
    }

    public class CullingAPISimple
    {
        private int maxElement;
        private BoundingSphere[] boundingSpheres;
        private CullingGroup cullingGroup;
        private CullingElement[] elements;
        private float[] distanceBands;

        private int count;
        private int generatedId;
        private int[] ids;

        private int[] queryArray;

        private Queue<int> leftoverIds;
        private int bandCount;

        public CullingAPISimple(int maxElementCount)
        {
            maxElement = maxElementCount;
            
            boundingSpheres = new BoundingSphere[maxElementCount];
            queryArray = new int[maxElementCount];
            elements = new CullingElement[maxElementCount];
            ids = new int[maxElementCount];
            cullingGroup = new CullingGroup();
            cullingGroup.SetBoundingSphereCount(0);
            cullingGroup.SetBoundingSpheres(boundingSpheres);            
            count = 0;
            leftoverIds = new Queue<int>();
        }

        public void SetCamera(Camera camera)
        {
            cullingGroup.targetCamera = camera;
        }
        public void SetReferencePoint(Vector3 point)
        {
            cullingGroup.SetDistanceReferencePoint(point);
        }      
        
        public void SetDistanceBands(float[] distanceBands)
        {
            bandCount = distanceBands.Length;
            this.distanceBands = distanceBands;

            cullingGroup.SetBoundingDistances(distanceBands);
        }

        public int AddNewElement()
        {
            int id = GenerateNewId();

            ids[count] = id;
            ref var ele = ref elements[count];

            ele.id = id;

            count++;
            cullingGroup.SetBoundingSphereCount(count);
            return id;
        }

        private int GenerateNewId()
        {
            if (leftoverIds.Count > 0)
            {
                int id = leftoverIds.Dequeue();
                return id;
            }
            return generatedId++;
        }

        public void UpdateElement(int id, Vector3 position, float? radius = null)
        {
            if (!TryGetIndex(id, out int index))
                return;

            ref var bs = ref boundingSpheres[index];
            bs.position = position;
            if (radius.HasValue)
            {
                bs.radius = radius.Value;
            }
        }
        private bool TryGetIndex(int id, out int index)
        {
            index = Array.BinarySearch(ids, 0, count, id);
            return index >= 0;
        }

        public void RemoveElement(int id)
        {
            if (!TryGetIndex(id, out int index))
                return;

            for (int i = index + 1; i < count; i++)
            {
                boundingSpheres[i - 1] = boundingSpheres[i];
                ids[i - 1] = ids[i];
                elements[i - 1] = elements[i];
            }
            count--;
            cullingGroup.SetBoundingSphereCount(count);
        }

        public Span<CullingElement> GetElements()
        {
            return new Span<CullingElement>(elements, 0, count);
        }

        public void Update()
        {
            for (int i = 0; i < count; i++)
            {
                ref var ele = ref elements[i];
                ele.queryVisibility = -1;
            }

            int qCount;

            for (int i = 0; i < bandCount; i++)
            {
                int band = i;
                qCount = cullingGroup.QueryIndices(true, band, queryArray, 0);
                for (int j = 0; j < qCount; j++)
                {
                    ref var ele = ref elements[queryArray[j]];
                    ele.queryVisibility = band;
                }
            }

            for (int i = 0; i < count; i++)
            {
                ref var ele = ref elements[i];
                if(ele.queryVisibility != ele.visiblity)
                {
                    if(ele.queryVisibility == -1 || ele.visiblity == -1)
                    {
                        ele.HasVisibleChanged = true;
                    }
                    else
                    {
                        ele.HasLODChanged = true;
                    }
                    ele.visiblity = ele.queryVisibility;
                }
            }
        }

        public void Dispose()
        {
            if(cullingGroup != null)
            {
                cullingGroup.Dispose();
            }
        }
    }
}
