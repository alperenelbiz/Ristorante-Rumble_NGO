using UnityEngine;

[CreateAssetMenu(fileName = "PhaseSettings", menuName = "RistoranteRumble/PhaseSettings")]
public class PhaseSettingsSO : ScriptableObject
{
    [Header("Timers")]
    public float dayPhaseDuration = 120f;
    public float nightPhaseDuration = 60f;
    public float startCountdown = 5f;
    public float transitionDuration = 3f;

    [Header("Players")]
    public int minPlayersToStart = 2;
}
