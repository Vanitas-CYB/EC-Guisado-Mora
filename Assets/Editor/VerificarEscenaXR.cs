using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Comprueba que la escena cumple los requisitos de la rubrica y escribe un informe
/// en Temp/EC_XR_verificacion.log.
///
/// Menu: EC_XR / 3. Verificar escena XR
/// CLI : Unity.exe -batchmode -quit -projectPath "RUTA" -executeMethod VerificarEscenaXR.Verificar
/// </summary>
public static class VerificarEscenaXR
{
    private const string RutaEscena = "Assets/Scenes/EC_XR_GuisadoMoraChristopher.unity";

    [MenuItem("EC_XR/3. Verificar escena XR")]
    public static void Verificar()
    {
        var informe = new StringBuilder();
        int fallos = 0;

        informe.AppendLine("=== VERIFICACION ESCENA EC_XR ===");
        informe.AppendLine("Fecha: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        // Escena
        if (!File.Exists(RutaEscena))
        {
            informe.AppendLine("[FALLO] No existe la escena " + RutaEscena);
            Volcar(informe, 1);
            return;
        }

        Scene escena = SceneManager.GetActiveScene();
        if (escena.path != RutaEscena)
        {
            escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        }

        informe.AppendLine("[OK] Escena abierta: " + escena.path + "  (raices: " + escena.rootCount + ")");

        // Requisito 1: configuracion XR
        bool openXR = ConfigurarXRPlugins.OpenXRHabilitado();
        informe.AppendLine((openXR ? "[OK] " : "[FALLO] ") + "XR Plug-in Management: OpenXR habilitado en Standalone = " + openXR);
        if (!openXR)
        {
            fallos++;
        }

        informe.AppendLine(Comprobar("XR Interaction Manager", Contar<XRInteractionManager>() >= 1, ">=1"));
        informe.AppendLine(Comprobar("XR Origin", BuscarPorNombre("XR Origin (XR Rig)") != null, "presente"));
        informe.AppendLine(Comprobar("XR Interaction Simulator", BuscarPorNombre("XR Interaction Simulator") != null, "presente"));

        // Requisito 2: escenario (piso, limites, iluminacion, 5 objetos)
        int luces = Contar<Light>();
        int renderers = Contar<MeshRenderer>();
        informe.AppendLine(Comprobar("Luces", luces >= 2, ">=2 (hay " + luces + ")"));
        informe.AppendLine(Comprobar("Objetos 3D con malla", renderers >= 5, ">=5 (hay " + renderers + ")"));
        informe.AppendLine(Comprobar("Piso (prefab SUELO)", Contar<MeshCollider>() >= 1, ">=1"));
        informe.AppendLine(Comprobar("Muros / limites visuales", ContarPorNombre("Muro_") >= 4, ">=4"));

        // Requisito 3: manipulacion
        int agarrables = Contar<XRGrabInteractable>();
        int conRigidbody = ContarAgarrablesConRigidbody();
        informe.AppendLine(Comprobar("XR Grab Interactable", agarrables >= 2, ">=2 (hay " + agarrables + ")"));
        informe.AppendLine(Comprobar("Rigidbody en agarrables", conRigidbody == agarrables && agarrables > 0, "todos"));

        // Requisito 4: interaccion a distancia
        int simples = Contar<XRSimpleInteractable>();
        informe.AppendLine(Comprobar("XR Simple Interactable (botones de rayo)", simples >= 1, ">=1 (hay " + simples + ")"));
        informe.AppendLine(Comprobar("Script InterruptorLuz", ContarPorTipo("InterruptorLuz") >= 1, ">=1"));
        informe.AppendLine(Comprobar("Script PuertaDeslizante", ContarPorTipo("PuertaDeslizante") >= 1, ">=1"));
        informe.AppendLine(Comprobar("Script CambiarColorObjetivo", ContarPorTipo("CambiarColorObjetivo") >= 1, ">=1"));

        // Requisito 5: reto libre
        informe.AppendLine(Comprobar("Teletransporte (TeleportationArea)", Contar<TeleportationArea>() >= 1, ">=1"));
        informe.AppendLine(Comprobar("UI espacial (Canvas World Space)", ContarCanvasWorldSpace() >= 1, ">=1"));
        informe.AppendLine(Comprobar("Script ContadorXR", ContarPorTipo("ContadorXR") >= 1, ">=1"));

        Volcar(informe, fallos);
    }

    private static void Volcar(StringBuilder informe, int fallos)
    {
        informe.AppendLine("=== RESULTADO: " + (fallos == 0 ? "TODO CORRECTO" : fallos + " comprobacion(es) pendiente(s)") + " ===");

        string ruta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "EC_XR_verificacion.log");
        File.WriteAllText(ruta, informe.ToString(), Encoding.UTF8);

        if (fallos == 0)
        {
            Debug.Log("[EC_XR] Verificacion correcta.\n" + informe);
        }
        else
        {
            Debug.LogWarning("[EC_XR] Verificacion con pendientes:\n" + informe);
        }
    }

    private static string Comprobar(string descripcion, bool correcto, string esperado)
    {
        return (correcto ? "[OK] " : "[FALLO] ") + descripcion + " (esperado: " + esperado + ")";
    }

    private static int Contar<T>() where T : Component
    {
        return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;
    }

    private static int ContarAgarrablesConRigidbody()
    {
        int total = 0;
        foreach (XRGrabInteractable agarrable in UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None))
        {
            if (agarrable.GetComponent<Rigidbody>() != null)
            {
                total++;
            }
        }

        return total;
    }

    private static int ContarCanvasWorldSpace()
    {
        int total = 0;
        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                total++;
            }
        }

        return total;
    }

    private static int ContarPorNombre(string fragmento)
    {
        int total = 0;
        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name.StartsWith(fragmento, StringComparison.OrdinalIgnoreCase))
            {
                total++;
            }
        }

        return total;
    }

    private static int ContarPorTipo(string nombreTipo)
    {
        int total = 0;
        foreach (MonoBehaviour componente in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (componente != null && componente.GetType().Name == nombreTipo)
            {
                total++;
            }
        }

        return total;
    }

    private static GameObject BuscarPorNombre(string nombre)
    {
        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name == nombre)
            {
                return go;
            }
        }

        return null;
    }
}
