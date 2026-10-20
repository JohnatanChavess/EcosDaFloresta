using UnityEngine;

namespace Ecos
{
    /// <summary>
    /// Área de escalada. O colisor (gatilho) cobre a escada inteira; o colisor do topo é uma
    /// plataforma de mão única que permite ficar de pé sobre a escada e descer por ela.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Ladder : MonoBehaviour
    {
        [SerializeField] Collider2D topCollider;

        BoxCollider2D area;

        public Collider2D TopCollider => topCollider;
        public float CenterX => Area.bounds.center.x;
        public float TopY => Area.bounds.max.y;
        public float BottomY => Area.bounds.min.y;

        BoxCollider2D Area
        {
            get
            {
                if (area == null) area = GetComponent<BoxCollider2D>();
                return area;
            }
        }

        public void Configure(float height, Collider2D top)
        {
            var box = Area;
            box.isTrigger = true;
            box.size = new Vector2(0.6f, height);
            box.offset = new Vector2(0f, height * 0.5f);
            topCollider = top;
        }
    }
}
