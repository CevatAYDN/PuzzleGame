using System.Collections;
using BottleShaders.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BottleShaders
{
    public class GameManager : MonoBehaviour
    {
        private BottleController selectedBottle;
        private Vector3 selectedOriginalPos;

        [Header("Bottle Animation")]
        public float liftHeight = 1.0f;
        public float animationDuration = 0.4f;

        [Header("Raycast")]
        public LayerMask bottleLayerMask = ~0;

        [Header("HUD")]
        [SerializeField] private bool showRuntimeHud = true;

        [Header("Pour Line")]
        public int pourLinePoolSize = 6;

        private bool isAnimating = false;
        private bool gameWon = false;
        private int moveCount = 0;

        private Camera mainCam;

        private Material pourLineMaterial;
        private LineRenderer[] pourLinePool;
        private int currentPourLineIndex = 0;
        private Color lastPourColor;

        private GUIStyle hudTitleStyle;
        private GUIStyle hudTextStyle;
        private GUIStyle hudButtonStyle;

        private void Start()
        {
            mainCam = Camera.main;
            if (mainCam == null) mainCam = FindAnyObjectByType<Camera>();
            InitializePourLinePool();
            EnsureCameraPostFx();
        }

        private void OnDestroy()
        {
            if (pourLinePool != null)
            {
                foreach (var line in pourLinePool)
                {
                    if (line != null)
                    {
                        if (Application.isPlaying) Destroy(line.gameObject);
                        else DestroyImmediate(line.gameObject);
                    }
                }
            }

            if (pourLineMaterial != null)
            {
                if (Application.isPlaying) Destroy(pourLineMaterial);
                else DestroyImmediate(pourLineMaterial);
            }
        }

        private void InitializePourLinePool()
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
                unlitShader = Shader.Find("Unlit/Color");

            if (unlitShader == null)
            {
                BottleLogger.LogWarning("No suitable shader found for pour line rendering.");
                return;
            }

            pourLineMaterial = new Material(unlitShader);
            pourLinePool = new LineRenderer[Mathf.Max(1, pourLinePoolSize)];

            for (int i = 0; i < pourLinePool.Length; i++)
            {
                GameObject lineObj = new GameObject($"PourLine_{i}");
                lineObj.transform.SetParent(transform, false);

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.material = pourLineMaterial;
                lr.useWorldSpace = true;
                lr.numCapVertices = 8;
                lr.positionCount = 3;
                lr.enabled = false;

                pourLinePool[i] = lr;
            }
        }

        private void EnsureCameraPostFx()
        {
            if (mainCam == null) return;

            // Keep camera aligned for puzzle readability
            mainCam.transform.position = new Vector3(0f, 0.5f, -14f);
            mainCam.transform.LookAt(new Vector3(0f, 0.5f, 0f));
            mainCam.fieldOfView = 52f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.08f, 0.05f, 0.16f, 1f);
        }

        private void Update()
        {
            if (isAnimating || gameWon) return;

            bool pointerDown = false;
            Vector2 pointerPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerPos = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                pointerDown = true;
                pointerPos = Input.mousePosition;
            }
