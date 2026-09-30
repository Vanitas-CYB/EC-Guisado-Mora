using UnityEngine;

namespace ECXR
{
    /// <summary>
    /// Abre y cierra una puerta desplazando su hoja.
    /// Se invoca a distancia desde un XR Ray Interactor o un XR Simple Interactable.
    /// </summary>
    [AddComponentMenu("EC XR/Puerta Deslizante")]
    public class PuertaDeslizante : MonoBehaviour
    {
        [Header("Partes de la puerta")]
        [Tooltip("Transform de la hoja que se desplaza. Si se deja vacio se usa este GameObject.")]
        [SerializeField] private Transform hoja;

        [Tooltip("Desplazamiento local que aplica la hoja al abrirse.")]
        [SerializeField] private Vector3 desplazamientoLocal = new Vector3(0f, 0f, 1.15f);

        [Header("Movimiento")]
        [SerializeField] private float velocidad = 1.6f;
        [Tooltip("Tiempo minimo entre activaciones para evitar rebotes.")]
        [SerializeField] private float intervaloMinimo = 0.35f;

        [Header("Opcional")]
        [SerializeField] private AudioSource sonido;

        private Vector3 posicionCerrada;
        private Vector3 posicionAbierta;
        private bool abierta;
        private float ultimaActivacion = -10f;

        /// <summary>Indica si la puerta esta abierta.</summary>
        public bool EstaAbierta => abierta;

        /// <summary>Evento que informa el nuevo estado (true = abierta).</summary>
        public event System.Action<bool> AlCambiarEstado;

        private void Awake()
        {
            if (hoja == null)
            {
                hoja = transform;
            }

            posicionCerrada = hoja.localPosition;
            posicionAbierta = posicionCerrada + desplazamientoLocal;
        }

        private void Update()
        {
            Vector3 destino = abierta ? posicionAbierta : posicionCerrada;
            hoja.localPosition = Vector3.MoveTowards(hoja.localPosition, destino, velocidad * Time.deltaTime);
        }

        /// <summary>Abre la puerta si esta cerrada y la cierra si esta abierta.</summary>
        public void Alternar()
        {
            if (Time.time - ultimaActivacion < intervaloMinimo)
            {
                return;
            }

            ultimaActivacion = Time.time;
            Establecer(!abierta);
        }

        /// <summary>Abre la puerta.</summary>
        public void Abrir()
        {
            Establecer(true);
        }

        /// <summary>Cierra la puerta.</summary>
        public void Cerrar()
        {
            Establecer(false);
        }

        /// <summary>Define el estado de la puerta.</summary>
        public void Establecer(bool valor)
        {
            if (abierta == valor)
            {
                return;
            }

            abierta = valor;

            if (sonido != null)
            {
                sonido.Play();
            }

            AlCambiarEstado?.Invoke(abierta);
        }
    }
}
