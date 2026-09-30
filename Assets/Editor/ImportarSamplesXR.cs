using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Importa los samples del paquete XR Interaction Toolkit copiandolos desde Library/PackageCache
/// a Assets/Samples, conservando los .meta para no perder los GUID originales.
/// Ademas limpia de los .asmdef copiados las referencias a assemblies que no existen en el proyecto
/// (por ejemplo Unity.XR.Hands, que es opcional), para que el proyecto compile sin errores.
///
/// Uso desde el menu:  EC_XR / 0. Importar samples de XR Interaction Toolkit
/// Uso por linea de comandos:
///   Unity.exe -batchmode -quit -projectPath "RUTA" -executeMethod ImportarSamplesXR.Importar
/// </summary>
public static class ImportarSamplesXR
{
    private const string Paquete = "com.unity.xr.interaction.toolkit";
    private const string NombreVisible = "XR Interaction Toolkit";

    private static readonly string[] Samples =
    {
        "Starter Assets",
        "XR Interaction Simulator"
    };

    private static readonly Regex ReferenciaGuid = new Regex("\"GUID:([0-9a-fA-F]{32})\"", RegexOptions.Compiled);
    private static readonly Regex BloqueReferencias = new Regex("\"references\"\\s*:\\s*\\[[^\\]]*\\]", RegexOptions.Compiled);

    private static readonly HashSet<string> GuidsConocidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    [MenuItem("EC_XR/0. Importar samples de XR Interaction Toolkit")]
    public static void Importar()
    {
        var log = new StringBuilder();

        try
        {
            string proyecto = Directory.GetParent(Application.dataPath).FullName;
            log.AppendLine("Proyecto: " + proyecto);

            string paqueteDir = BuscarPaquete(proyecto);
            if (string.IsNullOrEmpty(paqueteDir))
            {
                Debug.LogError("[EC_XR] No se encontro el paquete " + Paquete + ". Verifica Packages/manifest.json.");
                return;
            }

            string version = LeerVersion(paqueteDir);
            string origenBase = Path.Combine(paqueteDir, "Samples~");
            string destinoBase = Path.Combine(proyecto, "Assets", "Samples", NombreVisible, version);

            log.AppendLine("Paquete : " + paqueteDir);
            log.AppendLine("Version : " + version);
            log.AppendLine("Origen  : " + origenBase);
            log.AppendLine("Destino : " + destinoBase);

            if (!Directory.Exists(origenBase))
            {
                Debug.LogError("[EC_XR] El paquete no contiene la carpeta Samples~.");
                return;
            }

            int copiados = 0;
            foreach (string sample in Samples)
            {
                string origen = Path.Combine(origenBase, sample);
                if (!Directory.Exists(origen))
                {
                    log.AppendLine("  [AVISO] Sample no encontrado: " + sample);
                    continue;
                }

                CopiarCarpeta(origen, Path.Combine(destinoBase, sample));
                copiados++;
                log.AppendLine("  [OK] " + sample);
            }

            // Los GUID de los samples ya estan en Assets, asi que se pueden recopilar todos.
            RecopilarGuidsAsmdef(Path.Combine(proyecto, "Library", "PackageCache"));
            RecopilarGuidsAsmdef(Path.Combine(proyecto, "Packages"));
            RecopilarGuidsAsmdef(Path.Combine(proyecto, "Assets"));
            log.AppendLine("GUID de asmdef conocidos: " + GuidsConocidos.Count);

            int limpiados = LimpiarReferencias(destinoBase, log);
            log.AppendLine("Referencias no resueltas eliminadas: " + limpiados);

            AssetDatabase.Refresh();

            log.AppendLine("Samples copiados: " + copiados + "/" + Samples.Length);
            File.WriteAllText(Path.Combine(proyecto, "Logs", "EC_XR_import_samples.log"), log.ToString(), Encoding.UTF8);
            Debug.Log("[EC_XR] Importacion de samples finalizada.\n" + log);
        }
        catch (Exception e)
        {
            Debug.LogError("[EC_XR] Error importando samples: " + e);
            throw;
        }
    }

