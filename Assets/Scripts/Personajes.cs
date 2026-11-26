// Datos de los personajes que se pueden elegir.
public static class Personajes
{
    public struct Info
    {
        public string nombre;
        public string titulo;
        public string descripcion;
        public float velocidad;
        public float velocidadSprint;
        public float energia;
        public float recarga;
        public int vidas;
    }

    public static readonly Info[] Lista =
    {
        new Info
        {
            nombre = "Pino", titulo = "El duende veloz",
            descripcion = "El duende más confiable del taller. Equilibrado en todo.",
            velocidad = 6f, velocidadSprint = 9.5f, energia = 100f, recarga = 22f, vidas = 3
        },
        new Info
        {
            nombre = "Nieve", titulo = "La patinadora",
            descripcion = "Muy rápida, pero se cansa antes al correr.",
            velocidad = 6.8f, velocidadSprint = 10.2f, energia = 75f, recarga = 20f, vidas = 3
        },
        new Info
        {
            nombre = "Chispa", titulo = "La incansable",
            descripcion = "Un poco más lenta, pero puede correr muchísimo tiempo.",
            velocidad = 5.7f, velocidadSprint = 9.2f, energia = 150f, recarga = 32f, vidas = 3
        },
        new Info
        {
            nombre = "Tito", titulo = "El constructor",
            descripcion = "Fuerte y resistente: tiene 4 vidas, aunque es más lento.",
            velocidad = 5.5f, velocidadSprint = 8.8f, energia = 100f, recarga = 22f, vidas = 4
        },
    };
}
