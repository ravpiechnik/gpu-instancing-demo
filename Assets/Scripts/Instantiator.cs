using System.Collections.Generic;
using UnityEngine;

public class Instantiator : MonoBehaviour
{
    public enum InstantiationType
    {
        CPU,
        GPU
    }

    [Header("Instantiation Settings")]
    [SerializeField] private int numberOfObjects = 1000;
    [SerializeField] private GameObject[] plantPrefabs;
    [SerializeField] private float instantiationRadius = 10f;
    [SerializeField] private InstantiationType instantiationType = InstantiationType.CPU;
    [SerializeField] private bool enableShadows = true;
    
    private const int MAX_INSTANCES_PER_BATCH = 1023;

    #region API
    public int NumberOfObjects
    {
        get => numberOfObjects;
        set 
        { 
            if (numberOfObjects != value) // number of objects was changed, refresh
            {
                numberOfObjects = value;
                RefreshInstances();
            }
        }
    }

    public bool EnableShadows
    {
        get => enableShadows;
        set 
        { 
            if (enableShadows != value) // shadow setting was changed, refresh
            {
                enableShadows = value;
                RefreshInstances();
            }
        }
    }
    #endregion

    void UpdateWindListener()
    {
        if (cachedMaterials != null && WindManager.Instance != null)
        {
            foreach (var material in cachedMaterials)
            {
                if (material == null) continue;
                material.SetFloat("_WindStrength", WindManager.Instance.WindStrength);
                material.SetFloat("_WindSpeed", WindManager.Instance.WindSpeed);
                material.SetFloat("_WindEnabled", WindManager.Instance.WindEnabled ? 1f : 0f);
            }
        }
    }

    // used for even distribution of objects on terrain, unrelated to GPU instancing
    private Terrain terrain;

    // GPU instancing data (full arrays)
    private Vector3[] positions;
    private Vector3[] scales;
    private float[] offsets;
    private int[] prefabIndices;

    // precomputed instance matrices (TRS) - built once in GenerateInstanceData
    private Matrix4x4[] instanceMatrices;

    // reusable batch buffers
    private Matrix4x4[] batchMatrices;
    private float[] batchOffsets;
    private MaterialPropertyBlock propertyBlock;

    // pre-sorted indices per-prefab
    private List<int>[] instanceIndicesByPrefab;

    // cached mesh/material per-prefab
    private Mesh[] cachedMeshes;
    private Material[] cachedMaterials;

    void Start()
    {
        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (plantPrefabs == null || plantPrefabs.Length == 0)
        {
            Debug.LogError("Plant prefabs array is empty!");
            return;
        }
     

        // prepare reusable batch buffers
        batchMatrices = new Matrix4x4[MAX_INSTANCES_PER_BATCH];
        batchOffsets = new float[MAX_INSTANCES_PER_BATCH];
        propertyBlock = new MaterialPropertyBlock();

        // cache mesh/material to avoid GetComponent in Update
        cachedMeshes = new Mesh[plantPrefabs.Length];
        cachedMaterials = new Material[plantPrefabs.Length];
        for (int i = 0; i < plantPrefabs.Length; i++)
        {
            var mf = plantPrefabs[i].GetComponent<MeshFilter>();
            var mr = plantPrefabs[i].GetComponent<MeshRenderer>();
            cachedMeshes[i] = mf != null ? mf.sharedMesh : null;
            cachedMaterials[i] = mr != null ? mr.sharedMaterial : null;
            if (cachedMaterials[i] != null) cachedMaterials[i].enableInstancing = true;
        }


        // generate data once at start (only regenerated on RefreshInstances)
        GenerateInstanceData();

        if (instantiationType == InstantiationType.CPU)
        {
            InstantiateOnCPU();
        }
    }

    void Update()
    {
        if (instantiationType == InstantiationType.GPU)
        {
            UpdateAndRenderGPU();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, instantiationRadius);
    }

    public InstantiationType SwitchInstantiationType()
    {
        if (instantiationType == InstantiationType.CPU)
        {
            DestroyCPUInstances();
            instantiationType = InstantiationType.GPU;
        }
        else
        {
            instantiationType = InstantiationType.CPU;
            if (positions == null || prefabIndices == null) GenerateInstanceData();
            InstantiateOnCPU();
        }

        return instantiationType;
    }

