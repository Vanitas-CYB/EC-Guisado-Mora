using System;
using System.IO;
using System.Text;
using ECXR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Construye de forma automatica la escena de la evaluacion EC_XR:
/// sala de entrenamiento XR con piso, muros, iluminacion, cinco o mas objetos 3D,
/// dos objetos manipulables con Rigidbody + XR Grab Interactable, interacciones a distancia
/// por rayo (luz, puerta y color), teletransporte y contador en UI espacial.
///
/// Menu: EC_XR / 1. Construir escena XR
/// CLI : Unity.exe -batchmode -quit -projectPath "RUTA" -executeMethod ConstruirEscenaXR.Construir
/// </summary>
public static class ConstruirEscenaXR
{
    private const string RutaEscena = "Assets/Scenes/EC_XR_GuisadoMoraChristopher.unity";
    private const string RutaSamples = "Assets/Samples/XR Interaction Toolkit/3.6.1";
    private const string RutaMateriales = "Assets/Materiales";

    private const string PrefabSuelo = "Assets/SUELO.prefab";
    private const string PrefabPared1 = "Assets/Escenario/PARED1.prefab";
    private const string PrefabPared2 = "Assets/Escenario/PARED2.prefab";
    private const string PrefabPared3 = "Assets/Escenario/PARED3.prefab";

    // Limites de la sala
    private const float MitadSala = 7f;
    private const float AlturaMuro = 3f;
    private const float GrosorMuro = 0.3f;

