using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrashSpawner : MonoBehaviour
{
    public static event Action<TrashSpawner, Trash> OnTrashSpawned;
    public List<GameObject> _trashPrefabs = new List<GameObject>();

    [SerializeField] private GameObject _trashPrefab = null;
    [SerializeField] private float _timeBetweenSpawn = 5.0f;

    private void Awake()
    {
        StartCoroutine(SpawnTrashAtRegularInterval());
    }

    private IEnumerator SpawnTrashAtRegularInterval()
    {
        while (true)
        {
            SpawnTrash();
            yield return new WaitForSeconds(_timeBetweenSpawn);
        }
    }

    private void SpawnTrash()
    {
        GameObject trashPrefab = _trashPrefabs[Random.Range(0, _trashPrefabs.Count)];
        var go = Instantiate(trashPrefab, transform);
        go.transform.position = new Vector3(Random.Range(-50f, 50f), 0);
        var trash = go.GetComponent<Trash>();
        OnTrashSpawned?.Invoke(this, trash);
    }
}
