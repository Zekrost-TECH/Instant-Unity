using System;
using UnityEngine;

/// <summary>
/// Una skin de la tienda. El id es la clave que usa SaveManager para persistir
/// (PlayerPrefs "SkinUnlocked_&lt;id&gt;" y "EquippedSkin").
/// </summary>
[Serializable]
public class SkinDefinition
{
    [Tooltip("Clave interna. NO la cambies una vez publicada: es la que se guarda en PlayerPrefs.")]
    public string id = "Cyan";

    [Tooltip("Nombre visible en la tienda.")]
    public string displayName = "Cian";

    [Tooltip("Color con el que se tiñe el jugador y el icono del menú.")]
    public Color color = Color.cyan;

    [Tooltip("Precio en Cronos. 0 = gratis (se desbloquea sola).")]
    public int price = 0;

    [Tooltip("Sprite opcional. Si se deja vacío se usa el sprite por defecto del jugador teñido con el color.")]
    public Sprite icon;

    public bool IsFree => price <= 0;
}
