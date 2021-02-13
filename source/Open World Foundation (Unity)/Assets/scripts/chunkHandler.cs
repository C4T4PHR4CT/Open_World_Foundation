using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class chunkHandler : MonoBehaviour
{
    public static readonly int[] LODdetail = {20, 10, 4, 2, 1};     //chose values which are divisors of chunkFactory.CHUNK_WIDTH
    public static readonly int[] LODdistance = {6, 9, 12, 15, 18};  //distances (in chunks) where LOD groups are activated
    public static readonly int   LOD_CHANGE_RATE = 1;               //Hz

    private float[,] terrain;
    private int chunkPosX, chunkPosY;

    private struct meshData
    {
        public Vector3[] vertices;
        public Vector2[] uvs;
        public int[] triangles;
    }

    private meshData[] LOD;
    private Mesh[] meshes;

    public void init(int chunkPosX, int chunkPosY, GameObject parent)
    {
        this.chunkPosX = chunkPosX;
        this.chunkPosY = chunkPosY;
        gameObject.name = chunkPosX + ";" + chunkPosY;
        gameObject.transform.SetParent(parent.transform);
        gameObject.transform.position = new Vector3(chunkPosX * (chunkFactory.CHUNK_WIDTH), 0, chunkPosY * (chunkFactory.CHUNK_WIDTH));
        terrain = new float[chunkFactory.CHUNK_WIDTH + 1, chunkFactory.CHUNK_WIDTH + 1];
        LOD = new meshData[LODdetail.Length + 1];
        meshes = new Mesh[LODdetail.Length + 1];
        StartCoroutine(generateChunk());
    }

    private IEnumerator LODprocessor()
    {
        int camPosX, camPosY;
        float distance;
        while (true)
        {
            camPosX = (int)(Camera.main.transform.position.x - chunkFactory.CHUNK_WIDTH / 2) / chunkFactory.CHUNK_WIDTH;
            camPosY = (int)(Camera.main.transform.position.z - chunkFactory.CHUNK_WIDTH / 2) / chunkFactory.CHUNK_WIDTH;
            distance = Mathf.Sqrt(Mathf.Pow(camPosX - chunkPosX, 2) + Mathf.Pow(camPosY - chunkPosY, 2));
            if (distance < LODdistance[0])
            {
                gameObject.GetComponent<MeshFilter>().mesh = meshes[0];
            }
            else if (distance >= LODdistance[LODdistance.Length - 1])
            {
                gameObject.GetComponent<MeshFilter>().mesh = meshes[meshes.Length - 1];
            }
            else
            {
                for (int i = 1; i < LODdistance.Length; i++)
                {
                    if (distance < LODdistance[i])
                    {
                        gameObject.GetComponent<MeshFilter>().mesh = meshes[i];
                        break;
                    }
                }
            }
            yield return new WaitForSecondsRealtime(1 / LOD_CHANGE_RATE);
        }
    }

    private IEnumerator generateChunk()
    {
        bool done = false;
        while (chunkFactory.generatorThreadsRunning >= chunkFactory.MAX_GENERATOR_THREADS)
        {
            yield return null;
        }
        chunkFactory.generatorThreadsRunning++;
        new Thread(() => {

            addToTerrain(modByCurve(chunkFactory.hugeMountainNoise.generateWhiteNoise(chunkPosX, chunkPosY, chunkFactory.sin), chunkFactory.hugeMountainPostMod));
            addToTerrain(modByCurve(chunkFactory.testNoise.generateWhiteNoise(chunkPosX, chunkPosY, chunkFactory.sin), chunkFactory.testPostMod));

            LOD[0] = generateContinuousMeshData(terrain, 1);
            for (int i = 0; i < LODdetail.Length; i++)
                LOD[i + 1] = generateContinuousMeshData(generateLowerDetailMap(terrain, LODdetail[i]), (float)chunkFactory.CHUNK_WIDTH / LODdetail[i]);
            done = true;
        }).Start();
        bool exit = false;
        while (!exit)
        {
            if (done)
            {
                chunkFactory.generatorThreadsRunning--;
                for (int i = 0; i < LODdetail.Length + 1; i++)
                {
                    meshes[i] = new Mesh();
                    meshes[i].vertices = LOD[i].vertices;
                    meshes[i].uv = LOD[i].uvs;
                    meshes[i].triangles = LOD[i].triangles;
                    meshes[i].Optimize();
                    meshes[i].RecalculateNormals();
                }
                gameObject.GetComponent<MeshFilter>().mesh = meshes[0];
                StartCoroutine(LODprocessor());
                exit = true;
            }
            yield return null;
        }
    }

    private float[,] modByCurve(float[,] noiseMap, noiseGenerator.curve modCurve)
    {
        float[,] temp = new float[noiseMap.GetLength(0), noiseMap.GetLength(1)];
        for (int i = 0; i < noiseMap.GetLength(0); i++)
        {
            for (int j = 0; j < noiseMap.GetLength(1); j++)
            {
                temp[i, j] = modCurve(noiseMap[i, j]);
            }
        }
        return temp;
    }

    private void addToTerrain(float[,] noiseMap)
    {
        for (int i = 0; i < terrain.GetLength(0); i++)
        {
            for (int j = 0; j < terrain.GetLength(0); j++)
            {
                terrain[i, j] += noiseMap[i, j];
            }
        }
    }

    private meshData generateDeveloperMeshData(float[,] heightMap, float step)
    {
        meshData devMesh;
        devMesh.vertices = new Vector3[3 * 2 * (heightMap.GetLength(0) - 1) * (heightMap.GetLength(0) - 1)];
        devMesh.uvs = new Vector2[3 * 2 * (heightMap.GetLength(0) - 1) * (heightMap.GetLength(0) - 1)];
        devMesh.triangles = new int[3 * 2 * (heightMap.GetLength(0) - 1) * (heightMap.GetLength(0) - 1)];
        for (int i = 0; i < heightMap.GetLength(0) - 1; i++)
        {
            for (int j = 0; j < heightMap.GetLength(0) - 1; j++)
            {
                int rectCnt = i * 3 * 2 * (heightMap.GetLength(0) - 1) + j * 3 * 2;
                if ((heightMap[i + 0, j + 0] >= heightMap[i + 1, j + 1] &&
                     heightMap[i + 0, j + 0] >= heightMap[i + 0, j + 1] &&
                     heightMap[i + 0, j + 0] >= heightMap[i + 1, j + 0]) ||
                    (heightMap[i + 1, j + 1] >= heightMap[i + 0, j + 0] &&
                     heightMap[i + 1, j + 1] >= heightMap[i + 0, j + 1] &&
                     heightMap[i + 1, j + 1] >= heightMap[i + 1, j + 0]))
                {
                    devMesh.vertices[rectCnt + 0] = new Vector3((i + 1) * step, heightMap[i + 1, j + 0], (j + 0) * step);
                    devMesh.vertices[rectCnt + 1] = new Vector3((i + 1) * step, heightMap[i + 1, j + 1], (j + 1) * step);
                    devMesh.vertices[rectCnt + 2] = new Vector3((i + 0) * step, heightMap[i + 0, j + 0], (j + 0) * step);
                    devMesh.vertices[rectCnt + 3] = new Vector3((i + 0) * step, heightMap[i + 0, j + 1], (j + 1) * step);
                    devMesh.vertices[rectCnt + 4] = new Vector3((i + 0) * step, heightMap[i + 0, j + 0], (j + 0) * step);
                    devMesh.vertices[rectCnt + 5] = new Vector3((i + 1) * step, heightMap[i + 1, j + 1], (j + 1) * step);
                }
                else
                if ((heightMap[i + 1, j + 0] >= heightMap[i + 1, j + 1] &&
                     heightMap[i + 1, j + 0] >= heightMap[i + 0, j + 1] &&
                     heightMap[i + 1, j + 0] >= heightMap[i + 0, j + 0]) ||
                    (heightMap[i + 0, j + 1] >= heightMap[i + 0, j + 0] &&
                     heightMap[i + 0, j + 1] >= heightMap[i + 1, j + 1] &&
                     heightMap[i + 0, j + 1] >= heightMap[i + 1, j + 0]))
                {
                    devMesh.vertices[rectCnt + 0] = new Vector3((i + 0) * step, heightMap[i + 0, j + 0], (j + 0) * step);
                    devMesh.vertices[rectCnt + 1] = new Vector3((i + 1) * step, heightMap[i + 1, j + 0], (j + 0) * step);
                    devMesh.vertices[rectCnt + 2] = new Vector3((i + 0) * step, heightMap[i + 0, j + 1], (j + 1) * step);
                    devMesh.vertices[rectCnt + 3] = new Vector3((i + 1) * step, heightMap[i + 1, j + 1], (j + 1) * step);
                    devMesh.vertices[rectCnt + 4] = new Vector3((i + 0) * step, heightMap[i + 0, j + 1], (j + 1) * step);
                    devMesh.vertices[rectCnt + 5] = new Vector3((i + 1) * step, heightMap[i + 1, j + 0], (j + 0) *step);
                }
                if ((heightMap[i + 0, j + 0] == heightMap[i + 0, j + 1] && heightMap[i + 1, j + 1] == heightMap[i + 1, j + 0]) ||
                    (heightMap[i + 0, j + 0] == heightMap[i + 1, j + 0] && heightMap[i + 1, j + 1] == heightMap[i + 0, j + 1]))
                {
                    devMesh.uvs[rectCnt + 0] = new Vector2(0.5f, 0);
                    devMesh.uvs[rectCnt + 1] = new Vector2(0.5f, 1);
                    devMesh.uvs[rectCnt + 2] = new Vector2(1, 0);
                    devMesh.uvs[rectCnt + 3] = new Vector2(0.5f, 0);
                    devMesh.uvs[rectCnt + 4] = new Vector2(0.5f, 1);
                    devMesh.uvs[rectCnt + 5] = new Vector2(1, 0);
                }
                else
                {
                    devMesh.uvs[rectCnt + 0] = new Vector2(0, 0);
                    devMesh.uvs[rectCnt + 1] = new Vector2(0, 1);
                    devMesh.uvs[rectCnt + 2] = new Vector2(0.5f, 0);
                    devMesh.uvs[rectCnt + 3] = new Vector2(0, 0);
                    devMesh.uvs[rectCnt + 4] = new Vector2(0, 1);
                    devMesh.uvs[rectCnt + 5] = new Vector2(0.5f, 0);
                }
            }
        }
        for (int i = 0; i < devMesh.triangles.Length / 3; i++)
        {
            devMesh.triangles[i * 3 + 0] = i * 3 + 2;
            devMesh.triangles[i * 3 + 1] = i * 3 + 1;
            devMesh.triangles[i * 3 + 2] = i * 3 + 0;
        }
        return devMesh;
    }

    private meshData generateContinuousMeshData(float[,] heightMap, float step)
    {
        meshData devMesh;
        devMesh.vertices = new Vector3[heightMap.GetLength(0) * heightMap.GetLength(0)];
        devMesh.uvs = new Vector2[heightMap.GetLength(0) * heightMap.GetLength(0)];
        devMesh.triangles = new int[3 * 2 * (heightMap.GetLength(0) - 1) * (heightMap.GetLength(0) - 1)];
        for (int i = 0; i < heightMap.GetLength(0); i++)
        {
            for (int j = 0; j < heightMap.GetLength(0); j++)
            {
                devMesh.vertices[i * heightMap.GetLength(0) + j] = new Vector3(i * step, heightMap[i, j], j * step);
                devMesh.uvs[i * heightMap.GetLength(0) + j] = new Vector2(i / (float)(heightMap.GetLength(0) - 1) * step, j / (float)(heightMap.GetLength(0) - 1) * step);
            }
        }
        for (int i = 0; i < heightMap.GetLength(0) - 1; i++)
        {
            for (int j = 0; j < heightMap.GetLength(0) - 1; j++)
            {
                if ((heightMap[i + 0, j + 0] >= heightMap[i + 1, j + 1] &&
                     heightMap[i + 0, j + 0] >= heightMap[i + 0, j + 1] &&
                     heightMap[i + 0, j + 0] >= heightMap[i + 1, j + 0]) ||
                    (heightMap[i + 1, j + 1] >= heightMap[i + 0, j + 0] &&
                     heightMap[i + 1, j + 1] >= heightMap[i + 0, j + 1] &&
                     heightMap[i + 1, j + 1] >= heightMap[i + 1, j + 0]))
                {
                    devMesh.triangles[0 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 0) * heightMap.GetLength(0) + (j + 0);
                    devMesh.triangles[1 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 0) * heightMap.GetLength(0) + (j + 1);
                    devMesh.triangles[2 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 1) * heightMap.GetLength(0) + (j + 1);
                    devMesh.triangles[3 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 0) * heightMap.GetLength(0) + (j + 0);
                    devMesh.triangles[4 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 1) * heightMap.GetLength(0) + (j + 1);
                    devMesh.triangles[5 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 1) * heightMap.GetLength(0) + (j + 0);
                }
                else
                {
                    devMesh.triangles[0 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 0) * heightMap.GetLength(0) + (j + 0);
                    devMesh.triangles[1 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 0) * heightMap.GetLength(0) + (j + 1);
                    devMesh.triangles[2 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 1) * heightMap.GetLength(0) + (j + 0);
                    devMesh.triangles[3 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 0) * heightMap.GetLength(0) + (j + 1);
                    devMesh.triangles[4 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 1) * heightMap.GetLength(0) + (j + 1);
                    devMesh.triangles[5 + 6 * (i * (heightMap.GetLength(0) - 1) + j)] = (i + 1) * heightMap.GetLength(0) + (j + 0);
                }
            }
        }
        return devMesh;
    }

    public float[,] generateLowerDetailMap(float[,] noiseMap, int newSize)
    {

        int wavelength = (noiseMap.GetLength(0) - 1) / newSize;
        newSize++;
        float[,] downScaled = new float[newSize, newSize];
        int xSample, ySample;
        for (int i = 0; i < newSize; i++)
        {
            xSample = wavelength * i;
            for (int j = 0; j < newSize; j++)
            {
                ySample = wavelength * j;
                downScaled[i, j] = noiseMap[xSample, ySample];
            }
        }
        return downScaled;
    }
}
