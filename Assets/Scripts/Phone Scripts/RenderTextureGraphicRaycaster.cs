using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Maps window coordinates through the image that displays this canvas's camera.
public class RenderTextureGraphicRaycaster : GraphicRaycaster
{
    [SerializeField] private RawImage displayImage;

    public override void Raycast(PointerEventData eventData, List<RaycastResult> results)
    {
        Camera camera = eventCamera;
        if (camera == null || camera.targetTexture == null)
        {
            base.Raycast(eventData, results);
            return;
        }

        if (displayImage == null || !displayImage.isActiveAndEnabled ||
            displayImage.texture != camera.targetTexture || displayImage.canvas == null)
            return;

        Canvas displayCanvas = displayImage.canvas.rootCanvas;
        Camera displayCamera = displayCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : displayCanvas.worldCamera;
        RectTransform displayRect = displayImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                displayRect, eventData.position, displayCamera, out Vector2 localPoint))
            return;

        Rect rect = displayRect.rect;
        if (rect.width <= 0f || rect.height <= 0f || !rect.Contains(localPoint))
            return;

        Vector2 normalized = Rect.PointToNormalized(rect, localPoint);
        Rect uv = displayImage.uvRect;
        Vector2 texturePoint = new Vector2(
            (uv.x + normalized.x * uv.width) * camera.targetTexture.width,
            (uv.y + normalized.y * uv.height) * camera.targetTexture.height);

        // The EventSystem shares this object with other raycasters and handlers.
        // Only the phone's hit test should see texture-space coordinates.
        Vector2 screenPoint = eventData.position;
        int firstResult = results.Count;
        try
        {
            eventData.position = texturePoint;
            base.Raycast(eventData, results);
        }
        finally
        {
            eventData.position = screenPoint;
        }

        for (int i = firstResult; i < results.Count; i++)
        {
            RaycastResult result = results[i];
            result.screenPosition = screenPoint;
            results[i] = result;
        }
    }
}
