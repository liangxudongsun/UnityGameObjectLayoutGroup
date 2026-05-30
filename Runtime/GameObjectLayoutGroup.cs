using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;
using DG.Tweening;
using System.Collections;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.UI;

[ExecuteAlways]
public partial class GameObjectLayoutGroup : MonoBehaviour
{
    [Serializable]
    public class Padding
    {
        public float left;
        public float right;
        public float top;
        public float bottom;
        public float horizontal { get { return MathF.Abs(left) + MathF.Abs(right); } }
        public float vertical { get { return MathF.Abs(top) + MathF.Abs(bottom); } }

        public Padding(float left, float right, float top, float bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }
    }

    [Serializable]
    public class ControlChildSize
    {
        public bool ControlX;
        public bool ControlY;
    }

    [Serializable]
    public class ControlChildRotation
    {
        public bool ControlX;
        public bool ControlY;
        public bool ControlZ;
    }

    [Serializable]
    public class Sector
    {
        public float angle;
        public float radius;
    }
    public enum LayoutAxis
    {
        Horizontal,
        Vertical,
    }
    [SerializeField] Padding m_padding;
    [SerializeField, Tooltip("只有 ControlX 和 ControlY 都未勾选时，m_cellSize 才会自动从第一个子物体的实际尺寸赋值")]
    private Vector2 m_cellSize = new Vector2(100, 100);
    [SerializeField] Vector3 m_cellRotation = new Vector3(0, 0, 0);
    [SerializeField, Tooltip("子物体之间的间距,注意区分Rect和Sprite")]
    private Vector2 m_spacing = new Vector2(10, 10);
    [SerializeField, Tooltip("当子物体数量发生变化并且不不播放动画时，是否自动更新布局")]
    private bool m_autoUpdateLayout = true; // 自动更新开关
    [SerializeField, Tooltip("是否包含未激活的子物体")]
    private bool m_includeInactive = false; // 包含未激活的子物体
    [SerializeField] LayoutAxis m_axis = LayoutAxis.Horizontal;
    public LayoutAxis Axis { get { return m_axis; } }
    [SerializeField] Corner m_startCorner;
    [SerializeField] TextAnchor m_childAlignment;
    private Vector2 m_size;
    [SerializeField] ControlChildSize m_controlChildSize;
    [SerializeField] ControlChildRotation m_controlChildRotation;
    [SerializeField] bool enableSector;
    [SerializeField] Sector m_sector;
    [SerializeField] bool m_enableTweenAnim;
    [SerializeField] TweenAnim m_tweenAnim;
    private bool m_isPlayingTweenAnim = false;
    public bool isPlayingTweenAnim
    {
        get { return m_isPlayingTweenAnim; }
        set { m_isPlayingTweenAnim = value; }
    }
    private List<Coroutine> activeTweens = new List<Coroutine>();
    private List<Tween> activeTweensTween = new List<Tween>(); // 记录DOTween动画实例，用于强制停止
    public int remainingTweens { get; set; } = 0;
    public void RegisterTween(Tween tween) { activeTweensTween.Add(tween); }
    public void UnregisterTween(Tween tween) { activeTweensTween.Remove(tween); }
    /// <summary>停止所有播放中的动画</summary>
    public void StopAnim() { StopAllTweens(); }
    private bool m_hasStartedTween = false;
    public bool hasStartedTween
    {
        get { return m_hasStartedTween; }
        set { m_hasStartedTween = value; }
    }

    // 脏标记：子物体数量变化时重新初始化
    private int m_lastChildCount = -1;
    private bool m_layoutDirty = true;

    private void MarkLayoutDirty()
    {
        m_layoutDirty = true;
    }

    private void OnEnable()
    {
        MarkLayoutDirty();
    }

