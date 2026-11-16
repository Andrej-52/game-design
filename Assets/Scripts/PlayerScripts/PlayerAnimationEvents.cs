using UnityEngine;

// Put this on the Towelman model (the object with the Animator).
// Animation events can only call methods on the Animator's own GameObject,
// so this just forwards them to the player.
public class PlayerAnimationEvents : MonoBehaviour
{
    PlayerAttack attack;

    void Awake()
    {
        attack = GetComponentInParent<PlayerAttack>();
    }

    // Called by the animation event on the Whip clip
    public void WhipHit()
    {
        if (attack != null)
            attack.DoHit();
    }
}
