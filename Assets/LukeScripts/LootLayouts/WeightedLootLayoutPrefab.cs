using UnityEngine;

[System.Serializable]
public class WeightedLootLayoutPrefab
{
    public GameObject layoutPrefab;

    [Range(0f, 10f)]
    public float weight = 1f;
}