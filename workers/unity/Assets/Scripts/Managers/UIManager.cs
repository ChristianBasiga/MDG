using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Player;


namespace MDG.ClientSide.UserInterface
{
   
    public class UIManager : MonoBehaviour
    {
        //Inject these later.
        public GameObject roleSelectionUI;
        public Transform selectionGrid;

        public delegate void RoleSelectedHandler(PlayerType type);
        public event RoleSelectedHandler OnRoleSelected;


        public void SelectRole(string role)
        {
            PlayerType type = (PlayerType) System.Enum.Parse(typeof(PlayerType), role);
            OnRoleSelected?.Invoke(type);

            roleSelectionUI.SetActive(false);
        }
    }
}