using UnityEngine;

namespace ECXR
{
    /// <summary>
    /// Enciende y apaga una o varias luces de la escena.
    /// Se invoca desde un XR Simple Interactable (evento Activated) o desde un XR Ray Interactor.
    /// Tambien cambia la emision de los materiales indicados para dar respuesta visual.
    /// </summary>
    [AddComponentMenu("EC XR/Interruptor de Luz")]
    public class InterruptorLuz : MonoBehaviour
    {
        [Header("Luces controladas")]
        [Tooltip("Luces que se encenderan y apagaran.")]
        [SerializeField] private Light[] luces;

        [Header("Respuesta visual (opcional)")]
        [Tooltip("Renderers cuyo material cambiara de emision al encender o apagar.")]
        [SerializeField] private Renderer[] renderersEmisivos;

        [SerializeField] private Color colorEmisionEncendido = new Color(1f, 0.92f, 0.75f);
        [SerializeField] private Color colorEmisionApagado = new Color(0.03f, 0.03f, 0.03f);

        [Header("Estado inicial")]
        [SerializeField] private bool encendida = true;
        [SerializeField] private float intensidadEncendida = 3.5f;

        private float intensidadOriginal = 1f;

        /// <summary>Cuantas veces se ha activado el interruptor.</summary>
        public int VecesActivada { get; private set; }

        /// <summary>Indica si la luz esta encendida.</summary>
        public bool EstaEncendida => encendida;

        /// <summary>Evento que informa el nuevo estado (true = encendida).</summary>
        public event System.Action<bool> AlCambiarEstado;

        private void Awake()
        {
            if (luces == null || luces.Length == 0)
            {
                luces = GetComponentsInChildren<Light>(true);
            }

            if (luces.Length > 0)
            {
                intensidadOriginal = luces[0].intensity;
            }

            AplicarEstado();
        }

        /// <summary>Alterna el estado de la luz.</summary>
        public void Alternar()
        {
            Establecer(!encendida);
        }

        /// <summary>Enciende la luz.</summary>
        public void Encender()
        {
            Establecer(true);
        }

        /// <summary>Apaga la luz.</summary>
        public void Apagar()
        {
            Establecer(false);
        }

        /// <summary>Define el estado de la luz.</summary>
        public void Establecer(bool valor)
        {
            encendida = valor;
            VecesActivada++;
            AplicarEstado();
            AlCambiarEstado?.Invoke(encendida);
        }

        private void AplicarEstado()
        {
            if (luces != null)
            {
                foreach (Light luz in luces)
                {
                    if (luz != null)
                    {
                        luz.enabled = encendida;
                        luz.intensity = encendida
                            ? Mathf.Max(intensidadEncendida, intensidadOriginal)
                            : 0f;
                    }
                }
            }

            if (renderersEmisivos == null)
            {
                return;
            }

            Color color = encendida ? colorEmisionEncendido : colorEmisionApagado;

            foreach (Renderer renderer in renderersEmisivos)
            {
                if (renderer == null)
                {
                    continue;
                }

                Material material = renderer.material;
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", color);
                }

                if (encendida)
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            }
        }
    }
}
