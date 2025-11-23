using UnityEngine;

public class HitBox : MonoBehaviour
{
    [Tooltip("Pon 1 para cuerpo, 2 para cabeza")]
    public float damageMultiplier = 1.0f;

    // Esta función es llamada por la bala cuando choca
    public void OnHit(float damage)
    {
        // 1. Calculamos el daño final (Base * Multiplicador)
        float finalDamage = damage * damageMultiplier;

        // 2. Obtenemos el nombre del jugador principal (la raíz del objeto)
        // Así sabrás si le diste a "Player 1" o "Enemigo"
        string nombreJugador = transform.root.name;

        // 3. Obtenemos qué parte ha sido golpeada (Cabeza o Cuerpo)
        string parteGolpeada = gameObject.name;

        // 4. Imprimimos el LOG
        Debug.Log($" IMPACTO: {nombreJugador} recibió {finalDamage} de daño en {parteGolpeada} (Multiplicador: x{damageMultiplier})");

        // AQUI iría la lógica de restar vida:
        // HealthSystem health = GetComponentInParent<HealthSystem>();
        // if(health) health.TakeDamage(finalDamage);
    }
}