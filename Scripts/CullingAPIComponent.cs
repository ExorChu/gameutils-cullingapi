using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUtils.CullingAPI
{
    public interface ICullingAPIComponent
    {
        public delegate void DelCullingCallback(bool isVisible, int lod);

        public DelCullingCallback OnVisibilityChanged { get; set; }
        public Action<ICullingAPIComponent> OnRemove { get; set; }
        public ref Property GetPropertyRef();
        bool CheckTransformChanged(out Vector3 pos, out float radius);
        void GetCullInfo(out Vector3 pos, out float radius);

        public struct Property
        {
            public const int VISIBLE_CHANGED = 1 << 0;
            public const int LOD_CHANGED = 1 << 1;

            public int id;
            public int visible;
            public int flags;

            public bool HasVisibleChanged
            {
                get => (flags & VISIBLE_CHANGED) == VISIBLE_CHANGED;
                set
                {
                    if(value)
                        flags |= VISIBLE_CHANGED;
                    else
                        flags &= ~VISIBLE_CHANGED;
                }
            }
            public bool HasLODChanged
            {
                get => (flags & LOD_CHANGED) == LOD_CHANGED;
                set
                {
                    if(value)
                        flags |= LOD_CHANGED;
                    else
                        flags &= ~LOD_CHANGED;
                }
            }
            public bool HasAnyChanged => (flags & (VISIBLE_CHANGED | LOD_CHANGED)) > 0;
            public void ClearAnyChanges()
            {
                flags = 0;
            }
        }
    }

    public class CullingAPIComponent : MonoBehaviour, ICullingAPIComponent
    {
        [SerializeField] private bool hasPositionChanged = false;
        [SerializeField] private Vector3 worldPosition;

        public Vector3 offsetPosition;
        public float radius;
        private ICullingAPIComponent.Property property;

        private void Start()
        {
            worldPosition = Vector3.zero + offsetPosition;
        }

        public bool CheckTransformChanged(out Vector3 pos, out float radius)
        {
            if (hasPositionChanged)
            {
                pos = worldPosition;
                radius = this.radius;
                hasPositionChanged = false;
                return true;
            }
            pos = default;
            radius = default;
            return false;
        }

        public void SetPosition(Vector3 position)
        {
            worldPosition = position + offsetPosition;            
            hasPositionChanged = true;
        }

        public void GetCullInfo(out Vector3 pos, out float radius)
        {
            pos = worldPosition;
            radius = this.radius;
        }

        public ICullingAPIComponent.DelCullingCallback OnVisibilityChanged { get; set; }
        public  Action<ICullingAPIComponent> OnRemove { get; set; }

        private void LateUpdate()
        {            
            if(property.HasAnyChanged)
            {
                property.ClearAnyChanges();

                OnVisibilityChanged?.Invoke(property.visible >=0 , property.visible);
            }
        }

        private void OnDestroy()
        {
            OnRemove?.Invoke(this);
        }

        public ref ICullingAPIComponent.Property GetPropertyRef() => ref property;
    }

}