    private static int LimpiarReferencias(string raiz, StringBuilder log)
    {
        int modificados = 0;

        if (!Directory.Exists(raiz))
        {
            return 0;
        }

        foreach (string asmdef in Directory.GetFiles(raiz, "*.asmdef", SearchOption.AllDirectories))
        {
            string texto = File.ReadAllText(asmdef);
            Match bloque = BloqueReferencias.Match(texto);

            if (!bloque.Success)
            {
                continue;
            }

            var conservadas = new List<string>();

            foreach (Match entrada in Regex.Matches(bloque.Value, "\"([^\"]+)\""))
            {
                string valor = entrada.Groups[1].Value;

                if (valor == "references")
                {
                    continue;
                }

                if (!valor.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
                {
                    conservadas.Add("\"" + valor + "\"");
                    continue;
                }

                if (GuidsConocidos.Contains(valor.Substring(5)))
                {
                    conservadas.Add("\"" + valor + "\"");
                }
                else
                {
                    log.AppendLine("    - " + Path.GetFileName(asmdef) + ": se quita " + valor);
                }
            }

            string nuevo = "\"references\": [" + string.Join(", ", conservadas) + "]";
            string resultado = texto.Substring(0, bloque.Index) + nuevo + texto.Substring(bloque.Index + bloque.Length);

            if (resultado != texto)
            {
                File.WriteAllText(asmdef, resultado);
                modificados++;
            }
        }

        return modificados;
    }

    private static void RecopilarGuidsAsmdef(string raiz)
    {
        if (!Directory.Exists(raiz))
        {
            return;
        }

        foreach (string meta in Directory.GetFiles(raiz, "*.asmdef.meta", SearchOption.AllDirectories))
        {
            string guid = LeerGuid(meta);
            if (!string.IsNullOrEmpty(guid))
            {
                GuidsConocidos.Add(guid);
            }
        }
    }

    private static string LeerGuid(string rutaMeta)
    {
        foreach (string linea in File.ReadLines(rutaMeta))
        {
            if (linea.StartsWith("guid:", StringComparison.OrdinalIgnoreCase))
            {
                return linea.Substring(5).Trim();
            }
        }

        return null;
    }

    private static string BuscarPaquete(string proyecto)
    {
        string[] raices =
        {
            Path.Combine(proyecto, "Library", "PackageCache")
        };

        foreach (string raiz in raices)
        {
            if (!Directory.Exists(raiz))
            {
                continue;
            }

            foreach (string dir in Directory.GetDirectories(raiz, Paquete + "@*"))
            {
                return dir;
            }
        }

        return null;
    }

    private static string LeerVersion(string paqueteDir)
    {
        string json = Path.Combine(paqueteDir, "package.json");
        if (!File.Exists(json))
        {
            return "desconocida";
        }

        foreach (string linea in File.ReadAllLines(json))
        {
            int pos = linea.IndexOf("\"version\"", StringComparison.Ordinal);
            if (pos < 0)
            {
                continue;
            }

            int inicio = linea.IndexOf('"', pos + 9);
            if (inicio < 0)
            {
                continue;
            }

            int fin = linea.IndexOf('"', inicio + 1);
            if (fin > inicio)
            {
                return linea.Substring(inicio + 1, fin - inicio - 1);
            }
        }

        return "desconocida";
    }

    private static void CopiarCarpeta(string origen, string destino)
    {
        Directory.CreateDirectory(destino);

        foreach (string archivo in Directory.GetFiles(origen))
        {
            File.Copy(archivo, Path.Combine(destino, Path.GetFileName(archivo)), true);
        }

        foreach (string sub in Directory.GetDirectories(origen))
        {
            CopiarCarpeta(sub, Path.Combine(destino, Path.GetFileName(sub)));
        }
    }
}
