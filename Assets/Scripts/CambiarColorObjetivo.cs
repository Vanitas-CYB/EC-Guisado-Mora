using UnityEngine;

namespace ECXR
{
    /// <summary>
    /// Cambia el color de uno o varios objetos recorriendo una paleta.
    /// Pensado para activarse a distancia con un XR Ray Interactor (evento Activated).
    /// </summary>
    [AddComponentMenu("EC XR/Cambiar Color de Objeto")]
    public class CambiarColorObjetivo : MonoBehaviour
    {
        [Header("Objetivos")]
        [Tooltip("Renderer principal. Si se deja vacio se usa el de este GameObject.")]
        [SerializeField] private Renderer objetivo;

        [Tooltip("Renderers adicionales que recibiran el mismo color.")]
        [SerializeField] private Renderer[] objetivosAdicionales;

        [Header("Paleta de colores")]
        [SerializeField] private Color[] paleta =
        {
            new Color(0.18f, 0.55f, 0.97f), // azul
            new Color(0.16f, 0.83f, 0.47f), // verde
            new Color(0.98f, 0.75f, 0.16f), // ambar
            new Color(0.91f, 0.28f, 0.28f), // rojo
            new Color(0.68f, 0.36f, 0.95f)  // violeta
        };

        [SerializeField] private int indiceInicial;

        /// <summary>Cuantas veces se ha activado el cambio de color.</summary>
        public int VecesActivado { get; private set; }

        /// <summary>Indice actual dentro de la paleta.</summary>
        public int IndiceActual => indice;

        /// <summary>Color que tiene el objeto en este momento.</summary>
        public Color ColorActual => (paleta != null && paleta.Length > 0) ? paleta[indice] : Color.white;

        /// <summary>Evento que informa el color aplicado.</summary>
        public event System.Action<Color> AlCambiarColor;

        private int indice;

        private void Awake()
        {
            if (objetivo == null)
            {
                objetivo = GetComponent<Renderer>();
            }

            indice = Mathf.Clamp(indiceInicial, 0, Mathf.Max(0, (paleta?.Length ?? 0) - 1));
            AplicarColor();
        }

        /// <summary>Avanza al siguiente color de la paleta.</summary>
        public void SiguienteColor()
        {
            if (paleta == null || paleta.Length == 0)
            {
                return;
            }

            indice = (indice + 1) % paleta.Length;
            VecesActivado++;
            AplicarColor();
            AlCambiarColor?.Invoke(ColorActual);
        }

        /// <summary>Retrocede al color anterior de la paleta.</summary>
        public void ColorAnterior()
        {
            if (paleta == null || paleta.Length == 0)
            {
                return;
            }

            indice = (indice - 1 + paleta.Length) % paleta.Length;
            VecesActivado++;
            AplicarColor();
            AlCambiarColor?.Invoke(ColorActual);
        }

        /// <summary>Aplica un color concreto por indice.</summary>
        public void EstablecerIndice(int nuevoIndice)
        {
            if (paleta == null || paleta.Length == 0)
            {
                return;
            }

            indice = ((nuevoIndice % paleta.Length) + paleta.Length) % paleta.Length;
            AplicarColor();
            AlCambiarColor?.Invoke(ColorActual);
        }

        private void AplicarColor()
        {
            if (paleta == null || paleta.Length == 0)
            {
                return;
            }

            AplicarEn(objetivo);

            if (objetivosAdicionales == null)
            {
                return;
            }

            foreach (Renderer renderer in objetivosAdicionales)
            {
                AplicarEn(renderer);
            }
        }

        private void AplicarEn(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            Material material = renderer.material;
            if (material == null)
            {
                return;
            }

            Color color = ColorActual;

            // URP usa _BaseColor y el pipeline integrado usa _Color.
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
