using System.Collections.Generic;
using UnityEngine;

public class InstancingData
{
    public Vector3[] Positions { get; private set; }
    public Vector3[] Scales { get; private set; }
    public float[] Offsets { get; private set; }
    public int[] PrefabIndices { get; private set; }
    public Matrix4x4[] InstanceMatrices { get; private set; }
    public List<int>[] IndicesByPrefab { get; private set; }


    public void Generate(int count, GameObject[] plantPrefabs, Vector3 center, float radius, Terrain terrain)
    {
        if (plantPrefabs == null || plantPrefabs.Length == 0 || count <= 0)
        {
            Clear();
            return;
        }

        Positions = new Vector3[count];
        Scales = new Vector3[count];
        Offsets = new float[count];
        PrefabIndices = new int[count];
        InstanceMatrices = new Matrix4x4[count];

        IndicesByPrefab = new List<int>[plantPrefabs.Length];
        for (int p = 0; p < plantPrefabs.Length; p++)
        {
            IndicesByPrefab[p] = new List<int>();
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 rp = Random.insideUnitCircle * radius;
            Vector3 pos = new Vector3(center.x + rp.x, 0f, center.z + rp.y);
            if (terrain != null) pos.y = terrain.SampleHeight(pos);

            Positions[i] = pos;
            Offsets[i] = Random.Range(0f, 10f);
            int p = Random.Range(0, plantPrefabs.Length);
            PrefabIndices[i] = p;
            Scales[i] = plantPrefabs[p].transform.localScale;

            InstanceMatrices[i] = Matrix4x4.TRS(Positions[i], Quaternion.identity, Scales[i]);

            IndicesByPrefab[p].Add(i);
        }
    }

    public void Clear()
    {
        Positions = null;
        Scales = null;
        Offsets = null;
        PrefabIndices = null;
        InstanceMatrices = null;
        IndicesByPrefab = null;
    }
}