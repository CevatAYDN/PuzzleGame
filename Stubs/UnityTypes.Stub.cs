// Unity Engine stub types for dotnet build
// Minimal type definitions to allow compilation without Unity SDK references

using System;

namespace UnityEngine
{
    public enum LogType { Log, Warning, Error, Exception, Assert }

    public enum RuntimeInitializeLoadType { BeforeSceneLoad, AfterSceneLoad }

    public struct Vector2
    {
        public float x; public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
    }

    public struct Vector3
    {
        public float x; public float y; public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public float magnitude => 0f;
    }

    public struct Quaternion
    {
        public float x; public float y; public float z; public float w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        public static Quaternion Euler(float x, float y, float z) => default;
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => default;
    }

    public struct Color
    {
        public float r; public float g; public float b; public float a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color red => new Color(1, 0, 0);
        public static Color green => new Color(0, 1, 0);
        public static Color blue => new Color(0, 0, 1);
        public Color(byte r, byte g, byte b, byte a = 255) { this.r = r / 255f; this.g = g / 255f; this.b = b / 255f; this.a = a / 255f; }
    }

    public struct Mathf
    {
        public const float PI = 3.14159274f;
        public const float Deg2Rad = 0.0174532924f;
        public const float Rad2Deg = 57.29578f;
        public static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
        public static float Clamp01(float value) => Clamp(value, 0, 1);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        public static float MoveTowards(float current, float target, float maxDelta) => target - current > 0 ? Min(current + maxDelta, target) : Max(current - maxDelta, target);
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Abs(float f) => f < 0 ? -f : f;
        public static int Abs(int i) => i < 0 ? -i : i;
        public static float Floor(float f) => (float)(int)f;
        public static float Ceil(float f) => (float)((int)f + (f > (int)f ? 1 : 0));
        public static float Round(float f) => (float)(int)(f + 0.5f);
        public static float Sqrt(float f) => (float)System.Math.Sqrt(f);
        public static float Pow(float f, float p) => (float)System.Math.Pow(f, p);
        public static float Sin(float f) => (float)System.Math.Sin(f);
        public static float Cos(float f) => (float)System.Math.Cos(f);
        public static float Tan(float f) => (float)System.Math.Tan(f);
        public static float Asin(float f) => (float)System.Math.Asin(f);
        public static float Acos(float f) => (float)System.Math.Acos(f);
        public static float Atan(float f) => (float)System.Math.Atan(f);
        public static float Atan2(float y, float x) => (float)System.Math.Atan2(y, x);
        public static float Sign(float f) => f < 0 ? -1 : f > 0 ? 1 : 0;
    }

    public class Debug
    {
        public static void Log(object message) { }
        public static void Log(object message, UnityEngine.Object context) { }
        public static void LogWarning(object message) { }
        public static void LogWarning(object message, UnityEngine.Object context) { }
        public static void LogError(object message) { }
        public static void LogError(object message, UnityEngine.Object context) { }
        public static void LogException(System.Exception exception) { }
    }

    public struct LayerMask
    {
        public int value;
        public static implicit operator LayerMask(int value) => new LayerMask { value = value };
        public static implicit operator int(LayerMask mask) => mask.value;
    }

    public struct RaycastHit
    {
        public Vector3 point;
        public Vector3 normal;
        public GameObject collider;
    }

    public struct MaterialPropertyBlock
    {
        public void SetColor(string name, Color color) { }
        public void SetFloat(string name, float value) { }
        public void SetInt(string name, int value) { }
    }

    public class Object
    {
        public static void Destroy(UnityEngine.Object obj) { }
        public static void DestroyImmediate(UnityEngine.Object obj) { }
        public static T Instantiate<T>(T original) where T : UnityEngine.Object => default;
        public static T FindObjectOfType<T>() where T : UnityEngine.Object => default;
        public static T[] FindObjectsOfType<T>() where T : UnityEngine.Object => default;
        public static GameObject[] FindGameObjectsWithTag(string tag) => default;
        public string name;
    }

    public class GameObject : UnityEngine.Object
    {
        public Transform transform;
        public string tag;
        public LayerMask layer;
        public bool activeSelf;
        public bool activeInHierarchy;
        public T GetComponent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public T[] GetComponents<T>() => default;
        public void SetActive(bool value) { }
    }

    public class Component : UnityEngine.Object
    {
        public GameObject gameObject;
        public Transform transform;
        public T GetComponent<T>() => default;
        public T GetComponentInParent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public T[] GetComponents<T>() => default;
    }

