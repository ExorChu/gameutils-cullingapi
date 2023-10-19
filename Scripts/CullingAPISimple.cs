using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameUtils.CullingAPI.Simple
{    
    public struct CullingElement
    {
        internal const int VISIBLE_CHANGED = 1 << 0;
        internal const int LOD_CHANGED = 1 << 1;

        public int id;
        internal bool isVisible;
        internal int distanceBand;
        internal int flags;
        internal bool queryVisible;
        internal int queryDistanceBand;

        public bool IsVisible => isVisible;
        public int LOD => distanceBand;

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
        private int elementChangeCount;

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
            index = Array.IndexOf(ids, id, 0, count);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasAnyElementChanged() => elementChangeCount > 0;

        public int GetChangedElements(int[] indices) => GetChangedElements(new Span<int>(indices));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref CullingElement GetElementRef(int index) => ref elements[index];

        public int GetChangedElements(Span<int> indices)
        {
            int len = indices.Length;
            int min = elementChangeCount > len ? len : elementChangeCount;

            int totalElement = 0;
            for (int i = 0; i < count; i++)
            {
                if (totalElement >= min)
                    break;
                ref var ele = ref elements[i];
                if (ele.HasAnyChanged)
                {
                    indices[totalElement++] = ele.id;
                }
            }
            return totalElement;
        }

        public void Update()
        {
            for (int i = 0; i < count; i++)
            {
                ref var ele = ref elements[i];
                ele.queryDistanceBand = -1;
                ele.queryVisible = false;
            }

            int qCount;

            qCount = cullingGroup.QueryIndices(true, queryArray, 0);

            for (int i = 0; i < qCount; i++)
            {
                ref var ele = ref elements[queryArray[i]];
                ele.queryVisible = true;
            }

            for (int i = 0; i < bandCount; i++)
            {
                int band = i;
                qCount = cullingGroup.QueryIndices(band, queryArray, 0);
                for (int j = 0; j < qCount; j++)
                {
                    ref var ele = ref elements[queryArray[j]];
                    ele.queryDistanceBand = band;         
                }
            }

            elementChangeCount = 0;
            for (int i = 0; i < count; i++)
            {
                ref var ele = ref elements[i];

                if (ele.queryVisible ^ ele.isVisible)
                {
                    ele.isVisible = ele.queryVisible;
                    ele.HasVisibleChanged = true;
                }

                if(ele.queryDistanceBand != ele.distanceBand)
                {
                    ele.HasLODChanged = true;
                    ele.distanceBand = ele.queryDistanceBand;
                }

                if (ele.HasAnyChanged)
                {
                    elementChangeCount++;
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
