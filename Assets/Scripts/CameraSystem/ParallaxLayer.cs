using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Camada de fundo com efeito parallax: acompanha a câmera em uma fração do movimento horizontal,
    /// dando sensação de profundidade.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] Transform cameraTransform;
        [SerializeField, Range(0f, 1f)] float horizontalFactor = 0.3f;
        [SerializeField, Range(0f, 1f)] float verticalFactor = 1f;
        [SerializeField] float verticalOffset;

        float startX;
        float camStartY;

        public void Configure(Transform cam, float horizontal, float vertical, float yOffset)
        {
            cameraTransform = cam;
            horizontalFactor = horizontal;
            verticalFactor = vertical;
            verticalOffset = yOffset;
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            startX = transform.position.x;
            camStartY = cameraTransform != null ? cameraTransform.position.y : 0f;
        }

        void LateUpdate()
        {
            if (cameraTransform == null) return;
            Vector3 cam = cameraTransform.position;
            float x = startX + cam.x * horizontalFactor;
            float y = camStartY + (cam.y - camStartY) * verticalFactor + verticalOffset;
            transform.position = new Vector3(x, y, transform.position.z);
        }
    }
}
