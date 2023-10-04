using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUtils.CullingAPI.Samples
{
    public class CullingAPITest : MonoBehaviour
    {
        [SerializeField] private Camera camera;
        [SerializeField] private CullableCube[] cullableCubes;
        [SerializeField] private CullableCube prefab;
        [SerializeField] private int spawn = 250;

        private void Start()
        {
            var cullAPI = GetComponent<ICullingAPI>();

            for (int i = 0; i < spawn; i++)
            {
                var ele = Instantiate(prefab);
                cullAPI.AddElement(ele.GetComponent<ICullingAPIComponent>());
                ele.gameObject.SetActive(true);
            }

            cullAPI.SetCamera(camera);
            cullAPI.SetAnchorTransform(camera.transform);
        }
    }
}
