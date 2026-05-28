// using UnityEngine;

// /// <summary>
// /// Separate detector object for carry system
// /// Handles trigger collider detection without conflicting with CharacterController
// /// </summary>
// public class GP_CarryDetector : MonoBehaviour
// {
//     private GP_PlayerCarrySystem carrySystem;

//     public void Initialize(GP_PlayerCarrySystem system)
//     {
//         carrySystem = system;
//     }

//     private void OnTriggerStay(Collider other)
//     {
//         if (carrySystem == null) return;
        
//         if (other.CompareTag("Player"))
//         {
//             var player = other.GetComponent<Player_Components>();
//             if (player != null && player.currentActionState == ActionState.Depressed)
//             {
//                 carrySystem.SetNearbyDepressedPlayer(player);
//             }
//         }
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (carrySystem == null) return;
        
//         if (other.GetComponent<Player_Components>() == carrySystem.GetNearbyDepressedPlayer())
//         {
//             carrySystem.ClearNearbyDepressedPlayer();
//         }
//     }
// }
