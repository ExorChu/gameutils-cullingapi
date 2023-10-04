using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUtils.CullingAPI.Samples
{
    public class CullableCube : MonoBehaviour
    {
        private ICullingAPIComponent component;
        [SerializeField] private Renderer cubeRenderer;

        private Vector3 targetPosition;

        private void Awake()
        {
            component = GetComponent<ICullingAPIComponent>();
            component.OnVisibilityChanged += Component_OnVisibilityChanged;

            targetPosition = GetRandomTargetPosition();
        }

        private void Component_OnVisibilityChanged(bool isVisible, int lod)
        {
            if (!isVisible)
            {
                //cubeRenderer.enabled = false;
                cubeRenderer.material.color = Color.gray;
            }
            else
            {
                //cubeRenderer.enabled = true;
                switch (lod)
                {
                    case 0:
                        cubeRenderer.material.color = Color.green;
                        break;
                    case 1:
                        cubeRenderer.material.color = Color.yellow;
                        break;
                    default:
                        cubeRenderer.material.color = Color.red;
                        break;
                }
            }
        }

        private void Update()
        {
            //if ((int)Time.time % 2 == 0)
            //    return;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * 25);
            if(Vector3.SqrMagnitude(transform.position - targetPosition) < 0.1f)
            {
                targetPosition = GetRandomTargetPosition();
            }
        }

        private Vector3 GetRandomTargetPosition()
        {

            //rework the random here, and check why the lod isn't called
            var rd = Random.insideUnitSphere;
            rd.y = 0;
            rd.Normalize();

            rd *= 80;

            return rd;
        }
    }
}
