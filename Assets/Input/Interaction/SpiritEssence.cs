using UnityEngine;

public class SpiritEssence : MonoBehaviour, I_Interactable
{
    public void Interact()
    {
        Debug.Log("You have collected a Spirit Essence!");

        Destroy(gameObject);
    }

}
