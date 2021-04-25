using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrashSpawner : MonoBehaviour
{
    public static event Action<TrashSpawner, Trash> OnTrashSpawned;    

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
        var go = Instantiate(_trashPrefab, transform);
        go.transform.position = new Vector3(Random.Range(-50f, 50f), 0);
        var trash = go.GetComponent<Trash>();
        OnTrashSpawned?.Invoke(this, trash);
    }
}