    private bool CheckLayoutDirty()
    {
        int count = transform.childCount;
        if (count != m_lastChildCount)
        {
            m_lastChildCount = count;
            m_layoutDirty = true;
        }
        if (m_layoutDirty)
        {
            m_layoutDirty = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 当动画开始时
    /// </summary>
    /// <value></value>
    public UnityAction OnStartTweenAnim
    {
        get { return m_tweenAnim.OnStartTweenAnim; }
        set { m_tweenAnim.OnStartTweenAnim = value; }
    }
    /// <summary>
    /// 当动画播放时,在update中执行
    /// </summary>
    /// <value></value>
    public UnityAction OnPlayTweenAnim
    {
        get { return m_tweenAnim.OnPlayTweenAnim; }
        set { m_tweenAnim.OnPlayTweenAnim = value; }
    }
    /// <summary>
    /// 当动画结束时
    /// </summary>
    /// <value></value>
    public UnityAction OnEndTweenAnim
    {
        get { return m_tweenAnim.OnEndTweenAnim; }
        set { m_tweenAnim.OnEndTweenAnim = value; }
    }
    #region Sprite
    private SpriteRenderer m_spriteRenderer;
    private List<SpriteRenderer> m_childSpriteRenderers;
    #endregion

#if UNITY_EDITOR
    private static GUIStyle _sectorLabelStyle;
    private static GUIStyle GetSectorLabelStyle()
    {
        if (_sectorLabelStyle == null)
        {
            _sectorLabelStyle = new GUIStyle
            {
                fontSize = 2,
                normal = { textColor = Color.red }
            };
        }
        return _sectorLabelStyle;
    }
#endif

    #region Rect
    private RectTransform m_contentRect; // 内容区域
    private List<RectTransform> m_childRects = new List<RectTransform>(); // 子项
    #endregion

    // 水平对齐
    private float AdjustHorizontalAlignment(float currentX, Corner startCorner, float contentWidth, float totalGridWidth)
    {
        switch (startCorner)
        {
            case Corner.UpperLeft:
            case Corner.LowerLeft:
                return AdjustLeftCornerHorizontal(currentX, contentWidth, totalGridWidth);
            case Corner.UpperRight:
            case Corner.LowerRight:
                return AdjustRightCornerHorizontal(currentX, contentWidth, totalGridWidth);
            default:
                return currentX;
        }
    }

    // 垂直对齐
    private float AdjustVerticalAlignment(float currentY, Corner startCorner, float contentHeight, float totalGridHeight)
    {
        switch (startCorner)
        {
            case Corner.UpperLeft:
            case Corner.UpperRight:
                return AdjustUpperCornerVertical(currentY, contentHeight, totalGridHeight);
            case Corner.LowerLeft:
            case Corner.LowerRight:
                return AdjustLowerCornerVertical(currentY, contentHeight, totalGridHeight);
            default:
                return currentY;
        }
    }

    // 左角落（UpperLeft/LowerLeft）水平对齐
    private float AdjustLeftCornerHorizontal(float x, float contentWidth, float totalGridWidth)
    {
        switch (m_childAlignment)
        {
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                return x + (contentWidth - totalGridWidth) / 2;
            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                return x + (contentWidth - totalGridWidth);
            default:
                return x;
        }
    }

    // 右角落（UpperRight/LowerRight）水平对齐
    private float AdjustRightCornerHorizontal(float x, float contentWidth, float totalGridWidth)
    {
        switch (m_childAlignment)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.MiddleLeft:
            case TextAnchor.LowerLeft:
                return x - (contentWidth - totalGridWidth);
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                return x - (contentWidth - totalGridWidth) / 2;
            default:
                return x;
        }
    }

    // 上角落（UpperLeft/UpperRight）垂直对齐
    private float AdjustUpperCornerVertical(float y, float contentHeight, float totalGridHeight)
    {
        switch (m_childAlignment)
        {
            case TextAnchor.MiddleLeft:
            case TextAnchor.MiddleCenter:
            case TextAnchor.MiddleRight:
                return y - (contentHeight - totalGridHeight) / 2;
            case TextAnchor.LowerLeft:
            case TextAnchor.LowerCenter:
            case TextAnchor.LowerRight:
                return y - (contentHeight - totalGridHeight);
            default:
                return y;
        }
    }

    // 下角落（LowerLeft/LowerRight）垂直对齐
    private float AdjustLowerCornerVertical(float y, float contentHeight, float totalGridHeight)
    {
        switch (m_childAlignment)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.UpperCenter:
            case TextAnchor.UpperRight:
                return y + (contentHeight - totalGridHeight);
            case TextAnchor.MiddleLeft:
            case TextAnchor.MiddleCenter:
            case TextAnchor.MiddleRight:
                return y + (contentHeight - totalGridHeight) / 2;
            default:
                return y;
        }
    }
    // 停止所有动画并重置状态
    private void StopAllTweens()
    {
        // 停止协程
        foreach (var coroutine in activeTweens)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeTweens.Clear();

        // 停止DOTween动画
        foreach (var tween in activeTweensTween)
        {
            if (tween != null && tween.IsActive())
                tween.Kill();
        }
        activeTweensTween.Clear();

        if (m_childSpriteRenderers != null)
        {
            foreach (var child in m_childSpriteRenderers)
            {
                child.gameObject.SetActive(true);
                ApplyChildSizeControl(child.gameObject, false);
            }
        }

        if (m_childRects != null)
        {
            foreach (var child in m_childRects)
            {
                child.gameObject.SetActive(true);
                ApplyChildSizeControl(child, false);
            }
        }

        m_isPlayingTweenAnim = false;
        remainingTweens = 0;
        m_hasStartedTween = false;
    }

