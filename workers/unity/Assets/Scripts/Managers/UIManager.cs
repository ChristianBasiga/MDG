using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MdgSchema.Player;
using MdgSchema.Common;

namespace MDG.ClientSide.UserInterface
{
   
    public class UIManager : MonoBehaviour
    {
        //Inject these later.
        public GameObject roleSelectionUI;
        public Transform selectionGrid;

        public delegate void RoleSelectedHandler(GameEntityTypes type);
        public event RoleSelectedHandler OnRoleSelected;


        public void SelectRole(string role)
        {
            GameEntityTypes type = (GameEntityTypes) System.Enum.Parse(typeof(GameEntityTypes), role);
            OnRoleSelected?.Invoke(type);

            roleSelectionUI.SetActive(false);
        }
    }
}