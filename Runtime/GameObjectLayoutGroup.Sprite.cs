using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public partial class GameObjectLayoutGroup
{
    private void InitSpriteMode()
    {
        TryGetComponent<SpriteRenderer>(out m_spriteRenderer);
        if (m_spriteRenderer != null && m_spriteRenderer.sprite != null)
        {
            m_childSpriteRenderers = new List<SpriteRenderer>();
            m_size.x = (m_spriteRenderer.sprite.rect.size.x / m_spriteRenderer.sprite.pixelsPerUnit);
            m_size.y = (m_spriteRenderer.sprite.rect.size.y / m_spriteRenderer.sprite.pixelsPerUnit);

            m_childSpriteRenderers.Clear();
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<SpriteRenderer>(out SpriteRenderer spriteRenderer))
                {
                    m_childSpriteRenderers.Add(spriteRenderer);
                }
            }

            // 用子物体实际尺寸更新 m_cellSize（确保网格间距与子物体大小匹配）
            if (m_childSpriteRenderers.Count > 0 && (!m_controlChildSize.ControlX && !m_controlChildSize.ControlY))
            {
                var first = m_childSpriteRenderers[0];
                if (first.sprite != null)
                {
                    float w = first.sprite.rect.size.x / first.sprite.pixelsPerUnit;
                    float h = first.sprite.rect.size.y / first.sprite.pixelsPerUnit;
                    m_cellSize = new Vector2(w, h);
                    // 间距默认取子物体尺寸的 10%，避免像素默认值(10,10)在世界单位中过大
                    if (m_spacing.x > w || m_spacing.y > h)
                        m_spacing = new Vector2(w * 0.1f, h * 0.1f);
                }
            }
        }
    }

    private void SpriteLayout()
    {
        if (m_spriteRenderer == null) return;
        if (m_childSpriteRenderers.Count == 0) return;

        StopAllTweens();

        switch (m_axis)
        {
            case LayoutAxis.Horizontal:
                if (enableSector)
                    SpriteSectorHorizontal();
                else
                    SpriteHorizontal();
                break;
            case LayoutAxis.Vertical:
                if (enableSector)
                    SpriteSectorVertical();
                else
                    SpriteVertical();
                break;
        }
    }

    private void SpriteHorizontal()
    {
        float contentWidth = m_size.x - m_padding.horizontal;
        float contentHeight = m_size.y - m_padding.vertical;
        float elementWidth = m_cellSize.x + m_spacing.x;
        float elementHeight = m_cellSize.y + m_spacing.y;
        int elementsPerRow = Mathf.Max(1, Mathf.FloorToInt(contentWidth / elementWidth));
        int totalRows = Mathf.CeilToInt((float)m_childSpriteRenderers.Count / elementsPerRow);
        float startX = m_padding.left;
        float startY = -m_padding.top;

        float totalGridWidth = elementsPerRow * elementWidth - m_spacing.x;
        float totalGridHeight = totalRows * elementHeight - m_spacing.y;
        var remainingChildren = m_childSpriteRenderers
            .Where(c => c.gameObject.activeSelf || m_includeInactive)
            .ToList();
        for (int i = 0; i < remainingChildren.Count; i++)
        {
            int columnIndex = i % elementsPerRow;
            int rowIndex = i / elementsPerRow;
            float currentX = 0;
            float currentY = 0;

            switch (m_startCorner)
            {
                case Corner.UpperLeft:
                    currentX = startX + columnIndex * elementWidth + m_cellSize.x / 2;
                    currentY = startY - rowIndex * elementHeight - m_cellSize.y / 2;
                    break;
                case Corner.UpperRight:
                    currentX = startX + contentWidth - columnIndex * elementWidth - m_cellSize.x / 2;
                    currentY = startY - rowIndex * elementHeight - m_cellSize.y / 2;
                    break;
                case Corner.LowerLeft:
                    currentX = startX + columnIndex * elementWidth + m_cellSize.x / 2;
                    currentY = startY - (contentHeight - rowIndex * elementHeight - m_cellSize.y / 2);
                    break;
                case Corner.LowerRight:
                    currentX = startX + contentWidth - columnIndex * elementWidth - m_cellSize.x / 2;
                    currentY = startY - (contentHeight - rowIndex * elementHeight - m_cellSize.y / 2);
                    break;
            }

            currentX = AdjustHorizontalAlignment(currentX, m_startCorner, contentWidth, totalGridWidth);
            currentY = AdjustVerticalAlignment(currentY, m_startCorner, contentHeight, totalGridHeight);

            Vector2 localPos = new Vector2(currentX, currentY);
            Vector2 realPos = SpriteOriginPosition(localPos);
            var childObj = remainingChildren[i].gameObject;

            if (m_enableTweenAnim)
            {
                InitTweenState(childObj);
                float delay = i * m_tweenAnim.delayBetween;
                if (!m_hasStartedTween)
                {
                    m_tweenAnim.OnStartTweenAnim?.Invoke();
                    m_hasStartedTween = true;
                    remainingTweens = remainingChildren.Count;
                    m_isPlayingTweenAnim = true;
                }
                var coroutine = StartCoroutine(m_tweenAnim.PlayCoroutine(
                    childObj, realPos, m_cellSize,
                    m_controlChildSize.ControlX, m_controlChildSize.ControlY, delay
                ));
                activeTweens.Add(coroutine);
            }
            else
            {
                childObj.transform.localPosition = realPos;
                childObj.SetActive(true);
                ApplyChildSizeControl(remainingChildren[i].gameObject, false);
            }

            ApplyChildRotationControl(remainingChildren[i].gameObject);
        }
    }

    private void SpriteVertical()
    {
        float contentWidth = m_size.x - m_padding.horizontal;
        float contentHeight = m_size.y - m_padding.vertical;
        float elementWidth = m_cellSize.x + m_spacing.x;
        float elementHeight = m_cellSize.y + m_spacing.y;
        int elementsPerColumn = Mathf.Max(1, Mathf.FloorToInt(contentHeight / elementHeight));
        int totalColumns = Mathf.CeilToInt((float)m_childSpriteRenderers.Count / elementsPerColumn);
        float startX = m_padding.left;
        float startY = -m_padding.top;

        float totalGridWidth = totalColumns * elementWidth - m_spacing.x;
        float totalGridHeight = elementsPerColumn * elementHeight - m_spacing.y;
        var remainingChildren = m_childSpriteRenderers
            .Where(c => c.gameObject.activeSelf || m_includeInactive)
            .ToList();
        for (int i = 0; i < remainingChildren.Count; i++)
        {
            int indexInColumn = i % elementsPerColumn;
            int columnIndex = i / elementsPerColumn;
            float currentX = 0;
            float currentY = 0;

            switch (m_startCorner)
            {
                case Corner.UpperLeft:
                    currentX = startX + columnIndex * elementWidth + m_cellSize.x / 2;
                    currentY = startY - indexInColumn * elementHeight - m_cellSize.y / 2;
                    break;
                case Corner.UpperRight:
                    currentX = startX + contentWidth - columnIndex * elementWidth - m_cellSize.x / 2;
                    currentY = startY - indexInColumn * elementHeight - m_cellSize.y / 2;
                    break;
                case Corner.LowerLeft:
                    currentX = startX + columnIndex * elementWidth + m_cellSize.x / 2;
                    currentY = startY - (contentHeight - indexInColumn * elementHeight - m_cellSize.y / 2);
                    break;
                case Corner.LowerRight:
                    currentX = startX + contentWidth - columnIndex * elementWidth - m_cellSize.x / 2;
                    currentY = startY - (contentHeight - indexInColumn * elementHeight - m_cellSize.y / 2);
                    break;
            }

            currentX = AdjustHorizontalAlignment(currentX, m_startCorner, contentWidth, totalGridWidth);
            currentY = AdjustVerticalAlignment(currentY, m_startCorner, contentHeight, totalGridHeight);

            Vector2 localPos = new Vector2(currentX, currentY);
            Vector2 realPos = SpriteOriginPosition(localPos);
            var childObj = remainingChildren[i].gameObject;

            if (m_enableTweenAnim)
            {
                InitTweenState(childObj);
                float delay = i * m_tweenAnim.delayBetween;
                if (!m_hasStartedTween)
                {
                    m_tweenAnim.OnStartTweenAnim?.Invoke();
                    m_hasStartedTween = true;
                    remainingTweens = remainingChildren.Count;
                    m_isPlayingTweenAnim = true;
                }
                var coroutine = StartCoroutine(m_tweenAnim.PlayCoroutine(
                    childObj, realPos, m_cellSize,
                    m_controlChildSize.ControlX, m_controlChildSize.ControlY, delay
                ));
                activeTweens.Add(coroutine);
            }
            else
            {
                childObj.transform.localPosition = realPos;
                childObj.SetActive(true);
                ApplyChildSizeControl(remainingChildren[i].gameObject, false);
            }

            ApplyChildRotationControl(remainingChildren[i].gameObject);
        }
    }

    private void SpriteSectorVertical()
    {
        if (m_childSpriteRenderers.Count == 0 || !enableSector) return;

        float contentWidth = m_size.x - m_padding.horizontal;
        float contentHeight = m_size.y - m_padding.vertical;
        Vector2 center = CalculateSectorCenter(contentWidth, contentHeight);
        float totalAngle = m_sector.angle;
        float startAngle = -totalAngle / 2;
        float angleStep = m_childSpriteRenderers.Count > 1 ? totalAngle / (m_childSpriteRenderers.Count - 1) : 0;
        var remainingChildren = m_childSpriteRenderers
            .Where(c => c.gameObject.activeSelf || m_includeInactive)
            .ToList();
        for (int i = 0; i < remainingChildren.Count; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            float radian = currentAngle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radian) * m_sector.radius;
            float y = Mathf.Sin(radian) * m_sector.radius + i * m_spacing.y;
            Vector2 localPos = new Vector2(center.x + x, center.y + y);
            Vector2 realPos = SpriteOriginPosition(localPos);
            var childObj = remainingChildren[i].gameObject;

            if (m_enableTweenAnim)
            {
                InitTweenState(childObj);
                float delay = i * m_tweenAnim.delayBetween;
                if (!m_hasStartedTween)
                {
                    m_tweenAnim.OnStartTweenAnim?.Invoke();
                    m_hasStartedTween = true;
                    remainingTweens = remainingChildren.Count;
                    m_isPlayingTweenAnim = true;
                }
                var coroutine = StartCoroutine(
                    m_tweenAnim.PlayCoroutine(childObj, realPos, m_cellSize,
                        m_controlChildSize.ControlX, m_controlChildSize.ControlY, delay, currentAngle)
                );
                activeTweens.Add(coroutine);
            }
            else
            {
                childObj.transform.localPosition = realPos;
                childObj.SetActive(true);
                ApplyChildSizeControl(remainingChildren[i].gameObject, false);
                remainingChildren[i].transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }
        }
    }

    private void SpriteSectorHorizontal()
    {
        if (m_childSpriteRenderers.Count == 0 || !enableSector) return;

        float contentWidth = m_size.x - m_padding.horizontal;
        float contentHeight = m_size.y - m_padding.vertical;
        Vector2 center = CalculateSectorCenter(contentWidth, contentHeight);
        float totalAngle = m_sector.angle;
        float startAngle = 90 - totalAngle / 2;
        float angleStep = m_childSpriteRenderers.Count > 1 ? totalAngle / (m_childSpriteRenderers.Count - 1) : 0;
        var remainingChildren = m_childSpriteRenderers
            .Where(c => c.gameObject.activeSelf || m_includeInactive)
            .ToList();
        for (int i = 0; i < remainingChildren.Count; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            float radian = currentAngle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radian) * m_sector.radius + i * m_spacing.x;
            float y = Mathf.Sin(radian) * m_sector.radius;
            Vector2 localPos = new Vector2(center.x + x, center.y + y);
            Vector2 realPos = SpriteOriginPosition(localPos);
            var childObj = remainingChildren[i].gameObject;

            if (m_enableTweenAnim)
            {
                InitTweenState(childObj);
                float delay = i * m_tweenAnim.delayBetween;
                if (!m_hasStartedTween)
                {
                    m_tweenAnim.OnStartTweenAnim?.Invoke();
                    m_hasStartedTween = true;
                    remainingTweens = remainingChildren.Count;
                    m_isPlayingTweenAnim = true;
                }
                var coroutine = StartCoroutine(
                    m_tweenAnim.PlayCoroutine(childObj, realPos, m_cellSize,
                        m_controlChildSize.ControlX, m_controlChildSize.ControlY, delay, currentAngle - 90)
                );
                activeTweens.Add(coroutine);
            }
            else
            {
                childObj.transform.localPosition = realPos;
                childObj.SetActive(true);
                ApplyChildSizeControl(remainingChildren[i].gameObject, false);
                remainingChildren[i].transform.localRotation = Quaternion.Euler(0, 0, currentAngle - 90);
            }
        }
    }

    private Vector2 SpriteOriginPosition(Vector2 pos)
    {
        float x = m_size.x / 2;
        float y = m_size.y / 2;
        return new Vector2(pos.x - x, pos.y + y);
    }

    private void ApplyChildSizeControl(GameObject child, bool isTween)
    {
        if (isTween) return;

        Vector2 targetScale = child.transform.localScale;
        if (m_controlChildSize.ControlX)
            targetScale.x = m_cellSize.x;
        if (m_controlChildSize.ControlY)
            targetScale.y = m_cellSize.y;
        child.transform.localScale = targetScale;
    }
}
