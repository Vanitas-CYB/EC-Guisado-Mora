using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Genera capturas de la escena usando la camara principal del XR Origin (la misma que se ve
/// al pulsar Play, con su post-procesado) para que las evidencias coincidan con la ejecucion real.
/// Se guardan en la carpeta Capturas/ del proyecto (fuera de Assets para que Unity no las importe).
///
/// Menu: EC_XR / 4. Capturar evidencias de la escena
/// CLI : Unity.exe -batchmode -quit -projectPath "RUTA" -executeMethod CapturarEvidencias.Capturar
/// </summary>
public static class CapturarEvidencias
{
    private const string RutaEscena = "Assets/Scenes/EC_XR_GuisadoMoraChristopher.unity";
    private const string CarpetaCapturas = "Capturas";
    private const int Ancho = 1920;
    private const int Alto = 1080;

    [MenuItem("EC_XR/4. Capturar evidencias de la escena")]
    public static void Capturar()
    {
        try
        {
            if (!File.Exists(RutaEscena))
            {
                Debug.LogError("[EC_XR] No existe la escena " + RutaEscena + ". Construyela primero.");
                return;
            }

            Scene escena = SceneManager.GetActiveScene();
            if (escena.path != RutaEscena)
            {
                EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            }

            Directory.CreateDirectory(CarpetaCapturas);

            Camera camara = Camera.main;
            bool camaraPropia = false;

            if (camara == null)
            {
                GameObject go = new GameObject("Camara_Evidencias");
                camara = go.AddComponent<Camera>();
                camara.fieldOfView = 55f;
                camara.nearClipPlane = 0.05f;
                camara.farClipPlane = 300f;
                camaraPropia = true;
            }

            Transform transform = camara.transform;
            Vector3 posicionOriginal = transform.localPosition;
            Quaternion rotacionOriginal = transform.localRotation;

            RenderTexture destino = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);

            // 1) Vista general del escenario (aerea: muros, puerta, objetos y panel)
            CamaraMirando(transform, new Vector3(-12.5f, 8.5f, -13.5f), new Vector3(0f, 1.1f, 0f));
            Guardar(camara, destino, CarpetaCapturas + "/01_vista_general.png");

            // 2) Vista desde la posicion del jugador (objetos, panel y UI espacial)
            CamaraMirando(transform, new Vector3(0f, 1.7f, -5.2f), new Vector3(0f, 1.5f, 1.5f));
            Guardar(camara, destino, CarpetaCapturas + "/02_escena_desde_jugador.png");

            // 3) Detalle del panel de control y contador de UI espacial
            CamaraMirando(transform, new Vector3(0.4f, 1.9f, -0.4f), new Vector3(0f, 1.85f, 2.6f));
            Guardar(camara, destino, CarpetaCapturas + "/03_detalle_panel_y_ui.png");

            // 4) Primer plano de los botones del panel de control
            CamaraMirando(transform, new Vector3(0f, 1.55f, 0.6f), new Vector3(0f, 1.55f, 2.6f));
            Guardar(camara, destino, CarpetaCapturas + "/04_detalle_botones.png");

            // 5) Objetos manipulables sobre la mesa
            CamaraMirando(transform, new Vector3(0.1f, 1.75f, -3.2f), new Vector3(0f, 1.05f, -1.4f));
            Guardar(camara, destino, CarpetaCapturas + "/05_objetos_manipulables.png");

            camara.targetTexture = null;

            if (camaraPropia)
            {
                UnityEngine.Object.DestroyImmediate(camara.gameObject);
            }
            else
            {
                transform.localPosition = posicionOriginal;
                transform.localRotation = rotacionOriginal;
            }

            UnityEngine.Object.DestroyImmediate(destino);
            AssetDatabase.Refresh();

            Debug.Log("[EC_XR] Capturas generadas en " + Path.GetFullPath(CarpetaCapturas));
        }
        catch (Exception e)
        {
            Debug.LogError("[EC_XR] Error capturando evidencias: " + e);
            throw;
        }
    }

    private static void CamaraMirando(Transform transform, Vector3 posicion, Vector3 objetivo)
    {
        transform.position = posicion;
        transform.LookAt(objetivo);
    }

    private static void Guardar(Camera camara, RenderTexture destino, string ruta)
    {
        camara.targetTexture = destino;
        camara.Render();

        RenderTexture anterior = RenderTexture.active;
        RenderTexture.active = destino;

        Texture2D textura = new Texture2D(destino.width, destino.height, TextureFormat.RGB24, false);
        textura.ReadPixels(new Rect(0, 0, destino.width, destino.height), 0, 0);
        textura.Apply();

        RenderTexture.active = anterior;
        camara.targetTexture = null;

        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(textura);

        Debug.Log("[EC_XR] Captura guardada: " + ruta);
    }
}