    public class Transform : Component
    {
        public Vector3 position;
        public Vector3 localPosition;
        public Quaternion rotation;
        public Quaternion localRotation;
        public Vector3 eulerAngles;
        public Vector3 localEulerAngles;
        public Vector3 localScale;
        public Transform parent;
        public int childCount;
        public Transform GetChild(int index) => default;
        public void SetParent(Transform parent) { }
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation) { }
        public void Translate(Vector3 by) { }
        public void Rotate(Vector3 eulers) { }
        public void LookAt(Vector3 target) { }
    }

    public class MonoBehaviour : Component { }

    public class ScriptableObject : UnityEngine.Object { }

    public class Renderer : Component
    {
        public Material material;
        public MaterialPropertyBlock sharedMaterialProperties;
        public Color materialColor;
    }

    public class LineRenderer : Renderer
    {
        public void SetPosition(int index, Vector3 position) { }
        public void SetColors(Color start, Color end) { }
        public void SetWidth(float start, float end) { }
        public int positionCount;
    }

    public class ParticleSystem : Component
    {
        public void Play() { }
        public void Stop() { }
        public void Clear() { }
        public bool isPlaying;
        public bool isStopped;
        public int particleCount;
        public class MainModule
        {
            public Vector3 startSize;
            public Color startColor;
            public float startLifetime;
            public float duration;
            public bool loop;
            public float speed;
        }
        public MainModule main => default;
        public class EmissionModule
        {
            public int rateOverTime;
        }
        public EmissionModule emission => default;
    }

    public class Collider : Component
    {
        public bool isTrigger;
    }

    public class Material : UnityEngine.Object
    {
        public Color color;
        public string shaderKeywords;
    }

    public class AudioClip : UnityEngine.Object { }

    public class AudioMixerGroup : UnityEngine.Object { }

    public class Sprite : UnityEngine.Object { }

    public class Font : UnityEngine.Object { }

    public class Texture : UnityEngine.Object { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SerializeFieldAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class HideInInspectorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MinAttribute : Attribute
    {
        public MinAttribute(float min) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CreateAssetMenuAttribute : Attribute
    {
        public string fileName;
        public string menuName;
        public int order;
    }

    public class CanvasGroup : Component
    {
        public float alpha;
        public bool blocksRaycasts;
        public bool interactable;
    }

    public class RectTransform : Transform
    {
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public void SetSizeWithCurrentAnchors(Vector2 axis, float size) { }
    }

    public static class Application
    {
        public static event System.Action<string, string, LogType> logMessageReceivedThreaded;
    }
}

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single, Additive }

    public class SceneManager
    {
        public static void LoadScene(string sceneName) { }
        public static void LoadScene(int sceneBuildIndex) { }
        public static UnityEngine.SceneManagement.Scene GetActiveScene() => default;
        public static int sceneCount;
    }

    public struct Scene
    {
        public string name;
        public int buildIndex;
    }
}

namespace UnityEngine.Rendering
{
    public class RenderPipelineAsset : UnityEngine.Object { }
    public class ScriptableRendererFeature : UnityEngine.Object { }
}

namespace UnityEngine.UI
{
    public class Button : UnityEngine.UI.Selectable
    {
        public class ButtonClickedEvent : UnityEngine.Events.UnityEvent { }
        public ButtonClickedEvent onClick;
    }

    public class Selectable : UnityEngine.MonoBehaviour
    {
        public bool interactable;
    }

    public class MaskableGraphic : UnityEngine.MonoBehaviour
    {
        public UnityEngine.UI.Graphic canvasRenderer;
    }

    public class Graphic : UnityEngine.UI.MaskableGraphic
    {
        public Color color;
        public Material material;
    }

    public class Image : UnityEngine.UI.MaskableGraphic
    {
        public UnityEngine.Sprite sprite;
        public Color color;
    }

    public class RawImage : UnityEngine.UI.MaskableGraphic
    {
        public UnityEngine.Texture texture;
    }

    public class Text : UnityEngine.UI.Graphic
    {
        public string text;
        public Color color;
        public UnityEngine.Font font;
    }
}

namespace TMPro
{
    public class TextMeshProUGUI : UnityEngine.UI.MaskableGraphic
    {
        public string text;
        public UnityEngine.Color color;
        public float fontSize;
    }
}

namespace UnityEngine.UIElements
{
    public class VisualElement
    {
        public string styleSheets;
    }
}

