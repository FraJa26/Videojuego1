using UnityEngine;

public static class CoinBurstEffect
{
    private static Material sharedMaterial;

    public static void SpawnAt(Vector3 position)
    {
        var go = new GameObject("CoinBurstFX");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.startLifetime = 0.4f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.85f, 0.2f);
        main.duration = 0.3f;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetSharedMaterial();

        ps.Play();

        Object.Destroy(go, main.duration + main.startLifetime.constantMax + 0.5f);
    }

    private static Material GetSharedMaterial()
    {
        if (sharedMaterial == null)
        {
            sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        return sharedMaterial;
    }
}
