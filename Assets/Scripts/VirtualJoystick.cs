using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Referencias del Joystick")]
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [Header("Configuración")]
    [SerializeField] private float handleRange = 60f;
    [SerializeField] private float jumpThreshold = 0.55f;

    private Vector2 inputVector = Vector2.zero;
    private bool jumpTriggered = false;

    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public System.Action OnJumpTriggered;

    private void Awake()
    {
        if (background == null) background = GetComponent<RectTransform>();
        if (handle == null && transform.childCount > 0)
        {
            handle = transform.GetChild(0).GetComponent<RectTransform>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            localPoint = Vector2.ClampMagnitude(localPoint, handleRange);
            inputVector = localPoint / handleRange;

            if (handle != null)
            {
                handle.anchoredPosition = localPoint;
            }

            // Detectar si el usuario empuja el joystick hacia arriba para saltar
            if (inputVector.y >= jumpThreshold && !jumpTriggered)
            {
                jumpTriggered = true;
                OnJumpTriggered?.Invoke();
            }
            else if (inputVector.y < jumpThreshold * 0.5f)
            {
                jumpTriggered = false;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        jumpTriggered = false;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
