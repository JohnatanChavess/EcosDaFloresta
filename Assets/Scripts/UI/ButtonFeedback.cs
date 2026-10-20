using UnityEngine;
using UnityEngine.EventSystems;

namespace Ecos
{
    /// <summary>
    /// Aumenta levemente o botão quando o mouse passa por cima ou quando ele é selecionado pelo teclado.
    /// </summary>
    public class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] float highlightScale = 1.06f;

        bool highlighted;

        void OnDisable()
        {
            highlighted = false;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            float target = highlighted ? highlightScale : 1f;
            float s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 1.5f);
            transform.localScale = new Vector3(s, s, 1f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            highlighted = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnPointerExit(PointerEventData eventData) => highlighted = false;
        public void OnSelect(BaseEventData eventData) => highlighted = true;
        public void OnDeselect(BaseEventData eventData) => highlighted = false;
    }
}
