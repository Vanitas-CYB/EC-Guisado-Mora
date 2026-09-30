using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Prueba de ejecucion: abre la escena, entra en Play mode, espera unos segundos,
/// recoge cualquier error o excepcion de runtime, guarda una captura desde la camara
/// principal y escribe el informe en Logs/EC_XR_prueba_play.log.
///
/// Usa SessionState para sobrevivir al recargado de dominio que hace Unity al entrar en Play.
///
/// Menu: EC_XR / 8. Prueba de ejecucion (Play mode)
/// CLI : Unity.exe -batchmode -projectPath "RUTA" -executeMethod PruebaPlayXR.Ejecutar
/// </summary>
[InitializeOnLoad]
public static class PruebaPlayXR
{
    private const string RutaEscena = "Assets/Scenes/EC_XR_GuisadoMoraChristopher.unity";
    private const string CarpetaCapturas = "Capturas";

    private const string ClaveActiva = "EC_XR_PruebaActiva";
    private const string ClaveErrores = "EC_XR_PruebaErrores";
    private const string ClaveInicio = "EC_XR_PruebaInicio";
    private const string ClaveFin = "EC_XR_PruebaFin";

    private const float DuracionPlay = 9f;
    private const float LimiteArranque = 40f;

    static PruebaPlayXR()
    {
        if (!SessionState.GetBool(ClaveActiva, false))
        {
            return;
        }

        // Se vuelve a enganchar tras el recargado de dominio del Play mode.
        EditorApplication.update += Paso;
        Application.logMessageReceived += Registrar;
    }

    [MenuItem("EC_XR/8. Prueba de ejecucion (Play mode)")]
    public static void Ejecutar()
    {
        SessionState.SetBool(ClaveActiva, true);
        SessionState.SetString(ClaveErrores, string.Empty);
        SessionState.SetFloat(ClaveInicio, (float)EditorApplication.timeSinceStartup);
        SessionState.SetFloat(ClaveFin, -1f);

        EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);

        EditorApplication.update += Paso;
        Application.logMessageReceived += Registrar;
        EditorApplication.EnterPlaymode();
    }

    private static void Registrar(string condicion, string traza, LogType tipo)
    {
        if (tipo != LogType.Error && tipo != LogType.Exception && tipo != LogType.Assert)
        {
            return;
        }

        string previos = SessionState.GetString(ClaveErrores, string.Empty);
        if (previos.Contains(condicion))
        {
            return;
        }

        SessionState.SetString(ClaveErrores, previos + "- " + condicion + "\n");
    }

    private static void Paso()
    {
        if (!SessionState.GetBool(ClaveActiva, false))
        {
            EditorApplication.update -= Paso;
            return;
        }

        float ahora = (float)EditorApplication.timeSinceStartup;
        float inicio = SessionState.GetFloat(ClaveInicio, ahora);
        float fin = SessionState.GetFloat(ClaveFin, -1f);

        if (fin < 0f)
        {
            if (!EditorApplication.isPlaying)
            {
                if (ahora - inicio > LimiteArranque)
                {
                    Terminar("No se pudo entrar en Play mode en " + LimiteArranque + " s.");
                }

                return;
            }

            SessionState.SetFloat(ClaveFin, ahora + DuracionPlay);
            return;
        }

        if (ahora < fin)
        {
            return;
        }

        Terminar(null);
    }

    private static void Terminar(string problema)
    {
        var informe = new StringBuilder();
        informe.AppendLine("=== PRUEBA DE EJECUCION (PLAY MODE) ===");
        informe.AppendLine("Play mode activo     : " + EditorApplication.isPlaying);
        informe.AppendLine("Raices en la escena  : " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().rootCount);
        informe.AppendLine("Camara principal     : " + (Camera.main != null ? Camera.main.name : "NO ENCONTRADA"));
        informe.AppendLine("XR activo (subsistema): " + (UnityEngine.XR.XRSettings.isDeviceActive
            ? UnityEngine.XR.XRSettings.loadedDeviceName
            : "sin dispositivo XR (se usa el simulador)"));

        string errores = SessionState.GetString(ClaveErrores, string.Empty);
        informe.AppendLine("Errores / excepciones de runtime:");
        informe.AppendLine(string.IsNullOrEmpty(errores) ? "  (ninguno)" : errores);

        if (!string.IsNullOrEmpty(problema))
        {
            informe.AppendLine("PROBLEMA: " + problema);
        }

        GuardarCaptura(informe);

        string ruta = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs", "EC_XR_prueba_play.log");
        File.WriteAllText(ruta, informe.ToString(), Encoding.UTF8);

        Debug.Log("[EC_XR] Prueba de ejecucion finalizada. " + informe.ToString().Replace("\r", " ").Replace("\n", " | "));

        SessionState.SetBool(ClaveActiva, false);
        EditorApplication.update -= Paso;
        Application.logMessageReceived -= Registrar;

        // Se cierra el editor: la prueba ya termino y el informe esta en disco.
        EditorApplication.Exit(0);
    }

    private static void GuardarCaptura(StringBuilder informe)
    {
        try
        {
            Camera camara = Camera.main;
            if (camara == null)
            {
                return;
            }

            Directory.CreateDirectory(CarpetaCapturas);

            const int ancho = 1920;
            const int alto = 1080;
            RenderTexture destino = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);

            camara.targetTexture = destino;
            camara.Render();

            RenderTexture anterior = RenderTexture.active;
            RenderTexture.active = destino;

            Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            textura.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            textura.Apply();

            RenderTexture.active = anterior;
            camara.targetTexture = null;

            string ruta = CarpetaCapturas + "/06_ejecucion_en_play.png";
            File.WriteAllBytes(ruta, textura.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(textura);
            UnityEngine.Object.DestroyImmediate(destino);

            informe.AppendLine("Captura de ejecucion: " + ruta);
        }
        catch (Exception e)
        {
            informe.AppendLine("No se pudo guardar la captura: " + e.Message);
        }
    }
}