    private Vector2 CalculateSectorCenter(float contentWidth, float contentHeight)
    {
        float contentLeft = m_padding.left;
        float contentTop = -m_padding.top;
        float contentRight = contentLeft + contentWidth;
        float contentBottom = contentTop - contentHeight;
        float contentCenterX = contentLeft + contentWidth / 2;
        float contentCenterY = contentTop - contentHeight / 2;

        switch (m_childAlignment)
        {
            case TextAnchor.UpperLeft:
                return new Vector2(contentLeft, contentTop);
            case TextAnchor.UpperCenter:
                return new Vector2(contentCenterX, contentTop);
            case TextAnchor.UpperRight:
                return new Vector2(contentRight, contentTop);
            case TextAnchor.MiddleLeft:
                return new Vector2(contentLeft, contentCenterY);
            case TextAnchor.MiddleCenter:
                return new Vector2(contentCenterX, contentCenterY);
            case TextAnchor.MiddleRight:
                return new Vector2(contentRight, contentCenterY);
            case TextAnchor.LowerLeft:
                return new Vector2(contentLeft, contentBottom);
            case TextAnchor.LowerCenter:
                return new Vector2(contentCenterX, contentBottom);
            case TextAnchor.LowerRight:
                return new Vector2(contentRight, contentBottom);
            default:
                return new Vector2(contentCenterX, contentCenterY);
        }
    }



    private void ApplyChildRotationControl(GameObject child)
    {
        Vector3 currentRot = child.transform.localEulerAngles;
        if (m_controlChildRotation.ControlX) currentRot.x = m_cellRotation.x;
        if (m_controlChildRotation.ControlY) currentRot.y = m_cellRotation.y;
        if (m_controlChildRotation.ControlZ) currentRot.z = m_cellRotation.z;
        child.transform.localEulerAngles = currentRot;
    }

    private void ResetRectScale()
    {
        if (m_childSpriteRenderers != null)
        {
            foreach (var child in m_childSpriteRenderers)
            {
                child.transform.localScale = Vector3.one;
            }
        }
        if (m_childRects != null)
        {
            foreach (var child in m_childRects)
            {
                child.localScale = Vector3.one;
            }
        }
    }

    private void Update()
    {
        // 仅在子物体数量变化时重新初始化（避免每帧 GetComponents 开销）
        if (CheckLayoutDirty())
        {
            InitSpriteMode();
            InitRectMode();

            if (m_autoUpdateLayout)
            {
                SpriteLayout();
                RectLayout();
            }
        }

        // 动画播放中每帧触发回调
        if (m_isPlayingTweenAnim)
        {
            m_tweenAnim.OnPlayTweenAnim?.Invoke();
        }
    }

    private void OnDestroy()
    {
        StopAllTweens();
    }
    /// <summary>
    /// 手动更新
    /// </summary>
    public void UpdateLayout(bool playAnim)
    {
        m_enableTweenAnim = playAnim;
        m_hasStartedTween = false;
        // 缓存在本次有效，跳过重复初始化
        if (m_childSpriteRenderers == null || m_contentRect == null)
        {
            MarkLayoutDirty();
            InitSpriteMode();
            InitRectMode();
        }
        SpriteLayout();
        RectLayout();
    }
}