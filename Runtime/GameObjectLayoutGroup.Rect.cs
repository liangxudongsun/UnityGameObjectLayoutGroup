using System.Linq;
using DG.Tweening;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public partial class GameObjectLayoutGroup
{
    private void InitRectMode()
    {
        TryGetComponent<RectTransform>(out m_contentRect);
        if (m_contentRect == null) return;
        m_size = m_contentRect.rect.size;
        m_childRects.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            var childRect = transform.GetChild(i).GetComponent<RectTransform>();
            if (childRect != null)
            {
                childRect.anchorMax = new Vector2(0, 1);
                childRect.anchorMin = new Vector2(0, 1);
                childRect.pivot = new Vector2(0.5f, 0.5f);
                m_childRects.Add(childRect);
            }
        }

        // 用子物体实际尺寸更新 m_cellSize（网格间距与子物体大小匹配）
        if (m_childRects.Count > 0 && (!m_controlChildSize.ControlX && !m_controlChildSize.ControlY))
        {
            m_cellSize = m_childRects[0].rect.size;
        }
    }

    private void RectLayout()
    {
        if (m_contentRect == null) return;
        if (m_childRects.Count == 0) return;

        StopAllTweens();

        switch (m_axis)
        {
            case LayoutAxis.Horizontal:
                if (enableSector)
                    RectSectorHorizontal();
                else
                    RectHorizontal();
                break;
            case LayoutAxis.Vertical:
                if (enableSector)
                    RectSectorVertical();
                else
                    RectVertical();
                break;
        }
        ResetRectScale();
    }

    private void RectHorizontal()
    {
        float contentWidth = m_size.x - m_padding.horizontal;
        float contentHeight = m_size.y - m_padding.vertical;
        float elementWidth = m_cellSize.x + m_spacing.x;
        float elementHeight = m_cellSize.y + m_spacing.y;
        int elementsPerRow = Mathf.Max(1, Mathf.FloorToInt(contentWidth / elementWidth));
        int totalRows = Mathf.CeilToInt((float)m_childRects.Count / elementsPerRow);
        float startX = m_padding.left;
        float startY = -m_padding.top;

        float totalGridWidth = elementsPerRow * elementWidth - m_spacing.x;
        float totalGridHeight = totalRows * elementHeight - m_spacing.y;
        var remainingChildren = m_childRects
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

            var childRect = remainingChildren[i];

            var sc = transform.localScale;
            if (m_enableTweenAnim)
            {
                InitTweenState(childRect.gameObject);
                float delay = i * m_tweenAnim.delayBetween;
                if (!m_hasStartedTween)
                {
                    m_tweenAnim.OnStartTweenAnim?.Invoke();
                    m_hasStartedTween = true;
                    remainingTweens = remainingChildren.Count;
                    m_isPlayingTweenAnim = true;
                }
                var coroutine = StartCoroutine(m_tweenAnim.PlayCoroutine(
                    childRect.gameObject, new Vector3(currentX, currentY, 0), sc,
                    m_controlChildSize.ControlX, m_controlChildSize.ControlY, delay
                ));
                activeTweens.Add(coroutine);
            }
            else
            {
                childRect.anchoredPosition = new Vector2(currentX, currentY);
                childRect.gameObject.SetActive(true);
                ApplyChildSizeControl(childRect, false);
            }
            ApplyChildRotationControl(childRect.gameObject);
        }
    }

    private void RectVertical()
    {
        float contentWidth = m_size.x - m_padding.horizontal;
        float contentHeight = m_size.y - m_padding.vertical;
        float elementWidth = m_cellSize.x + m_spacing.x;
        float elementHeight = m_cellSize.y + m_spacing.y;
        int elementsPerColumn = Mathf.Max(1, Mathf.FloorToInt(contentHeight / elementHeight));
        int totalColumns = Mathf.CeilToInt((float)m_childRects.Count / elementsPerColumn);
        float startX = m_padding.left;
        float startY = -m_padding.top;

        float totalGridWidth = totalColumns * elementWidth - m_spacing.x;
        float totalGridHeight = elementsPerColumn * elementHeight - m_spacing.y;
        var remainingChildren = m_childRects
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

            var childRect = remainingChildren[i];
            var sc = transform.localScale;
            if (m_enableTweenAnim)
            {
                InitTweenState(childRect.gameObject);
                float delay = i * m_tweenAnim.delayBetween;
                if (!m_hasStartedTween)
                {
                    m_tweenAnim.OnStartTweenAnim?.Invoke();
                    m_hasStartedTween = true;
                    remainingTweens = remainingChildren.Count;
                    m_isPlayingTweenAnim = true;
                }
                var coroutine = StartCoroutine(m_tweenAnim.PlayCoroutine(
                    childRect.gameObject, new Vector3(currentX, currentY, 0), sc,
                    m_controlChildSize.ControlX, m_controlChildSize.ControlY, delay
                ));
                activeTweens.Add(coroutine);
            }
            else
            {
                childRect.anchoredPosition = new Vector2(currentX, currentY);
                childRect.gameObject.SetActive(true);
                ApplyChildSizeControl(childRect, false);
            }

            ApplyChildRotationControl(childRect.gameObject);
        }
    }

    private void RectSectorHorizontal()
    {
        if (m_childRects.Count == 0 || !enableSector) return;

        float contentWidth = m_size.x - m_padding.horizontal;
        float contentHeight = m_size.y - m_padding.vertical;
        Vector2 center = CalculateSectorCenter(contentWidth, contentHeight);
        float totalAngle = m_sector.angle;
        float startAngle = 90 - totalAngle / 2;
        float angleStep = m_childRects.Count > 1 ? totalAngle / (m_childRects.Count - 1) : 0;
        var remainingChildren = m_childRects
            .Where(c => c.gameObject.activeSelf || m_includeInactive)
            .ToList();
        for (int i = 0; i < remainingChildren.Count; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            float radian = currentAngle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radian) * m_sector.radius + i * m_spacing.x;
            float y = Mathf.Sin(radian) * m_sector.radius;
            Vector2 localPos = new Vector2(center.x + x, center.y + y);

            var childRect = remainingChildren[i];
            var sc = transform.localScale;
            if (m_enableTweenAnim)
            {
                InitTweenState(childRect.gameObject);
                float delay = i * m_tweenAnim.delayBetween;
                if (!m_hasStartedTween)
                {
                    m_tweenAnim.OnStartTweenAnim?.Invoke();
                    m_hasStartedTween = true;
                    remainingTweens = remainingChildren.Count;
                    m_isPlayingTweenAnim = true;
                }
                var coroutine = StartCoroutine(m_tweenAnim.PlayCoroutine(
                    childRect.gameObject, new Vector3(localPos.x, localPos.y, 0), sc,
                    m_controlChildSize.ControlX, m_controlChildSize.ControlY, delay, currentAngle - 90
                ));
                activeTweens.Add(coroutine);
            }
            else
            {
                childRect.anchoredPosition = localPos;
                childRect.gameObject.SetActive(true);
                ApplyChildSizeControl(childRect, false);
                childRect.localRotation = Quaternion.Euler(0, 0, currentAngle - 90);
            }
        }
    }

    private void RectSectorVertical()
    {
        if (m_childRects.Count == 0 || !enableSector) return;

        float contentWidth = m_size.x - m_padding.horizontal;
        float contentHeight = m_size.y - m_padding.vertical;
        Vector2 center = CalculateSectorCenter(contentWidth, contentHeight);
        float totalAngle = m_sector.angle;
        float startAngle = -totalAngle / 2;
        float angleStep = m_childRects.Count > 1 ? totalAngle / (m_childRects.Count - 1) : 0;
        var remainingChildren = m_childRects
            .Where(c => c.gameObject.activeSelf || m_includeInactive)
            .ToList();
        for (int i = 0; i < remainingChildren.Count; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            float radian = currentAngle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radian) * m_sector.radius;
            float y = Mathf.Sin(radian) * m_sector.radius + i * m_spacing.y;
            Vector2 localPos = new Vector2(center.x + x, center.y + y);

            var childRect = remainingChildren[i];
            var sc = transform.localScale;
            if (m_enableTweenAnim)
            {
                InitTweenState(childRect.gameObject);
                float delay = i * m_tweenAnim.delayBetween;
                if (!m_hasStartedTween)
                {
                    m_tweenAnim.OnStartTweenAnim?.Invoke();
                    m_hasStartedTween = true;
                    remainingTweens = remainingChildren.Count;
                    m_isPlayingTweenAnim = true;
                }
                var coroutine = StartCoroutine(m_tweenAnim.PlayCoroutine(
                    childRect.gameObject, new Vector3(localPos.x, localPos.y, 0), sc,
                    m_controlChildSize.ControlX, m_controlChildSize.ControlY, delay, currentAngle
                ));
                activeTweens.Add(coroutine);
            }
            else
            {
                childRect.anchoredPosition = localPos;
                childRect.gameObject.SetActive(true);
                ApplyChildSizeControl(childRect, false);
                childRect.localRotation = Quaternion.Euler(0, 0, currentAngle);
            }
        }
    }

    private void ApplyChildSizeControl(RectTransform child, bool isTween)
    {
        if (isTween) return;

        Vector2 targetSize = child.sizeDelta;
        if (m_controlChildSize.ControlX)
            targetSize.x = m_cellSize.x;
        if (m_controlChildSize.ControlY)
            targetSize.y = m_cellSize.y;
        child.sizeDelta = targetSize;
    }
}
