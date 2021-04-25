using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PollutionSystem : MonoBehaviour
{
    public static event Action<PollutionSystem, int> OnPollutionSet;

    private int _pollution = 0;

    public int Pollution
    {
        get => _pollution;
        private set
        {
            _pollution = Mathf.Clamp(value, 0, 100);
            OnPollutionSet?.Invoke(this, _pollution);
            if (_pollution == 100) SceneManager.LoadScene("LossScene");
        }
    }

    private void Awake()
    {
        TrashSpawner.OnTrashSpawned += OnTrashSpawned;
        BoatTop.OnTrashHit += OnTrashHitBoatTop;
    }

    private void Start()
    {
        Pollution = 0;
    }

    private void OnDestroy()
    {
        TrashSpawner.OnTrashSpawned -= OnTrashSpawned;
        BoatTop.OnTrashHit -= OnTrashHitBoatTop;
    }

    private void OnTrashHitBoatTop(BoatTop boatTop, Trash trash)
    {
        Pollution -= 1;
    }

    private void OnTrashSpawned(TrashSpawner spawner, Trash trash)
    {
        Pollution += 1;
    }
}