namespace UnityEngine.VFX
{
    public class VisualEffect : UnityEngine.Component
    {
        public void Play() { }
        public void Stop() { }
        public void SetFloat(string name, float value) { }
        public void SetVector3(string name, UnityEngine.Vector3 value) { }
    }
}

namespace UnityEngine.Audio
{
    // Stub for Audio - UnityEngine.AudioSettings would go here
}

namespace UnityEngine.Events
{
    public class UnityEvent { }
}

namespace UnityEngine
{
    // EntityId: Used for Unity component instance identification.
    public struct EntityId { }
}

namespace GoogleMobileAds.Ump
{
    // ConsentRequestParameters - stub for dotnet build
    public class ConsentRequestParameters
    {
        public bool TagForUnderAgeOfConsent { get; set; }
        public bool TagForAgeRestricted { get; set; }
    }

    // FormError - stub for dotnet build
    public class FormError
    {
        public string Message { get; set; }
    }

    // ConsentStatus - stub for dotnet build
    public enum ConsentStatus
    {
        Unknown,
        NotRequired,
        Required,
        Obtained
    }

    // ConsentInformation - stub for dotnet build
    public static class ConsentInformation
    {
        public static ConsentStatus ConsentStatus { get; set; }
        public static bool IsConsentFormAvailable() => true;
        public static bool CanRequestAds() => true;
        public static void Update(ConsentRequestParameters parameters, System.Action<FormError> callback) { }
    }

    // ConsentForm - stub for dotnet build
    public class ConsentForm
    {
        public static void Load(System.Action<ConsentForm, FormError> callback) { }
        public void Show(System.Action callback) { }
        public void Dispose() { }
    }
}

namespace PrimeTween
{
    public enum Ease
    {
        Linear,
        OutCubic,
        InCubic,
        InOutCubic,
        OutQuart,
        InQuart,
        InOutQuart,
        OutElastic,
        InElastic,
    }

    public class Sequence : IDisposable
    {
        public void Dispose() { }
        public void Kill(bool completed = false) { }
        public void Pause() { }
        public void Resume() { }
        public bool isPlaying;
        public bool isComplete;
        public float duration;
        public float elapsed;
    }

    public class Tween
    {
        public bool isPlaying;
        public bool isComplete;
        public float duration;
        public float elapsed;
        public void Kill(bool completed = false) { }
        public void Pause() { }
        public void Resume() { }
    }

    public static class TweenFactory
    {
        public static void Configure<T>(T target, System.Action<T, float> setTargetValue, float endValue, float duration, Ease ease = Ease.Linear) { }
        public static Tween Alpha(UnityEngine.CanvasGroup target, float endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween Position(UnityEngine.Transform target, UnityEngine.Vector3 endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween Rotation(UnityEngine.Transform target, UnityEngine.Quaternion endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween Scale(UnityEngine.Transform target, UnityEngine.Vector3 endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween LocalPosition(UnityEngine.Transform target, UnityEngine.Vector3 endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween LocalScale(UnityEngine.Transform target, UnityEngine.Vector3 endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween Color(UnityEngine.Renderer target, UnityEngine.Color endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween MaterialColor(UnityEngine.Renderer target, UnityEngine.Color endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween PIVector2(UnityEngine.UI.Graphic target, UnityEngine.Vector2 endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween SizeDelta(UnityEngine.RectTransform target, UnityEngine.Vector2 endValue, float duration, Ease ease = Ease.Linear) => default;
        public static Tween AnimateColor(UnityEngine.Color[] colors, float[] times, UnityEngine.UI.Graphic target, float duration, Ease ease = Ease.Linear) => default;
        public static void AnimateVector(UnityEngine.Vector2[] values, float[] times, UnityEngine.UI.Graphic target, System.Action<UnityEngine.UI.Graphic, UnityEngine.Vector2> setValue, float duration, Ease ease = Ease.Linear) { }
        public static void AnimateValue<T>(T start, T end, float duration, Ease ease, System.Action<T> setValue, System.Action onCompleted = null) where T : struct { }
        public static Sequence Sequence(System.Action<Sequence> setup = null) => default;
        public static Tween Delay(float duration, Ease ease = Ease.Linear) => default;
        public static Tween DelayAndChain(UnityEngine.GameObject target, float duration, Ease ease = Ease.Linear) => default;
    }
}

namespace Unity.Collections
{
    public struct EntityId { }
}
