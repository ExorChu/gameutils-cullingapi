using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUtils.CullingAPI
{
    public class CullingAPIAutoUpdateComponent : MonoBehaviour, ICullingAPIComponent
    {
        [SerializeField] private bool hasPositionChanged = false;
        [SerializeField] private Vector3 worldPosition;

        public Vector3 offsetPosition;
        public float radius;
        private Transform cacheTransform;
        private ICullingAPIComponent.Property property;

        private void Awake()
        {
            cacheTransform = transform;
        }

        private void Start()
        {
            worldPosition = cacheTransform.position + offsetPosition;
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

        public void GetCullInfo(out Vector3 pos, out float radius)
        {
            pos = worldPosition;
            radius = this.radius;
        }

        public ICullingAPIComponent.DelCullingCallback OnVisibilityChanged { get; set; }
        public Action<ICullingAPIComponent> OnRemove { get; set; }

        private void Update()
        {
            if (cacheTransform.hasChanged)
            {
                hasPositionChanged = true;
                worldPosition = cacheTransform.position + offsetPosition;
            }
        }

        private void LateUpdate()
        {
            if (property.HasAnyChanged)
            {
                property.ClearAnyChanges();

                OnVisibilityChanged?.Invoke(property.visible >= 0, property.visible);
            }
        }

        private void OnDestroy()
        {
            OnRemove?.Invoke(this);
        }

        public ref ICullingAPIComponent.Property GetPropertyRef() => ref property;
    }
}
