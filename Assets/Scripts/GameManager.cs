using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using BottleShaders.Logging;

namespace BottleShaders
{
    public class GameManager : MonoBehaviour
    {
        private BottleController selectedBottle;
        private Vector3 selectedOriginalPos;
        
        public float liftHeight = 1.0f;
        public float animationDuration = 0.4f;
        
        private bool isAnimating = false;

        public Material liquidLineMaterial;
        private LineRenderer currentPourLine;
        private Color lastPourColor;
        private Camera mainCam;

        void Start()
        {
            mainCam = Camera.main;
            if (mainCam == null) mainCam = FindObjectOfType<Camera>();
        }

        void Update()
        {
            if (isAnimating) return;

            bool pointerDown = false;
            Vector2 pointerPos = Vector2.zero;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerPos = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
            {
                var touch = Touchscreen.current.touches[0];
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    pointerDown = true;
                    pointerPos = touch.position.ReadValue();
                }
            }

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
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                BottleController clickedBottle = hit.collider.GetComponent<BottleController>();
                if (clickedBottle != null)
                {
                    BottleLogger.LogDebug($"Clicked on bottle: {clickedBottle.gameObject.name}");
                    
                    if (selectedBottle == null)
                    {
                        if (!clickedBottle.IsEmpty())
                        {
                            BottleLogger.LogDebug("Selecting bottle for pouring");
                            selectedBottle = clickedBottle;
                            selectedOriginalPos = selectedBottle.transform.position;
                            StartCoroutine(LiftBottle(selectedBottle));
                        }
                        else
                        {
                            BottleLogger.LogDebug("Cannot select empty bottle");
                        }
                    }
                    else
                    {
                        if (selectedBottle == clickedBottle)
                        {
                            BottleLogger.LogDebug("Deselecting same bottle");
                            StartCoroutine(PutDownBottle(selectedBottle, selectedOriginalPos));
                            selectedBottle = null;
                        }
                        else
                        {
                            int topLayer = selectedBottle.GetTopLayerIndex();
                            if (topLayer >= 0) 
                            {
                                lastPourColor = selectedBottle.GetLayerColor(topLayer);
                                BottleLogger.LogDebug($"Attempting to pour from {selectedBottle.gameObject.name} to {clickedBottle.gameObject.name}");
                                
                                if (selectedBottle.TryPourTo(clickedBottle))
                                {
                                    BottleLogger.LogDebug("Pour successful, starting animation");
                                    StartCoroutine(PourAnimation(selectedBottle, clickedBottle, selectedOriginalPos));
                                    selectedBottle = null;
                                    return;
                                }
                                else
                                {
                                    BottleLogger.LogDebug("Pour failed, putting down bottle");
                                }
                            }
                            
                            StartCoroutine(PutDownBottle(selectedBottle, selectedOriginalPos));
                            selectedBottle = null;
                        }
                    }
                }
                else
                {
                    BottleLogger.LogDebug("No bottle found at click position");
                }
            }
            else
            {
                if (selectedBottle != null)
                {
                    BottleLogger.LogDebug("No valid click target, deselecting current bottle");
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
            float liftDuration = animationDuration * 1.5f; // Slower lifting motion
            while (elapsed < liftDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / liftDuration);
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
            float dropDuration = animationDuration * 1.5f; // Slower dropping motion
            while (elapsed < dropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / dropDuration);
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
            
            // Calculate positions for pour animation
            Vector3 targetRim = target.transform.position + Vector3.up * 2.3f; 
            float sideOffset = source.transform.position.x < target.transform.position.x ? -0.5f : 0.5f; // Reduced distance
            Vector3 pourPos = targetRim + Vector3.right * sideOffset;
            
            // 1. Move to pour position (more fluid movement)
            float elapsed = 0f;
            float moveDuration = animationDuration * 0.8f; // Faster movement to position
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / moveDuration);
                source.transform.position = Vector3.Lerp(startPos, pourPos, t);
                yield return null;
            }
 
            // 2. Rotate to tilt with more realistic arc
            elapsed = 0f;
            Quaternion startRot = source.transform.rotation;
            float tiltAngle = source.transform.position.x < target.transform.position.x ? -70f : 70f; // Less dramatic tilt
            Quaternion targetRot = Quaternion.Euler(0, 0, tiltAngle);
            float tiltDuration = animationDuration * 0.6f; // Faster tilt
            while (elapsed < tiltDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / tiltDuration);
                source.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
 
            // 3. Pour animation with more realistic timing
            Vector3 sourceMouth = source.transform.position + source.transform.up * 1.0f; 
            Vector3 targetMouth = target.transform.position + target.transform.up * 2.1f;
            
            // Create curved pour effect with multiple points
            float pourTime = 0.6f; // Faster pour
            float pourStartTime = Time.time;
            float pourEndTime = pourStartTime + pourTime;
            
            while (Time.time < pourEndTime)
            {
                float pourT = (Time.time - pourStartTime) / pourTime;
                
                // Calculate curved path for pour
                Vector3 midPoint = Vector3.Lerp(sourceMouth, targetMouth, 0.5f) + Vector3.down * 0.3f; // Less curve
                Vector3 pourEndPos = Vector3.Lerp(midPoint, targetMouth, pourT * 1.2f); // Direct path to target
                
                // Ensure pourEndPos doesn't go below target
                if (pourEndPos.y < targetMouth.y) pourEndPos = targetMouth;
                
                // Create arc for pour line
                Vector3 controlPoint = Vector3.Lerp(sourceMouth, midPoint, pourT) + Vector3.down * (pourT * 0.2f);
                Vector3 pourStartPos = Vector3.Lerp(sourceMouth, controlPoint, pourT);
                
                CreatePourLine(pourStartPos, pourEndPos, lastPourColor);
                
                yield return null;
            }
            
            // Brief pause before returning
            yield return new WaitForSeconds(0.1f); // Shorter pause
            DestroyPourLine();
 
            // 4. Rotate back to original rotation
            elapsed = 0f;
            float returnTiltDuration = animationDuration * 0.6f; // Faster return
            while (elapsed < returnTiltDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / returnTiltDuration);
                source.transform.rotation = Quaternion.Slerp(targetRot, startRot, t);
                yield return null;
            }
 
            // 5. Move back to original position
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
        }

        private void CreatePourLine(Vector3 start, Vector3 end, Color color)
        {
            GameObject lineObj = new GameObject("PourLine");
            currentPourLine = lineObj.AddComponent<LineRenderer>();
            Shader unlitColorShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitColorShader != null)
            {
                Material mat = new Material(unlitColorShader);
                mat.SetColor("_BaseColor", color);
                currentPourLine.material = mat;
            }
            // Make the pour line more visually appealing with gradient and transparency
            Color startColor = new Color(color.r, color.g, color.b, 0.8f);
            Color endColor = new Color(color.r * 0.8f, color.g * 0.8f, color.b * 0.8f, 0.3f);
            currentPourLine.startColor = startColor;
            currentPourLine.endColor = endColor;
            currentPourLine.startWidth = 0.1f;
            currentPourLine.endWidth = 0.05f;
            currentPourLine.positionCount = 2;
            currentPourLine.SetPosition(0, start);
            currentPourLine.SetPosition(1, end);
            currentPourLine.numCapVertices = 8;
            currentPourLine.useWorldSpace = true;
        }

        private void DestroyPourLine()
        {
            if (currentPourLine != null) { Destroy(currentPourLine.gameObject); currentPourLine = null; }
        }
    }
}