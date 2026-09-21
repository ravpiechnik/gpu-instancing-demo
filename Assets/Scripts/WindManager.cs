using System;
using UnityEngine;

public class WindManager : MonoBehaviour
{
    public static WindManager Instance { get; private set; }
    public static event Action OnWindChanged;

    

    [SerializeField] private float windStrength = 1f;
    [SerializeField] private float windSpeed = 2f;
    //values are retained even if wind is disabled

    [SerializeField] private bool windEnabled = true;

    public float WindStrength 
    { 
        get => windEnabled ? windStrength : 0f;
        set {
            windStrength = value;
            OnWindChanged?.Invoke();
        }
    }

    public float WindSpeed 
    {
        get => windEnabled ? windSpeed : 0f;
        set {
            windSpeed = value;
            OnWindChanged?.Invoke();
        }
    }

    public bool WindEnabled
    {
        get => windEnabled;
        set {
            windEnabled = value;
            OnWindChanged?.Invoke();
        }   
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
