using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PollutionSystem : MonoBehaviour
{
    public static event Action<PollutionSystem, int> OnPollutionSet;

    public Material water;
    public Material sky;

    private int _pollution = 0;
    private int _pollutionID;

    public int Pollution
    {
        get => _pollution;
        private set
        {
            _pollution = Mathf.Clamp(value, 0, 100);
            water.SetFloat(_pollutionID, _pollution / 100.0f);
            sky.SetFloat(_pollutionID, _pollution / 100.0f);
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
        //water.SetFloat("Pollution", _pollution / 100.0f);
        _pollutionID = Shader.PropertyToID("_Pollution");
        water.SetFloat(_pollutionID, _pollution / 100.0f);
        sky.SetFloat(_pollutionID, _pollution / 100.0f);
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
