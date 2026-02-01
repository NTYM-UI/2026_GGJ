using UnityEngine;
using UnityEngine.UI;

namespace ChatSystem
{
    public class ProfilePopupUI : MonoBehaviour
    {
        [SerializeField] private Image profileImageDisplay;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject panelRoot; // The root object to toggle visibility
        [SerializeField] private RectTransform contentPanel; // The actual UI panel to position

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => 
                {
                    Hide();
                });
            }
            // Ensure it's hidden on start
            Hide();
        }

        private void Update()
        {
            // Detect click outside to close
            if (panelRoot != null && panelRoot.activeSelf)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    // If we have a content panel, check if the click is inside it
                    if (contentPanel != null)
                    {
                        // Note: For Screen Space - Overlay, camera is null. 
                        // For Screen Space - Camera, we might need the UI camera.
                        // Assuming Overlay or standard setup where null works or Camera.main works.
                        if (!RectTransformUtility.RectangleContainsScreenPoint(contentPanel, Input.mousePosition, null))
                        {
                            Hide();
                        }
                    }
                }
            }
        }

        public void Show(Sprite sprite, RectTransform targetAvatar)
        {
            if (sprite == null) return;
            
            // Core.AudioManager.Instance?.PlayPopupSound();
            
            if (profileImageDisplay != null)
            {
                profileImageDisplay.sprite = sprite;
                // Preserve aspect ratio if needed
                profileImageDisplay.preserveAspect = true;
            }
            
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            // Position next to the avatar
            if (targetAvatar != null && contentPanel != null)
            {
                PositionNextTo(targetAvatar);
            }
        }

        private void PositionNextTo(RectTransform target)
        {
            // 1. Get target position in World Space
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            // 0=BottomLeft, 1=TopLeft, 2=TopRight, 3=BottomRight
            
            // Calculate a position to the right of the avatar
            Vector3 targetRightCenter = (corners[2] + corners[3]) / 2f;
            
            // 2. Set Popup Pivot to (0, 0.5) so it aligns to its left edge
            contentPanel.pivot = new Vector2(0, 0.5f);
            
            // 3. Set Position
            // Since UI elements are in World Space (unless different canvases), 
            // we can try setting position directly.
            contentPanel.position = targetRightCenter;
            
            // 4. Add some padding
            float padding = 20f;
            // Need to account for Canvas scale factor if adding pixel offset to world position,
            // but simpler to just add to anchoredPosition if they were in same space.
            // Since we set world position, we can just shift it slightly in world space.
            // However, 20 units in World Space might be huge if Canvas is Scaled.
            
            // Better approach: Convert World Point to Local Point in Parent
            if (contentPanel.parent is RectTransform parentRect)
            {
                Vector2 localPos;
                // Convert World Position of target edge to Local Position in Popup's Parent
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, 
                    RectTransformUtility.WorldToScreenPoint(null, targetRightCenter), 
                    null, 
                    out localPos
                );
                
                contentPanel.anchoredPosition = localPos + new Vector2(padding, 0);
                
                // Ensure it stays on screen
                KeepInScreen();
            }
        }

        private void KeepInScreen()
        {
            // Find the root Canvas to compare bounds against
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null) return;
            RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();

            // Update layout first to get correct size
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);

            Vector3[] panelCorners = new Vector3[4];
            contentPanel.GetWorldCorners(panelCorners);
            // 0=BL, 1=TL, 2=TR, 3=BR

            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
            // Canvas corners might be rotated if camera is rotated, but usually aligned.
            // Assuming AABB for simplicity in standard UI.
            
            float canvasTop = canvasCorners[1].y;
            float canvasBottom = canvasCorners[0].y;
            float canvasRight = canvasCorners[2].x;
            float canvasLeft = canvasCorners[0].x;

            float panelTop = panelCorners[1].y;
            float panelBottom = panelCorners[0].y;
            float panelRight = panelCorners[2].x;
            float panelLeft = panelCorners[0].x;

            float shiftY = 0;
            float shiftX = 0;
            
            // Use a small padding to avoid sticking exactly to the edge
            // In Overlay mode this is pixels, in Camera mode this is world units.
            // We'll estimate a safe padding based on the panel's own height to be relative.
            float padding = (panelTop - panelBottom) * 0.05f; 

            // Vertical constraint
            if (panelTop > canvasTop)
            {
                shiftY = (canvasTop - padding) - panelTop;
            }
            else if (panelBottom < canvasBottom)
            {
                shiftY = (canvasBottom + padding) - panelBottom;
            }

            // Horizontal constraint
            if (panelRight > canvasRight)
            {
                shiftX = (canvasRight - padding) - panelRight;
            }
            else if (panelLeft < canvasLeft)
            {
                shiftX = (canvasLeft + padding) - panelLeft;
            }

            if (shiftX != 0 || shiftY != 0)
            {
                contentPanel.position += new Vector3(shiftX, shiftY, 0);
            }
        }

        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
