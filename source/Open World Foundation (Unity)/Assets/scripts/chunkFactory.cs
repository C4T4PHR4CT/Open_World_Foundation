using System;
using System.Collections;
using UnityEngine;

public class chunkFactory : MonoBehaviour
{
    public static readonly int SEED = 2;                        //seed
    public static readonly int CHUNK_GENERATING_DISTANCE = 10;  //radius in chunks
    public static readonly int CHUNK_WIDTH = 100;               //chunk width in meters
    public static readonly int CHUNK_GENERATOR_RATE = 2;        //Hz
    public static readonly int MAX_GENERATOR_THREADS = Environment.ProcessorCount - 2;
    public static int generatorThreadsRunning = 0;

    public GameObject chunkDev;
    public GameObject noiseMap;

    public static noiseGenerator.curve sin;

    public static noiseGenerator hugeMountainNoise;
    public static noiseGenerator.curve hugeMountainPostMod;

    public static noiseGenerator testNoise;
    public static noiseGenerator.curve testPostMod;

    void Start()
    {
        sin = delegate (float x) { return (Mathf.Cos((x + 1) * Mathf.PI) + 1) / 2; };

        hugeMountainNoise = (Instantiate(noiseMap) as GameObject).GetComponent<noiseGenerator>();
        hugeMountainNoise.init(12000, SEED, 1f / 4000000000f, "hugeMountainNoise", gameObject);
        hugeMountainPostMod = delegate (float x) { return 8000 * x; };

        testNoise = (Instantiate(noiseMap) as GameObject).GetComponent<noiseGenerator>();
        testNoise.init(20, SEED + 1, "testNoise", gameObject);
        testPostMod = delegate (float x) { return 7 * x; };

        StartCoroutine(chunkGenerator());
    }

    public void generateChunk(int chunkPosX, int chunkPosY)
    {
        if (transform.Find(chunkPosX + ";" + chunkPosY) == null)
        {
            GameObject chunk = Instantiate(chunkDev) as GameObject;
            chunk.GetComponent<chunkHandler>().init(chunkPosX, chunkPosY, gameObject);
        }
    }

    private IEnumerator chunkGenerator()
    {
        int sgn;
        int chunkPosX = (int)(Camera.main.transform.position.x - CHUNK_WIDTH / 2) / CHUNK_WIDTH;
        int chunkPosY = (int)(Camera.main.transform.position.z - CHUNK_WIDTH / 2) / CHUNK_WIDTH;
        int chunkPosXold = chunkPosX;
        int chunkPosYold = chunkPosY;
        for (int i = 0; i < Mathf.Pow(CHUNK_GENERATING_DISTANCE * 2 + 1, 2); i++)
        {
            Vector2Int temp = fromSpiralValue(i);
            generateChunk(temp.x + chunkPosX, temp.y + chunkPosY);
        }
        while (true)
        {
            chunkPosX = (int)(Camera.main.transform.position.x - CHUNK_WIDTH / 2) / CHUNK_WIDTH;
            chunkPosY = (int)(Camera.main.transform.position.z - CHUNK_WIDTH / 2) / CHUNK_WIDTH;
            if (chunkPosX != chunkPosXold)
            {
                sgn = Math.Sign(chunkPosX - chunkPosXold);
                for (int i = CHUNK_GENERATING_DISTANCE * -1; i <= CHUNK_GENERATING_DISTANCE; i++)
                    generateChunk(chunkPosX + CHUNK_GENERATING_DISTANCE * sgn, chunkPosY + i);
            }
            if (chunkPosY != chunkPosYold)
            {
                sgn = Math.Sign(chunkPosY - chunkPosYold);
                for (int i = CHUNK_GENERATING_DISTANCE * -1; i <= CHUNK_GENERATING_DISTANCE; i++)
                    generateChunk(chunkPosX + i, chunkPosY + CHUNK_GENERATING_DISTANCE * sgn);
            }
            chunkPosXold = chunkPosX;
            chunkPosYold = chunkPosY;
            yield return new WaitForSecondsRealtime(1 / CHUNK_GENERATOR_RATE);
        }
    }

    private Vector2Int fromSpiralValue(int spiralValue)
    {
        if (spiralValue == 0)
            return new Vector2Int(0, 0);
        int ring = Mathf.FloorToInt((-1 * 4 + Mathf.Sqrt(16 + 16 * (spiralValue - 1))) / 8f) + 1;
        int sDiff = spiralValue - (int)Mathf.Pow((ring - 1) * 2 + 1, 2) + 1;
        int x = 0;
        int y = 0;
        if (sDiff >= 1 && sDiff <= ring * 2 + 1)
        {
            x = ring;
            y = ring + 1 - sDiff;
        }
        else
        if (sDiff > ring * 2 + 1 && sDiff < ring * 4 + 1)
        {
            x = ring * 3 + 1 - sDiff;
            y = -1 * ring;
        }
        else
        if (sDiff >= ring * 4 + 1 && sDiff <= ring * 6 + 1)
        {
            x = -1 * ring;
            y = sDiff - ring * 5 - 1;
        }
        else
        if (sDiff > ring * 6 + 1 && sDiff < ring * 8 + 1)
        {
            x = sDiff - ring * 7 - 1;
            y = ring;
        }
        return new Vector2Int(x, y);
    }
}