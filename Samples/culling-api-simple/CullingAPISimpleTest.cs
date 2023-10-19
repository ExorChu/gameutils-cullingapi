using GameUtils.CullingAPI.Simple;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUtils.CullingAPI
{
    public class CullingAPISimpleTest : MonoBehaviour
    {
        private CullingAPISimple simpleAPI;

        [SerializeField]
        private float[] distanceBands = new[]
        {
            10f,25f,50f
        };

        [SerializeField] private int testElementCount = 250;
        [SerializeField] private Mesh meshToDraw;
        [SerializeField] private Material materialToDraw;

        private CubeData[] cubeDatas;

        private struct CubeData
        {
            public int id;
            public Vector3 position;
            public float radius;

            public Vector3 targetPosition;

            public bool isVisible;
            public int lod;
        }

        private void Start()
        {            
            simpleAPI = new CullingAPISimple(1024);
            simpleAPI.SetCamera(Camera.main);
            simpleAPI.SetReferencePoint(Camera.main.transform.position);
            simpleAPI.SetDistanceBands(distanceBands);

            cubeDatas = new CubeData[testElementCount];

            for (int i = 0; i < testElementCount; i++)
            {
                var cube = new CubeData();

                cube.id = simpleAPI.AddNewElement();

                cube.radius = 0.5f;
                cube.position = Vector3.zero;

                cubeDatas[i] = cube;

                simpleAPI.UpdateElement(cube.id, cube.position, cube.radius);
            }
        }

        private void Update()
        {
            simpleAPI.SetReferencePoint(Camera.main.transform.position);
            for (int i = 0; i < testElementCount; i++)
            {
                ref var cube = ref cubeDatas[i];

                cube.position = Vector3.MoveTowards(cube.position, cube.targetPosition, Time.deltaTime * 25);
                if (Vector3.SqrMagnitude(cube.position - cube.targetPosition) < 0.1f)
                {
                    cube.targetPosition = GetRandomTargetPosition();
                }

                simpleAPI.UpdateElement(cube.id, cube.position);
            }
            simpleAPI.Update();
        }

        private Vector3 GetRandomTargetPosition()
        {

            //rework the random here, and check why the lod isn't called
            var rd = UnityEngine.Random.insideUnitSphere;
            rd.y = 0;
            rd.Normalize();

            rd *= 80;

            return rd;
        }

        private MaterialPropertyBlock mpb;
        int[] cacheArray = new int[1024]; // get max 100 element at a time. 

        private void LateUpdate()
        {
            //old method, which need to travel through all elements
            //Span<CullingElement> span = simpleAPI.GetElements();
            //for (int i = 0; i < span.Length; i++)
            //{
            //    ref var ce = ref span[i];
            //    if (ce.HasAnyChanged)
            //    {
            //        ce.ClearFlags();

            //        int id = ce.id;

            //        int indexOf = Array.FindIndex(cubeDatas, (e) => e.id == id);

            //        if(indexOf >= 0)
            //        {
            //            ref var cube = ref cubeDatas[indexOf];
            //            cube.visibility = ce.LOD;
            //        }
            //    }
            //}

            if (simpleAPI.HasAnyElementChanged())
            {
                Span<int> changedElement = new Span<int>(cacheArray);

                int count = simpleAPI.GetChangedElements(changedElement);

                for (int i = 0; i < count; i++)
                {
                    var index = changedElement[i];
                    ref var ce = ref simpleAPI.GetElementRef(index);
                    if(ce.HasAnyChanged) // no need to check
                    {
                        ce.ClearFlags();

                        int id = ce.id;
                        int indexOf = Array.FindIndex(cubeDatas, (e) => e.id == id);
                        if(indexOf >= 0)
                        {
                            ref var cube = ref cubeDatas[indexOf];
                            cube.lod = ce.LOD;
                            cube.isVisible = ce.IsVisible;
                            // check visibilty as well as lod here too.
                        }
                    }
                }
            }



            mpb = mpb ?? new MaterialPropertyBlock();

            for (int i = 0; i < testElementCount; i++)
            {
                ref var cube = ref cubeDatas[i];              
                
                Matrix4x4 mtx = Matrix4x4.TRS(cube.position, Quaternion.identity, Vector3.one);

                //Graphics.DrawMesh(meshToDraw, mtx, materialToDraw, 0);
                RenderParams renderParams = new RenderParams(materialToDraw);

                //var mpb = new MaterialPropertyBlock();          

                mpb.SetColor("_Color", GetColorFromLOD(cube.lod, cube.isVisible));
                renderParams.matProps = mpb;

                Graphics.RenderMesh(in renderParams, meshToDraw, 0, mtx);
            }
        }

        private Color GetColorFromLOD(int lod, bool isVisible = true)
        {
            Color mul = isVisible ? Color.white : Color.gray;
            switch (lod)
            {
                case 0: return Color.green * mul;
                case 1: return Color.yellow * mul;
                case 2: return Color.red * mul;
            }
            return Color.gray;
        }

        private void OnDestroy()
        {
            simpleAPI.Dispose();
        }
    }
}
