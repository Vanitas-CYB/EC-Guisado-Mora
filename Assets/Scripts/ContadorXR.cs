using UnityEngine;

namespace ECXR
{
    /// <summary>
    /// Contador de interacciones en UI espacial. Muestra cuantos objetos se estan agarrando
    /// y cuantas activaciones (botones / rayos) se han realizado.
    /// Se suscribe automaticamente a todos los interactables XR presentes en la escena.
    /// </summary>
    [AddComponentMenu("EC XR/Contador XR")]
    public class ContadorXR : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Texto de la UI espacial que muestra el contador.")]
        [SerializeField] private UnityEngine.UI.Text texto;

        [SerializeField] private string titulo = "SALA DE ENTRENAMIENTO XR";

        [Header("Rastreo automatico")]
        [Tooltip("Si esta activo, busca todos los interactables XR de la escena y los cuenta.")]
        [SerializeField] private bool rastrearAutomaticamente = true;

        /// <summary>Objetos agarrados en este momento.</summary>
        public int ObjetosAgarrados { get; private set; }

        /// <summary>Total de agarres realizados desde el inicio.</summary>
        public int AgarresTotales { get; private set; }

        /// <summary>Total de activaciones (botones, rayos, etc.).</summary>
        public int Activaciones { get; private set; }

        private void Start()
        {
            if (texto == null)
            {
                texto = GetComponentInChildren<UnityEngine.UI.Text>(true);
            }

            if (rastrearAutomaticamente)
            {
                RastrearInteractables();
            }

            ActualizarTexto();
        }

        /// <summary>Busca todos los interactables de la escena y se suscribe a sus eventos.</summary>
        public void RastrearInteractables()
        {
            int encontrados = 0;

            foreach (MonoBehaviour componente in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (componente is UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
                {
                    interactable.selectEntered.AddListener(AlSeleccionar);
                    interactable.selectExited.AddListener(AlSoltar);
                    interactable.activated.AddListener(AlActivar);
                    encontrados++;
                }
            }

            Debug.Log("[EC_XR] ContadorXR rastreando " + encontrados + " interactables.");
        }

        private void AlSeleccionar(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
        {
            if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable)
            {
                ObjetosAgarrados++;
                AgarresTotales++;
                ActualizarTexto();
            }
        }

        private void AlSoltar(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args)
        {
            if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable)
            {
                ObjetosAgarrados = Mathf.Max(0, ObjetosAgarrados - 1);
                ActualizarTexto();
            }
        }

        private void AlActivar(UnityEngine.XR.Interaction.Toolkit.ActivateEventArgs args)
        {
            Activaciones++;
            ActualizarTexto();
        }

        /// <summary>Refresca el texto de la UI espacial.</summary>
        public void ActualizarTexto()
        {
            if (texto == null)
            {
                return;
            }

            texto.text = string.Format(
                "{0}\n\nObjetos agarrados: {1}\nAgarres totales: {2}\nActivaciones: {3}",
                titulo, ObjetosAgarrados, AgarresTotales, Activaciones);
        }
    }
}
