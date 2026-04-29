using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class SelectableFocusOutline : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color normalOutlineColor = new Color(0f, 0f, 0f, 0.20f);
    [SerializeField] private Color highlightedOutlineColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private Vector2 normalOutlineDistance = new Vector2(1f, -1f);
    [SerializeField] private Vector2 highlightedOutlineDistance = new Vector2(2f, -2f);

    private Outline outline;
    private bool pointerInside;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }

        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void OnSelect(BaseEventData eventData)
    {
        Refresh();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        Refresh();
    }

    private void Refresh()
    {
        bool selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        bool highlighted = selected || pointerInside;
        outline.effectColor = highlighted ? highlightedOutlineColor : normalOutlineColor;
        outline.effectDistance = highlighted ? highlightedOutlineDistance : normalOutlineDistance;
    }
}
