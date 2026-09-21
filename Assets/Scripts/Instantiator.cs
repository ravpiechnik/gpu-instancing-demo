using System.Collections.Generic;
using UnityEngine;

public class Instantiator : MonoBehaviour
{
    public enum InstantiationType
    {
        CPU,
        GPU
    }

    #region INSPECTOR FIELDS
    [Header("Instantiation Settings")]
    [SerializeField] private int numberOfObjects = 1000;
    [SerializeField] private GameObject[] plantPrefabs;
    [SerializeField] private float instantiationRadius = 10f;
    [SerializeField] private InstantiationType instantiationType = InstantiationType.CPU;
    [SerializeField] private bool enableShadows = true;
    #endregion

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


    private Terrain terrain;
    private InstancingData instancingData;

    private Matrix4x4[] batchMatrices;
    private float[] batchOffsets;
    private MaterialPropertyBlock propertyBlock;
    private Mesh[] cachedMeshes;
    private Material[] cachedMaterials;
    private const int MAX_INSTANCES_PER_BATCH = 1023;

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

        batchMatrices = new Matrix4x4[MAX_INSTANCES_PER_BATCH];
        batchOffsets = new float[MAX_INSTANCES_PER_BATCH];
        propertyBlock = new MaterialPropertyBlock();

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

        instancingData = new InstancingData();
        instancingData.Generate(numberOfObjects, plantPrefabs, transform.position, instantiationRadius, terrain);

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
            if (instancingData == null) instancingData = new InstancingData();
            if (instancingData.Positions == null) instancingData.Generate(numberOfObjects, plantPrefabs, transform.position, instantiationRadius, terrain);
            InstantiateOnCPU();
        }

        return instantiationType;
    }

    void RefreshInstances()
    {
        // destroy CPU instances and regenerate instance data
        DestroyCPUInstances();

        if (instancingData != null) instancingData.Clear();
        instancingData = new InstancingData();
        instancingData.Generate(numberOfObjects, plantPrefabs, transform.position, instantiationRadius, terrain);

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


    void InstantiateOnCPU()
    {
        if (instancingData == null || instancingData.Positions == null) instancingData.Generate(numberOfObjects, plantPrefabs, transform.position, instantiationRadius, terrain);

        for (int i = 0; i < numberOfObjects; i++)
        {
            int p = instancingData.PrefabIndices[i];
            GameObject prefab = plantPrefabs[p];
            Vector3 pos = instancingData.Positions[i];
            GameObject instance = Instantiate(prefab, pos, Quaternion.identity, transform);
            var mr = instance.GetComponent<MeshRenderer>();
            var mpb = new MaterialPropertyBlock();
            mpb.SetFloat("_Offset", instancingData.Offsets[i]);

            if (mr != null)
            {
                mr.shadowCastingMode = enableShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.SetPropertyBlock(mpb);
            }
        }
    }

    void UpdateAndRenderGPU()
    {
        if (WindManager.Instance == null || instancingData == null || instancingData.Positions == null) return;

        var shadowMode = enableShadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;

        for (int prefabIndex = 0; prefabIndex < plantPrefabs.Length; prefabIndex++)
        {
            var indices = instancingData.IndicesByPrefab[prefabIndex];
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
                    batchMatrices[b] = instancingData.InstanceMatrices[idx];
                    batchOffsets[b] = instancingData.Offsets[idx];
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
