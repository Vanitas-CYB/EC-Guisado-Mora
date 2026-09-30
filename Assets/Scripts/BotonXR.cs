using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ECXR
{
    /// <summary>
    /// Conecta un XR Simple Interactable con un metodo publico de otro componente.
    /// Sirve para accionar a distancia (rayo XR) elementos como luces, puertas o cambios de material.
    /// Incluye un pequeno antidoble para que una misma pulsacion no dispare la accion dos veces.
    /// </summary>
    [AddComponentMenu("EC XR/Boton XR")]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class BotonXR : MonoBehaviour
    {
        [Header("Destino")]
        [Tooltip("Componente que contiene el metodo a ejecutar.")]
        [SerializeField] private MonoBehaviour destino;

        [Tooltip("Nombre del metodo publico del destino (por ejemplo Alternar, Abrir, SiguienteColor).")]
        [SerializeField] private string metodo = "Alternar";

        [Header("Eventos escuchados")]
        [SerializeField] private bool alActivar = true;
        [SerializeField] private bool alSeleccionar;

        [Header("Antidoble")]
        [Tooltip("Tiempo minimo entre dos ejecuciones consecutivas.")]
        [SerializeField] private float intervaloMinimo = 0.3f;

        private XRSimpleInteractable interactable;
        private float ultimaEjecucion = -10f;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();

            if (interactable == null)
            {
                Debug.LogWarning("[EC_XR] BotonXR necesita un XR Simple Interactable en el mismo GameObject.", this);
                return;
            }

            if (alActivar)
            {
                interactable.activated.AddListener(AlActivar);
            }

            if (alSeleccionar)
            {
                interactable.selectEntered.AddListener(AlSeleccionar);
            }
        }

        private void OnDestroy()
        {
            if (interactable == null)
            {
                return;
            }

            interactable.activated.RemoveListener(AlActivar);
            interactable.selectEntered.RemoveListener(AlSeleccionar);
        }

        private void AlActivar(ActivateEventArgs args)
        {
            Ejecutar();
        }

        private void AlSeleccionar(SelectEnterEventArgs args)
        {
            Ejecutar();
        }

        /// <summary>Ejecuta la accion configurada.</summary>
        public void Ejecutar()
        {
            if (Time.time - ultimaEjecucion < intervaloMinimo)
            {
                return;
            }

            ultimaEjecucion = Time.time;

            if (destino == null)
            {
                Debug.LogWarning("[EC_XR] BotonXR sin destino asignado.", this);
                return;
            }

            destino.SendMessage(metodo, SendMessageOptions.DontRequireReceiver);
        }
    }
}
