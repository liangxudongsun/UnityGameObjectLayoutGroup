using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public partial class GameObjectLayoutGroup
{
    [System.Serializable]
    public class TweenAnim
    {
        [Header("Movement")]
        public Ease tweenEase = Ease.OutQuad;
        public Vector2 startPoint;
        public float duration = 0.3f;
        public float delayBetween = 0.2f;

        [Header("Scale")]
        public Vector3 startScale = new Vector2(0.1f, 0.1f);

        [Header("Rotation")]
        [Tooltip("起始Z轴旋转角度（如 -15° 让物体倾斜飞出）")]
        public float startAngle = -10f;
        [Tooltip("旋转动画曲线 — OutBack=轻微回弹过冲，最自然")]
        public Ease rotateEase = Ease.OutBack;

        [Header("Path Arc")]
        [Tooltip("路径弧度高度（>0向上拱，<0向下沉），让运动轨迹呈抛物线")]
        public float pathArcHeight = 30f;

        [Header("Callbacks")]
        public UnityAction OnStartTweenAnim;
        public UnityAction OnPlayTweenAnim;
        public UnityAction OnEndTweenAnim;

        public IEnumerator PlayCoroutine(GameObject obj, Vector3 endPos, Vector3 targetSize, bool controlX, bool controlY, float delay, float targetEndAngle = 0f)
        {
            obj.transform.parent.TryGetComponent<GameObjectLayoutGroup>(out var layoutGroup);

            yield return new WaitForSeconds(delay);

            obj.SetActive(true);

            DG.Tweening.Sequence sequence = DOTween.Sequence();

            // === 1. 位置动画（支持弧形路径） ===
            Tween moveTween = null;
            if (obj.TryGetComponent(out SpriteRenderer sp))
            {
                if (pathArcHeight != 0f)
                {
                    // 用 CatmullRom 路径实现抛物线弧：起点 → 弧顶中点 → 终点
                    Vector3 mid = (obj.transform.localPosition + (Vector3)endPos) / 2f;
                    mid.y += pathArcHeight;
                    moveTween = obj.transform.DOLocalPath(
                        new Vector3[] { mid, endPos },
                        duration, PathType.CatmullRom
                    ).SetEase(tweenEase);
                }
                else
                {
                    moveTween = obj.transform.DOLocalMove(endPos, duration).SetEase(tweenEase);
                }
            }
            else if (obj.TryGetComponent(out RectTransform rt))
            {
                if (pathArcHeight != 0f)
                {
                    // RectTransform: 用虚拟浮点驱动抛物线 Y 偏移，叠加在 DOAnchorPos 之上
                    Vector2 startAnchor = rt.anchoredPosition;
                    float arcProgress = 0f;
                    Tween arcDrive = DOTween.To(() => arcProgress, v => arcProgress = v, 1f, duration)
                        .SetEase(tweenEase)
                        .OnUpdate(() =>
                        {
                            float parabola = 4f * arcProgress * (1f - arcProgress); // 0→1→0
                            float baseX = Mathf.Lerp(startAnchor.x, endPos.x, arcProgress);
                            float baseY = Mathf.Lerp(startAnchor.y, endPos.y, arcProgress);
                            rt.anchoredPosition = new Vector2(baseX, baseY + pathArcHeight * parabola);
                        });
                    sequence.Join(arcDrive);
                    // arcDrive 已加入 sequence，moveTween 留 null 避免二次 Join
                }
                else
                {
                    moveTween = rt.DOAnchorPos(endPos, duration).SetEase(tweenEase);
                }
            }

            if (moveTween != null)
                sequence.Join(moveTween);

            // === 2. 缩放动画 ===
            if (controlX || controlY)
            {
                Vector3 targetScale = obj.transform.localScale;
                if (controlX) targetScale.x = targetSize.x;
                if (controlY) targetScale.y = targetSize.y;

                Tween scaleTween = obj.transform.DOScale(targetScale, duration).SetEase(tweenEase);
                sequence.Join(scaleTween);
            }

            // === 3. 旋转动画（仅在扇形模式下由 targetEndAngle 驱动） ===
            if (targetEndAngle != 0f)
            {
                obj.transform.localEulerAngles = new Vector3(0, 0, startAngle);
                Tween rotTween = obj.transform.DOLocalRotate(new Vector3(0, 0, targetEndAngle), duration)
                    .SetEase(rotateEase);
                sequence.Join(rotTween);
            }

            if (layoutGroup != null)
                layoutGroup.RegisterTween(sequence);

            yield return sequence.WaitForCompletion();

            if (layoutGroup != null)
            {
                layoutGroup.UnregisterTween(sequence);
                layoutGroup.remainingTweens--;
                if (layoutGroup.remainingTweens <= 0)
                {
                    layoutGroup.m_tweenAnim.OnEndTweenAnim?.Invoke();
                    layoutGroup.m_isPlayingTweenAnim = false;
                    layoutGroup.hasStartedTween = false;
                    layoutGroup.isPlayingTweenAnim = false;
                }
            }
        }
    }

    private void InitTweenState(GameObject childObj)
    {
        childObj.SetActive(false);
        childObj.transform.localPosition = m_tweenAnim.startPoint;

        Vector2 initScale = childObj.transform.localScale;
        if (m_controlChildSize.ControlX)
            initScale.x = m_tweenAnim.startScale.x;
        if (m_controlChildSize.ControlY)
            initScale.y = m_tweenAnim.startScale.y;
        childObj.transform.localScale = initScale;
        childObj.transform.rotation = Quaternion.identity;
    }
}
