using UnityEngine;

namespace EchoProtocol
{
    public sealed class EchoProtocolAmbientParticles2D : MonoBehaviour
    {
        public void Configure(float worldWidth, float height)
        {
            ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.055f);
            main.startColor = new Color(0.62f, 0.71f, 0.67f, 0.3f);
            main.maxParticles = 140;
            var emission = particles.emission;
            emission.rateOverTime = 9f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(worldWidth, height, 0.2f);
            particles.transform.position = new Vector3(worldWidth * 0.5f, 0f, 3f);
            particles.Play();
        }
    }
}
