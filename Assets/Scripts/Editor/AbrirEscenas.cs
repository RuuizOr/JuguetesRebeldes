using UnityEditor;
using UnityEditor.SceneManagement;

// Accesos rápidos para abrir las escenas del juego.
public static class AbrirEscenas
{
    static void Abrir(string nombre)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene("Assets/Scenes/" + nombre + ".unity");
    }

    [MenuItem("Juguetes Rebeldes/Abrir escena/Intro", priority = 40)] static void A1() => Abrir("Intro");
    [MenuItem("Juguetes Rebeldes/Abrir escena/Menú principal", priority = 41)] static void A2() => Abrir("MenuPrincipal");
    [MenuItem("Juguetes Rebeldes/Abrir escena/Selección de personaje", priority = 42)] static void A3() => Abrir("SeleccionPersonaje");
    [MenuItem("Juguetes Rebeldes/Abrir escena/Nivel 1", priority = 43)] static void A4() => Abrir("Nivel1");
    [MenuItem("Juguetes Rebeldes/Abrir escena/Nivel 2", priority = 44)] static void A5() => Abrir("Nivel2");
    [MenuItem("Juguetes Rebeldes/Abrir escena/Nivel 3", priority = 45)] static void A6() => Abrir("Nivel3");
    [MenuItem("Juguetes Rebeldes/Abrir escena/Final", priority = 46)] static void A7() => Abrir("Final");
}
