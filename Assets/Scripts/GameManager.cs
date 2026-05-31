using System.Collections;
using BottleShaders.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private Text moveCountText;
        [SerializeField] private Text titleText;
        [SerializeField] private GameObject winPanel;
        [SerializeField] private Button restartButton;

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

        private void Start()
        {
            mainCam = Camera.main;
            if (mainCam == null) mainCam = FindAnyObjectByType<Camera>();
            InitializePourLinePool();
            InitializeHUD();
            EnsureCameraPostFx();
        }

        private void InitializeHUD()
        {
            if (!showRuntimeHud) return;

            // Create Canvas if not assigned
            if (hudCanvas == null)
            {
                GameObject canvasObj = new GameObject("HUDCanvas");
                hudCanvas = canvasObj.AddComponent<Canvas>();
                hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                hudCanvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create title text
            if (titleText == null)
            {
                GameObject titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(hudCanvas.transform, false);
                titleText = titleObj.AddComponent<Text>();
                titleText.text = "Puzzle Bottle Sort";
                titleText.fontSize = 28;
                titleText.fontStyle = FontStyle.Bold;
                titleText.alignment = TextAnchor.UpperCenter;
                titleText.color = Color.white;
                RectTransform titleRect = titleText.rectTransform;
                titleRect.anchorMin = new Vector2(0.5f, 1f);
                titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0, -20);
                titleRect.sizeDelta = new Vector2(400, 50);
            }

            // Create move count text
            if (moveCountText == null)
            {
                GameObject moveObj = new GameObject("MoveCountText");
                moveObj.transform.SetParent(hudCanvas.transform, false);
                moveCountText = moveObj.AddComponent<Text>();
                moveCountText.text = "Hamle: 0";
                moveCountText.fontSize = 20;
                moveCountText.alignment = TextAnchor.UpperCenter;
                moveCountText.color = new Color(0.9f, 0.92f, 1f);
                RectTransform moveRect = moveCountText.rectTransform;
                moveRect.anchorMin = new Vector2(0.5f, 1f);
                moveRect.anchorMax = new Vector2(0.5f, 1f);
                moveRect.pivot = new Vector2(0.5f, 1f);
                moveRect.anchoredPosition = new Vector2(0, -70);
                moveRect.sizeDelta = new Vector2(400, 40);
            }

            // Create win panel
            if (winPanel == null)
            {
                GameObject panelObj = new GameObject("WinPanel");
                panelObj.transform.SetParent(hudCanvas.transform, false);
                winPanel = panelObj;
                RectTransform panelRect = winPanel.AddComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelRect.sizeDelta = new Vector2(350, 180);

                // Add background image
                Image panelBg = winPanel.AddComponent<Image>();
                panelBg.color = new Color(0.1f, 0.08f, 0.2f, 0.95f);

                // Add outline
                Outline panelOutline = winPanel.AddComponent<Outline>();
                panelOutline.effectColor = new Color(0.5f, 0.4f, 0.8f);
                panelOutline.effectDistance = new Vector2(2, 2);

                // Create win text
                GameObject winTextObj = new GameObject("WinText");
                winTextObj.transform.SetParent(winPanel.transform, false);
                Text winText = winTextObj.AddComponent<Text>();
                winText.text = "🎉 Tebrikler!\nLevel tamamlandı!";
                winText.fontSize = 24;
                winText.fontStyle = FontStyle.Bold;
                winText.alignment = TextAnchor.MiddleCenter;
                winText.color = Color.white;
                RectTransform winTextRect = winText.rectTransform;
                winTextRect.anchorMin = Vector2.zero;
                winTextRect.anchorMax = Vector2.one;
                winTextRect.sizeDelta = new Vector2(-20, -80);
                winTextRect.anchoredPosition = Vector2.zero;

                // Create restart button
                GameObject buttonObj = new GameObject("RestartButton");
                buttonObj.transform.SetParent(winPanel.transform, false);
                restartButton = buttonObj.AddComponent<Button>();
                Image buttonImg = buttonObj.AddComponent<Image>();
                buttonImg.color = new Color(0.3f, 0.5f, 0.8f);
                restartButton.targetGraphic = buttonImg;

                RectTransform buttonRect = restartButton.transform.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0f);
                buttonRect.anchorMax = new Vector2(0.5f, 0f);
                buttonRect.pivot = new Vector2(0.5f, 0f);
                buttonRect.anchoredPosition = new Vector2(0, 25);
                buttonRect.sizeDelta = new Vector2(160, 40);

                // Button text
                GameObject buttonTextObj = new GameObject("ButtonText");
                buttonTextObj.transform.SetParent(buttonObj.transform, false);
                Text buttonTxt = buttonTextObj.AddComponent<Text>();
                buttonTxt.text = "Yeniden Başlat";
                buttonTxt.fontSize = 16;
                buttonTxt.fontStyle = FontStyle.Bold;
                buttonTxt.alignment = TextAnchor.MiddleCenter;
                buttonTxt.color = Color.white;
                RectTransform buttonTxtRect = buttonTxt.rectTransform;
                buttonTxtRect.anchorMin = Vector2.zero;
                buttonTxtRect.anchorMax = Vector2.one;
                buttonTxtRect.sizeDelta = Vector2.zero;

                // Add click listener
                restartButton.onClick.AddListener(() =>
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                });

                winPanel.SetActive(false);
            }
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
                pourLinePool = null;
            }

            if (pourLineMaterial != null)
            {
                if (Application.isPlaying) Destroy(pourLineMaterial);
                else DestroyImmediate(pourLineMaterial);
                pourLineMaterial = null;
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
                        UpdateHUD();
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
            UpdateHUD();
        }

        private void UpdateHUD()
        {
            if (!showRuntimeHud) return;

            if (moveCountText != null)
            {
                moveCountText.text = $"Hamle: {moveCount}";
            }

            if (gameWon && winPanel != null)
            {
                winPanel.SetActive(true);
            }
        }
    }
}
