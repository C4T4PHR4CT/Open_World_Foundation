using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

public class noiseGenerator : MonoBehaviour
{
    public delegate float curve(float x);

    private int wavelength;
    private int seed;
    private curve probability;
    //riggedProbabilityTrigger trigger value (0-1) wher the point will affect the surrounding point's generation
    //vertical/horizontalRiggedCurve rigged points will take the value on this curve (interpolated between the trigger point and the wavelength endpoint)
    private float riggedProbabilityTrigger;
    private curve verticalRiggedCurve;
    private int verticalRiggedWavelength;
    private curve horizontalRiggedCurve;
    private int horizontalRiggedWavelength;
    private float[] baseNoise;
    private System.Random generator;

    public void init(int wavelength, int seed, string name, GameObject parent)
    {
        init(wavelength, seed, delegate (float x) { return x; }, name, parent);
    }

    public void init(int wavelength, int seed, float probability, string name, GameObject parent)
    {
        init(wavelength, seed, delegate (float x) { if (x > (1 - probability * Mathf.Pow(wavelength, 2))) return 1; else return 0; }, name, parent);
    }

    public void init(int wavelength, int seed, curve probability, string name, GameObject parent)
    {
        init(wavelength, seed, probability, 1.1f, null, 0, null, 0, name, parent);
    }

    public void init(int wavelength, int seed, curve probability, float riggedProbabilityTrigger, curve verticalRiggedCurve, int verticalRiggedWavelength, curve horizontalRiggedCurve, int horizontalRiggedWavelength, string name, GameObject parent)
    {
        this.wavelength = wavelength;
        this.seed = seed;
        this.probability = probability;
        this.riggedProbabilityTrigger = riggedProbabilityTrigger;
        this.verticalRiggedCurve = verticalRiggedCurve;
        this.verticalRiggedWavelength = verticalRiggedWavelength;
        this.horizontalRiggedCurve = horizontalRiggedCurve;
        this.horizontalRiggedWavelength = horizontalRiggedWavelength;
        this.baseNoise = new float[0];
        this.generator = new System.Random(seed);
        gameObject.name = name;
        gameObject.transform.SetParent(parent.transform);
    }

    public float[,] generateWhiteNoise(int chunkPosX, int chunkPosY, curve interpolation)
    {
        int axisXshift, axisYshift;
        float verticalBlend, horizontalBlend;
        float top, bottom;
        float[,] whiteNoise = new float[chunkFactory.CHUNK_WIDTH + 1, chunkFactory.CHUNK_WIDTH + 1];
        for (int x = 0; x < chunkFactory.CHUNK_WIDTH + 1; x++)
        {
            horizontalBlend = wavePhase(chunkPosX, x, wavelength) / (float)wavelength;
            for (int y = 0; y < chunkFactory.CHUNK_WIDTH + 1; y++)
            {
                verticalBlend = wavePhase(chunkPosY, y, wavelength) / (float)wavelength;
                if (chunkPosY * chunkFactory.CHUNK_WIDTH + y < 0)
                    axisYshift = -1 * wavelength;
                else
                    axisYshift = 0;
                if (chunkPosX * chunkFactory.CHUNK_WIDTH + x < 0)
                    axisXshift = -1 * wavelength;
                else
                    axisXshift = 0;
                top = interpolate(
                      getBaseNoise(axisXshift + chunkPosX * chunkFactory.CHUNK_WIDTH + x, axisYshift + chunkPosY * chunkFactory.CHUNK_WIDTH + y + wavelength),
                      getBaseNoise(axisXshift + chunkPosX * chunkFactory.CHUNK_WIDTH + x + wavelength, axisYshift + chunkPosY * chunkFactory.CHUNK_WIDTH + y + wavelength),
                      horizontalBlend, interpolation);
                bottom = interpolate(
                      getBaseNoise(axisXshift + chunkPosX * chunkFactory.CHUNK_WIDTH + x, axisYshift + chunkPosY * chunkFactory.CHUNK_WIDTH + y),
                      getBaseNoise(axisXshift + chunkPosX * chunkFactory.CHUNK_WIDTH + x + wavelength, axisYshift + chunkPosY * chunkFactory.CHUNK_WIDTH + y),
                      horizontalBlend, interpolation);
                whiteNoise[x, y] = interpolate(bottom, top, verticalBlend, interpolation);
            }
        }
        return whiteNoise;
    }

    private float getBaseNoise(int posX, int posY)
    {
        int spiral = toSpiralValue(posX / wavelength, posY / wavelength);
        if (baseNoise.Length <= spiral)
        {
            extendBaseNoiseTo(posX, posY);
            return baseNoise[spiral];
        }
        else
        {
            return baseNoise[spiral];
        }
    }

    private void extendBaseNoiseTo(int posX, int posY)
    {
        extendBaseNoise(toSpiralValue(posX / wavelength, posY / wavelength));
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void extendBaseNoise(int spiral)
    {
        float[] temp = new float[spiral + 1];
        try
        {
            baseNoise.CopyTo(temp, 0);
            for (int i = baseNoise.Length; i <= spiral; i++)
                temp[i] = probability((float)generator.NextDouble());
            baseNoise = temp;
        }
        catch (Exception) {}
    }

    private int toSpiralValue(int x, int y)
    {
        if (x == 0 && y == 0)
            return 0;
        int ring = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
        int sValue = (int)Mathf.Pow(2 * (ring - 1) + 1, 2) - 1;
        if (Mathf.Abs(x) >= Mathf.Abs(y) && x > 0)
        {
            sValue += ring - y + 1;
        }
        else
        if (Mathf.Abs(y) > Mathf.Abs(x) && y < 0)
        {
            sValue += 2 * ring + 1;
            sValue += ring - x;
        }
        else
        if (Mathf.Abs(x) >= Mathf.Abs(y) && x < 0)
        {
            sValue += 2 * ring + 1;
            sValue += 2 * ring - 1;
            sValue += ring + y + 1;
        }
        else
        if (Mathf.Abs(y) > Mathf.Abs(x) && y > 0)
        {
            sValue += 2 * ring + 1;
            sValue += 2 * ring - 1;
            sValue += 2 * ring + 1;
            sValue += ring + x;
        }
        return sValue;
    }

    private float interpolate(float x0, float x1, float alpha, curve interpolationCurve)
    {
        return interpolationCurve(alpha) * (x1 - x0) + x0;
    }

    private int wavePhase(int chunkPos, int coordinate, int waveLength)
    {
        if (chunkPos * chunkFactory.CHUNK_WIDTH + coordinate >= 0)
            return ((chunkPos * chunkFactory.CHUNK_WIDTH + coordinate) % waveLength);
        else
            return waveLength - (((chunkPos) * -1 * chunkFactory.CHUNK_WIDTH - coordinate) % waveLength);
    }
}