#endif

            if (pointerDown)
            {
                HandleInput(pointerPos);
            }
        }

        private void HandleInput(Vector2 screenPos)
        {
            if (mainCam == null)
            {
                BottleLogger.LogDebug("Main camera not found");
                return;
            }

            Ray ray = mainCam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, bottleLayerMask))
            {
                BottleController clickedBottle = hit.collider.GetComponent<BottleController>();
                if (clickedBottle == null)
                {
                    if (selectedBottle != null)
                    {
                        StartCoroutine(PutDownBottle(selectedBottle, selectedOriginalPos));
                        selectedBottle = null;
                    }
                    return;
                }

                if (selectedBottle == null)
                {
                    if (!clickedBottle.IsEmpty())
                    {
                        selectedBottle = clickedBottle;
                        selectedOriginalPos = selectedBottle.transform.position;
                        StartCoroutine(LiftBottle(selectedBottle));
                    }
                    return;
                }

                if (selectedBottle == clickedBottle)
                {
                    StartCoroutine(PutDownBottle(selectedBottle, selectedOriginalPos));
                    selectedBottle = null;
                    return;
                }

                int topLayer = selectedBottle.GetTopLayerIndex();
                if (topLayer >= 0)
                {
                    lastPourColor = selectedBottle.GetLayerColor(topLayer);

                    if (selectedBottle.TryPourTo(clickedBottle))
                    {
                        moveCount++;
                        StartCoroutine(PourAnimation(selectedBottle, clickedBottle, selectedOriginalPos));
                        selectedBottle = null;
                        return;
                    }
                }

                StartCoroutine(PutDownBottle(selectedBottle, selectedOriginalPos));
                selectedBottle = null;
            }
            else
            {
                if (selectedBottle != null)
                {
                    StartCoroutine(PutDownBottle(selectedBottle, selectedOriginalPos));
                    selectedBottle = null;
                }
            }
        }

        private IEnumerator LiftBottle(BottleController bottle)
        {
            isAnimating = true;
            Vector3 startPos = bottle.transform.position;
            Vector3 endPos = startPos + Vector3.up * liftHeight;
            float elapsed = 0f;
            float duration = animationDuration * 1.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                bottle.transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            bottle.transform.position = endPos;
            isAnimating = false;
        }

        private IEnumerator PutDownBottle(BottleController bottle, Vector3 targetPos)
        {
            isAnimating = true;
            Vector3 startPos = bottle.transform.position;
            float elapsed = 0f;
            float duration = animationDuration * 1.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                bottle.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            bottle.transform.position = targetPos;
            isAnimating = false;
        }

        private IEnumerator PourAnimation(BottleController source, BottleController target, Vector3 originalSourcePos)
        {
            isAnimating = true;
            Vector3 startPos = source.transform.position;

            Vector3 targetRim = target.transform.position + Vector3.up * 2.3f;
            float sideOffset = source.transform.position.x < target.transform.position.x ? -0.5f : 0.5f;
            Vector3 pourPos = targetRim + Vector3.right * sideOffset;

            float elapsed = 0f;
            float moveDuration = animationDuration * 0.8f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / moveDuration);
                source.transform.position = Vector3.Lerp(startPos, pourPos, t);
                yield return null;
            }

            elapsed = 0f;
            Quaternion startRot = source.transform.rotation;
            float tiltAngle = source.transform.position.x < target.transform.position.x ? -70f : 70f;
            Quaternion targetRot = Quaternion.Euler(0, 0, tiltAngle);
            float tiltDuration = animationDuration * 0.6f;
            while (elapsed < tiltDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / tiltDuration);
                source.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            Vector3 sourceMouth = source.transform.position + source.transform.up * 1.0f;
            Vector3 targetMouth = target.transform.position + target.transform.up * 2.1f;

            float pourTime = 0.55f;
            float pourElapsed = 0f;

            while (pourElapsed < pourTime)
            {
                pourElapsed += Time.deltaTime;
                float pourT = Mathf.Clamp01(pourElapsed / pourTime);

                Vector3 midPoint = Vector3.Lerp(sourceMouth, targetMouth, 0.5f) + Vector3.down * 0.25f;
                Vector3 controlPoint = Vector3.Lerp(sourceMouth, midPoint, pourT) + Vector3.down * (pourT * 0.22f);
                Vector3 streamStart = Vector3.Lerp(sourceMouth, controlPoint, pourT);
                Vector3 streamEnd = Vector3.Lerp(midPoint, targetMouth, pourT * 1.2f);
                if (streamEnd.y < targetMouth.y) streamEnd = targetMouth;

                CreatePourLine(streamStart, streamEnd, lastPourColor);
                yield return null;
            }

            yield return new WaitForSeconds(0.08f);
            DestroyPourLine();

            elapsed = 0f;
            float returnTiltDuration = animationDuration * 0.6f;
            while (elapsed < returnTiltDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / returnTiltDuration);
                source.transform.rotation = Quaternion.Slerp(targetRot, startRot, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / moveDuration);
                source.transform.position = Vector3.Lerp(pourPos, originalSourcePos, t);
                yield return null;
            }

            source.transform.position = originalSourcePos;
            source.transform.rotation = startRot;

            isAnimating = false;
            CheckWinCondition();
        }

        private void CreatePourLine(Vector3 start, Vector3 end, Color color)
        {
            if (pourLinePool == null || pourLinePool.Length == 0) return;

            LineRenderer line = pourLinePool[currentPourLineIndex];
            line.enabled = true;

            Color startColor = new Color(color.r, color.g, color.b, 0.82f);
            Color endColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 0.28f);
            line.startColor = startColor;
            line.endColor = endColor;
            line.startWidth = 0.1f;
            line.endWidth = 0.04f;

            Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.down * 0.14f;
            line.positionCount = 3;
            line.SetPosition(0, start);
            line.SetPosition(1, mid);
            line.SetPosition(2, end);

            currentPourLineIndex = (currentPourLineIndex + 1) % pourLinePool.Length;
        }

        private void DestroyPourLine()
        {
            if (pourLinePool == null) return;

            foreach (var line in pourLinePool)
            {
                if (line != null) line.enabled = false;
            }
        }

        private void CheckWinCondition()
        {
            var bottles = FindObjectsByType<BottleController>(FindObjectsInactive.Exclude);
            if (bottles == null || bottles.Length == 0) return;

            bool hasLiquid = false;
            foreach (var bottle in bottles)
            {
                if (bottle == null) continue;
                if (bottle.IsEmpty()) continue;

                hasLiquid = true;
                if (!bottle.HasSingleColorContent() || !bottle.IsFull())
                    return;
            }

            if (!hasLiquid) return;

            gameWon = true;
            BottleLogger.LogInfo($"Puzzle solved in {moveCount} moves!");
        }

        private void OnGUI()
        {
            if (!showRuntimeHud) return;

            if (hudTitleStyle == null)
            {
                hudTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 24,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = Color.white }
                };

                hudTextStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = new Color(0.9f, 0.92f, 1f, 1f) }
                };

                hudButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold
                };
            }

            Rect panel = new Rect(Screen.width * 0.5f - 170f, 16f, 340f, gameWon ? 140f : 80f);
            GUI.Box(panel, GUIContent.none);

            GUI.Label(new Rect(panel.x, panel.y + 8f, panel.width, 28f), "Puzzle Bottle Sort", hudTitleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 40f, panel.width, 24f), $"Hamle: {moveCount}", hudTextStyle);

            if (gameWon)
            {
                GUI.Label(new Rect(panel.x, panel.y + 64f, panel.width, 24f), "🎉 Tebrikler, level tamamlandı!", hudTextStyle);
                if (GUI.Button(new Rect(panel.x + 95f, panel.y + 94f, 150f, 34f), "Yeniden Başlat", hudButtonStyle))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
        }
    }
}