    [MenuItem("EC_XR/1. Construir escena XR")]
    public static void Construir()
    {
        try
        {
            Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Materiales.Crear();

            GameObject raizEscenario = new GameObject("Escenario");
            GameObject raizObjetos = new GameObject("Objetos_3D");
            GameObject raizLogica = new GameObject("Logica_XR");

            GameObject luzDireccional = CrearIluminacion();
            GameObject luzSala = CrearLamparaDeSala();

            GameObject piso = CrearHabitacion(raizEscenario.transform);

            (GameObject cubo, GameObject esfera, GameObject llave) = CrearObjetosManipulables(raizObjetos.transform);
            GameObject capsula = CrearCapsulaDeColor(raizObjetos.transform);

            GameObject puerta = CrearPuerta(raizEscenario.transform);
            GameObject panelBotones = CrearPanelDeControl(raizEscenario.transform, out GameObject botonLuz, out GameObject botonPuerta, out GameObject botonColor);

            InterruptorLuz interruptor = CrearInterruptorDeLuz(raizLogica.transform, luzSala);
            PuertaDeslizante puertaScript = puerta.GetComponent<PuertaDeslizante>();
            CambiarColorObjetivo colorScript = capsula.GetComponent<CambiarColorObjetivo>();

            // Interacciones a distancia (rayo XR) sobre el panel de control.
            ConfigurarBotonXR(botonLuz, interruptor, "Alternar");
            ConfigurarBotonXR(botonPuerta, puertaScript, "Alternar");
            ConfigurarBotonXR(botonColor, colorScript, "SiguienteColor");

            // La capsula tambien responde al rayo directamente.
            ConfigurarBotonXR(capsula, colorScript, "SiguienteColor");

            // Objetos manipulables: Rigidbody + XR Grab Interactable.
            ConfigurarAgarrable(cubo);
            ConfigurarAgarrable(esfera);
            ConfigurarAgarrable(llave);

            // Teletransporte sobre el piso.
            ConfigurarTeletransporte(piso);

            GameObject interactableManager = CrearAdministradorDeInteracciones();
            CrearEventSystem();

            GameObject uiEspacial = CrearUIEspacial(out ContadorXR contador);

            CrearRigXR();
            CrearSimulador();

            // Los modelos del rig XR usan materiales del pipeline integrado: se convierten a URP
            // para que no aparezcan en magenta.
            AdaptarMaterialesAUrp(escena);

            // La escena debe quedar guardada y marcada como escena de build.
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(escena);
            bool guardado = EditorSceneManager.SaveScene(escena, RutaEscena);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };

            PlayerSettings.productName = "EC_XR_GuisadoMoraChristopher";
            PlayerSettings.companyName = "Universidad Autonoma del Peru";

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[EC_XR] Escena construida correctamente (" + guardado + "): " + RutaEscena
                + "\n  Piso: " + piso.name
                + "\n  Manipulables: cubo, esfera, llave"
                + "\n  Rayo: " + panelBotones.name + " -> luz, puerta y color"
                + "\n  Teletransporte, UI espacial: " + uiEspacial.name
                + "\n  Interactables manager: " + interactableManager.name);
        }
        catch (Exception e)
        {
            Debug.LogError("[EC_XR] Error construyendo la escena: " + e);
            throw;
        }
    }

    // ------------------------------------------------------------------
    // Iluminacion
    // ------------------------------------------------------------------

    private static GameObject CrearIluminacion()
    {
        GameObject go = new GameObject("Luz_Direccional");
        go.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.intensity = 0.55f;
        luz.color = new Color(1f, 0.96f, 0.9f);
        luz.shadows = LightShadows.Soft;
        return go;
    }

    private static GameObject CrearLamparaDeSala()
    {
        GameObject go = new GameObject("Luz_Puntual_Sala");
        go.transform.position = new Vector3(0f, 2.9f, 0f);
        Light luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.intensity = 4f;
        luz.range = 18f;
        luz.color = new Color(1f, 0.95f, 0.85f);
        luz.shadows = LightShadows.Soft;

        // Panel emisivo que hace visible el estado de la luz.
        GameObject panel = Primitiva(PrimitiveType.Cube, "Panel_Lampara", go.transform,
            new Vector3(0f, 0.05f, 0f), new Vector3(1.4f, 0.08f, 1.4f), null, Materiales.Lampara);
        UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());

        return go;
    }

    // ------------------------------------------------------------------
    // Habitacion
    // ------------------------------------------------------------------

    private static GameObject CrearHabitacion(Transform padre)
    {
        // Piso: se reutiliza el prefab SUELO del estudiante (Plano + MeshCollider).
        GameObject piso = InstanciarPrefab(PrefabSuelo, "Piso_Sala", padre, Vector3.zero,
            new Vector3(1.4f, 1f, 1.4f), null, Materiales.Piso);
        if (piso == null)
        {
            piso = Primitiva(PrimitiveType.Cube, "Piso_Sala", padre, new Vector3(0f, -0.1f, 0f),
                new Vector3(MitadSala * 2f, 0.2f, MitadSala * 2f), null, Materiales.Piso);
        }

        // Muros: se reutilizan los prefabs PARED1/2/3 del estudiante (Cubo + BoxCollider).
        InstanciarMuro(PrefabPared1, "Muro_Norte", padre, new Vector3(0f, AlturaMuro * 0.5f, MitadSala),
            new Vector3(MitadSala * 2f + GrosorMuro, AlturaMuro, GrosorMuro), 0f);

        InstanciarMuro(PrefabPared2, "Muro_Sur", padre, new Vector3(0f, AlturaMuro * 0.5f, -MitadSala),
            new Vector3(MitadSala * 2f + GrosorMuro, AlturaMuro, GrosorMuro), 0f);

        InstanciarMuro(PrefabPared3, "Muro_Este", padre, new Vector3(MitadSala, AlturaMuro * 0.5f, 0f),
            new Vector3(GrosorMuro, AlturaMuro, MitadSala * 2f), 0f);

        // Muro oeste con hueco para la puerta.
        InstanciarMuro(PrefabPared1, "Muro_Oeste_A", padre, new Vector3(-MitadSala, AlturaMuro * 0.5f, 4.05f),
            new Vector3(GrosorMuro, AlturaMuro, 5.9f), 0f);

        InstanciarMuro(PrefabPared2, "Muro_Oeste_B", padre, new Vector3(-MitadSala, AlturaMuro * 0.5f, -4.05f),
            new Vector3(GrosorMuro, AlturaMuro, 5.9f), 0f);

        InstanciarMuro(PrefabPared3, "Dintel_Oeste", padre, new Vector3(-MitadSala, 2.6f, 0f),
            new Vector3(GrosorMuro, 0.8f, 2.2f), 0f);

        return piso;
    }

    private static void InstanciarMuro(string rutaPrefab, string nombre, Transform padre, Vector3 posicion, Vector3 escala, float eulerY)
    {
        GameObject muro = InstanciarPrefab(rutaPrefab, nombre, padre, posicion, escala,
            new Vector3(0f, eulerY, 0f), Materiales.Pared);

        if (muro == null)
        {
            Primitiva(PrimitiveType.Cube, nombre, padre, posicion, escala, new Vector3(0f, eulerY, 0f), Materiales.Pared);
        }
    }

    private static GameObject CrearPuerta(Transform padre)
    {
        GameObject hoja = Primitiva(PrimitiveType.Cube, "Hoja_Puerta", padre,
            new Vector3(-MitadSala, 1.1f, 0f), new Vector3(0.2f, 2.2f, 2.2f), null, Materiales.Puerta);

        PuertaDeslizante puerta = hoja.AddComponent<PuertaDeslizante>();
        AsignarObjeto(puerta, "hoja", hoja.transform);
        AsignarVector3(puerta, "desplazamientoLocal", new Vector3(0f, 0f, -2.1f));

        // Marco decorativo de la puerta.
        Primitiva(PrimitiveType.Cube, "Marco_Puerta", padre,
            new Vector3(-MitadSala + 0.16f, 1.1f, -1.16f), new Vector3(0.1f, 2.2f, 0.12f), null, Materiales.Panel);
        Primitiva(PrimitiveType.Cube, "Marco_Puerta_2", padre,
            new Vector3(-MitadSala + 0.16f, 1.1f, 1.16f), new Vector3(0.1f, 2.2f, 0.12f), null, Materiales.Panel);

        return hoja;
    }

    // ------------------------------------------------------------------
    // Objetos 3D
    // ------------------------------------------------------------------

    private static (GameObject, GameObject, GameObject) CrearObjetosManipulables(Transform padre)
    {
        // Mesa de entrenamiento
        Primitiva(PrimitiveType.Cube, "Mesa_Central", padre, new Vector3(0f, 0.45f, -1.4f),
            new Vector3(2f, 0.9f, 1.2f), null, Materiales.Madera);

        GameObject cubo = Primitiva(PrimitiveType.Cube, "Cubo_Grabable", padre,
            new Vector3(-0.6f, 1.15f, -1.4f), new Vector3(0.35f, 0.35f, 0.35f), null, Materiales.Objeto);

        GameObject esfera = Primitiva(PrimitiveType.Sphere, "Esfera_Grabable", padre,
            new Vector3(0f, 1.15f, -1.4f), new Vector3(0.35f, 0.35f, 0.35f), null, Materiales.ObjetoVerde);

        // Llave compuesta por un mango y una cabeza (Rigidbody compuesto en el padre).
        GameObject llave = new GameObject("Llave_Grabable");
        llave.transform.SetParent(padre, false);
        llave.transform.localPosition = new Vector3(0.6f, 1.15f, -1.4f);

        Primitiva(PrimitiveType.Cylinder, "Mango", llave.transform, Vector3.zero,
            new Vector3(0.06f, 0.22f, 0.06f), new Vector3(0f, 0f, 90f), Materiales.Metal);

        Primitiva(PrimitiveType.Cube, "Cabeza", llave.transform, new Vector3(0.24f, 0f, 0f),
            new Vector3(0.18f, 0.07f, 0.12f), null, Materiales.Metal);

        // Quinto objeto: rampa de entrenamiento.
        Primitiva(PrimitiveType.Cube, "Rampa_Entrenamiento", padre, new Vector3(-3.4f, 0.45f, 0.6f),
            new Vector3(2.6f, 0.25f, 2.2f), new Vector3(-20f, 25f, 0f), Materiales.Madera);

        // Sexto objeto: columna decorativa.
        Primitiva(PrimitiveType.Cylinder, "Columna_Decorativa", padre, new Vector3(-4.8f, 0.6f, 3.6f),
            new Vector3(0.6f, 0.6f, 0.6f), null, Materiales.Pared);

        return (cubo, esfera, llave);
    }

    private static GameObject CrearCapsulaDeColor(Transform padre)
    {
        Primitiva(PrimitiveType.Cube, "Pedestal_Color", padre, new Vector3(2.9f, 0.3f, 0.2f),
            new Vector3(0.7f, 0.6f, 0.7f), null, Materiales.Panel);

        GameObject capsula = Primitiva(PrimitiveType.Capsule, "Capsula_Color", padre,
            new Vector3(2.9f, 1.0f, 0.2f), new Vector3(0.4f, 0.4f, 0.4f), null, Materiales.Objeto);

        CambiarColorObjetivo script = capsula.AddComponent<CambiarColorObjetivo>();
        AsignarObjeto(script, "objetivo", capsula.GetComponent<Renderer>());

        return capsula;
    }

    // ------------------------------------------------------------------
    // Panel de control con botones (interaccion a distancia por rayo)
    // ------------------------------------------------------------------

    private static GameObject CrearPanelDeControl(Transform padre, out GameObject botonLuz, out GameObject botonPuerta, out GameObject botonColor)
    {
        // El contenedor se deja con escala 1 para que los botones hijos no se deformen.
        GameObject panel = new GameObject("Panel_Control");
        panel.transform.SetParent(padre, false);
        panel.transform.localPosition = new Vector3(0f, 1.35f, 2.6f);

        Primitiva(PrimitiveType.Cube, "Tablero", panel.transform, Vector3.zero,
            new Vector3(3.2f, 1.5f, 0.12f), null, Materiales.Panel);

        botonLuz = CrearBoton(panel.transform, "Boton_Luz", new Vector3(-0.8f, 0.18f, -0.14f), "LUZ");
        botonPuerta = CrearBoton(panel.transform, "Boton_Puerta", new Vector3(0f, 0.18f, -0.14f), "PUERTA");
        botonColor = CrearBoton(panel.transform, "Boton_Color", new Vector3(0.8f, 0.18f, -0.14f), "COLOR");

        // Cabecera del panel
        CrearEtiqueta("PANEL DE CONTROL XR", panel.transform, new Vector3(0f, 0.58f, -0.1f), 0.035f, new Color(0.7f, 0.9f, 1f));

        return panel;
    }

    private static GameObject CrearBoton(Transform padre, string nombre, Vector3 posicion, string etiqueta)
    {
        GameObject boton = Primitiva(PrimitiveType.Cube, nombre, padre, posicion,
            new Vector3(0.5f, 0.5f, 0.14f), null, Materiales.Boton);

        XRSimpleInteractable simple = boton.AddComponent<XRSimpleInteractable>();
        simple.selectMode = InteractableSelectMode.Single;

        CrearEtiqueta(etiqueta, padre, new Vector3(posicion.x, -0.32f, -0.14f), 0.03f, Color.white);

        return boton;
    }

    private static void ConfigurarBotonXR(GameObject objetivo, MonoBehaviour destino, string metodo)
    {
        XRSimpleInteractable simple = objetivo.GetComponent<XRSimpleInteractable>();

        if (simple == null)
        {
            simple = objetivo.AddComponent<XRSimpleInteractable>();
            simple.selectMode = InteractableSelectMode.Single;
        }

        BotonXR boton = objetivo.GetComponent<BotonXR>();
        if (boton == null)
        {
            boton = objetivo.AddComponent<BotonXR>();
        }

        AsignarObjeto(boton, "destino", destino);
        AsignarTexto(boton, "metodo", metodo);
    }

    private static InterruptorLuz CrearInterruptorDeLuz(Transform padre, GameObject luzSala)
    {
        GameObject go = new GameObject("Control_Luz");
        go.transform.SetParent(padre, false);

        InterruptorLuz interruptor = go.AddComponent<InterruptorLuz>();
        AsignarArray(interruptor, "luces", luzSala.GetComponent<Light>());

        Renderer panelLampara = luzSala.GetComponentInChildren<Renderer>();
        if (panelLampara != null)
        {
            AsignarArray(interruptor, "renderersEmisivos", panelLampara);
        }

        return interruptor;
    }

    // ------------------------------------------------------------------
    // Componentes XR
    // ------------------------------------------------------------------

    private static void ConfigurarAgarrable(GameObject go)
    {
        Rigidbody cuerpo = go.GetComponent<Rigidbody>();
        if (cuerpo == null)
        {
            cuerpo = go.AddComponent<Rigidbody>();
        }

        cuerpo.mass = 1f;
        cuerpo.useGravity = true;
        cuerpo.isKinematic = false;

        XRGrabInteractable agarrable = go.GetComponent<XRGrabInteractable>();
        if (agarrable == null)
        {
            agarrable = go.AddComponent<XRGrabInteractable>();
        }

        agarrable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        agarrable.throwOnDetach = true;
        agarrable.useDynamicAttach = true;

        if (go.GetComponent<XRGeneralGrabTransformer>() == null)
        {
            go.AddComponent<XRGeneralGrabTransformer>();
        }
    }

    private static void ConfigurarTeletransporte(GameObject piso)
    {
        TeleportationArea area = piso.GetComponent<TeleportationArea>();
        if (area == null)
        {
            area = piso.AddComponent<TeleportationArea>();
        }

        area.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
    }

    private static GameObject CrearAdministradorDeInteracciones()
    {
        GameObject go = new GameObject("XR Interaction Manager");
        go.AddComponent<XRInteractionManager>();
        return go;
    }

    private static void CrearEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<XRUIInputModule>();
    }

    private static void CrearRigXR()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSamples + "/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");

        if (prefab == null)
        {
            Debug.LogError("[EC_XR] No se encontro el prefab 'XR Origin (XR Rig)'. Ejecuta primero 'EC_XR / 0. Importar samples'.");
            return;
        }

        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        rig.name = "XR Origin (XR Rig)";
        rig.transform.position = new Vector3(0f, 0f, -4f);
        rig.transform.rotation = Quaternion.identity;
    }

    private static void CrearSimulador()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSamples + "/XR Interaction Simulator/XR Interaction Simulator.prefab");

        if (prefab == null)
        {
            Debug.LogError("[EC_XR] No se encontro el prefab 'XR Interaction Simulator'. Ejecuta primero 'EC_XR / 0. Importar samples'.");
            return;
        }

        GameObject simulador = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        simulador.name = "XR Interaction Simulator";
        simulador.transform.position = Vector3.zero;
    }

    // ------------------------------------------------------------------
    // UI espacial
    // ------------------------------------------------------------------

    private static GameObject CrearUIEspacial(out ContadorXR contador)
    {
        GameObject canvasGO = new GameObject("UI_Espacial");
        canvasGO.transform.position = new Vector3(0f, 2.55f, 2.3f);
        canvasGO.transform.rotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.0022f;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform rt = canvasGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(660f, 360f);
        }

        GameObject fondoGO = new GameObject("Fondo");
        fondoGO.transform.SetParent(canvasGO.transform, false);
        Image fondo = fondoGO.AddComponent<Image>();
        fondo.color = new Color(0.04f, 0.07f, 0.14f, 0.9f);
        Estirar(fondoGO.GetComponent<RectTransform>(), 0f);

        GameObject textoGO = new GameObject("Texto_Contador");
        textoGO.transform.SetParent(canvasGO.transform, false);
        Text texto = textoGO.AddComponent<Text>();
        texto.font = ObtenerFuente();
        texto.fontSize = 30;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = Color.white;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        texto.text = "SALA DE ENTRENAMIENTO XR";
        Estirar(textoGO.GetComponent<RectTransform>(), 26f);

        contador = canvasGO.AddComponent<ContadorXR>();
        AsignarObjeto(contador, "texto", texto);

        return canvasGO;
    }

    private static void Estirar(RectTransform rt, float margen)
    {
        if (rt == null)
        {
            return;
        }

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    // ------------------------------------------------------------------
    // Utilidades
    // ------------------------------------------------------------------

    /// <summary>
    /// Convierte los materiales del pipeline integrado (shader "Standard") que traen los prefabs
    /// del rig XR a materiales URP Lit equivalentes. Sin esto los controladores se ven magenta.
    /// </summary>
    private static void AdaptarMaterialesAUrp(Scene escena)
    {
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null)
        {
            Debug.LogWarning("[EC_XR] No se encontro el shader Universal Render Pipeline/Lit; no se adaptaron materiales.");
            return;
        }

        int cambiados = 0;

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (MeshRenderer renderer in raiz.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material == null || material.shader == null || material.shader.name != "Standard")
                {
                    continue;
                }

                Material nuevo = CrearMaterialUrp(material, urp);
                if (nuevo != null)
                {
                    renderer.sharedMaterial = nuevo;
                    cambiados++;
                }
            }
        }

        Debug.Log("[EC_XR] Materiales del pipeline integrado adaptados a URP: " + cambiados);
    }

    private static Material CrearMaterialUrp(Material original, Shader urp)
    {
        Directory.CreateDirectory(RutaMateriales);

        string nombre = "URP_" + Sanear(original.name);
        string ruta = RutaMateriales + "/" + nombre + ".mat";

        Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null)
        {
            return existente;
        }

        Material nuevo = new Material(urp) { name = nombre };

        Color color = Color.white;
        if (original.HasProperty("_BaseColor"))
        {
            color = original.GetColor("_BaseColor");
        }
        else if (original.HasProperty("_Color"))
        {
            color = original.GetColor("_Color");
        }

        if (nuevo.HasProperty("_BaseColor"))
        {
            nuevo.SetColor("_BaseColor", color);
        }

        if (nuevo.HasProperty("_Color"))
        {
            nuevo.SetColor("_Color", color);
        }

        if (nuevo.HasProperty("_Smoothness") && original.HasProperty("_Glossiness"))
        {
            nuevo.SetFloat("_Smoothness", original.GetFloat("_Glossiness"));
        }

        if (nuevo.HasProperty("_Metallic") && original.HasProperty("_Metallic"))
        {
            nuevo.SetFloat("_Metallic", original.GetFloat("_Metallic"));
        }

        AssetDatabase.CreateAsset(nuevo, ruta);
        return nuevo;
    }

    private static string Sanear(string nombre)
    {
        var limpio = new StringBuilder();

        foreach (char c in nombre)
        {
            limpio.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
        }

        return limpio.ToString();
    }

    private static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Vector3 posicion,
        Vector3 escala, Vector3? rotacionEuler = null, Material material = null)
    {
        GameObject go = GameObject.CreatePrimitive(tipo);
        Preparar(go, nombre, padre, posicion, escala, rotacionEuler, material);
        return go;
    }

    private static GameObject InstanciarPrefab(string ruta, string nombre, Transform padre, Vector3 posicion,
        Vector3 escala, Vector3? rotacionEuler = null, Material material = null)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null)
        {
            Debug.LogWarning("[EC_XR] No se encontro el prefab " + ruta + ". Se usara una primitiva equivalente.");
            return null;
        }

        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Preparar(go, nombre, padre, posicion, escala, rotacionEuler, material);
        return go;
    }

    private static void Preparar(GameObject go, string nombre, Transform padre, Vector3 posicion,
        Vector3 escala, Vector3? rotacionEuler, Material material)
    {
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = escala;
        go.transform.localEulerAngles = rotacionEuler ?? Vector3.zero;

        if (material != null)
        {
            foreach (MeshRenderer renderer in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }
    }

    private static void CrearEtiqueta(string contenido, Transform padre, Vector3 posicion, float tamano, Color color)
    {
        GameObject go = new GameObject("Etiqueta_" + contenido);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localRotation = Quaternion.identity;

        TextMesh texto = go.AddComponent<TextMesh>();
        texto.text = contenido;
        texto.fontSize = 60;
        texto.characterSize = tamano;
        texto.anchor = TextAnchor.MiddleCenter;
        texto.alignment = TextAlignment.Center;
        texto.color = color;

        Font fuente = ObtenerFuente();
        if (fuente != null)
        {
            texto.font = fuente;
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = fuente.material;
            }
        }
    }

    private static Font ObtenerFuente()
    {
        try
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (Exception)
        {
            // Se intenta con la fuente clasica antes de rendirse.
        }

        try
        {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        catch (Exception)
        {
            return null;
        }
    }

    // Asignacion de campos [SerializeField] privados mediante SerializedObject.

    private static void AsignarObjeto(Component componente, string propiedad, UnityEngine.Object valor)
    {
        SerializedObject so = new SerializedObject(componente);
        SerializedProperty prop = so.FindProperty(propiedad);

        if (prop == null)
        {
            Debug.LogWarning("[EC_XR] Propiedad no encontrada: " + propiedad + " en " + componente.GetType().Name);
            return;
        }

        prop.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AsignarArray(Component componente, string propiedad, params UnityEngine.Object[] valores)
    {
        SerializedObject so = new SerializedObject(componente);
        SerializedProperty prop = so.FindProperty(propiedad);

        if (prop == null)
        {
            Debug.LogWarning("[EC_XR] Propiedad no encontrada: " + propiedad + " en " + componente.GetType().Name);
            return;
        }

        prop.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AsignarVector3(Component componente, string propiedad, Vector3 valor)
    {
        SerializedObject so = new SerializedObject(componente);
        SerializedProperty prop = so.FindProperty(propiedad);

        if (prop == null)
        {
            Debug.LogWarning("[EC_XR] Propiedad no encontrada: " + propiedad + " en " + componente.GetType().Name);
            return;
        }

        prop.vector3Value = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AsignarTexto(Component componente, string propiedad, string valor)
    {
        SerializedObject so = new SerializedObject(componente);
        SerializedProperty prop = so.FindProperty(propiedad);

        if (prop == null)
        {
            Debug.LogWarning("[EC_XR] Propiedad no encontrada: " + propiedad + " en " + componente.GetType().Name);
            return;
        }

        prop.stringValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Materiales compartidos que se crean como assets dentro de Assets/Materiales.</summary>
    private static class Materiales
    {
        public static Material Piso;
        public static Material Pared;
        public static Material Madera;
        public static Material Objeto;
        public static Material ObjetoVerde;
        public static Material Metal;
        public static Material Boton;
        public static Material Panel;
        public static Material Puerta;
        public static Material Lampara;

        public static void Crear()
        {
            Directory.CreateDirectory(RutaMateriales);

            Piso = Crear("M_Piso", new Color(0.16f, 0.17f, 0.2f), 0.35f);
            Pared = Crear("M_Pared", new Color(0.72f, 0.73f, 0.76f), 0.2f);
            Madera = Crear("M_Madera", new Color(0.42f, 0.28f, 0.16f), 0.25f);
            Objeto = Crear("M_Objeto_Azul", new Color(0.18f, 0.55f, 0.97f), 0.6f);
            ObjetoVerde = Crear("M_Objeto_Verde", new Color(0.16f, 0.83f, 0.47f), 0.6f);
            Metal = Crear("M_Metal", new Color(0.75f, 0.76f, 0.8f), 0.85f);
            Boton = Crear("M_Boton", new Color(0.95f, 0.62f, 0.16f), 0.5f);
            Panel = Crear("M_Panel", new Color(0.1f, 0.12f, 0.16f), 0.3f);
            Puerta = Crear("M_Puerta", new Color(0.85f, 0.78f, 0.62f), 0.3f);
            Lampara = Crear("M_Lampara", new Color(0.96f, 0.96f, 0.92f), 0.5f);
        }

        private static Material Crear(string nombre, Color color, float suavidad)
        {
            string ruta = RutaMateriales + "/" + nombre + ".mat";
            Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (existente != null)
            {
                return existente;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.name = nombre;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", suavidad);
            }

            AssetDatabase.CreateAsset(material, ruta);
            return material;
        }
    }
}
