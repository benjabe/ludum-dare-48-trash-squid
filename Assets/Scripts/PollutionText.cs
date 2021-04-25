using UnityEngine;
using UnityEngine.UI;

public class PollutionText : MonoBehaviour
{
    private Text _text = null;
    
    private void Awake()
    {
        _text = GetComponent<Text>();
        PollutionSystem.OnPollutionSet += OnPollutionSet;
    }

    private void OnDestroy()
    {
        PollutionSystem.OnPollutionSet -= OnPollutionSet;
    }

    private void OnPollutionSet(PollutionSystem pollutionSystem, int pollution)
    {
        _text.text = $"Pollution: {pollution}";
    }
}
