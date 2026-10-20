using UnityEngine;

public class FeedbackVFX2D : MonoBehaviour
{
    private static ParticleSystem shared;

    private void Awake()
    {
        shared = GetComponent<ParticleSystem>();
        if (shared == null) shared = gameObject.AddComponent<ParticleSystem>();
        var main = shared.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        var emission = shared.emission;
        emission.enabled = false;
        shared.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public static void Burst(Vector3 position, Color color, int count = 10)
    {
        if (shared == null) return;
        var parameters = new ParticleSystem.EmitParams
        {
            position = position,
            startColor = color,
            startLifetime = Random.Range(0.28f, 0.65f),
            startSize = Random.Range(0.045f, 0.11f),
            velocity = Random.insideUnitCircle * Random.Range(1.5f, 4f)
        };
        shared.Emit(parameters, Mathf.Clamp(count, 1, 48));
    }

    public static void Shake(float strength, float duration)
    {
        CameraFollow2D cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
        if (cameraFollow != null) cameraFollow.Shake(strength, duration);
    }
}