    void RefreshInstances()
    {
        // full refresh only when count (or relevant settings) changed
        DestroyCPUInstances();
        ClearInstanceData();

        GenerateInstanceData();

        if (instantiationType == InstantiationType.CPU)
        {
            InstantiateOnCPU();
        }
    }

    void DestroyCPUInstances()
    {
        foreach (Transform child in transform) 
        {
            Destroy(child.gameObject);
        }
    }

    void ClearInstanceData()
    {
        positions = null;
        scales = null;
        offsets = null;
        prefabIndices = null;
        instanceIndicesByPrefab = null;
        instanceMatrices = null;
    }

    Vector3 GetRandomPositionInCircle()
    {
        Vector2 randomPoint = Random.insideUnitCircle * instantiationRadius;
        return new Vector3(
            transform.position.x + randomPoint.x,
            0f,
            transform.position.z + randomPoint.y);
    }

    Vector3 GetHeightOnTerrain(Vector3 position)
    {
        if (terrain == null) return position;
        position.y = terrain.SampleHeight(position);
        return position;
    }

    // CENTRALIZED data generation used by both CPU and GPU instancing
    private void GenerateInstanceData()
    {
        // allocate arrays
        positions = new Vector3[numberOfObjects];
        scales = new Vector3[numberOfObjects];
        offsets = new float[numberOfObjects];
        prefabIndices = new int[numberOfObjects];

        // prepare per-prefab lists
        instanceIndicesByPrefab = new List<int>[plantPrefabs.Length];
        for (int p = 0; p < plantPrefabs.Length; p++)
        {
            instanceIndicesByPrefab[p] = new List<int>();
        }

        // allocate instanceMatrices
        instanceMatrices = new Matrix4x4[numberOfObjects];

        // fill arrays and per-prefab lists (single pass, O(n))
        for (int i = 0; i < numberOfObjects; i++)
        {
            Vector3 pos = GetRandomPositionInCircle();
            pos = GetHeightOnTerrain(pos);

            positions[i] = pos;
            offsets[i] = Random.Range(0f, 10f);
            int p = Random.Range(0, plantPrefabs.Length);
            prefabIndices[i] = p;
            scales[i] = plantPrefabs[p].transform.localScale;

            // precompute TRS matrix once
            instanceMatrices[i] = Matrix4x4.TRS(positions[i], Quaternion.identity, scales[i]);

            instanceIndicesByPrefab[p].Add(i);
        }
    }

    void InstantiateOnCPU()
    {
        if (positions == null || prefabIndices == null) GenerateInstanceData();

        for (int i = 0; i < numberOfObjects; i++)
        {
            int p = prefabIndices[i];
            GameObject prefab = plantPrefabs[p];
            Vector3 pos = positions[i];
            GameObject instance = Instantiate(prefab, pos, Quaternion.identity, transform);
            var mr = instance.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = enableShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }


    void UpdateAndRenderGPU()
    {
        if (WindManager.Instance == null || positions == null) return;

        var shadowMode = enableShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;

        for (int prefabIndex = 0; prefabIndex < plantPrefabs.Length; prefabIndex++)
        {
            var indices = instanceIndicesByPrefab[prefabIndex];
            if (indices == null || indices.Count == 0) continue;

            var mesh = cachedMeshes[prefabIndex];
            var material = cachedMaterials[prefabIndex];

            int total = indices.Count;
            for (int start = 0; start < total; start += MAX_INSTANCES_PER_BATCH)
            {
                int batchSize = Mathf.Min(MAX_INSTANCES_PER_BATCH, total - start);

                for (int b = 0; b < batchSize; b++)
                {
                    int idx = indices[start + b];
                    // copy precomputed matrix - no TRS computation per frame
                    batchMatrices[b] = instanceMatrices[idx];
                    batchOffsets[b] = offsets[idx];
                }

                propertyBlock.Clear();
                propertyBlock.SetFloatArray("_Offset", batchOffsets);

                Graphics.DrawMeshInstanced(mesh, 0, material, batchMatrices, batchSize, propertyBlock, shadowMode);
            }
        }
    }

    private void OnDestroy()
    {
        WindManager.OnWindChanged -= UpdateWindListener;
    }

    private void OnEnable()
    {
        WindManager.OnWindChanged += UpdateWindListener;
    }

    private void OnDisable()
    {
        WindManager.OnWindChanged -= UpdateWindListener;
    }
}