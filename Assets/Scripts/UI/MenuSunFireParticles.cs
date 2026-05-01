using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class MenuSunFireParticles : MonoBehaviour
{
    [SerializeField] private bool configureCanvasForParticles = true;
    [SerializeField] private bool enableMeteorShower = false;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 sunViewportPosition = new Vector2(0.5f, 0.78f);
    [SerializeField] private Vector2 meteorViewportPosition = new Vector2(0.88f, 1.04f);
    [SerializeField] private float sunWorldRadius = 1.45f;
    [SerializeField] private float particleLayerZ = 0f;
    [SerializeField] private int sortingOrder = 6;
    [SerializeField] private ParticleSystem fireParticles;
    [SerializeField] private ParticleSystem meteorParticles;

    private Material runtimeParticleMaterial;
    private bool particlesVisible = true;

    private void OnEnable()
    {
        RefreshParticles();
    }

    private void Start()
    {
        RefreshParticles();
    }

    private void OnValidate()
    {
        RefreshParticles();
    }

    private void LateUpdate()
    {
        if (fireParticles == null || meteorParticles == null)
        {
            EnsureParticleSystem();
        }

        PositionParticleSystems();
    }

    public void RefreshParticles()
    {
        EnsureParticleSystem();
        ConfigureParticleSystems();
        PositionParticleSystems();
        SetParticlesVisible(particlesVisible);
    }

    public void SetParticlesVisible(bool visible)
    {
        particlesVisible = visible;

        if (fireParticles == null)
        {
            EnsureParticleSystem();
        }

        if (fireParticles == null)
        {
            SetMeteorParticlesVisible(visible && enableMeteorShower);
            return;
        }

        ParticleSystem.EmissionModule emission = fireParticles.emission;
        emission.enabled = visible;

        ParticleSystemRenderer renderer = fireParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
        }

        if (visible)
        {
            if (!fireParticles.isPlaying)
            {
                fireParticles.Play();
            }
        }
        else
        {
            fireParticles.Clear();
            fireParticles.Pause();
        }

        SetMeteorParticlesVisible(visible && enableMeteorShower);
    }

    private void EnsureParticleSystem()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        if (configureCanvasForParticles)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = targetCamera;
                canvas.planeDistance = 12f;
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            }
        }

        EnsureParticleObject(ref fireParticles, "MenuSunFireParticles");
        EnsureParticleObject(ref meteorParticles, "MenuMeteorShowerParticles");
    }

    private void EnsureParticleObject(ref ParticleSystem particles, string objectName)
    {
        if (targetCamera == null)
        {
            return;
        }

        if (particles == null)
        {
            Transform existing = targetCamera.transform.Find(objectName);
            if (existing == null)
            {
                ParticleSystem[] childParticles = targetCamera.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < childParticles.Length; i++)
                {
                    ParticleSystem candidate = childParticles[i];
                    if (candidate != null && candidate.name == objectName)
                    {
                        existing = candidate.transform;
                        break;
                    }
                }
            }

            if (existing != null)
            {
                particles = existing.GetComponent<ParticleSystem>();
            }
        }

        if (particles == null)
        {
            GameObject particleObject = new GameObject(objectName);
            particleObject.transform.SetParent(targetCamera.transform, false);
            particles = particleObject.AddComponent<ParticleSystem>();
        }

        if (particles == null)
        {
            return;
        }

        if (particles.transform.parent != targetCamera.transform)
        {
            particles.transform.SetParent(targetCamera.transform, false);
        }

        particles.gameObject.name = objectName;
        particles.gameObject.SetActive(true);
    }

    private void ConfigureParticleSystems()
    {
        ConfigureFireParticles();
        if (enableMeteorShower)
        {
            ConfigureMeteorParticles();
        }
        else
        {
            DisableMeteorParticles();
        }
    }

    private void ConfigureFireParticles()
    {
        if (fireParticles == null)
        {
            return;
        }

        ParticleSystem.MainModule main = fireParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 260;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.48f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.105f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.45f, 0.45f);
        main.gravityModifier = -0.035f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.56f, 0.18f, 0.70f),
            new Color(0.74f, 0.12f, 0.04f, 0.42f));

        ParticleSystem.EmissionModule emission = fireParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 34f;

        ParticleSystem.ShapeModule shape = fireParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = sunWorldRadius;
        shape.radiusThickness = 0f;
        shape.arc = 360f;
        shape.randomDirectionAmount = 0.62f;
        shape.sphericalDirectionAmount = 0.75f;

        ParticleSystem.VelocityOverLifetimeModule velocity = fireParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.11f, 0.11f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.06f, 0.26f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule color = fireParticles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.76f, 0.32f), 0f),
                new GradientColorKey(new Color(0.94f, 0.20f, 0.06f), 0.52f),
                new GradientColorKey(new Color(0.20f, 0.02f, 0.01f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.78f, 0.16f),
                new GradientAlphaKey(0.22f, 0.68f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = fireParticles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.42f),
            new Keyframe(0.22f, 1f),
            new Keyframe(1f, 0.08f));
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = fireParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = sortingOrder;
        renderer.minParticleSize = 0.001f;
        renderer.maxParticleSize = 0.08f;

        if (runtimeParticleMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                runtimeParticleMaterial = new Material(shader)
                {
                    name = "MenuSunFireParticles_Runtime"
                };
            }
        }

        if (runtimeParticleMaterial != null)
        {
            renderer.sharedMaterial = runtimeParticleMaterial;
        }

        if (!fireParticles.isPlaying)
        {
            fireParticles.Play();
        }

        SetParticlesVisible(particlesVisible);
    }

    private void ConfigureMeteorParticles()
    {
        if (meteorParticles == null)
        {
            return;
        }

        ParticleSystem.MainModule main = meteorParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.075f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.93f, 0.72f, 0.18f),
            new Color(1f, 0.64f, 0.28f, 0.28f));

        ParticleSystem.EmissionModule emission = meteorParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 3.2f;

        ParticleSystem.ShapeModule shape = meteorParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5.4f, 1.6f, 0.02f);
        shape.randomDirectionAmount = 0f;
        shape.sphericalDirectionAmount = 0f;

        ParticleSystem.VelocityOverLifetimeModule velocity = meteorParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-5.8f, -4.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(-4.4f, -2.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule color = meteorParticles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.95f, 0.80f), 0f),
                new GradientColorKey(new Color(0.98f, 0.62f, 0.25f), 0.46f),
                new GradientColorKey(new Color(0.54f, 0.15f, 0.06f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.42f, 0.10f),
                new GradientAlphaKey(0.18f, 0.60f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = meteorParticles.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.15f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0.12f));
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = meteorParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.sortingOrder = sortingOrder - 1;
        renderer.minParticleSize = 0.0005f;
        renderer.maxParticleSize = 0.06f;
        renderer.lengthScale = 8f;
        renderer.velocityScale = 0.45f;
        renderer.cameraVelocityScale = 0f;
        renderer.freeformStretching = true;

        if (runtimeParticleMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                runtimeParticleMaterial = new Material(shader)
                {
                    name = "MenuSunFireParticles_Runtime"
                };
            }
        }

        if (runtimeParticleMaterial != null)
        {
            renderer.sharedMaterial = runtimeParticleMaterial;
        }

        renderer.enabled = true;

        if (!meteorParticles.isPlaying)
        {
            meteorParticles.Play();
        }
    }

    private void DisableMeteorParticles()
    {
        if (meteorParticles == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission = meteorParticles.emission;
        emission.enabled = false;

        ParticleSystemRenderer renderer = meteorParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        meteorParticles.Clear();
        meteorParticles.Pause();
    }

    private void SetMeteorParticlesVisible(bool visible)
    {
        if (meteorParticles == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission = meteorParticles.emission;
        emission.enabled = visible;

        ParticleSystemRenderer renderer = meteorParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.enabled = visible;
        }

        if (visible)
        {
            if (!meteorParticles.isPlaying)
            {
                meteorParticles.Play();
            }
        }
        else
        {
            meteorParticles.Clear();
            meteorParticles.Pause();
        }
    }

    private void PositionParticleSystems()
    {
        PositionFireParticleSystem();
        PositionMeteorParticleSystem();
    }

    private void PositionFireParticleSystem()
    {
        if (fireParticles == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 worldPosition = targetCamera.ViewportToWorldPoint(new Vector3(sunViewportPosition.x, sunViewportPosition.y, Mathf.Abs(targetCamera.transform.position.z - particleLayerZ)));
        worldPosition.z = particleLayerZ;
        fireParticles.transform.position = worldPosition;
        fireParticles.transform.localScale = Vector3.one;
    }

    private void PositionMeteorParticleSystem()
    {
        if (meteorParticles == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 worldPosition = targetCamera.ViewportToWorldPoint(new Vector3(meteorViewportPosition.x, meteorViewportPosition.y, Mathf.Abs(targetCamera.transform.position.z - particleLayerZ)));
        worldPosition.z = particleLayerZ;
        meteorParticles.transform.position = worldPosition;
        meteorParticles.transform.localScale = Vector3.one;
    }
}
